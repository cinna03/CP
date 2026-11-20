// Delivery by rider inside Rwanda: a Kigali price and an other-province price.
// The cart lets the buyer pick; at checkout the province decides. The chosen tier is kept
// for this tab, and remembered against the order number so the confirmation can show it.
(function () {
  const TIERS = Object.freeze({
    kigali: Object.freeze({ id: 'kigali', label: 'Kigali rider', cents: 8000, currency: 'USD' }),
    province: Object.freeze({ id: 'province', label: 'Another province', cents: 15000, currency: 'USD' }),
  });
  const CHOSEN_KEY = 'marketplace.shipTier';
  const ORDER_KEY = 'cinnarios.delivery:';

  const read = (key) => { try { return sessionStorage.getItem(key); } catch { return null; } };
  const write = (key, value) => { try { sessionStorage.setItem(key, value); } catch { /* private mode */ } };

  class Delivery {
    static tiers() {
      return [TIERS.kigali, TIERS.province];
    }

    static tier(id) {
      return TIERS[id] || TIERS.kigali;
    }

    static forProvince(province) {
      return !province || province === 'Kigali City' ? TIERS.kigali : TIERS.province;
    }

    static chosen() {
      return Delivery.tier(read(CHOSEN_KEY));
    }

    static choose(id) {
      write(CHOSEN_KEY, Delivery.tier(id).id);
    }

    static rememberForOrder(number, id) {
      write(ORDER_KEY + number, Delivery.tier(id).id);
    }

    /** The tier used for an order placed in this tab, or null if it was placed elsewhere. */
    static forOrder(number) {
      const id = read(ORDER_KEY + number);
      return id ? Delivery.tier(id) : null;
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.Delivery = Delivery;
}());
