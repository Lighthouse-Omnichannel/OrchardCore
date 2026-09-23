import { useCallback, useEffect, useState } from 'react';
import { ManagedSiteSelector } from '../components/ManagedSiteSelector';
import { ManagedContentListPage } from './ManagedContentListPage';
import { ManagedContentOverridePage } from './ManagedContentOverridePage';
import { SuppressedOverridesPage } from './SuppressedOverridesPage';
import {
    ManagedSitesApi,
    ManagedSitesApiError,
    type ManagedSiteSessionResponse,
    type ManagedSiteSummary,
} from '../services/managedSitesApi';

export interface ManagedSiteAdminShellProps {
    api: ManagedSitesApi;
}

/** Which scoped screen is open once a managed site is active. */
type ScopedView = { kind: 'content' } | { kind: 'suppressed' } | { kind: 'override'; sourceContentItemId: string };

type ShellState =
    | { kind: 'loading' }
    | { kind: 'noClearance' }
    | { kind: 'noEnabledSite'; detail: string }
    | { kind: 'selecting'; managedSites: ManagedSiteSummary[] }
    | { kind: 'ready'; activeManagedSite: ManagedSiteSummary; managedSites: ManagedSiteSummary[] }
    | { kind: 'error'; message: string };

/**
 * Root of the Managed Site Admin Portal.
 *
 * Resolves the active Managed Site on mount. A user cleared for exactly one site is scoped
 * automatically; a user cleared for several must choose before any scoped screen is shown.
 */
export function ManagedSiteAdminShell({ api }: ManagedSiteAdminShellProps) {
    const [state, setState] = useState<ShellState>({ kind: 'loading' });
    const [isBusy, setIsBusy] = useState(false);
    const [view, setView] = useState<ScopedView>({ kind: 'content' });

    const applySession = useCallback((session: ManagedSiteSessionResponse): ShellState => {
        const managedSites = session.authorizedManagedSites ?? [];

        if (managedSites.length === 0) {
            return { kind: 'noClearance' };
        }

        if (session.requiresSelection || !session.managedSiteId) {
            return { kind: 'selecting', managedSites };
        }

        const activeManagedSite = managedSites.find((managedSite) => managedSite.id === session.managedSiteId);
        if (!activeManagedSite) {
            return { kind: 'selecting', managedSites };
        }

        return { kind: 'ready', activeManagedSite, managedSites };
    }, []);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const session = await api.getSession();
                if (!cancelled) {
                    setState(applySession(session));
                }
            } catch (error) {
                if (cancelled) {
                    return;
                }

                if (error instanceof ManagedSitesApiError && error.status === 403) {
                    // Clearance you hold for sites that are all switched off is not missing clearance,
                    // and pointing the user at an administrator for access would waste their time.
                    setState(
                        error.code === 'managed-sites.no-enabled-managed-site'
                            ? { kind: 'noEnabledSite', detail: error.message }
                            : { kind: 'noClearance' },
                    );
                    return;
                }

                setState({ kind: 'error', message: toMessage(error) });
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [api, applySession]);

    const handleSelect = useCallback(
        async (managedSiteId: string) => {
            setIsBusy(true);
            try {
                const session = await api.selectManagedSite(managedSiteId);
                setState(applySession(session));
            } catch (error) {
                setState({ kind: 'error', message: toMessage(error) });
            } finally {
                setIsBusy(false);
            }
        },
        [api, applySession],
    );

    if (state.kind === 'loading') {
        return (
            <p className="managed-site-admin__status" role="status">
                Loading the Managed Site Admin Portal...
            </p>
        );
    }

    if (state.kind === 'error') {
        return (
            <div className="managed-site-admin__error" role="alert">
                <p>{state.message}</p>
            </div>
        );
    }

    if (state.kind === 'noEnabledSite') {
        return (
            <div className="managed-site-admin__error" role="alert">
                <p>{state.detail}</p>
                <p>Enable a managed site under Managed Sites to start working on it.</p>
            </div>
        );
    }

    if (state.kind === 'noClearance') {
        return (
            <div className="managed-site-admin__error" role="alert">
                <p>You do not have clearance for any managed site. Contact a site blueprint administrator.</p>
            </div>
        );
    }

    if (state.kind === 'selecting') {
        return (
            <ManagedSiteSelector
                managedSites={state.managedSites}
                activeManagedSiteId={null}
                disabled={isBusy}
                onSelect={handleSelect}
            />
        );
    }

    const managedSiteId = state.activeManagedSite.id;

    return (
        <div className="managed-site-admin__shell">
            <header className="managed-site-admin__header">
                <h2 className="managed-site-admin__active">{state.activeManagedSite.name}</h2>
                {state.managedSites.length > 1 && (
                    <button
                        type="button"
                        className="managed-site-admin__switch"
                        disabled={isBusy}
                        onClick={() => setState({ kind: 'selecting', managedSites: state.managedSites })}
                    >
                        Switch managed site
                    </button>
                )}
            </header>
            <p className="managed-site-admin__scope-note">
                Every edit and preview in this session applies to {state.activeManagedSite.name} only.
            </p>

            <nav className="managed-site-admin__nav" aria-label="Managed site content">
                <button
                    type="button"
                    aria-current={view.kind === 'suppressed' ? undefined : 'page'}
                    onClick={() => setView({ kind: 'content' })}
                >
                    Content
                </button>
                <button
                    type="button"
                    aria-current={view.kind === 'suppressed' ? 'page' : undefined}
                    onClick={() => setView({ kind: 'suppressed' })}
                >
                    Not rendering
                </button>
            </nav>

            {view.kind === 'override' ? (
                <ManagedContentOverridePage
                    api={api}
                    managedSiteId={managedSiteId}
                    sourceContentItemId={view.sourceContentItemId}
                    onClose={() => setView({ kind: 'content' })}
                />
            ) : view.kind === 'suppressed' ? (
                <SuppressedOverridesPage
                    api={api}
                    managedSiteId={managedSiteId}
                    onOpen={(sourceContentItemId) => setView({ kind: 'override', sourceContentItemId })}
                />
            ) : (
                <ManagedContentListPage
                    api={api}
                    managedSiteId={managedSiteId}
                    onOpen={(item) => setView({ kind: 'override', sourceContentItemId: item.sourceContentItemId })}
                />
            )}
        </div>
    );
}

function toMessage(error: unknown): string {
    if (error instanceof ManagedSitesApiError) {
        return error.message;
    }

    if (error instanceof Error) {
        return error.message;
    }

    return 'The Managed Site Admin Portal could not load.';
}
