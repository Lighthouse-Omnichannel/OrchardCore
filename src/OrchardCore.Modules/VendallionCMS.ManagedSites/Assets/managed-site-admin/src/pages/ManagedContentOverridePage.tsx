import { useCallback, useEffect, useState } from 'react';
import { describeReason } from './ManagedContentListPage';
import {
    ManagedSitesApi,
    ManagedSitesApiError,
    type ManagedContentDetail,
} from '../services/managedSitesApi';

export interface ManagedContentOverridePageProps {
    api: ManagedSitesApi;
    managedSiteId: string;
    sourceContentItemId: string;
    onClose: () => void;
}

type PageState =
    | { kind: 'loading' }
    | { kind: 'ready'; detail: ManagedContentDetail }
    | { kind: 'error'; message: string };

/**
 * Registers and publishes the active managed site's version of one blueprint item.
 *
 * The override content itself is authored in the platform content editor, as a content item of the
 * same type as the item it replaces. This page owns only the link between the two and whether that
 * link is published, which is why it asks for a content item identifier rather than offering fields.
 */
export function ManagedContentOverridePage({
    api,
    managedSiteId,
    sourceContentItemId,
    onClose,
}: ManagedContentOverridePageProps) {
    const [state, setState] = useState<PageState>({ kind: 'loading' });
    const [overrideContentItemId, setOverrideContentItemId] = useState('');
    const [isBusy, setIsBusy] = useState(false);
    const [notice, setNotice] = useState<string | null>(null);
    const [failure, setFailure] = useState<string | null>(null);

    const load = useCallback(async () => {
        setState({ kind: 'loading' });

        try {
            const detail = await api.getManagedContent(managedSiteId, sourceContentItemId);
            setOverrideContentItemId(detail.override?.overrideContentItemId ?? '');
            setState({ kind: 'ready', detail });
        } catch (error) {
            setState({ kind: 'error', message: toMessage(error) });
        }
    }, [api, managedSiteId, sourceContentItemId]);

    useEffect(() => {
        void load();
    }, [load]);

    const save = useCallback(
        async (status: 'Draft' | 'Published') => {
            setIsBusy(true);
            setNotice(null);
            setFailure(null);

            try {
                await api.saveOverride(managedSiteId, sourceContentItemId, overrideContentItemId.trim(), status);
                setNotice(status === 'Published' ? 'Your version is live on this site.' : 'Draft saved.');
                await load();
            } catch (error) {
                setFailure(toMessage(error));
            } finally {
                setIsBusy(false);
            }
        },
        [api, load, managedSiteId, overrideContentItemId, sourceContentItemId],
    );

    const remove = useCallback(async () => {
        setIsBusy(true);
        setNotice(null);
        setFailure(null);

        try {
            await api.removeOverride(managedSiteId, sourceContentItemId);
            setNotice('Your version was removed. This site shows the blueprint content again.');
            await load();
        } catch (error) {
            setFailure(toMessage(error));
        } finally {
            setIsBusy(false);
        }
    }, [api, load, managedSiteId, sourceContentItemId]);

    if (state.kind === 'loading') {
        return (
            <p className="managed-content-override__status" role="status">
                Loading this item...
            </p>
        );
    }

    if (state.kind === 'error') {
        return (
            <div className="managed-content-override__error" role="alert">
                <p>{state.message}</p>
                <button type="button" onClick={onClose}>
                    Back to the list
                </button>
            </div>
        );
    }

    const { detail } = state;
    const canSave = overrideContentItemId.trim().length > 0 && !isBusy;

    return (
        <section className="managed-content-override">
            <header className="managed-content-override__header">
                <button type="button" className="managed-content-override__back" onClick={onClose}>
                    Back to the list
                </button>
                <h3>{detail.displayText || detail.sourceContentItemId}</h3>
                <p className="managed-content-override__type">{detail.contentType}</p>
            </header>

            {!detail.displayScopeIncludesManagedSite && (
                <p className="managed-content-override__warning" role="note">
                    This item does not render on your site, so a published version would not be seen by visitors.
                </p>
            )}

            {detail.override?.status === 'Suppressed' && (
                <p className="managed-content-override__warning" role="note">
                    Your version is not rendering because {describeReason(detail.override.suppressionReason)}. It is
                    kept as it is until that changes.
                </p>
            )}

            <label className="managed-content-override__field">
                Content item holding your version
                <input
                    type="text"
                    value={overrideContentItemId}
                    disabled={isBusy}
                    onChange={(event) => setOverrideContentItemId(event.target.value)}
                />
                <span className="managed-content-override__hint">
                    Create a {detail.contentType} item in the content editor, then name it here. It must be the same
                    content type as the item it replaces.
                </span>
            </label>

            <div className="managed-content-override__actions">
                <button type="button" disabled={!canSave} onClick={() => void save('Draft')}>
                    Save as draft
                </button>
                <button type="button" disabled={!canSave} onClick={() => void save('Published')}>
                    Publish to this site
                </button>
                {detail.override && (
                    <button type="button" disabled={isBusy} onClick={() => void remove()}>
                        Remove my version
                    </button>
                )}
            </div>

            {notice && (
                <p className="managed-content-override__notice" role="status">
                    {notice}
                </p>
            )}

            {failure && (
                <p className="managed-content-override__failure" role="alert">
                    {failure}
                </p>
            )}
        </section>
    );
}

function toMessage(error: unknown): string {
    if (error instanceof ManagedSitesApiError || error instanceof Error) {
        return error.message;
    }

    return 'The change could not be saved.';
}
