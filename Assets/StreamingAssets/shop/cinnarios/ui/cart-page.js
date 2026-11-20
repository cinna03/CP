// The Cart page: each print with a quantity stepper, a choice of delivery, and the order
// summary. Changing a quantity or removing a print is sent through `actions`, then the
// page redraws from the cart the server returns.
(function () {
  const MAX_QUANTITY = 10;

  class CartPage {
    /**
     * @param {{items: object[], totalAmount: string, currency: string}} cart
     * @param {{setQuantity: Function, remove: Function}} actions each returns the updated cart
     * @param {{esc: Function, money: Function}} format
     */
    constructor(cart, actions, format) {
      this.cart = cart;
      this.actions = actions;
      this.esc = format.esc;
      this.money = format.money;
      this.Delivery = window.Cinnarios.Delivery;
    }

    html() {
      return `<section class="cart-page"><h1>Cart</h1><div id="cart-body">${this.bodyHtml()}</div><p id="cart-error" class="error" role="alert"></p></section>`;
    }

    bodyHtml() {
      if (!this.cart.items.length) {
        return `<div class="empty-state"><h2>Your cart is empty</h2><p>Find a painting, try it on your wall, then add it here.</p><a class="button" href="#gallery">Go to Discover</a></div>`;
      }
      return `<div class="cart-layout"><div class="cart-lines">${this.cart.items.map((item) => this.lineHtml(item)).join('')}</div>${this.summaryHtml()}</div>`;
    }

    lineHtml(item) {
      const esc = this.esc;
      return `<div class="cart-line">
        <a class="cart-thumb" href="#work/${esc(item.sourceId)}"><img src="${esc(item.thumbnailUrl)}" alt="" /></a>
        <div class="cart-line-info">
          <strong>${esc(item.title)}</strong>
          <span>${esc(item.artist || 'Unknown artist')} · ${esc(item.widthCm)} × ${esc(item.heightCm)} cm</span>
          <div class="stepper" role="group" aria-label="Quantity of ${esc(item.title)}">
            <button type="button" data-item="${esc(item.id)}" data-to="${item.quantity - 1}" aria-label="One fewer" ${item.quantity <= 1 ? 'disabled' : ''}>−</button>
            <span aria-live="polite">${esc(item.quantity)}</span>
            <button type="button" data-item="${esc(item.id)}" data-to="${item.quantity + 1}" aria-label="One more" ${item.quantity >= MAX_QUANTITY ? 'disabled' : ''}>+</button>
          </div>
          <button type="button" class="text-button" data-remove="${esc(item.id)}">Remove</button>
        </div>
        <span class="cart-line-price">${esc(this.money(item.lineAmount, item.currency || this.cart.currency))}</span>
      </div>`;
    }

    summaryHtml() {
      const esc = this.esc;
      const chosen = this.Delivery.chosen();
      const total = BigInt(this.cart.totalAmount) + BigInt(chosen.cents);
      return `<aside class="cart-summary">
        <h2>Order summary</h2>
        <fieldset class="delivery-choice"><legend>Delivery</legend>
          ${this.Delivery.tiers().map((tier) => `<label class="delivery-option"><input type="radio" name="tier" value="${tier.id}" ${tier.id === chosen.id ? 'checked' : ''} /><span>${esc(tier.label)}</span><span>${esc(this.money(tier.cents, tier.currency))}</span></label>`).join('')}
        </fieldset>
        <div class="sum-row"><span>Subtotal</span><span>${esc(this.money(this.cart.totalAmount, this.cart.currency))}</span></div>
        <div class="sum-row"><span>Delivery</span><span>${esc(this.money(chosen.cents, chosen.currency))}</span></div>
        <div class="sum-row sum-total"><span>Total</span><span>${esc(this.money(total.toString(), this.cart.currency))}</span></div>
        <a class="button cart-checkout" href="#checkout">Check out</a>
        <p class="piece-note">A rider brings it to your district, sector and landmark. Each print is made at the painting's real size, as you saw it on your wall.</p>
      </aside>`;
    }

    paint() {
      document.querySelector('#cart-body').innerHTML = this.bodyHtml();
    }

    async change(action) {
      const error = document.querySelector('#cart-error');
      error.textContent = '';
      try {
        this.cart = await action();
        this.paint();
      } catch (err) {
        error.textContent = err.message;
      }
    }

    bind() {
      const body = document.querySelector('#cart-body');
      body.addEventListener('click', (event) => {
        const step = event.target.closest('[data-item]');
        if (step) this.change(() => this.actions.setQuantity(step.dataset.item, Number(step.dataset.to)));
        const remove = event.target.closest('[data-remove]');
        if (remove) this.change(() => this.actions.remove(remove.dataset.remove));
      });
      body.addEventListener('change', (event) => {
        if (event.target.name !== 'tier') return;
        this.Delivery.choose(event.target.value);
        this.paint();
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.CartPage = CartPage;
}());
