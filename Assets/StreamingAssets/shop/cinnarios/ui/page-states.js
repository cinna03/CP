// What a page shows while it loads and when it cannot: grey placeholders shaped like a page
// of paintings, and plain-word errors that say what happened, that saved paintings and the
// cart are safe, and offer Try again.
(function () {
  class PageStates {
    static loading() {
      const tiles = Array.from({ length: 8 }, () => '<span class="skeleton-tile"><span class="skeleton skeleton-img"></span><span class="skeleton skeleton-line"></span><span class="skeleton skeleton-line short"></span></span>').join('');
      return `<section class="page-loading" aria-busy="true" aria-label="Loading"><span class="skeleton skeleton-title"></span><span class="skeleton skeleton-line wide"></span><div class="skeleton-grid">${tiles}</div></section>`;
    }

    /** No connection at all, as opposed to the server answering with a problem. */
    static isOffline(error) {
      return !navigator.onLine || (error instanceof TypeError && /fetch|network|load failed/i.test(error.message));
    }

    /**
     * @param {Error & {code?: string}} error
     * @param {(text: string) => string} esc
     */
    static error(error, esc) {
      const icons = window.Cinnarios.icons;
      let title;
      let text;
      if (PageStates.isOffline(error)) {
        title = navigator.onLine ? "Couldn't reach Cinnarios" : "You're offline";
        text = navigator.onLine
          ? "The shop isn't answering right now. Your saved paintings and cart are safe."
          : "Check your Wi-Fi or mobile data. We'll pick up where you left off.";
      } else if (error && error.code) {
        title = "Couldn't load this page";
        text = error.message;
      } else {
        title = "Couldn't load the paintings";
        text = 'Something went wrong on our side. Your saved paintings and cart are safe.';
      }
      return `<section class="page-error" role="alert">
        <div class="auth-badge">${icons.close()}</div>
        <h1>${esc(title)}</h1>
        <p>${esc(text)}</p>
        <button type="button" data-retry>Try again</button>
      </section>`;
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.PageStates = PageStates;
}());
