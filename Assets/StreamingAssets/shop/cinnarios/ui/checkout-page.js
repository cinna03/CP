// Checking out: where the rider should bring the order, the order summary, then
// "Order placed". The delivery price follows the province. Saving the address and placing
// the order go through `actions`, so this file never calls the API itself.
(function () {
  const PHONE_PATTERN = '(\\+?250|0)7[0-9]{8}';

  class CheckoutPage {
    /**
     * @param {{items: object[], totalAmount: string, currency: string}} cart
     * @param {object|null} lastAddress   the buyer's most recent saved address, to prefill
     * @param {Object<string, string[]>} provinces  province name → districts
     * @param {{placeOrder: (address: object, tierId: string) => Promise<void>}} actions
     * @param {{esc: Function, money: Function}} format
     */
    constructor(cart, lastAddress, provinces, actions, format) {
      this.cart = cart;
      this.address = lastAddress || {};
      this.provinces = provinces;
      this.actions = actions;
      this.esc = format.esc;
      this.money = format.money;
      this.Delivery = window.Cinnarios.Delivery;
    }

    field(id, label, input, note = '') {
      return `<div class="field"><label for="${id}">${label}</label>${input}${note ? `<p class="piece-note">${note}</p>` : ''}</div>`;
    }

    options(names, selected, placeholder) {
      const esc = this.esc;
      return `<option value="">${placeholder}</option>${names.map((name) => `<option value="${esc(name)}" ${name === selected ? 'selected' : ''}>${esc(name)}</option>`).join('')}`;
    }

    html() {
      const esc = this.esc;
      const a = this.address;
      const value = (key) => esc(a[key] || '');
      return `
      <section class="checkout-page">
        <a class="pill-back" href="#cart">${window.Cinnarios.icons.back()}Back to Cart</a>
        <div class="checkout-layout">
          <form id="address-form" class="checkout-form" novalidate>
            <h1>Where should the rider bring it?</h1>
            ${this.field('recipientName', 'Name', `<input id="recipientName" name="recipientName" autocomplete="name" required value="${value('recipientName')}" />`)}
            ${this.field('phone', 'Phone', `<input id="phone" name="phone" type="tel" inputmode="tel" autocomplete="tel" required pattern="${PHONE_PATTERN}" placeholder="0788 123 456" value="${value('phone')}" />`, 'A mobile number the rider can call.')}
            <div class="field-pair">
              ${this.field('region', 'Province', `<select id="region" name="region" required>${this.options(Object.keys(this.provinces), a.region, 'Choose')}</select>`)}
              ${this.field('city', 'District', `<select id="city" name="city" required>${this.options(this.provinces[a.region] || [], a.city, 'Choose')}</select>`)}
            </div>
            ${this.field('line2', 'Sector', `<input id="line2" name="line2" required value="${value('line2')}" />`)}
            ${this.field('line1', 'Street or landmark', `<input id="line1" name="line1" required value="${value('line1')}" />`)}
            <p id="checkout-error" class="error" role="alert"></p>
          </form>
          <aside class="cart-summary" id="checkout-summary">${this.summaryHtml(a.region)}</aside>
        </div>
      </section>`;
    }

    summaryHtml(province) {
      const esc = this.esc;
      const tier = this.Delivery.forProvince(province);
      const total = BigInt(this.cart.totalAmount) + BigInt(tier.cents);
      return `
        <h2>Your order</h2>
        ${this.cart.items.map((item) => `<div class="summary-line">${item.thumbnailUrl ? `<img src="${esc(item.thumbnailUrl)}" alt="" />` : ''}<span><strong>${esc(item.title)}</strong>${esc(item.widthCm)} × ${esc(item.heightCm)} cm${item.quantity > 1 ? ` · ${esc(item.quantity)} prints` : ''}</span></div>`).join('')}
        <div class="sum-row"><span>Subtotal</span><span>${esc(this.money(this.cart.totalAmount, this.cart.currency))}</span></div>
        <div class="sum-row"><span>Delivery · ${esc(tier.label)}</span><span>${esc(this.money(tier.cents, tier.currency))}</span></div>
        <div class="sum-row sum-total"><span>Total</span><span>${esc(this.money(total.toString(), this.cart.currency))}</span></div>
        <button type="submit" form="address-form" class="cart-checkout" id="place-order">Place order</button>`;
    }

    /** The first field that is empty or malformed, with a sentence saying what to fix. */
    problem(form) {
      const labels = { recipientName: 'your name', phone: 'a mobile number like 0788123456', region: 'a province', city: 'a district', line2: 'the sector', line1: 'a street or landmark' };
      const bad = [...form.elements].find((element) => element.name && !element.checkValidity());
      return bad ? { element: bad, message: `Add ${labels[bad.name] || bad.name}.` } : null;
    }

    bind() {
      const form = document.querySelector('#address-form');
      const province = form.querySelector('#region');
      const district = form.querySelector('#city');
      const error = document.querySelector('#checkout-error');
      province.addEventListener('change', () => {
        district.innerHTML = this.options(this.provinces[province.value] || [], '', 'Choose');
        document.querySelector('#checkout-summary').innerHTML = this.summaryHtml(province.value);
      });
      form.addEventListener('submit', async (event) => {
        event.preventDefault();
        error.textContent = '';
        const problem = this.problem(form);
        if (problem) {
          error.textContent = problem.message;
          problem.element.focus();
          form.classList.remove('shake');
          void form.offsetWidth;
          form.classList.add('shake');
          return;
        }
        const button = document.querySelector('#place-order');
        button.disabled = true;
        const address = { ...Object.fromEntries(new FormData(form)), countryCode: 'RW', postalCode: 'N/A' };
        try {
          await this.actions.placeOrder(address, this.Delivery.forProvince(address.region).id);
        } catch (err) {
          error.textContent = err.message;
          button.disabled = false;
        }
      });
    }
  }

  /** "Order placed": the number, the total with delivery, where it is going, and what next. */
  class OrderPlacedPage {
    constructor(order, delivery, format) {
      this.order = order;
      this.delivery = delivery;
      this.esc = format.esc;
      this.money = format.money;
    }

    html() {
      const esc = this.esc;
      const { order, delivery } = this;
      const total = BigInt(order.totalAmount) + BigInt(delivery ? delivery.cents : 0);
      const place = [order.shipping.line1, order.shipping.line2, order.shipping.city, order.shipping.region].filter(Boolean).join(', ');
      return `
      <section class="order-placed">
        <div class="done-badge">${window.Cinnarios.icons.check()}</div>
        <h1>Order placed</h1>
        <p>Order <strong>${esc(order.number)}</strong> for ${esc(this.money(total.toString(), order.currency))}, going to ${esc(place)}. A rider brings it to that address.</p>
        <div class="order-placed-items">${order.items.map((item) => `<p>${esc(item.title)} · ${esc(item.widthCm)} × ${esc(item.heightCm)} cm${item.quantity > 1 ? ` · ${esc(item.quantity)} prints` : ''}</p>`).join('')}</div>
        <div class="piece-actions order-placed-actions"><a class="button" href="#account">See your orders</a><a class="button quiet" href="#gallery">Keep browsing</a></div>
      </section>`;
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.CheckoutPage = CheckoutPage;
  window.Cinnarios.OrderPlacedPage = OrderPlacedPage;
}());
