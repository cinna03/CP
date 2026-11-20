// Similar in feel: ranks the catalogue by how close each painting feels to one painting.
// Feel comes first (subject, then mood and colour); artist, shape and size only break ties.
// The traits come from the shop's own filing, passed in as `describe`, so this file holds
// only the ranking and knows nothing about where the filing is kept.
(function () {
  const WEIGHTS = Object.freeze({ subject: 4, mood: 3, colour: 3, artist: 1.5, shape: 1 });
  const UNKNOWN_SUBJECT = 'picture';

  /** The traits one painting is compared on. Built once per painting, then read only. */
  class FeelProfile {
    constructor(work, traits) {
      const width = Number(work.widthCm) || 0;
      const height = Number(work.heightCm) || 0;
      this.work = work;
      this.subject = traits.subject;
      this.mood = traits.mood;
      this.colour = traits.colour;
      this.artist = (work.artist || '').trim();
      this.shape = height > width ? 'portrait' : 'landscape';
      this.area = width * height;
      Object.freeze(this);
    }

    sharesSubject(other) {
      return this.subject !== UNKNOWN_SUBJECT && this.subject === other.subject;
    }

    sharesArtist(other) {
      return Boolean(this.artist) && this.artist === other.artist;
    }

    /** 1 for the same wall area, falling towards 0 as the sizes drift apart. */
    sizeCloseness(other) {
      if (!this.area) return 0;
      return 1 / (1 + Math.abs(this.area - other.area) / this.area);
    }
  }

  class SimilarityRanker {
    /** @param {(work: object) => {subject: string, mood: string, colour: string}} describe */
    constructor(describe) {
      this.describe = describe;
    }

    profile(work) {
      return new FeelProfile(work, this.describe(work));
    }

    score(a, b) {
      return WEIGHTS.subject * Number(a.sharesSubject(b))
        + WEIGHTS.mood * Number(a.mood === b.mood)
        + WEIGHTS.colour * Number(a.colour === b.colour)
        + WEIGHTS.artist * Number(a.sharesArtist(b))
        + WEIGHTS.shape * Number(a.shape === b.shape)
        + a.sizeCloseness(b);
    }

    /** At most two short tags saying why b was picked for a. */
    reasons(a, b) {
      const tags = [];
      if (a.sharesSubject(b)) tags.push(b.subject);
      if (a.mood === b.mood) tags.push(`${b.mood} mood`);
      else if (a.colour === b.colour) tags.push(`${b.colour} tones`);
      if (a.sharesArtist(b)) tags.push('same artist');
      if (!tags.length) tags.push(b.shape);
      return tags.slice(0, 2);
    }

    /** The `count` paintings closest in feel to `work`, best first, each with its reasons. */
    rank(work, catalog, count = 5) {
      const target = this.profile(work);
      return catalog
        .filter((other) => other.sourceId !== work.sourceId)
        .map((other) => {
          const candidate = this.profile(other);
          return { work: other, score: this.score(target, candidate), reasons: this.reasons(target, candidate) };
        })
        .sort((x, y) => y.score - x.score)
        .slice(0, count);
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.SimilarityRanker = SimilarityRanker;
}());
