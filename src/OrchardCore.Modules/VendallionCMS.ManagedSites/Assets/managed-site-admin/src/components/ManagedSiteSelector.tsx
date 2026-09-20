import type { ManagedSiteSummary } from '../services/managedSitesApi';

export interface ManagedSiteSelectorProps {
    managedSites: ManagedSiteSummary[];
    activeManagedSiteId: string | null;
    disabled?: boolean;
    onSelect: (managedSiteId: string) => void;
}

/**
 * Lets a user with clearance to several Managed Sites choose the one that scopes the session.
 *
 * The component is presentational. The server decides which sites are offered and re-validates the
 * choice, so nothing here is a security boundary.
 */
export function ManagedSiteSelector({
    managedSites,
    activeManagedSiteId,
    disabled = false,
    onSelect,
}: ManagedSiteSelectorProps) {
    if (managedSites.length === 0) {
        return null;
    }

    return (
        <div className="managed-site-selector">
            <h2 className="managed-site-selector__title">Select a managed site</h2>
            <p className="managed-site-selector__hint">
                Choose the managed site to work on. All edits and previews apply to this site only.
            </p>
            <ul className="managed-site-selector__list" role="listbox" aria-label="Authorized managed sites">
                {managedSites.map((managedSite) => {
                    const isActive = managedSite.id === activeManagedSiteId;

                    return (
                        <li key={managedSite.id} className="managed-site-selector__item">
                            <button
                                type="button"
                                role="option"
                                aria-selected={isActive}
                                disabled={disabled}
                                className={
                                    isActive
                                        ? 'managed-site-selector__button managed-site-selector__button--active'
                                        : 'managed-site-selector__button'
                                }
                                onClick={() => onSelect(managedSite.id)}
                            >
                                <span className="managed-site-selector__name">{managedSite.name}</span>
                                <span className="managed-site-selector__address">{describeAddress(managedSite)}</span>
                            </button>
                        </li>
                    );
                })}
            </ul>
        </div>
    );
}

/**
 * Describes where a managed site answers, the way a tenant's own address reads.
 *
 * An empty hostname answers on every host the tenant serves, and an empty prefix answers at the root,
 * so both halves are spelled out rather than left blank.
 */
function describeAddress(managedSite: ManagedSiteSummary): string {
    const host = managedSite.hostname?.trim() || 'every host';
    const prefix = managedSite.urlPrefix?.trim();

    return prefix ? `${host} /${prefix}` : `${host} /`;
}
