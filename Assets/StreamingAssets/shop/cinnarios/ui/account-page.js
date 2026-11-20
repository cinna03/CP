// The Account page: who is signed in, the paintings tried on a wall (with how each try
// ended), the saved paintings, orders, and the delivery address. Editing the name, removing
// an address and signing out go through `actions`.
(function () {
  const VERDICTS = Object.freeze({ satisfied: 'Liked it', declined: 'Not for this wall' });

  function wallPicture(image) {
    return /^https:\/\//.test(image) ? image : `${location.origin}/wall-tries/${image}`;
  }
  const SAVED_SHOWN = 4;

  /** "Today", "This week", or a short date: how long ago a try happened. */
  function whenLabel(iso, now = new Date()) {
    const at = new Date(iso);
    if (Number.isNaN(at.getTime())) return '';
    const days = Math.floor((now - at) / 86400000);
    if (days < 1 && at.getDate() === now.getDate()) return 'Today';
    if (days < 7) return 'This week';
    return at.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
  }

  class AccountPage {
    /**
     * @param {object} data  { email, displayName, orders, catalog, addresses, tries, savedIds }
     * @param {{rename: Function, removeAddress: Function, signOut: Function}} actions
     * @param {{esc: Function, money: Function}} format
     */
    constructor(data, actions, format) {
      this.data = data;
      this.byId = new Map(data.catalog.map((work) => [String(work.sourceId), work]));
      this.actions = actions;
      this.esc = format.esc;
      this.money = format.money;
    }

    html() {
      const esc = this.esc;
      const name = this.data.displayName || this.data.email || 'Account';
      const initial = name.trim().charAt(0).toUpperCase() || 'C';
      return `
      <section class="account-page">
        <div class="page-head">
          <div class="account-id"><span class="account-mark" id="account-mark">${esc(initial)}</span><div><h1 id="account-name">${esc(this.data.displayName || 'Account')}</h1><p>Signed in as ${esc(this.data.email)}</p></div></div>
          <div class="piece-actions account-actions">
            <button type="button" class="quiet" id="edit-profile">Edit profile</button>
            <button type="button" class="quiet" id="sign-out">Sign out</button>
          </div>
        </div>
        <form id="profile-form" class="profile-form" hidden>
          <label for="displayName">Your name</label>
          <div class="profile-row"><input id="displayName" name="displayName" required minlength="2" autocomplete="name" value="${esc(this.data.displayName || '')}" /><button type="submit">Save</button></div>
          <p id="profile-result" class="error" role="alert"></p>
        </form>
        ${this.triesHtml()}
        ${this.savedHtml()}
        <div class="account-columns">${this.ordersHtml()}${this.addressHtml()}</div>
        <section class="account-block">
          <h2>Your data</h2>
          <p class="muted">The account, addresses, saved paintings and orders stay on the server after you sign out. <a href="#privacy">Privacy</a> says what is kept. Delete account removes them.</p>
          <button type="button" class="quiet" id="delete-account">Delete account</button>
          <p id="delete-result" class="error" role="alert"></p>
        </section>
      </section>`;
    }

    section(title, body, link = '') {
      return `<section class="account-block"><div class="block-head"><h2>${title}</h2>${link}</div>${body}</section>`;
    }

    triesHtml() {
      const esc = this.esc;
      const tries = this.data.tries.slice().reverse().map((item) => ({ ...item, work: this.byId.get(String(item.sourceId)) })).filter((item) => item.work);
      const body = tries.length
        ? `<div class="tries-row">${tries.map(({ work, at, outcome, image }) => `
            <a class="try-card" href="#work/${esc(work.sourceId)}/account">
              <span class="try-picture">${image ? `<img src="${esc(wallPicture(image))}" alt="${esc(work.title)} on your wall" />` : `<img src="${esc(work.thumbnailUrl || work.imageUrl)}" alt="" />`}</span>
              <span class="try-when">${esc(whenLabel(at))}</span>
              <strong>${esc(work.title)}</strong>
              <span class="tag ${outcome === 'satisfied' ? 'tag-blush' : ''}">${esc(VERDICTS[outcome] || 'Not sure yet')}</span>
              <span class="try-meta">${esc(work.widthCm)} × ${esc(work.heightCm)} cm · ${esc(this.money(work.priceAmount, work.currency))}</span>
            </a>`).join('')}</div>`
        : `<div class="empty-state empty-inline"><p>Every painting you hang is kept here with a picture of your wall.</p><a class="button" href="#gallery">Find a painting</a></div>`;
      return this.section('Your tries on the wall', body);
    }

    savedHtml() {
      const esc = this.esc;
      const saved = this.data.savedIds.slice().reverse().map((id) => this.byId.get(String(id))).filter(Boolean);
      const body = saved.length
        ? `<div class="tile-grid account-saved">${saved.slice(0, SAVED_SHOWN).map((work) => `<a class="tile" href="#work/${esc(work.sourceId)}/saved"><img src="${esc(work.thumbnailUrl || work.imageUrl)}" alt="" loading="lazy" /><strong>${esc(work.title)}</strong><span>${esc(work.artist || 'Unknown artist')}</span><span class="tile-price">${esc(this.money(work.priceAmount, work.currency))}</span></a>`).join('')}</div>`
        : '<p class="muted">Tap the heart on any painting to keep it here.</p>';
      return this.section('Saved paintings', body, saved.length > SAVED_SHOWN ? '<a href="#saved">See all</a>' : '');
    }

    ordersHtml() {
      const esc = this.esc;
      const orders = this.data.orders;
      const body = orders.length
        ? `<div class="order-list">${orders.map((order) => `<a class="order-row" href="#receipt/${esc(order.number)}"><span><strong>${esc(order.number)}</strong>${esc(order.items.map((item) => item.title).join(', '))}</span><span>Placed · ${esc(this.money(order.totalAmount, order.currency))}</span></a>`).join('')}</div>`
        : '<p class="muted">No orders yet. When you buy a painting, its delivery shows up here.</p>';
      return this.section('Orders', body);
    }

    addressHtml() {
      const esc = this.esc;
      const address = this.data.addresses[this.data.addresses.length - 1];
      const body = address
        ? `<div class="address-card"><strong>${esc(address.recipientName)}</strong><span>${esc([address.line1, address.line2].filter(Boolean).join(', '))}</span><span>${esc([address.city, address.region].filter(Boolean).join(', '))}</span><button type="button" class="text-button" data-remove-address="${esc(address.id)}">Remove</button></div>`
        : '<p class="muted">Add where your painting should go: province, district and street. Checkout asks for it and keeps it here.</p>';
      return this.section('Delivery address', body);
    }

    bind() {
      const form = document.querySelector('#profile-form');
      document.querySelector('#edit-profile').addEventListener('click', () => {
        form.hidden = !form.hidden;
        if (!form.hidden) form.querySelector('input').focus();
      });
      form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const name = new FormData(form).get('displayName').trim();
        try {
          await this.actions.rename(name);
          document.querySelector('#account-name').textContent = name;
          document.querySelector('#account-mark').textContent = name.charAt(0).toUpperCase();
          form.hidden = true;
        } catch (err) {
          document.querySelector('#profile-result').textContent = err.message;
        }
      });
      document.querySelector('#sign-out').addEventListener('click', () => this.actions.signOut());
      document.querySelector('#delete-account').addEventListener('click', async () => {
        const result = document.querySelector('#delete-result');
        if (!window.confirm('Delete this account? The cart, orders, saved paintings and wall tries are removed from the server. Signing out does not do this.')) return;
        try {
          await this.actions.deleteAccount();
          result.textContent = '';
        } catch (err) {
          result.textContent = err.message;
        }
      });
      document.querySelector('[data-remove-address]')?.addEventListener('click', async (event) => {
        await this.actions.removeAddress(event.currentTarget.dataset.removeAddress);
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.AccountPage = AccountPage;
}());
