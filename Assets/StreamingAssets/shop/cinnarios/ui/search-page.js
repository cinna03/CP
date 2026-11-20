// The Search page: one big search box, a greeting with suggestions, then either the results
// as you type or the whole catalogue, a page at a time. It keeps the last query for this tab
// so Back from a painting returns to the same results.
(function () {
  const QUERY_KEY = 'cinnarios.search';
  const PAGE_SIZE = 24;
  const SUGGESTIONS = Object.freeze(['flowers', 'warm', 'large landscape', 'van Gogh', 'portrait', 'small']);

  class SearchPage {
    /**
     * @param {object[]} catalog
     * @param {CatalogSearch} engine
     * @param {{esc: Function, money: Function}} format
     */
    constructor(catalog, engine, format) {
      this.catalog = catalog;
      this.engine = engine;
      this.esc = format.esc;
      this.money = format.money;
      this.shown = PAGE_SIZE;
      this.query = SearchPage.lastQuery();
    }

    static lastQuery() {
      try { return sessionStorage.getItem(QUERY_KEY) || ''; } catch { return ''; }
    }

    static remember(query) {
      try { sessionStorage.setItem(QUERY_KEY, query); } catch { /* private mode: nothing to keep */ }
    }

    static greeting(now = new Date()) {
      const hour = now.getHours();
      if (hour < 12) return 'Good morning';
      return hour < 18 ? 'Good afternoon' : 'Good evening';
    }

    html() {
      const esc = this.esc;
      const icons = window.Cinnarios.icons;
      return `
      <section class="search-page">
        <h1 class="search-title"><svg width="40" height="40" viewBox="0 0 26 26" fill="none" aria-hidden="true"><rect x="1" y="5" width="15" height="19" rx="3" stroke="currentColor" stroke-width="2"/><rect x="10" y="2" width="15" height="19" rx="3" fill="currentColor"/></svg>Search</h1>
        <div class="search-box">
          <label for="find" class="sr-only">Search the collection</label>
          <span class="search-icon">${icons.search()}</span>
          <input id="find" type="search" autocomplete="off" placeholder="Search by title, artist, mood or size" value="${esc(this.query)}" />
          <button type="button" id="clear-find" class="search-clear" aria-label="Clear search" ${this.query.trim() ? '' : 'hidden'}>${icons.close()}</button>
        </div>
        <hr class="search-rule" />
        <div id="search-body">${this.bodyHtml()}</div>
      </section>`;
    }

    bodyHtml() {
      const esc = this.esc;
      const query = this.query.trim();
      if (!query) {
        return `
          <div class="search-hello">
            <h2>${SearchPage.greeting()}</h2>
            <p>Not sure what to type? Try one of these.</p>
            <div class="chips">${SUGGESTIONS.map((word) => `<button type="button" class="chip" data-suggest="${esc(word)}">${esc(word)}</button>`).join('')}</div>
          </div>
          <div class="search-all-head"><h2>All ${this.catalog.length} pieces</h2></div>
          <div class="tile-grid">${this.catalog.slice(0, this.shown).map((work) => this.tile(work)).join('')}</div>
          ${this.shown < this.catalog.length ? '<p class="load-more"><button type="button" class="quiet" id="load-more">Load more</button></p>' : ''}`;
      }
      const found = this.engine.search(query, this.catalog);
      if (!found.length) {
        return `<div class="search-empty"><h2>Nothing matches that yet</h2><p>Try an artist, a subject like flowers, a mood like warm, or a size like large.</p></div>`;
      }
      return `
          <p class="search-count">${found.length} ${found.length === 1 ? 'piece' : 'pieces'} for “${esc(query)}”</p>
          <div class="tile-grid">${found.map((work) => this.tile(work)).join('')}</div>`;
    }

    tile(work) {
      const esc = this.esc;
      return `<a class="tile" href="#work/${esc(work.sourceId)}/search">
        <img class="pending" src="${esc(work.thumbnailUrl || work.imageUrl)}" alt="" loading="lazy" decoding="async" onload="this.classList.remove('pending')" />
        <strong>${esc(work.title)}</strong>
        <span>${esc(work.artist || 'Unknown artist')} · ${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span>
        <span class="tile-price">${esc(this.money(work.priceAmount, work.currency))}</span>
      </a>`;
    }

    setQuery(query) {
      this.query = query;
      this.shown = PAGE_SIZE;
      SearchPage.remember(query);
      document.querySelector('#clear-find').hidden = !query.trim();
      this.paint();
    }

    paint() {
      document.querySelector('#search-body').innerHTML = this.bodyHtml();
    }

    /** Back from a painting lands where the buyer was, with enough of the catalogue drawn to reach it. */
    restoreScroll() {
      let y = 0;
      try { y = Number(sessionStorage.getItem('marketplace.scroll:#search')) || 0; } catch { y = 0; }
      if (!y) return;
      while (!this.query.trim() && this.shown < this.catalog.length && document.documentElement.scrollHeight < y + window.innerHeight) {
        this.shown += PAGE_SIZE;
        this.paint();
      }
      requestAnimationFrame(() => window.scrollTo(0, y));
    }

    bind() {
      this.restoreScroll();
      const input = document.querySelector('#find');
      input.addEventListener('input', () => this.setQuery(input.value));
      document.querySelector('#clear-find').addEventListener('click', () => {
        input.value = '';
        this.setQuery('');
        input.focus();
      });
      document.querySelector('#search-body').addEventListener('click', (event) => {
        const chip = event.target.closest('[data-suggest]');
        if (chip) {
          input.value = chip.dataset.suggest;
          this.setQuery(chip.dataset.suggest);
          return;
        }
        if (event.target.closest('#load-more')) {
          this.shown += PAGE_SIZE;
          this.paint();
        }
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.SearchPage = SearchPage;
}());
