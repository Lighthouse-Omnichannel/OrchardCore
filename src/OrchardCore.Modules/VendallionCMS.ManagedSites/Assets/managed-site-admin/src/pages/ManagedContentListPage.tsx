import { useCallback, useEffect, useState } from 'react';
import {
    ManagedSitesApi,
    ManagedSitesApiError,
    type ManagedContentListItem,
    type ManagedContentOverrideStatus,
} from '../services/managedSitesApi';

export interface ManagedContentListPageProps {
    api: ManagedSitesApi;
    managedSiteId: string;
    onOpen: (item: ManagedContentListItem) => void;
}

type ListState =
    | { kind: 'loading' }
    | { kind: 'ready'; items: ManagedContentListItem[]; totalCount: number }
    | { kind: 'error'; message: string };

const STATUS_FILTERS: { value: ManagedContentOverrideStatus | ''; label: string }[] = [
    { value: '', label: 'All items' },
    { value: 'None', label: 'Not overridden' },
    { value: 'Draft', label: 'Draft override' },
    { value: 'Published', label: 'Published override' },
    { value: 'Suppressed', label: 'Not rendering' },
];

/**
 * Lists everything the active managed site is allowed to customize.
 *
 * The list is the server's answer to one question: which items name this managed site in their edit
 * scope. Nothing is filtered client side for access, so an item that appears here is one the site may
 * override, and one that does not appear is not hidden but out of reach.
 */
export function ManagedContentListPage({ api, managedSiteId, onOpen }: ManagedContentListPageProps) {
    const [state, setState] = useState<ListState>({ kind: 'loading' });
    const [statusFilter, setStatusFilter] = useState<ManagedContentOverrideStatus | ''>('');

    const load = useCallback(async () => {
        setState({ kind: 'loading' });

        try {
            const response = await api.listManagedContent(managedSiteId, {
                overrideStatus: statusFilter === '' ? undefined : statusFilter,
            });

            setState({ kind: 'ready', items: response.items ?? [], totalCount: response.totalCount ?? 0 });
        } catch (error) {
            setState({ kind: 'error', message: toMessage(error) });
        }
    }, [api, managedSiteId, statusFilter]);

    useEffect(() => {
        void load();
    }, [load]);

    if (state.kind === 'loading') {
        return (
            <p className="managed-content-list__status" role="status">
                Loading the content you can customize...
            </p>
        );
    }

    if (state.kind === 'error') {
        return (
            <div className="managed-content-list__error" role="alert">
                <p>{state.message}</p>
                <button type="button" onClick={() => void load()}>
                    Try again
                </button>
            </div>
        );
    }

    return (
        <section className="managed-content-list">
            <header className="managed-content-list__header">
                <h3>Content you can customize</h3>
                <label className="managed-content-list__filter">
                    Show
                    <select
                        value={statusFilter}
                        onChange={(event) => setStatusFilter(event.target.value as ManagedContentOverrideStatus | '')}
                    >
                        {STATUS_FILTERS.map((filter) => (
                            <option key={filter.value} value={filter.value}>
                                {filter.label}
                            </option>
                        ))}
                    </select>
                </label>
            </header>

            {state.items.length === 0 ? (
                <p className="managed-content-list__empty">
                    No blueprint content is open to this managed site yet. A site blueprint administrator decides
                    which items you can override.
                </p>
            ) : (
                <table className="managed-content-list__table">
                    <thead>
                        <tr>
                            <th scope="col">Item</th>
                            <th scope="col">Type</th>
                            <th scope="col">Your version</th>
                            <th scope="col">
                                <span className="visually-hidden">Actions</span>
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {state.items.map((item) => (
                            <tr key={item.sourceContentItemId}>
                                <td>
                                    <span className="managed-content-list__name">
                                        {item.displayText || item.sourceContentItemId}
                                    </span>
                                    {item.isContainer && (
                                        <span className="managed-content-list__badge">
                                            Container: your version replaces its children
                                        </span>
                                    )}
                                    {!item.displayScopeIncludesManagedSite && (
                                        <span className="managed-content-list__warning" role="note">
                                            This item does not render on your site, so an override would not be seen.
                                        </span>
                                    )}
                                </td>
                                <td>{item.contentType}</td>
                                <td>{describeOverride(item)}</td>
                                <td>
                                    <button type="button" onClick={() => onOpen(item)}>
                                        {item.override ? 'Edit your version' : 'Create your version'}
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            <p className="managed-content-list__count">
                {state.totalCount} {state.totalCount === 1 ? 'item' : 'items'}
            </p>
        </section>
    );
}

function describeOverride(item: ManagedContentListItem): string {
    if (!item.override) {
        return 'Using the blueprint content';
    }

    if (item.override.status === 'Suppressed') {
        return `Not rendering: ${describeReason(item.override.suppressionReason)}`;
    }

    return item.override.status === 'Published' ? 'Published' : 'Draft, not yet published';
}

/** Turns a suppression reason into something an editor can act on. */
export function describeReason(reason: string | null): string {
    switch (reason) {
        case 'EditScopeRemoved':
            return 'this managed site can no longer override the item';
        case 'SourceUnpublished':
            return 'the blueprint item is no longer published';
        case 'SourceDeleted':
            return 'the blueprint item no longer exists';
        case 'CapabilityDetached':
            return 'the item is no longer open to managed sites';
        case 'ManagedSiteDisabled':
            return 'this managed site is not currently serving content';
        default:
            return 'the reason was not recorded';
    }
}

function toMessage(error: unknown): string {
    if (error instanceof ManagedSitesApiError || error instanceof Error) {
        return error.message;
    }

    return 'The content list could not be loaded.';
}
