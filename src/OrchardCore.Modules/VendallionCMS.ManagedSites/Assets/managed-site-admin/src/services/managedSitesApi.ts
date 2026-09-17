/**
 * Client for the Managed Sites API.
 *
 * Every scoped call carries the active Managed Site as the X-Managed-Site-Id consistency header. The
 * server treats that header only as metadata: it must agree with the operation scope, and clearance
 * always comes from the caller's signed claims.
 */

export type ManagedSiteStatus = 'Draft' | 'Enabled' | 'Disabled' | 'Archived';

export interface ManagedSiteSummary {
    id: string;
    name: string;
    status: ManagedSiteStatus;
    urls: string[];
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
