// The 500 paintings change only when the shop updates its catalogue, yet almost every page
// needs them. This keeps one copy per visit: the first page that asks downloads it, every
// later page (and every request made at the same time) shares that same download. A copy
// in session storage lets a reload skip the download for ten minutes.
(function () {
  const STORE_KEY = 'cinnarios.catalog';
  const FRESH_MS = 10 * 60 * 1000;

  class CatalogCache {
    /** @param {() => Promise<object[]>} download fetches the full list from the API */
    constructor(download) {
      this.download = download;
      this.pending = null;
      this.list = CatalogCache.stored();
    }

    static stored() {
      try {
        const saved = JSON.parse(sessionStorage.getItem(STORE_KEY) || 'null');
        return saved && Date.now() - saved.at < FRESH_MS && Array.isArray(saved.list) ? saved.list : null;
      } catch {
        return null;
      }
    }

    static store(list) {
      try { sessionStorage.setItem(STORE_KEY, JSON.stringify({ at: Date.now(), list })); } catch { /* full or private: memory copy still works */ }
    }

    /** The whole catalogue, downloaded at most once at a time. */
    all() {
      if (this.list) return Promise.resolve(this.list);
      if (!this.pending) {
        this.pending = this.download()
          .then((list) => {
            this.list = list;
            CatalogCache.store(list);
            return list;
          })
          .finally(() => { this.pending = null; });
      }
      return this.pending;
    }

    /** One painting from the shared copy, or null if it is not in it. */
    async find(sourceId) {
      const list = await this.all();
      return list.find((work) => String(work.sourceId) === String(sourceId)) || null;
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.CatalogCache = CatalogCache;
}());
