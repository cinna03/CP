// Scroll motion for the homepage, written so it runs in every browser and in the phone app's
// web view (the design mockup used CSS scroll timelines, which not every browser has).
// One animation loop reads where each marked element sits in the window, turns that into a
// progress value, and eases the shown value towards it with linear interpolation (lerp), so
// fast scrolling still glides. Each kind of motion is a subclass of Effect with its own
// draw() (polymorphism); the keyframes and ranges are the design's own.
(function () {
  const EASE = 0.16;
  const clamp = (value, low = 0, high = 1) => Math.min(high, Math.max(low, value));
  const lerp = (from, to, amount) => from + (to - from) * amount;
  const number = (el, name) => parseFloat(el.style.getPropertyValue(name)) || 0;

  /** Base class: eases towards target() each frame and hands the eased value to draw(). */
  class Effect {
    constructor(el) {
      this.el = el;
      this.shown = null;
    }

    /** How far `node` has travelled through the window: 0 as its top meets the bottom edge, 1 as its bottom leaves the top. */
    static cover(node) {
      const rect = node.getBoundingClientRect();
      const height = window.innerHeight;
      return clamp((height - rect.top) / (height + rect.height));
    }

    /** Maps cover progress between `start` and `end` onto 0..1. */
    static within(progress, start, end) {
      return clamp((progress - start) / (end - start));
    }

    target() {
      return Effect.cover(this.el);
    }

    /** The value shown when motion is reduced: everything at rest in its final place. */
    rest() {
      return 1;
    }

    step(still) {
      const goal = still ? this.rest() : this.target();
      this.shown = still || this.shown === null ? goal : lerp(this.shown, goal, EASE);
      this.draw(this.shown);
    }

    draw() {}
  }

  /** Hero headline: word i inks in between i × 34 px and i × 34 + 90 px of scrolling. */
  class ScrollInk extends Effect {
    constructor(el) {
      super(el);
      this.words = [...el.querySelectorAll('.w')];
    }

    target() {
      return window.scrollY;
    }

    rest() {
      return 1e6;
    }

    draw(scrolled) {
      this.words.forEach((word, i) => ScrollInk.ink(word, clamp((scrolled - i * 34) / 90)));
    }

    static ink(word, amount) {
      word.style.color = `color-mix(in srgb, #111110 ${Math.round(amount * 100)}%, #cfcbc2)`;
    }
  }

  /** Section headings: words ink in one after another as the heading crosses the window. */
  class InkWords extends Effect {
    constructor(el) {
      super(el);
      this.words = [...el.querySelectorAll('.w')];
      this.gap = Math.min(0.04, 0.5 / Math.max(1, this.words.length));
    }

    draw(progress) {
      this.words.forEach((word, i) => {
        const start = 0.12 + i * this.gap;
        ScrollInk.ink(word, Effect.within(progress, start, start + 0.08));
      });
    }
  }

  /** The hero deck: a stacked pile that fans out over the first 380 px; the two badges pop in after. */
  class Deck extends Effect {
    constructor(el) {
      super(el);
      this.cards = [...el.querySelectorAll('.card')];
      this.pops = [...el.querySelectorAll('.pop')];
    }

    target() {
      return window.scrollY;
    }

    rest() {
      return 400;
    }

    draw(scrolled) {
      const spread = clamp(scrolled / 380);
      const narrow = Math.min(1, window.innerWidth / 900);
      this.cards.forEach((card) => {
        card.style.transform = `translateX(${number(card, '--x') * spread * narrow}px) rotate(${number(card, '--r') * spread}deg)`;
      });
      const pop = clamp((scrolled - 260) / 140);
      this.pops.forEach((badge) => {
        badge.style.opacity = String(pop);
        badge.style.translate = `0 ${(1 - pop) * 14}px`;
      });
    }
  }

  /** The "Your photo" panel opens from a rounded window to its full frame. */
  class Grow extends Effect {
    draw(progress) {
      const left = 1 - Effect.within(progress, 0, 0.45);
      this.el.style.clipPath = `inset(${left * 10}% ${left * 18}% round ${28 + left * 28}px)`;
    }
  }

  /** Effects timed by their whole section rather than by themselves. */
  class SectionEffect extends Effect {
    target() {
      return Effect.cover(this.el.closest('section') || this.el);
    }
  }

  /** Cards fly in from the top left, rest beside the panel, then fly off as the section passes. */
  class Travel extends SectionEffect {
    rest() {
      return 0.5;
    }

    draw(progress) {
      const p = Effect.within(progress, 0.05, 0.95);
      const r = number(this.el, '--r');
      let x = 0; let y = 0; let turn = r; let alpha = 1;
      if (p < 0.35) {
        const t = p / 0.35;
        x = lerp(-320, 0, t); y = lerp(-420, 0, t); turn = lerp(-26, r, t); alpha = t;
      } else if (p > 0.7) {
        const t = (p - 0.7) / 0.3;
        x = lerp(0, 60, t); y = lerp(0, -260, t); turn = r + 10 * t; alpha = 1 - t;
      }
      this.el.style.opacity = String(alpha);
      this.el.style.transform = `translate(${x}px, ${y}px) rotate(${turn}deg)`;
    }
  }

  /** The four stacked cards shuffle out to their own places. */
  class Shuffle extends SectionEffect {
    draw(progress) {
      const p = Effect.within(progress, 0.1, 0.55);
      this.el.style.transform = `translate(${number(this.el, '--x') * p}px, ${number(this.el, '--y') * p}px) rotate(${number(this.el, '--r') * p}deg)`;
    }
  }

  /** Mood tiles drift in from far away and settle at a slight angle. */
  class Drift extends Effect {
    constructor(el) {
      super(el);
      this.final = el.style.getPropertyValue('--t').trim();
    }

    draw(progress) {
      const away = 1 - Effect.within(progress, 0, 0.4);
      this.el.style.opacity = String(1 - away);
      this.el.style.transform = `translate(${number(this.el, '--dx') * away}px, ${number(this.el, '--dy') * away}px) rotate(${number(this.el, '--dr') * away}deg) ${this.final}`;
    }
  }

  /** The ring of 24: each painting flies in to its place, then drifts upward at its own pace. */
  class Orbit extends SectionEffect {
    rest() {
      return 0.38;
    }

    draw(progress) {
      const rot = number(this.el, '--rot');
      if (progress < 0.38) {
        const t = progress / 0.38;
        const away = 1 - t;
        this.el.style.opacity = String(t);
        this.el.style.transform = `translate(${number(this.el, '--dx') * away}px, ${number(this.el, '--dy') * away}px) rotate(${lerp(number(this.el, '--r0'), rot, t)}deg) scale(${lerp(0.5, 1, t)})`;
        return;
      }
      const t = (progress - 0.38) / 0.62;
      this.el.style.opacity = '1';
      this.el.style.transform = `translate(0, ${number(this.el, '--px') * t}px) rotate(${rot}deg)`;
    }
  }

  /** Cards fade and lift into place, each a little after the one before. */
  class Rise extends Effect {
    draw(progress) {
      const i = number(this.el, '--i');
      const p = Effect.within(progress, 0.02 + i * 0.05, 0.3 + i * 0.06);
      this.el.style.opacity = String(p);
      this.el.style.translate = `0 ${(1 - p) * 70}px`;
    }
  }

  /** The carousel slides in from the right. */
  class SlideIn extends Effect {
    draw(progress) {
      const p = Effect.within(progress, 0, 0.4);
      this.el.style.opacity = String(0.2 + p * 0.8);
      this.el.style.translate = `${(1 - p) * 40}% 0`;
    }
  }

  const KINDS = Object.freeze({
    'scroll-ink': ScrollInk, ink: InkWords, deck: Deck, grow: Grow, travel: Travel,
    shuffle: Shuffle, drift: Drift, orb: Orbit, rise: Rise, slide: SlideIn,
  });

  class Motion {
    /** @param {HTMLElement} root every [data-motion] inside it moves */
    constructor(root) {
      this.root = root;
      this.still = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      this.effects = [...root.querySelectorAll('[data-motion]')]
        .map((el) => (KINDS[el.dataset.motion] ? new KINDS[el.dataset.motion](el) : null))
        .filter(Boolean);
    }

    start() {
      const frame = () => {
        if (!this.root.isConnected) return;
        this.effects.forEach((effect) => effect.step(this.still));
        if (!this.still) requestAnimationFrame(frame);
      };
      frame();
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.Motion = Motion;
}());
