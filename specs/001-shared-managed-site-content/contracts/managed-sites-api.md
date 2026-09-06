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
      "urls": ["/example"]
    }
  ]
}
```

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

### Create or update Managed Site

`PUT /api/managed-sites/{managedSiteId}`

**Request**:
```json
{
  "name": "Managed Site Name",
  "status": "Enabled",
  "urls": ["/example", "/example/section"]
}
```

**Responses**:
- `200 OK`: Managed Site updated.
- `400 Bad Request`: Invalid name or URL.
- `409 Conflict`: URL conflicts with another active registration.

## Navigation Placeholders and Contributions

### List navigation placeholders available to a Managed Site

`GET /api/managed-sites/{managedSiteId}/navigation/placeholders`

**Response**:
```json
{
  "items": [
    {
      "id": "menu-placeholder-id",
      "name": "Primary Navigation Slot",
      "type": "PlaceholderMenu",
      "isEnabled": true,
      "existingContributionStatus": "Published"
    }
  ]
}
```

**Responses**:
- `200 OK`: Placeholder list returned for the active Managed Site.
- `403 Forbidden`: User lacks clearance for the Managed Site.

### Save navigation contribution

`PUT /api/managed-sites/{managedSiteId}/navigation-contributions/{menuPlaceholderId}`

**Request**:
```json
{
  "menuItemIds": ["menu-item-id-1", "menu-item-id-2"],
  "status": "Draft"
}
```

**Behavior**:
- Contributions can target Site Blueprint placeholder menus or placeholder menu items.
- Contributions render only for the owning Managed Site.
- Missing contributions render empty placeholder output without breaking navigation.

**Responses**:
- `200 OK`: Navigation contribution saved.
- `403 Forbidden`: User lacks clearance for the Managed Site.
- `409 Conflict`: Placeholder is disabled or unavailable.

## Page Overrides

### Get override policy for a Blueprint Page

`GET /api/managed-sites/{managedSiteId}/blueprint-pages/{contentItemId}/override-policy`

**Response**:
```json
{
  "blueprintPageContentItemId": "content-item-id",
  "allowManagedSiteOverride": true,
  "existingOverrideStatus": "Published"
}
```

### Save managed-site page override

`PUT /api/managed-sites/{managedSiteId}/page-overrides/{blueprintPageContentItemId}`

**Request**:
```json
{
  "overrideContentItemId": "content-item-id",
  "status": "Draft"
}
```

**Responses**:
- `200 OK`: Override saved.
- `403 Forbidden`: User lacks clearance for the Managed Site.
- `409 Conflict`: Blueprint Page is not overrideable.

## Layer Contributions

### Save layer contribution

`PUT /api/managed-sites/{managedSiteId}/layer-contributions/{contributionPointId}`

**Request**:
```json
{
  "contentItemIds": ["content-item-id-1", "content-item-id-2"],
  "status": "Draft"
}
```

**Responses**:
- `200 OK`: Contribution saved.
- `403 Forbidden`: User lacks clearance.
- `409 Conflict`: Contribution point is disabled or unavailable.

## Placeholder Assignments

### Save placeholder assignment

`PUT /api/managed-sites/{managedSiteId}/placeholder-assignments/{placeholderId}`

**Request**:
```json
{
  "contentItemIds": ["content-item-id-1"],
  "status": "Draft"
}
```

**Behavior**:
- Assigned managed-site content renders for the owning Managed Site.
- If no assigned managed-site content exists, blueprint fallback content renders when available.
- If no assignment and no fallback exist, the placeholder renders empty.

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
- Applies the same composition precedence rules as published rendering.

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