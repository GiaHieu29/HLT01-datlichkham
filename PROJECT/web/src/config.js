export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5080'

export const oidcConfig = {
    authority: import.meta.env.VITE_OIDC_AUTHORITY || 'http://localhost:8080/realms/hlt01',
    client_id: import.meta.env.VITE_OIDC_CLIENT_ID || 'hlt-web',
    redirect_uri: window.location.origin + '/',
    post_logout_redirect_uri: window.location.origin + '/',
    scope: 'openid profile email',
    automaticSilentRenew: true,

    onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
    }