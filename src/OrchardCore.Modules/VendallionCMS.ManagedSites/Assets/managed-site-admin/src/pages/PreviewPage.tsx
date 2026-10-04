import { useCallback, useState } from 'react';
import { ManagedSitesApi, ManagedSitesApiError, type ManagedSitePreview } from '../services/managedSitesApi';

export interface PreviewPageProps {
    api: ManagedSitesApi;
    managedSiteId: string;
    managedSiteName: string;
}

/**
 * Opens a page as this managed site's visitors would receive it.
 *
 * The preview is the managed site's own address, not a separate rendering path, so what it shows is
 * composed by the same pipeline that serves the site. Unpublished work is asked for in the address and
 * granted by your preview clearance when the page loads, so a link shared with someone without it
 * shows them only what is published.
 */
export function PreviewPage({ api, managedSiteId, managedSiteName }: PreviewPageProps) {
    const [url, setUrl] = useState('/');
    const [includeDrafts, setIncludeDrafts] = useState(true);
    const [preview, setPreview] = useState<ManagedSitePreview | null>(null);
    const [failure, setFailure] = useState<string | null>(null);
    const [isBusy, setIsBusy] = useState(false);

    const build = useCallback(async () => {
        setIsBusy(true);
        setFailure(null);
        setPreview(null);

        try {
            setPreview(await api.createPreview(managedSiteId, url.trim() || '/', includeDrafts));
        } catch (error) {
            setFailure(toMessage(error));
        } finally {
            setIsBusy(false);
        }
    }, [api, includeDrafts, managedSiteId, url]);

    return (
        <section className="managed-site-preview">
            <header className="managed-site-preview__header">
                <h3>Preview {managedSiteName}</h3>
                <p>
                    Opens the page at this managed site's own address, composed exactly as a visitor's request
                    is.
                </p>
            </header>

            <label className="managed-site-preview__field">
                Path
                <input
                    type="text"
                    value={url}
                    disabled={isBusy}
                    placeholder="/"
                    onChange={(event) => setUrl(event.target.value)}
                />
            </label>

            <label className="managed-site-preview__drafts">
                <input
                    type="checkbox"
                    checked={includeDrafts}
                    disabled={isBusy}
                    onChange={(event) => setIncludeDrafts(event.target.checked)}
                />
                Include work I have not published
            </label>

            <button type="button" disabled={isBusy} onClick={() => void build()}>
                Build preview link
            </button>

            {preview && (
                <p className="managed-site-preview__result">
                    <a href={preview.previewUrl} target="_blank" rel="noreferrer">
                        {preview.previewUrl}
                    </a>
                    <span className="managed-site-preview__mode">
                        {preview.compositionMode === 'ManagedSite'
                            ? ' — composes as this managed site'
                            : ' — composes as the site blueprint'}
                    </span>
                </p>
            )}

            {failure && (
                <p className="managed-site-preview__failure" role="alert">
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

    return 'The preview link could not be built.';
}
