// Keeps the buyer signed in. The short-lived access token lives in this tab's session
// storage; the long-lived refresh cookie lives with the browser. Two rules:
//   1. Renew once. When several requests find the token expired at the same moment, they
//      all wait for one renewal. (The API ends every session if one refresh token is used
//      twice, so parallel renewals used to sign buyers out.)
//   2. Restore on start. A new tab or a reload has no access token yet, so the app asks
//      for one with the cookie before drawing the first page.
(function () {
  const TOKEN_KEY = 'marketplace.access';

  class Session {
    /** @param {string} apiBase e.g. "/v1" or "http://192.168.1.5:3001/v1" */
    constructor(apiBase) {
      this.apiBase = apiBase;
      this.renewing = null;
    }

    token() {
      try { return sessionStorage.getItem(TOKEN_KEY) || ''; } catch { return ''; }
    }

    keep(token) {
      try { sessionStorage.setItem(TOKEN_KEY, token); } catch { /* private mode: lasts this page only */ }
    }

    forget() {
      try { sessionStorage.removeItem(TOKEN_KEY); } catch { /* nothing kept */ }
    }

    signedIn() {
      return Boolean(this.token());
    }

    /** One renewal at a time; everyone who asks while it runs shares its answer. Resolves true when renewed. */
    renew() {
      if (!this.renewing) {
        this.renewing = fetch(`${this.apiBase}/auth/refresh`, {
          method: 'POST',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: '{}',
        })
          .then(async (response) => {
            if (!response.ok) {
              if (response.status === 401) this.forget();
              return false;
            }
            const data = await response.json();
            this.keep(data.accessToken);
            return true;
          })
          .catch(() => false)
          .finally(() => { this.renewing = null; });
      }
      return this.renewing;
    }

    /** On start: if this tab has no token, try the cookie once. Never throws. */
    async restore() {
      if (this.signedIn()) return true;
      return this.renew();
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.Session = Session;
}());
