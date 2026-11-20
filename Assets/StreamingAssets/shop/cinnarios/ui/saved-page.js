// The Saved page: the paintings a buyer kept, newest first, with sorting and a heart to
// remove one. A removal can be undone for five seconds from the bar at the bottom.
(function () {
  const UNDO_MS = 5000;
  const SORTS = Object.freeze({
    recent: { label: 'Recently saved', order: (list) => list },
    price: { label: 'Price, low to high', order: (list) => list.slice().sort((a, b) => Number(a.priceAmount) - Number(b.priceAmount)) },
    size: { label: 'Largest first', order: (list) => list.slice().sort((a, b) => b.widthCm * b.heightCm - a.widthCm * a.heightCm) },
  });

  class SavedPage {
    /**
     * @param {object[]} catalog
     * @param {{savedIds: Function, toggleSaved: Function, insertSaved: Function}} store the buyer's saved list
     * @param {{esc: Function, money: Function}} format
     */
    constructor(catalog, store, format) {
      this.byId = new Map(catalog.map((work) => [String(work.sourceId), work]));
      this.store = store;
      this.esc = format.esc;
      this.money = format.money;
      this.sort = 'recent';
      this.undo = null;
    }

    /** Newest first, skipping ids no longer in the catalogue. */
    works() {
      return this.store.savedIds().slice().reverse().map((id) => this.byId.get(String(id))).filter(Boolean);
    }

    html() {
      return `<section class="saved-page"><div id="saved-body">${this.bodyHtml()}</div><div id="undo-bar" class="undo-bar" role="status" hidden></div></section>`;
    }

    bodyHtml() {
      const esc = this.esc;
      const list = SORTS[this.sort].order(this.works());
      const head = `<div class="page-head"><div><h1>Saved</h1><p>${list.length} ${list.length === 1 ? 'piece' : 'pieces'} you want to come back to.</p></div>
        ${list.length ? `<div class="chips">${Object.entries(SORTS).map(([key, sort]) => `<button type="button" class="chip" data-sort="${key}" aria-pressed="${key === this.sort}">${esc(sort.label)}</button>`).join('')}</div>` : ''}</div>`;
      if (!list.length) {
        return `${head}<div class="empty-state"><h2>Nothing saved yet</h2><p>Press the heart on any painting and it will wait for you here.</p><a class="button" href="#gallery">Go to Discover</a></div>`;
      }
      return `${head}<div class="saved-grid">${list.map((work) => `
        <div class="saved-item">
          <a class="saved-link" href="#work/${esc(work.sourceId)}/saved">
            <span class="saved-frame"><img src="${esc(work.thumbnailUrl || work.imageUrl)}" alt="" loading="lazy" /></span>
            <strong>${esc(work.title)}</strong>
            <span class="saved-artist">${esc(work.artist || 'Unknown artist')}</span>
            <span class="saved-meta"><span>${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span><span class="saved-price">${esc(this.money(work.priceAmount, work.currency))}</span></span>
          </a>
          <button type="button" class="saved-heart" data-unsave="${esc(work.sourceId)}" aria-label="Remove ${esc(work.title)} from Saved">${window.Cinnarios.icons.heart(true)}</button>
        </div>`).join('')}</div>`;
    }

    paint() {
      document.querySelector('#saved-body').innerHTML = this.bodyHtml();
    }

    remove(id) {
      const index = this.store.savedIds().map(String).indexOf(String(id));
      this.store.toggleSaved(id);
      this.paint();
      this.showUndo(id, index);
    }

    showUndo(id, index) {
      const bar = document.querySelector('#undo-bar');
      const work = this.byId.get(String(id));
      clearTimeout(this.undo);
      bar.innerHTML = `<span>${this.esc(work ? work.title : 'Painting')} removed</span><button type="button" id="undo">Undo</button>`;
      bar.hidden = false;
      bar.querySelector('#undo').addEventListener('click', () => {
        this.store.insertSaved(id, index);
        bar.hidden = true;
        this.paint();
      });
      this.undo = setTimeout(() => { bar.hidden = true; }, UNDO_MS);
    }

    bind() {
      document.querySelector('#saved-body').addEventListener('click', (event) => {
        const unsave = event.target.closest('[data-unsave]');
        if (unsave) {
          this.remove(unsave.dataset.unsave);
          return;
        }
        const sort = event.target.closest('[data-sort]');
        if (sort) {
          this.sort = sort.dataset.sort;
          this.paint();
        }
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.SavedPage = SavedPage;
}());
