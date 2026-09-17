import { useCallback, useEffect, useState } from 'react';
import { ManagedSiteSelector } from '../components/ManagedSiteSelector';
import {
    ManagedSitesApi,
    ManagedSitesApiError,
    type ManagedSiteSessionResponse,
    type ManagedSiteSummary,
} from '../services/managedSitesApi';

export interface ManagedSiteAdminShellProps {
    api: ManagedSitesApi;
}

type ShellState =
    | { kind: 'loading' }
    | { kind: 'noClearance' }
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
                    setState({ kind: 'noClearance' });
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
