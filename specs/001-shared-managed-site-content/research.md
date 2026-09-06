# Research: Site Blueprint and Managed Site Content

## Decision: Use one VendallionCMS.ManagedSites module family for v1

**Rationale**: A single module family keeps the first implementation reviewable while still allowing feature-level activation boundaries for routing, permissions, composition, admin portal, and customization behavior.

**Alternatives considered**:
- Multiple module projects from day one: clearer physical separation, but higher coordination and setup cost before responsibilities stabilize.
- Extending existing OrchardCore modules directly: lower initial code footprint, but risks coupling custom behavior to upstream module internals.

## Decision: Represent the Site Blueprint as the common composition authority

**Rationale**: The Site Blueprint owns common content, navigation structure, placeholder definitions, page override policies, and layer contribution points inside a single OrchardCore tenant. This separates governance from Managed Site authoring without requiring Managed Sites to be separate OrchardCore tenants.

**Alternatives considered**:
- Treat each Managed Site as a separate OrchardCore tenant: familiar OrchardCore model, but unnecessary for this scope and weaker for composing blueprint content with URL-scoped managed-site content in one site.
- Duplicate blueprint content into each Managed Site: simple runtime lookup, but creates content drift and migration complexity.

## Decision: Use a hybrid content targeting model

**Rationale**: Route-scoped content entries fit page-like URL ownership, while layer-based targeting fits layout-rendered supplemental content. The clarified spec requires both: page overrides affect the page surface, and layer items stay independent because the layout renders them.

**Alternatives considered**:
- Autoroute-only: efficient for page URLs, but insufficient for independent layout layer rendering.
- Layers-only: strong for widgets and layout contributions, but weak for page-level override semantics.

## Decision: Keep layout layer contributions independent from page overrides

**Rationale**: OrchardCore layers are layout-level composition surfaces. If items are defined in layers, the layout renders them independently of whether a page is overridden. A page can contribute to layers through widget lists, but those contributions are not mutually exclusive with page rendering.

**Alternatives considered**:
- Page override suppresses all layer contributions: easier precedence model, but contradicts OrchardCore layer behavior and would surprise editors.
- Always merge every page and placeholder contribution: flexible, but too ambiguous when a full page override is active.

## Decision: Suppress blueprint page placeholders when a managed-site page override is active

**Rationale**: A page override replaces the blueprint page for that Managed Site. Rendering placeholders from the overridden blueprint page would create a mixed page that is hard for administrators to reason about.

**Alternatives considered**:
- Keep placeholders from the blueprint page after override: allows reuse, but weakens the meaning of a full override.
- Let each override choose placeholder behavior: flexible, but creates a larger testing and UX matrix.

## Decision: Disabled page overrides stop rendering but remain recoverable

**Rationale**: If a blueprint page becomes non-overrideable, the blueprint's governance must take effect immediately. Preserving existing managed-site overrides for review and cleanup prevents data loss.

**Alternatives considered**:
- Keep existing overrides rendering: preserves local content, but violates the blueprint administrator's policy change.
- Delete overrides automatically: simple state, but unsafe and hard to audit.

## Decision: Empty managed-site placeholders use blueprint fallback content when available

**Rationale**: Blueprint fallback content lets blueprint authors provide a graceful default. Rendering empty when no fallback exists keeps behavior deterministic without blocking publishing.

**Alternatives considered**:
- Always empty: clear but misses useful default content.
- Block publishing until filled: heavy workflow constraint and too strict for optional placeholders.
- Hide containing sections automatically: visually appealing in some themes, but too dependent on layout structure.

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

## Decision: Shell URL synchronization is event-driven from blueprint and managed-site URL changes

**Rationale**: URL registrations affect request resolution. Synchronizing shell URLs when mappings change avoids manual refresh operations and keeps single-tenant blueprint routing consistent with Managed Site URL contexts.

**Alternatives considered**:
- Manual shell URL refresh: operationally risky and contradicts the spec.
- Recompute on every request: simple correctness model, but costly and unnecessary for mostly administrative changes.

## Decision: Composition cache state is maintained through OrchardCore cache infrastructure

**Rationale**: OrchardCore already provides cache infrastructure through cache contexts, tags, signals, dynamic cache, and site cache management settings. ManagedSites composition should participate in that infrastructure so published changes appear immediately after the relevant cache invalidation completes according to configured cache management behavior.

**Alternatives considered**:
- Fixed freshness window such as 60 seconds: simple to describe, but weaker than OrchardCore's cache invalidation model and potentially misleading when cache settings differ.
- Recompute every composition dependency on each request: strongest freshness guarantee, but unnecessary runtime cost for mostly administrative changes.