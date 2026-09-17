# Research: Site Blueprint and Managed Site Content

## Decision: Use one VendallionCMS.ManagedSites module family for v1

**Rationale**: A single module family keeps the first implementation reviewable while still allowing feature-level activation boundaries for routing, permissions, composition, admin portal, and managed content behavior.

**Alternatives considered**:

- Multiple module projects from day one: clearer physical separation, but higher coordination and setup cost before responsibilities stabilize.
- Extending existing OrchardCore modules directly: lower initial code footprint, but risks coupling custom behavior to upstream module internals.

## Decision: The tenant is the Site Blueprint, implicitly

**Rationale**: The tenant owns common content and content structure, and Managed Sites are URL-scoped contexts inside it. Enabling the Managed Sites feature on a tenant is therefore the designation; there is nothing further to declare. An explicit toggle would restate what feature enablement already says and could contradict it, and an identifier would be a constant that no code branches on.

**Consequences**:

- No blueprint settings screen, service, entity, or stored record.
- A Managed Site carries no blueprint identifier, because there is only ever one blueprint per tenant.
- "Site Blueprint" survives as the name of the tenant's common-content context, which display scope still needs to distinguish from each Managed Site.

**Alternatives considered**:

- Keep an explicit designation toggle and name: reads as configurable governance, but gated nothing, and its identifier was hardcoded so both branches behaved identically.
- Treat each Managed Site as a separate OrchardCore tenant: familiar OrchardCore model, but unnecessary for this scope and weaker for composing common content with URL-scoped managed-site content in one site.
- Duplicate common content into each Managed Site: simple runtime lookup, but creates content drift and migration complexity.

## Decision: Replace four customization mechanisms with one Managed Content capability

**Supersedes**: The earlier decisions covering page overrides, navigation contributions, layer contributions, and placeholder assignments as separate mechanisms.

**Rationale**: Pages, navigation entries, layer widgets, and page sections are all content items in OrchardCore. A single content part attached to a content type therefore covers every case that previously needed a dedicated model, driver, service, controller, and portal screen. Collapsing them removes four parallel authorization paths, four recovery models, and the cross-mechanism precedence rules that the earlier design needed. This directly serves the constitution's simplicity principle and reduces the implementation surface by roughly three quarters.

**Consequences**:

- No composition precedence rules are required. Each content item independently resolves to its override or its original content, so the earlier rule that page overrides suppress placeholders while layers render independently becomes unnecessary.
- No separate fallback content configuration is required. An item without an override renders its own original content, which is the fallback by construction.
- Layer independence is preserved for free. A layer widget is a content item whose override resolves on its own, unaffected by whether the page containing other content was overridden.
- One recovery model replaces four. Every suppressed override carries a reason and stays readable by administrators.

**Alternatives considered**:

- Keep four mechanisms: matches each surface's vocabulary precisely, but multiplies the data model, API surface, test matrix, and portal screens, and forces explicit precedence rules between mechanisms.
- One mechanism plus a dedicated navigation path: navigation has ordering semantics the generic part does not express directly, but menu items are content items and ordering lives in the container, so the generic model still covers it through container override.

## Decision: Govern managed content with two independent scopes

**Rationale**: Blueprint administrators need to answer two different questions per item. Who may change this, and who sees it. Keeping edit scope and display scope independent lets a blueprint author publish an item visible everywhere but editable by nobody, an item editable by two managed sites and visible only to those two, or an item hidden from the blueprint context and shown only to one managed site. Merging them into one list would conflate authoring rights with audience targeting and lose the third case entirely.

**Alternatives considered**:

- One combined scope list: simpler editor UI, but cannot express content visible to all yet editable by one, which is the common blueprint governance case.
- Display targeting through existing layer rules only: reuses platform behavior, but layer rules are layout-level and cannot target an individual content item nested inside a page.

## Decision: Store overrides as separate content items of the same content type

**Rationale**: Reusing the platform content lifecycle gives each managed site independent draft and publish state, versioning, and the same editor UI as the source item, at no additional cost. Storing per-managed-site content inside the part itself would put an unbounded dictionary in one document, break per-managed-site versioning, and require a bespoke editor.

**Alternatives considered**:

- Store override content inside the part as a per-managed-site map: fewer content items and one lookup, but loses draft and publish granularity, grows the source document without bound, and concentrates write contention on a single document across all managed sites.
- Store overrides as a distinct content type: allows an override-specific schema, but prevents reusing the source type's editor and validation, and forces a parallel type per source type.

## Decision: Default to no edit rights and full display on attach

**Rationale**: The constitution makes backward compatibility the default contract. Attaching managed content to a type must not change what any visitor sees or grant any managed site authority it did not have. Defaulting display scope to every context and edit scope to none means attachment is observably inert until a blueprint administrator opts in per item.

**Alternatives considered**:

- Default edit scope to all managed sites: faster setup for the common case, but silently grants write access to blueprint content on attach, which is an unsafe default.
- Default display scope to none: safe against leakage, but makes attachment immediately hide content, breaking existing pages on a content-definition change.

## Decision: Evaluate display scope before override resolution

**Rationale**: Display scope answers whether the item participates in this request at all. Checking it first gives one deterministic answer when a managed site holds a published override for an item it is not permitted to display, and avoids loading override content that will not be rendered.

**Alternatives considered**:

- Resolve the override first and then filter: same visible outcome, but performs unnecessary content loads and makes the suppression reason ambiguous in logs and admin views.

## Decision: Managed-site additions happen through container override

**Rationale**: The unified model overrides existing items rather than creating new ones. A managed site that needs its own extra entries overrides the container that holds them, such as a menu or a widget list, and supplies its own children inside that override. This keeps one mechanism while still permitting additive content where the blueprint author placed a container.

**Consequences**: A managed site cannot place content where the blueprint placed no container. That is an accepted limitation for this version and is recorded in the spec's out-of-scope section.

**Alternatives considered**:

- Add a second mechanism for standalone managed-site items: restores full additive freedom, but reintroduces the parallel model this design set out to remove.
- Allow managed sites to create content anywhere: maximum flexibility, but removes blueprint governance over where managed content can appear.

## Decision: Managed-site clearance is token-derived and rechecked on privileged actions

**Rationale**: Token claims/scopes align the portal with API authorization and provide a compact way to expose only authorized Managed Sites. Privileged mutations must validate scope again to handle changed clearance during an active session.

**Alternatives considered**:

- Portal-only filtering: improves UX but is insufficient security.
- Role-only authorization without managed-site IDs: simple but cannot express per-site clearance precisely.

## Decision: Managed Site Admin Portal uses OrchardCore APIs and existing preview behavior

**Rationale**: The portal is React-based but should not become a parallel CMS backend. OrchardCore APIs remain the content mutation and preview boundary, with preview simulating the ManagedSites request composition pipeline for the active Managed Site URL context so the composed page matches published rendering rules.

**Alternatives considered**:

- Direct document writes from the portal backend: fast but bypasses OrchardCore content lifecycle and permissions.
- Separate preview engine: flexible but likely to diverge from real rendering behavior.

## Decision: Address a Managed Site the way a tenant is addressed

**Rationale**: An OrchardCore tenant is addressed by a request host list plus one URL prefix, and administrators already know that model. Giving a Managed Site the same shape, a Hostname holding one or more host names and a single URL Prefix, means the mental model, the matching rules, and the precedence between a host-specific and a host-agnostic entry all carry over unchanged. It also removes a whole entity: there is no separate registration record to keep in step with the Managed Site that owns it.

**Consequences**:

- A Managed Site expands to one address per host name, mirroring how a tenant expands its request hosts.
- Conflicts are decided on the expanded addresses, so an empty Hostname collides with its prefix on every host.
- A Managed Site cannot hold two unrelated paths. That is the deliberate cost of matching the tenant shape; a second path means a second Managed Site, just as it would mean a second tenant.

**Alternatives considered**:

- A list of free-form address entries per Managed Site: more flexible, and it allowed one Managed Site to own several unrelated paths, but it invented a second addressing vocabulary next to the tenant one and needed its own registration entity, normalization rules, and conflict model.
- Host only, with no prefix: simplest to match, but forces a domain per Managed Site and rules out running several Managed Sites under one domain.

## Decision: Tenant hostname synchronization is event-driven and covers hosts only

**Rationale**: A Managed Site host name only works if the tenant answers on it. Synchronizing when a Managed Site is saved or removed avoids manual refresh operations and keeps the tenant reachable on every address it serves.

Only host names reach the tenant settings, because the two tenant URL settings are not symmetrical. The tenant Hostname holds a separator-delimited list, so every Managed Site host name can be added and the tenant will answer on it. The tenant URL Prefix is a single value for all of the tenant content, so Managed Site prefixes cannot each become a tenant prefix; they are resolved inside the tenant by the request pipeline instead. A Managed Site therefore needs synchronization exactly when it carries a host name.

**Consequences**:

- Synchronization records the host names it applied, so a later pass withdraws exactly those and leaves host names an operator configured for the tenant in place. Replacing the whole list would silently drop the tenant own domain.
- Host names are added even to a tenant whose Hostname is empty. That narrows the tenant from answering on every host to answering only on the declared ones, which is the intended model: a tenant serving Managed Sites declares the hosts it serves. The operational consequence is real and was observed in practice, so it is stated plainly rather than guarded against: an address that is not declared stops resolving, including a development host such as localhost, which must therefore be declared alongside the Managed Site domains.
- The Hostname is never emptied while the tenant URL Prefix is also empty. `RunningShellTable` treats a tenant as a catch-all only when both are empty, so a tenant carrying a prefix stays addressable and may have its Hostname cleared safely.
- Persisting shell settings reloads the tenant, so the update is deferred until the current request completes rather than pulled out from under it, and it only runs when the resulting host list actually differs.

**Alternatives considered**:

- Manual shell URL refresh: operationally risky and contradicts the spec.
- Replace the shell host list with the derived set: one simple assignment, but destroys operator-configured hosts and can make the tenant unreachable.
- Give each Managed Site its own shell prefix: impossible without turning Managed Sites into separate tenants, which the blueprint model explicitly rejects.
- Recompute on every request: simple correctness model, but costly and unnecessary for mostly administrative changes.

## Decision: Composition cache state is maintained through OrchardCore cache infrastructure

**Rationale**: OrchardCore already provides cache infrastructure through cache contexts, tags, signals, dynamic cache, and site cache management settings. ManagedSites composition should participate in that infrastructure so published changes appear immediately after the relevant cache invalidation completes according to configured cache management behavior. Scope changes must invalidate every managed site added to or removed from either scope, not only the edited item.

**Alternatives considered**:

- Fixed freshness window such as 60 seconds: simple to describe, but weaker than OrchardCore's cache invalidation model and potentially misleading when cache settings differ.
- Recompute every composition dependency on each request: strongest freshness guarantee, but unnecessary runtime cost for mostly administrative changes.
