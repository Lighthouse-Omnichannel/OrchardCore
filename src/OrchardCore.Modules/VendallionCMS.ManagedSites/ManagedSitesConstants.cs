namespace VendallionCMS.ManagedSites;

public static class ManagedSitesConstants
{
    public static class Features
    {
        public const string ManagedSites = "VendallionCMS.ManagedSites";
        public const string AdminPortal = "VendallionCMS.ManagedSites.AdminPortal";
        public const string Routing = "VendallionCMS.ManagedSites.Routing";
        public const string Permissions = "VendallionCMS.ManagedSites.Permissions";
    }

    /// <summary>
    /// Authorization policies used by the Managed Sites API surface.
    /// </summary>
    public static class AuthorizationPolicies
    {
        /// <summary>
        /// Requires an authenticated user on either the bearer API scheme or the admin cookie scheme.
        /// </summary>
        public const string ManagedSitesApi = "ManagedSitesApi";
    }

    /// <summary>
    /// Action scopes that Managed Site clearance can grant.
    /// </summary>
    public static class Scopes
    {
        /// <summary>
        /// Allows reading Managed Site content.
        /// </summary>
        public const string View = "view";

        /// <summary>
        /// Allows changing Managed Site content.
        /// </summary>
        public const string Edit = "edit";

        /// <summary>
        /// Allows publishing Managed Site content.
        /// </summary>
        public const string Publish = "publish";

        /// <summary>
        /// Allows previewing composed Managed Site output.
        /// </summary>
        public const string Preview = "preview";
    }

    /// <summary>
    /// Request headers understood by the Managed Sites API.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Optional consistency header carrying the Managed Site the client believes is active.
        /// </summary>
        public const string ManagedSiteId = "X-Managed-Site-Id";
    }

    /// <summary>
    /// Problem detail codes returned by the Managed Sites API.
    /// </summary>
    public static class ErrorCodes
    {
        public const string ScopeMismatch = "managed-sites.scope-mismatch";
        public const string SessionScopeMismatch = "managed-sites.session-scope-mismatch";
        public const string NoClearance = "managed-sites.no-clearance";
        public const string ManagedSiteUnavailable = "managed-sites.managed-site-unavailable";
        public const string SelectionRequired = "managed-sites.selection-required";
        public const string InvalidName = "managed-sites.invalid-name";
        public const string InvalidUrl = "managed-sites.invalid-url";
        public const string UrlConflict = "managed-sites.url-conflict";
        public const string NameConflict = "managed-sites.name-conflict";
        public const string ManagedSiteNotFound = "managed-sites.not-found";
        public const string SourceNotFound = "managed-sites.source-not-found";
        public const string SourceNotManagedContent = "managed-sites.not-managed-content";
        public const string EditScopeExcluded = "managed-sites.edit-scope-excluded";
        public const string OverrideNotFound = "managed-sites.override-not-found";
        public const string OverrideAlreadyExists = "managed-sites.override-exists";
        public const string ContentTypeMismatch = "managed-sites.content-type-mismatch";
        public const string InvalidOverrideStatus = "managed-sites.invalid-override-status";
    }
}
