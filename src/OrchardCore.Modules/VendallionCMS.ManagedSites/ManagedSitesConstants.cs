namespace VendallionCMS.ManagedSites;

public static class ManagedSitesConstants
{
    public static class Features
    {
        /// <summary>
        /// The module's one feature, which is also the area its controllers and views live in.
        /// </summary>
        public const string ManagedSites = "VendallionCMS.ManagedSites";
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
    /// How a request asks to be previewed.
    /// </summary>
    public static class Preview
    {
        /// <summary>
        /// Query value that asks for unpublished work, honoured only for a caller holding preview
        /// clearance for the Managed Site the request resolved to.
        /// </summary>
        public const string DraftsQueryKey = "managed-site-drafts";

        /// <summary>
        /// Composition mode reported when the previewed address resolves to a Managed Site.
        /// </summary>
        public const string ManagedSiteMode = "ManagedSite";

        /// <summary>
        /// Composition mode reported when the previewed address resolves to no Managed Site.
        /// </summary>
        public const string SiteBlueprintMode = "SiteBlueprint";
    }

    /// <summary>
    /// Request headers understood by the Managed Sites API.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// A header naming the Managed Site a caller believes it is addressing.
        ///
        /// Public rendering ignores it, and ignores anything else a caller supplies: the URL decides
        /// which Managed Site answers a request and nothing else does, or a visitor could ask for
        /// another Managed Site's content by sending one. It is named here so that the rule can be
        /// tested against the thing it forbids.
        /// </summary>
        public const string ManagedSiteId = "X-Managed-Site-Id";
    }

    /// <summary>
    /// Codes naming why something was refused, carried alongside the message explaining it.
    /// </summary>
    public static class ErrorCodes
    {
        public const string ScopeMismatch = "managed-sites.scope-mismatch";
        public const string SessionScopeMismatch = "managed-sites.session-scope-mismatch";
        public const string NoClearance = "managed-sites.no-clearance";
        public const string NoEnabledManagedSite = "managed-sites.no-enabled-managed-site";
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
