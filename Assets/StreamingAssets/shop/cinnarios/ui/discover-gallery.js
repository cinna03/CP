// The Discover gallery: paintings on a diagonal line in 3D that drift slowly and follow
// scroll, drag and the arrow buttons. One animation loop moves a `current` position towards
// a `target` with linear interpolation (lerp), which is what makes every move glide.
// Cards are created once and only their transforms change each frame.
(function () {
  const LAYOUTS = Object.freeze({
    wide: { cardH: 230, sx: 150, sy: 82, sz: 70, ry: -24, fade: 7.5, wheel: 420, dragPx: 180 },
    narrow: { cardH: 150, sx: 62, sy: 78, sz: 50, ry: -22, fade: 6, wheel: 300, dragPx: 110 },
  });
  const DRIFT = 0.0022;
  const EASE = 0.09;
  const IDLE_MS = 1600;
  const TAP_PX = 8;

  class DiscoverGallery {
    /**
     * @param {HTMLElement} stage   the element that takes scroll and drag
     * @param {HTMLElement} track   the 3D layer the cards sit in
     * @param {object[]} works
     * @param {(work: object) => void} onOpen
     */
    constructor(stage, track, works, onOpen) {
      this.stage = stage;
      this.track = track;
      this.works = works;
      this.onOpen = onOpen;
      this.current = 0;
      this.target = 0;
      this.lastInput = 0;
      this.drag = null;
      this.still = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      this.layout = window.innerWidth < 700 ? LAYOUTS.narrow : LAYOUTS.wide;
      this.cards = works.map((work) => this.makeCard(work));
      this.listeners = [];
    }

    makeCard(work) {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'gallery-card';
      button.dataset.id = work.sourceId;
      button.setAttribute('aria-label', `${work.title} by ${work.artist || 'an unknown artist'}`);
      const ratio = Math.min(Math.max((Number(work.widthCm) || 1) / (Number(work.heightCm) || 1), 0.62), 1.5);
      const img = document.createElement('img');
      img.alt = '';
      img.decoding = 'async';
      img.loading = 'eager';
      img.draggable = false;
      img.className = 'pending';
      const full = work.thumbnailUrl || work.imageUrl || '';
      const preview = full.includes('/web-large/') ? full.replace('/web-large/', '/web-additional/') : full;
      let triedFull = false;
      img.addEventListener('load', () => img.classList.remove('pending'));
      img.addEventListener('error', () => {
        if (!triedFull && full && img.src !== full) {
          triedFull = true;
          img.src = full;
          return;
        }
        button.classList.add('is-missing');
        button.textContent = work.title;
      });
      if (preview) img.src = preview;
      button.append(img);
      button.addEventListener('click', () => this.onOpen(work));
      this.track.append(button);
      return { button, img, ratio, work, loaded: false };
    }

    /** Position on the loop, wrapped so the line has no end. */
    offset(index) {
      const n = this.works.length;
      const t = index - this.current;
      return ((t + n / 2) % n + n) % n - n / 2;
    }

    draw() {
      const L = this.layout;
      this.cards.forEach((card, i) => {
        const { button, ratio } = card;
        const t = this.offset(i);
        const a = Math.abs(t);
        const h = L.cardH * (1 - Math.min(a, 6) * 0.045);
        const visible = Math.max(0, Math.min(1, (L.fade - a) / 1.5));
        button.style.width = `${Math.round(h * ratio)}px`;
        button.style.height = `${Math.round(h)}px`;
        button.style.transform = `translate(-50%, -50%) translate3d(${t * L.sx}px, ${-t * L.sy}px, ${-a * L.sz}px) rotateY(${L.ry}deg) rotateZ(${t * 0.8}deg)`;
        button.style.zIndex = String(100 - Math.round(a * 10));
        button.style.opacity = String(visible);
        // Faded cards at the ends ignore taps, so a tap always lands on a painting you can see.
        button.style.pointerEvents = visible > 0.25 ? 'auto' : 'none';
        button.tabIndex = visible > 0.25 ? 0 : -1;
      });
      const active = ((Math.round(this.current) % this.works.length) + this.works.length) % this.works.length;
      this.stage.querySelectorAll('[data-tick]').forEach((tick, i) => tick.classList.toggle('on', i === active));
    }

    tick() {
      if (!this.stage.isConnected) return this.stop();
      if (!this.still && Date.now() - this.lastInput > IDLE_MS) this.target += DRIFT;
      this.current = this.still ? this.target : this.current + (this.target - this.current) * EASE;
      this.draw();
      this.frame = requestAnimationFrame(() => this.tick());
      return undefined;
    }

    nudge(steps) {
      this.lastInput = Date.now();
      this.target = Math.round(this.target) + steps;
    }

    on(node, type, handler, options) {
      node.addEventListener(type, handler, options);
      this.listeners.push(() => node.removeEventListener(type, handler, options));
    }

    /** True when the pointer is on the paintings, including a small margin around each one. */
    overBunch(x, y) {
      const pad = 48;
      return this.cards.some((card) => {
        if (card.button.style.pointerEvents === 'none') return false;
        const rect = card.button.getBoundingClientRect();
        return x >= rect.left - pad && x <= rect.right + pad && y >= rect.top - pad && y <= rect.bottom + pad;
      });
    }

    start() {
      // The wheel stays on the paintings. Farther away, the same wheel scrolls the page.
      this.on(this.stage, 'wheel', (event) => {
        if (!this.overBunch(event.clientX, event.clientY)) return;
        event.preventDefault();
        this.lastInput = Date.now();
        const delta = Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY;
        this.target += delta / this.layout.wheel;
      }, { passive: false });
      this.on(this.stage, 'pointerdown', (event) => {
        if (!this.overBunch(event.clientX, event.clientY)) return;
        this.lastInput = Date.now();
        this.drag = { x: event.clientX, y: event.clientY, from: this.target, moved: 0 };
      });
      this.on(this.stage, 'touchmove', (event) => {
        if (this.drag) event.preventDefault();
      }, { passive: false });
      this.on(this.stage, 'pointermove', (event) => {
        if (!this.drag) return;
        this.lastInput = Date.now();
        const dx = event.clientX - this.drag.x;
        const dy = this.drag.y - event.clientY;
        this.drag.moved = Math.max(this.drag.moved, Math.abs(dx) + Math.abs(dy));
        this.target = this.drag.from + (dx + dy) / this.layout.dragPx;
      });
      this.on(this.stage, 'pointerup', (event) => {
        const drag = this.drag;
        this.drag = null;
        // A press that barely moved is a tap. The cards move every frame, so find the one under the finger now.
        if (!drag || drag.moved > TAP_PX) return;
        const hit = document.elementFromPoint(event.clientX, event.clientY);
        const card = hit && hit.closest('.gallery-card');
        const work = card && this.works.find((item) => String(item.sourceId) === card.dataset.id);
        if (work) this.onOpen(work);
      });
      ['pointercancel', 'pointerleave'].forEach((type) => this.on(this.stage, type, () => { this.drag = null; }));
      this.on(this.stage, 'keydown', (event) => {
        if (event.key === 'ArrowRight' || event.key === 'ArrowUp') this.nudge(1);
        if (event.key === 'ArrowLeft' || event.key === 'ArrowDown') this.nudge(-1);
      });
      this.stage.querySelector('[data-prev]')?.addEventListener('click', () => this.nudge(-1));
      this.stage.querySelector('[data-next]')?.addEventListener('click', () => this.nudge(1));
      this.draw();
      this.frame = requestAnimationFrame(() => this.tick());
    }

    stop() {
      cancelAnimationFrame(this.frame);
      this.listeners.forEach((off) => off());
      this.listeners = [];
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.DiscoverGallery = DiscoverGallery;
}());
