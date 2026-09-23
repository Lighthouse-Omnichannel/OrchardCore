/**
 * Client for the Managed Sites API.
 *
 * Every scoped call carries the active Managed Site as the X-Managed-Site-Id consistency header. The
 * server treats that header only as metadata: it must agree with the operation scope, and clearance
 * always comes from the caller's signed claims.
 */

export type ManagedSiteStatus = 'Enabled' | 'Disabled';

export interface ManagedSiteSummary {
    id: string;
    name: string;
    status: ManagedSiteStatus;
    /** Host names the managed site answers on, empty for every host the tenant serves. */
    hostname: string;
    /** Path prefix the managed site answers under, empty for the root. */
    urlPrefix: string;
}

export type ManagedContentOverrideStatus = 'None' | 'Draft' | 'Published' | 'Suppressed';

export type ManagedContentSuppressionReason =
    | 'EditScopeRemoved'
    | 'SourceUnpublished'
    | 'SourceDeleted'
    | 'CapabilityDetached'
    | 'ManagedSiteDisabled';

export interface ManagedContentOverrideSummary {
    overrideContentItemId: string;
    status: ManagedContentOverrideStatus;
    /** Null while the override renders. */
    suppressionReason: ManagedContentSuppressionReason | null;
    /**
     * Other content items claiming to override the same item, which are not served.
     *
     * Empty in normal operation. Content arriving by import or recipe can still produce one, and it has
     * to be visible or an editor would wonder why their changes have no effect.
     */
    supersededOverrideContentItemIds: string[];
}

export interface ManagedContentListItem {
    sourceContentItemId: string;
    contentType: string;
    displayText: string;
    /** Overriding a container replaces the children the blueprint placed inside it. */
    isContainer: boolean;
    displayScopeIncludesManagedSite: boolean;
    override: ManagedContentOverrideSummary | null;
}

export interface ManagedContentListResponse {
    items: ManagedContentListItem[];
    totalCount: number;
}

export interface ManagedContentDetail {
    sourceContentItemId: string;
    contentType: string;
    displayText: string;
    editScopeIncludesManagedSite: boolean;
    displayScopeIncludesManagedSite: boolean;
    override: ManagedContentOverrideSummary | null;
}

export interface SuppressedOverride {
    sourceContentItemId: string;
    overrideContentItemId: string;
    status: ManagedContentOverrideStatus;
    suppressionReason: ManagedContentSuppressionReason;
}

export interface SuppressedOverridesResponse {
    items: SuppressedOverride[];
}

export interface ManagedContentQuery {
    contentType?: string;
    overrideStatus?: ManagedContentOverrideStatus;
    page?: number;
    pageSize?: number;
}

export interface AuthorizedManagedSitesResponse {
    items: ManagedSiteSummary[];
}

export interface ManagedSiteSessionResponse {
    managedSiteId: string | null;
    selectedAt?: string;
    requiresSelection: boolean;
    authorizedManagedSites: ManagedSiteSummary[];
}

export interface ManagedSitesApiProblem {
    status: number;
    title: string;
    detail: string;
    code: string;
}

export class ManagedSitesApiError extends Error {
    readonly problem: ManagedSitesApiProblem;

    constructor(problem: ManagedSitesApiProblem) {
        super(problem.detail || problem.title || `Request failed with status ${problem.status}.`);
        this.name = 'ManagedSitesApiError';
        this.problem = problem;
    }

    get status(): number {
        return this.problem.status;
    }

    get code(): string {
        return this.problem.code;
    }
}

export interface ManagedSitesApiOptions {
    /** Base URL of the Managed Sites API, without a trailing slash. */
    apiBaseUrl: string;
    /** Optional bearer token used when the portal runs outside an authenticated admin session. */
    accessToken?: string | null;
    /**
     * Antiforgery request token.
     *
     * The API also accepts the admin cookie, so state-changing calls made from an admin session must
     * carry this token. Bearer-token callers are not cookie-authenticated and do not need it.
     */
    antiforgeryToken?: string | null;
}

export class ManagedSitesApi {
    private readonly baseUrl: string;
    private readonly accessToken: string | null;
    private readonly antiforgeryToken: string | null;

    constructor(options: ManagedSitesApiOptions) {
        this.baseUrl = options.apiBaseUrl.replace(/\/+$/, '');
        this.accessToken = options.accessToken ?? null;
        this.antiforgeryToken = options.antiforgeryToken ?? null;
    }

    /** Lists the Managed Sites the caller's clearance covers. */
    async getAuthorizedManagedSites(): Promise<ManagedSiteSummary[]> {
        const response = await this.send<AuthorizedManagedSitesResponse>('GET', '/authorized');

        return response.items ?? [];
    }

    /** Resolves the active Managed Site, auto-selecting when exactly one is authorized. */
    getSession(): Promise<ManagedSiteSessionResponse> {
        return this.send<ManagedSiteSessionResponse>('GET', '/session');
    }

    /** Selects the Managed Site that scopes the session. */
    selectManagedSite(managedSiteId: string): Promise<ManagedSiteSessionResponse> {
        return this.send<ManagedSiteSessionResponse>('POST', '/session', {
            body: { managedSiteId },
            managedSiteId,
        });
    }

    /** Lists the content items the managed site may override, with each item's override status. */
    listManagedContent(managedSiteId: string, query: ManagedContentQuery = {}): Promise<ManagedContentListResponse> {
        const search = new URLSearchParams();

        if (query.contentType) {
            search.set('contentType', query.contentType);
        }

        if (query.overrideStatus) {
            search.set('overrideStatus', query.overrideStatus);
        }

        if (query.page !== undefined) {
            search.set('page', String(query.page));
        }

        if (query.pageSize !== undefined) {
            search.set('pageSize', String(query.pageSize));
        }

        const suffix = search.size === 0 ? '' : `?${search.toString()}`;

        return this.send<ManagedContentListResponse>('GET', `/${managedSiteId}/managed-content${suffix}`, {
            managedSiteId,
        });
    }

    /** Gets one content item as the managed site sees it. */
    getManagedContent(managedSiteId: string, sourceContentItemId: string): Promise<ManagedContentDetail> {
        return this.send<ManagedContentDetail>('GET', `/${managedSiteId}/managed-content/${sourceContentItemId}`, {
            managedSiteId,
        });
    }

    /**
     * Creates this managed site's version of a blueprint item.
     *
     * The server creates the content item, starting from the blueprint content and owned by this
     * managed site from the moment it exists, which is what lets clearance alone authorize editing it.
     */
    createOverride(managedSiteId: string, sourceContentItemId: string): Promise<ManagedContentOverrideSummary> {
        return this.send<ManagedContentOverrideSummary>(
            'POST',
            `/${managedSiteId}/managed-content/${sourceContentItemId}/override`,
            { managedSiteId },
        );
    }

    /**
     * Sets whether this managed site's version is published, or points the item at a different one.
     */
    saveOverride(
        managedSiteId: string,
        sourceContentItemId: string,
        overrideContentItemId: string,
        status: 'Draft' | 'Published',
    ): Promise<ManagedContentOverrideSummary> {
        return this.send<ManagedContentOverrideSummary>(
            'PUT',
            `/${managedSiteId}/managed-content/${sourceContentItemId}/override`,
            { body: { overrideContentItemId, status }, managedSiteId },
        );
    }

    /** Removes this managed site's override, restoring the original content for it. */
    removeOverride(managedSiteId: string, sourceContentItemId: string): Promise<void> {
        return this.send<void>('DELETE', `/${managedSiteId}/managed-content/${sourceContentItemId}/override`, {
            managedSiteId,
        });
    }

    /** Lists the overrides that exist but do not render, each with the reason. */
    async listSuppressedOverrides(managedSiteId: string): Promise<SuppressedOverride[]> {
        const response = await this.send<SuppressedOverridesResponse>(
            'GET',
            `/${managedSiteId}/managed-content/suppressed`,
            { managedSiteId },
        );

        return response.items ?? [];
    }

    private async send<T>(
        method: string,
        path: string,
        options: { body?: unknown; managedSiteId?: string } = {},
    ): Promise<T> {
        const headers: Record<string, string> = { Accept: 'application/json' };

        if (options.body !== undefined) {
            headers['Content-Type'] = 'application/json';
        }

        if (options.managedSiteId) {
            headers['X-Managed-Site-Id'] = options.managedSiteId;
        }

        if (this.accessToken) {
            headers.Authorization = `Bearer ${this.accessToken}`;
        }

        if (this.antiforgeryToken && method !== 'GET') {
            headers.RequestVerificationToken = this.antiforgeryToken;
        }

        const response = await fetch(`${this.baseUrl}${path}`, {
            method,
            headers,
            credentials: 'include',
            body: options.body === undefined ? undefined : JSON.stringify(options.body),
        });

        if (!response.ok) {
            throw new ManagedSitesApiError(await readProblem(response));
        }

        if (response.status === 204) {
            return undefined as T;
        }

        return (await response.json()) as T;
    }
}

async function readProblem(response: Response): Promise<ManagedSitesApiProblem> {
    try {
        const problem = (await response.json()) as Partial<ManagedSitesApiProblem>;
        if (problem && typeof problem.status === 'number') {
            return problem as ManagedSitesApiProblem;
        }
    } catch {
        // Fall through to a synthetic problem when the body is absent or not JSON.
    }

    return {
        status: response.status,
        title: response.statusText || 'Request failed',
        detail: `The Managed Sites API returned status ${response.status}.`,
        code: 'managed-sites.request-failed',
    };
}
