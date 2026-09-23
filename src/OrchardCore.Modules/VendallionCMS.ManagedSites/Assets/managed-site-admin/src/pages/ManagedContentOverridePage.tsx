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
 * Creates and publishes the active managed site's version of one blueprint item.
 *
 * The server creates the version, as a content item of the same type as the item it replaces and owned
 * by this managed site from the start, so an editor needs no permission over the tenant's content to
 * author it. Its fields are edited in the platform content editor; this page owns whether it exists and
 * whether it is published.
 */
export function ManagedContentOverridePage({
    api,
    managedSiteId,
    sourceContentItemId,
    onClose,
}: ManagedContentOverridePageProps) {
    const [state, setState] = useState<PageState>({ kind: 'loading' });
    const [isBusy, setIsBusy] = useState(false);
    const [notice, setNotice] = useState<string | null>(null);
    const [failure, setFailure] = useState<string | null>(null);

    const load = useCallback(async () => {
        setState({ kind: 'loading' });

        try {
            setState({ kind: 'ready', detail: await api.getManagedContent(managedSiteId, sourceContentItemId) });
        } catch (error) {
            setState({ kind: 'error', message: toMessage(error) });
        }
    }, [api, managedSiteId, sourceContentItemId]);

    useEffect(() => {
        void load();
    }, [load]);

    const run = useCallback(
        async (action: () => Promise<unknown>, success: string) => {
            setIsBusy(true);
            setNotice(null);
            setFailure(null);

            try {
                await action();
                setNotice(success);
                await load();
            } catch (error) {
                setFailure(toMessage(error));
            } finally {
                setIsBusy(false);
            }
        },
        [load],
    );

    const create = useCallback(
        () =>
            run(
                () => api.createOverride(managedSiteId, sourceContentItemId),
                'Your version was created as a draft, starting from the blueprint content.',
            ),
        [api, managedSiteId, run, sourceContentItemId],
    );

    const publish = useCallback(
        (overrideContentItemId: string) =>
            run(
                () => api.saveOverride(managedSiteId, sourceContentItemId, overrideContentItemId, 'Published'),
                'Your version is live on this site.',
            ),
        [api, managedSiteId, run, sourceContentItemId],
    );

    const remove = useCallback(
        () =>
            run(
                () => api.removeOverride(managedSiteId, sourceContentItemId),
                'Your version was removed. This site shows the blueprint content again.',
            ),
        [api, managedSiteId, run, sourceContentItemId],
    );

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
    const existing = detail.override;

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

            {existing && existing.supersededOverrideContentItemIds.length > 0 && (
                <p className="managed-content-override__warning" role="alert">
                    {existing.supersededOverrideContentItemIds.length} other content item(s) also claim to override
                    this item. Only {existing.overrideContentItemId} is served. The rest should be removed.
                </p>
            )}

            {existing ? (
                <>
                    <p className="managed-content-override__existing">
                        Your version is content item <code>{existing.overrideContentItemId}</code>, currently{' '}
                        {existing.status === 'Published' ? 'published to this site' : 'a draft'}. Edit its content in
                        the content editor, then publish it here.
                    </p>

                    <div className="managed-content-override__actions">
                        <button
                            type="button"
                            disabled={isBusy || existing.status === 'Published'}
                            onClick={() => void publish(existing.overrideContentItemId)}
                        >
                            Publish to this site
                        </button>
                        <button type="button" disabled={isBusy} onClick={() => void remove()}>
                            Remove my version
                        </button>
                    </div>
                </>
            ) : (
                <div className="managed-content-override__actions">
                    <p className="managed-content-override__hint">
                        Creating your version copies the current blueprint content, so you change what differs rather
                        than starting from an empty {detail.contentType}.
                    </p>
                    <button type="button" disabled={isBusy} onClick={() => void create()}>
                        Create my version
                    </button>
                </div>
            )}

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
