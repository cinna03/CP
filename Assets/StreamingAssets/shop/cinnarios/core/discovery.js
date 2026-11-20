// Picks the 18 Discover pieces: as different from each other as the catalogue allows.
// Farthest-point sampling: start from one painting, then keep adding the painting whose
// nearest already-picked neighbour is furthest away. The random choices are seeded by the
// current 3-hour block, so everyone sees the same set until the block changes. A buyer's
// wall tries then tilt the picks towards the subjects and moods they have tried.
(function () {
  const BLOCK_MS = 3 * 60 * 60 * 1000;
  const DISTANCE = Object.freeze({ artist: 3, subject: 2, colour: 1.5, mood: 1, shape: 1, band: 1 });
  const JITTER = 0.6;
  const TILT = 1.5;

  /** Small deterministic random generator (mulberry32): same seed, same sequence. */
  function seededRandom(seed) {
    let state = seed >>> 0;
    return function next() {
      state = (state + 0x6D2B79F5) >>> 0;
      let t = state;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  class DiscoverySampler {
    /** @param {(work: object) => {subject: string, mood: string, colour: string}} describe */
    constructor(describe) {
      this.describe = describe;
    }

    static block(now = Date.now()) {
      return Math.floor(now / BLOCK_MS);
    }

    traits(work) {
      const feel = this.describe(work);
      const width = Number(work.widthCm) || 0;
      const height = Number(work.heightCm) || 0;
      const squareMetres = (width * height) / 10000;
      return {
        work,
        artist: (work.artist || '').trim(),
        subject: feel.subject,
        colour: feel.colour,
        mood: feel.mood,
        shape: height > width ? 'portrait' : 'landscape',
        band: squareMetres < 0.35 ? 'small' : squareMetres < 1.2 ? 'medium' : 'large',
      };
    }

    distance(a, b) {
      return DISTANCE.artist * Number(a.artist !== b.artist)
        + DISTANCE.subject * Number(a.subject !== b.subject)
        + DISTANCE.colour * Number(a.colour !== b.colour)
        + DISTANCE.mood * Number(a.mood !== b.mood)
        + DISTANCE.shape * Number(a.shape !== b.shape)
        + DISTANCE.band * Number(a.band !== b.band);
    }

    /** 0..1: how much a painting shares subject or mood with what this buyer has tried. */
    leaning(candidate, tried) {
      if (!tried.length) return 0;
      const close = tried.filter((t) => t.subject === candidate.subject || t.mood === candidate.mood).length;
      return close / tried.length;
    }

    /**
     * @param {object[]} catalog
     * @param {{count?: number, now?: number, triedIds?: string[]}} options
     * @returns {object[]} the picked works, in display order
     */
    pick(catalog, { count = 18, now = Date.now(), triedIds = [] } = {}) {
      const random = seededRandom(DiscoverySampler.block(now));
      const tried = new Set(triedIds.map(String));
      const triedTraits = catalog.filter((work) => tried.has(String(work.sourceId))).map((work) => this.traits(work));
      const pool = catalog.map((work) => this.traits(work));
      if (!pool.length) return [];
      const chosen = [pool.splice(Math.floor(random() * pool.length), 1)[0]];
      const nearest = pool.map((candidate) => this.distance(candidate, chosen[0]));
      while (chosen.length < count && pool.length) {
        let best = 0;
        let bestScore = -Infinity;
        for (let i = 0; i < pool.length; i += 1) {
          const score = nearest[i] + random() * JITTER + TILT * this.leaning(pool[i], triedTraits);
          if (score > bestScore) { bestScore = score; best = i; }
        }
        const next = pool.splice(best, 1)[0];
        nearest.splice(best, 1);
        chosen.push(next);
        for (let i = 0; i < pool.length; i += 1) nearest[i] = Math.min(nearest[i], this.distance(pool[i], next));
      }
      return chosen.map((item) => item.work);
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.DiscoverySampler = DiscoverySampler;
}());
