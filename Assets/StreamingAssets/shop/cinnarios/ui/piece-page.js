// The painting page: the piece, its facts, the camera button, and five paintings similar in feel.
// It draws markup and wires its own buttons. Fetching, saving and the cart stay with the caller,
// which passes them in as actions, so this file never talks to the API or to browser storage.
(function () {
  const BACK_LABELS = Object.freeze({ search: 'Back to Search', discover: 'Back to Discover', saved: 'Back to Saved', home: 'Back to Home' });

  class PiecePage {
    /**
     * @param {object} view
     * @param {object} view.work         the catalogue record
     * @param {{subject: string, mood: string}} view.feel  the shop's filing for this piece
     * @param {Array<{work: object, reasons: string[]}>} view.similar
     * @param {string} view.from         where the buyer came from (search, discover, saved, home)
     * @param {string} view.backHref
     * @param {boolean} view.saved
     * @param {boolean} view.canAdd      signed in and not already reserved
     * @param {string} view.extra        markup the shop adds under the actions (reservation note)
     * @param {{esc: Function, money: Function}} format
     */
    constructor(view, format) {
      this.view = view;
      this.esc = format.esc;
      this.money = format.money;
      this.icons = window.Cinnarios.icons;
    }

    html() {
      const { work, from, backHref } = this.view;
      const esc = this.esc;
      return `
      <article class="piece-page">
        <a class="pill-back" href="${esc(backHref)}">${this.icons.back()}${esc(BACK_LABELS[from] || 'Back')}</a>
        <div class="piece-main">
          <div class="piece-art"><img id="piece-picture" src="${esc(work.thumbnailUrl || work.imageUrl)}" data-full="${esc(work.imageUrl)}" alt="${esc(work.title)}" decoding="async" style="view-transition-name: work-${esc(work.sourceId)}" /></div>
          ${this.infoHtml()}
        </div>
        ${this.similarHtml()}
      </article>`;
    }

    infoHtml() {
      const { work, feel, saved, canAdd, extra } = this.view;
      const esc = this.esc;
      const byline = [work.artist || 'Unknown artist', (work.date || '').trim()].filter(Boolean).join(' · ');
      const subject = feel.subject && feel.subject !== 'picture' ? `<span class="tag">${esc(feel.subject)}</span>` : '';
      return `
          <div class="piece-info">
            <p class="piece-byline">${esc(byline)}</p>
            <h1>${esc(work.title)}</h1>
            ${work.mediumDescription ? `<p class="piece-medium">${esc(work.mediumDescription)}</p>` : ''}
            <div class="tags">
              <span class="tag tag-blush">${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span>
              ${subject}
              <span class="tag">${esc(feel.mood)} mood</span>
            </div>
            <p class="price">${esc(this.money(work.priceAmount, work.currency))}</p>
            <p class="piece-note">Reproduction at this size. Shipping not included.</p>
            <div class="piece-actions">
              <button type="button" class="button" id="open-space">${this.icons.camera()}Open in camera</button>
              <button type="button" class="quiet" id="add" ${canAdd ? '' : 'disabled'}>Add to cart</button>
              <button type="button" class="icon-button" id="piece-save" aria-pressed="${saved}" aria-label="${saved ? 'Saved' : 'Save'}">${this.icons.heart(saved)}</button>
            </div>
            <p class="piece-note">Opens your phone's camera with this painting at its real size.</p>
            ${extra || ''}
            <p id="work-error" class="error" role="alert"></p>
            ${work.creditLine ? `<p class="piece-credit">${esc(work.creditLine)}</p>` : ''}
          </div>`;
    }

    similarHtml() {
      const { similar, from } = this.view;
      if (!similar.length) return '';
      const esc = this.esc;
      const cards = similar.map(({ work, reasons }) => `
          <a class="similar-card" href="#work/${esc(work.sourceId)}/${esc(from || '')}">
            <img src="${esc(work.thumbnailUrl || work.imageUrl)}" alt="" loading="lazy" />
            <span class="tags">${reasons.map((reason) => `<span class="tag tag-small">${esc(reason)}</span>`).join('')}</span>
            <strong>${esc(work.title)}</strong>
            <span class="similar-artist">${esc(work.artist || 'Unknown artist')}</span>
            <span class="similar-price">${esc(this.money(work.priceAmount, work.currency))}</span>
          </a>`).join('');
      return `
        <section class="similar" aria-labelledby="similar-title">
          <h2 id="similar-title">Similar in feel</h2>
          <p>Matched on subject, mood and colour first. The tags say why.</p>
          <div class="similar-row">${cards}</div>
        </section>`;
    }

    /** @param {{openCamera: Function, addToCart: Function, toggleSaved: Function}} actions each may return a promise */
    bind(actions) {
      const picture = document.querySelector('#piece-picture');
      if (picture && picture.dataset.full && picture.dataset.full !== picture.src) {
        const full = new Image();
        full.decoding = 'async';
        full.onload = () => { picture.src = picture.dataset.full; };
        full.src = picture.dataset.full;
      }
      const error = document.querySelector('#work-error');
      const run = async (action) => {
        error.textContent = '';
        try {
          await action();
        } catch (err) {
          error.textContent = err.message;
        }
      };
      document.querySelector('#open-space').addEventListener('click', () => run(actions.openCamera));
      document.querySelector('#add').addEventListener('click', async (event) => {
        const button = event.currentTarget;
        button.disabled = true;
        button.textContent = 'Adding…';
        await run(actions.addToCart);
        button.disabled = !this.view.canAdd;
        button.textContent = 'Add to cart';
      });
      document.querySelector('#piece-save').addEventListener('click', (event) => {
        const button = event.currentTarget;
        const saved = actions.toggleSaved();
        button.setAttribute('aria-pressed', String(saved));
        button.setAttribute('aria-label', saved ? 'Saved' : 'Save');
        button.innerHTML = this.icons.heart(saved);
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.PiecePage = PiecePage;
}());
