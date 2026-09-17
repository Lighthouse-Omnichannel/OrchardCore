import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ManagedSiteAdminShell } from './pages/ManagedSiteAdminShell';
import { ManagedSitesApi } from './services/managedSitesApi';
import './index.css';

interface PortalBootstrap {
    apiBaseUrl: string;
    accessToken?: string | null;
    antiforgeryToken?: string | null;
}

const ROOT_ELEMENT_ID = 'managed-site-admin-root';
const BOOTSTRAP_ELEMENT_ID = 'managed-site-portal-bootstrap';

function readBootstrap(): PortalBootstrap | null {
    const element = document.getElementById(BOOTSTRAP_ELEMENT_ID);
    if (!element?.textContent) {
        return null;
    }

    try {
        const bootstrap = JSON.parse(element.textContent) as Partial<PortalBootstrap>;

        if (!bootstrap.apiBaseUrl) {
            return null;
        }

        return {
            apiBaseUrl: bootstrap.apiBaseUrl,
            accessToken: bootstrap.accessToken,
            antiforgeryToken: bootstrap.antiforgeryToken,
        };
    } catch {
        return null;
    }
}

function mount(): void {
    const container = document.getElementById(ROOT_ELEMENT_ID);
    if (!container) {
        return;
    }

    const bootstrap = readBootstrap();
    if (!bootstrap) {
        container.textContent = 'The Managed Site Admin Portal is not configured.';
        return;
    }

    container.textContent = '';

    createRoot(container).render(
        <StrictMode>
            <ManagedSiteAdminShell api={new ManagedSitesApi(bootstrap)} />
        </StrictMode>,
    );
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mount, { once: true });
} else {
    mount();
}
