import { useCallback, useEffect, useState } from 'react';
import { describeReason } from './ManagedContentListPage';
import { ManagedSitesApi, ManagedSitesApiError, type SuppressedOverride } from '../services/managedSitesApi';

export interface SuppressedOverridesPageProps {
    api: ManagedSitesApi;
    managedSiteId: string;
    onOpen: (sourceContentItemId: string) => void;
}

type PageState =
    | { kind: 'loading' }
    | { kind: 'ready'; items: SuppressedOverride[] }
    | { kind: 'error'; message: string };

/**
 * Shows the versions this managed site holds that are not reaching visitors.
 *
 * Nothing here is lost work. An override stops rendering when something outside it changes, and starts
 * again on its own once that is reversed, so this page exists to explain why and to let an editor
 * decide whether to chase the cause or clean the override up.
 */
export function SuppressedOverridesPage({ api, managedSiteId, onOpen }: SuppressedOverridesPageProps) {
    const [state, setState] = useState<PageState>({ kind: 'loading' });

    const load = useCallback(async () => {
        setState({ kind: 'loading' });

        try {
            setState({ kind: 'ready', items: await api.listSuppressedOverrides(managedSiteId) });
        } catch (error) {
            setState({ kind: 'error', message: toMessage(error) });
        }
    }, [api, managedSiteId]);

    useEffect(() => {
        void load();
    }, [load]);

    if (state.kind === 'loading') {
        return (
            <p className="suppressed-overrides__status" role="status">
                Checking your versions...
            </p>
        );
    }

    if (state.kind === 'error') {
        return (
            <div className="suppressed-overrides__error" role="alert">
                <p>{state.message}</p>
                <button type="button" onClick={() => void load()}>
                    Try again
                </button>
            </div>
        );
    }

    if (state.items.length === 0) {
        return (
            <p className="suppressed-overrides__empty">
                Every version you have made is reaching your visitors.
            </p>
        );
    }

    return (
        <section className="suppressed-overrides">
            <header className="suppressed-overrides__header">
                <h3>Versions that are not rendering</h3>
                <p>
                    These stay exactly as you left them. If the cause is reversed, they start rendering again without
                    being rebuilt.
                </p>
            </header>

            <ul className="suppressed-overrides__list">
                {state.items.map((item) => (
                    <li key={item.sourceContentItemId} className="suppressed-overrides__item">
                        <span className="suppressed-overrides__reason">
                            Not rendering because {describeReason(item.suppressionReason)}.
                        </span>
                        <span className="suppressed-overrides__target">
                            Blueprint item {item.sourceContentItemId}
                        </span>
                        <button type="button" onClick={() => onOpen(item.sourceContentItemId)}>
                            Review
                        </button>
                    </li>
                ))}
            </ul>
        </section>
    );
}

function toMessage(error: unknown): string {
    if (error instanceof ManagedSitesApiError || error instanceof Error) {
        return error.message;
    }

    return 'Your versions could not be checked.';
}
