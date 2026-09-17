# Contract: Managed Sites API

The Managed Site Admin Portal uses authenticated OrchardCore APIs. All write and preview operations require a token containing managed-site clearance claims/scopes for the selected Managed Site.

## Authentication and Scope

**Required headers**:

- `Authorization: Bearer <token>`

**Optional consistency headers**:

- `X-Managed-Site-Id: <managedSiteId>` for scoped read/write/preview operations. When present, this value must match the `{managedSiteId}` route value.

**Authorization rules**:

- The requested Managed Site must be present in the token claims/scopes.
- The active Managed Site scope must be validated on every privileged operation.
- The route `{managedSiteId}` is the API operation scope.
- The active portal session scope and token clearance must authorize the route `{managedSiteId}`.
- If `X-Managed-Site-Id` is present and does not match the route `{managedSiteId}`, the API returns `409 Conflict` and does not change content.
- If the token does not grant clearance for the route `{managedSiteId}`, the API returns `403 Forbidden`.
- A user with no Managed Site clearance receives no editable Managed Site list.

## Public Rendering Scope

Public page rendering resolves the active Managed Site from the incoming URL and stores it in request-scoped Managed Site context. Public rendering must not trust a client-provided `X-Managed-Site-Id` header for Managed Site resolution.

## Managed Site Selection

### List authorized Managed Sites

`GET /api/managed-sites/authorized`

**Response**:

```json
{
  "items": [
    {
      "id": "managed-site-id",
      "name": "Managed Site Name",
      "status": "Enabled",
      "hostname": "contoso.com",
      "urlPrefix": "shop"
    }
  ]
}
```

### Get the active Managed Site

`GET /api/managed-sites/session`

Resolves the active Managed Site for the caller. A caller cleared for exactly one Managed Site is
scoped to it automatically, so the portal can load straight into a scoped view. A caller cleared for
several receives `requiresSelection` and must choose before any scoped action is accepted. A stored
selection that clearance no longer covers is discarded before the response is produced.

**Response**:

```json
{
  "managedSiteId": "managed-site-id",
  "selectedAt": "2026-09-14T09:00:00+00:00",
  "requiresSelection": false,
  "authorizedManagedSites": [
    {
      "id": "managed-site-id",
      "name": "Managed Site Name",
      "status": "Enabled",
      "hostname": "contoso.com",
      "urlPrefix": "shop"
    }
  ]
}
```

**Responses**:

- `200 OK`: Active scope resolved, or selection required.
- `403 Forbidden`: The caller holds no effective Managed Site clearance.

### Select active Managed Site

`POST /api/managed-sites/session`

**Request**:

```json
{
  "managedSiteId": "managed-site-id"
}
```

**Responses**:

- `200 OK`: Active scope selected.
- `403 Forbidden`: Token does not grant clearance for the Managed Site.
- `409 Conflict`: Managed Site is disabled or unavailable.

## Managed Site Definitions

Defining Managed Sites is Site Blueprint governance rather than a scoped editor action, so these two
endpoints are the exception to the clearance rules above: they authorize on the Manage Managed Sites
permission and ignore the active session scope. Requiring clearance here could never be satisfied,
because nobody can hold clearance for a Managed Site that does not exist yet.

### Create or update Managed Site

`PUT /api/managed-sites/{managedSiteId}`

A Managed Site is addressed the way a tenant is: a `hostname` holding one or more host names and a single
`urlPrefix`. Both replace whatever the Managed Site carried before.

An empty `hostname` answers on every host the tenant serves, so it collides with that prefix on any host.
An empty `urlPrefix` addresses the root. Host names are added to the tenant Hostname setting when the
definition is saved; the prefix is resolved inside the tenant and never changes the tenant URL Prefix.

**Request**:

```json
{
  "name": "Managed Site Name",
  "status": "Enabled",
  "hostname": "contoso.com, www.contoso.com",
  "urlPrefix": "shop"
}
```

**Response**:

```json
{
  "id": "managed-site-id",
  "name": "Managed Site Name",
  "status": "Enabled",
  "hostname": "contoso.com, www.contoso.com",
  "urlPrefix": "shop"
}
```

**Responses**:

- `200 OK`: Managed Site created or updated.
- `400 Bad Request`: Missing name, missing URL, or unknown status.
- `403 Forbidden`: Caller lacks the Manage Managed Sites permission.
- `409 Conflict`: The name is already used, or the address collides with another Managed Site.

### Delete Managed Site

`DELETE /api/managed-sites/{managedSiteId}`

**Behavior**:

- Removes the Managed Site, freeing its address for reuse and withdrawing its host names from the tenant Hostname setting.

**Responses**:

- `204 No Content`: Managed Site removed.
- `403 Forbidden`: Caller lacks the Manage Managed Sites permission.
- `404 Not Found`: No Managed Site exists with that identifier.

## Managed Content Discovery

### List content items editable by a Managed Site

`GET /api/managed-sites/{managedSiteId}/managed-content`

Returns every content item carrying Managed Content whose edit scope includes the route Managed Site. This is the single discovery endpoint that replaces the earlier separate navigation, page override, layer, and placeholder listings.

**Query parameters**:

- `contentType`: Optional filter by content type name.
- `overrideStatus`: Optional filter such as `None`, `Draft`, `Published`, or `Suppressed`.
- `page` and `pageSize`: Optional paging.

**Response**:

```json
{
  "items": [
    {
      "sourceContentItemId": "content-item-id",
      "contentType": "Page",
      "displayText": "Home",
      "isContainer": true,
      "displayScopeIncludesManagedSite": true,
      "override": {
        "overrideContentItemId": "override-content-item-id",
        "status": "Published",
        "suppressionReason": null
      }
    }
  ],
  "totalCount": 1
}
```

**Behavior**:

- Items whose edit scope excludes the route Managed Site are never listed.
- `displayScopeIncludesManagedSite` is false when the Managed Site may edit an item it cannot display, so the portal can warn that the override will not render.
- `override` is null when the Managed Site has not created one.

**Responses**:

- `200 OK`: Editable item list returned for the route Managed Site.
- `403 Forbidden`: User lacks clearance for the Managed Site.

### Get managed content detail for one item

`GET /api/managed-sites/{managedSiteId}/managed-content/{sourceContentItemId}`

**Response**:

```json
{
  "sourceContentItemId": "content-item-id",
  "contentType": "Page",
  "editScopeIncludesManagedSite": true,
  "displayScopeIncludesManagedSite": true,
  "override": {
    "overrideContentItemId": "override-content-item-id",
    "status": "Published",
    "suppressionReason": null
  }
}
```

**Responses**:

- `200 OK`: Detail returned.
- `403 Forbidden`: Edit scope excludes the route Managed Site, or the user lacks clearance.
- `404 Not Found`: The source item does not exist or does not carry Managed Content.

## Managed Content Overrides

### Create or update an override

`PUT /api/managed-sites/{managedSiteId}/managed-content/{sourceContentItemId}/override`

**Request**:

```json
{
  "overrideContentItemId": "override-content-item-id",
  "status": "Draft"
}
```

**Behavior**:

- The override content item must use the same content type as the source content item.
- At most one active published override exists per Managed Site and source content item.
- Overrides use the platform draft and publish lifecycle, so a draft override does not change rendered output.
- The override renders only for the route Managed Site, and only when display scope includes it.

**Responses**:

- `200 OK`: Override saved.
- `400 Bad Request`: Override content type does not match the source content type.
- `403 Forbidden`: Edit scope excludes the route Managed Site, or the user lacks clearance.
- `409 Conflict`: The source item no longer carries Managed Content, or the Managed Site is disabled.

### Remove an override

`DELETE /api/managed-sites/{managedSiteId}/managed-content/{sourceContentItemId}/override`

**Behavior**:

- Removing an override restores the original content for that Managed Site on subsequent requests.

**Responses**:

- `204 No Content`: Override removed.
- `403 Forbidden`: Edit scope excludes the route Managed Site, or the user lacks clearance.
- `404 Not Found`: No override exists for this Managed Site and source item.

### List suppressed overrides for recovery

`GET /api/managed-sites/{managedSiteId}/managed-content/suppressed`

Returns overrides that exist but do not render, so administrators can review, reassign, or clean them up.

**Response**:

```json
{
  "items": [
    {
      "sourceContentItemId": "content-item-id",
      "overrideContentItemId": "override-content-item-id",
      "status": "Suppressed",
      "suppressionReason": "EditScopeRemoved"
    }
  ]
}
```

**Behavior**:

- `suppressionReason` is one of `EditScopeRemoved`, `SourceUnpublished`, `SourceDeleted`, `CapabilityDetached`, or `ManagedSiteDisabled`.
- Suppressed overrides remain readable even when the Managed Site can no longer edit the source item.

**Responses**:

- `200 OK`: Suppressed override list returned.
- `403 Forbidden`: User lacks clearance for the Managed Site.

## Preview

### Preview composed page

`POST /api/managed-sites/{managedSiteId}/preview`

**Request**:

```json
{
  "url": "/example/page",
  "includeDrafts": true
}
```

**Response**:

```json
{
  "previewUrl": "/preview/path",
  "managedSiteId": "managed-site-id",
  "compositionMode": "ManagedSite"
}
```

**Behavior**:

- Uses OrchardCore preview behavior.
- Simulates the ManagedSites request composition pipeline for the active Managed Site URL context.
- Resolves each managed content item using the same display scope and override rules as published rendering.
- Includes draft overrides visible to the current user when `includeDrafts` is true.

## Blueprint Administration

Managed content scope configuration is not exposed through this API. Blueprint administrators attach Managed Content to content types and set edit scope and display scope per item in the standard OrchardCore admin UI. Attempts to change either scope through the portal API are rejected with `403 Forbidden`.

## Error Shape

All errors use a consistent problem response.

```json
{
  "status": 409,
  "title": "URL conflict",
  "detail": "The URL is already assigned to another Managed Site.",
  "code": "managed-sites.url-conflict"
}
```
