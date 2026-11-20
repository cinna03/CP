// Spotlight-style search over the catalogue. Every word typed must match something;
// matches are weighted so the most specific ones come first: artist, then a title word,
// then the feel (subject, mood, colour), then size. No filters and no dates.
(function () {
  const WEIGHTS = Object.freeze({ artist: 5, title: 4, feel: 3, size: 2 });
  const SUBJECT_WORDS = Object.freeze({
    flower: 'flowers', flowers: 'flowers', bouquet: 'flowers', 'still-life': 'flowers',
    portrait: 'figure', portraits: 'figure', people: 'figure', person: 'figure', woman: 'figure', women: 'figure', man: 'figure', men: 'figure', figure: 'figure', figures: 'figure',
    landscape: 'landscape', landscapes: 'landscape', nature: 'landscape', field: 'landscape', trees: 'landscape', sea: 'landscape', river: 'landscape',
  });
  const MOOD_WORDS = Object.freeze(['bright', 'cool', 'fresh', 'warm', 'grounded', 'quiet', 'deep']);
  const COLOUR_WORDS = Object.freeze({ gold: 'gold', golden: 'gold', yellow: 'gold', blue: 'blue', green: 'green', red: 'red', pink: 'red', dark: 'dark', black: 'dark', pale: 'pale', light: 'pale', earth: 'earth', brown: 'earth' });
  const SMALL_M2 = 0.35;
  const LARGE_M2 = 1.2;

  class CatalogSearch {
    /** @param {(work: object) => {subject: string, mood: string, colour: string}} describe the shop's filing */
    constructor(describe) {
      this.describe = describe;
    }

    /** Splits "66x40 warm" into ["66", "40", "warm"]; single letters are ignored. */
    static tokens(query) {
      return String(query || '').toLowerCase().replace(/[×*]/g, ' ').replace(/(\d)\s*x\s*(\d)/g, '$1 $2').replace(/cm/g, ' ')
        .split(/[\s,]+/).filter((token) => token.length > 1 || /\d/.test(token));
    }

    scoreToken(work, token, feel) {
      const width = Number(work.widthCm) || 0;
      const height = Number(work.heightCm) || 0;
      const words = (text) => String(text || '').toLowerCase().split(/[\s,:;.()'-]+/);
      let best = 0;
      const offer = (points) => { best = Math.max(best, points); };
      if (words(work.artist).some((part) => part.startsWith(token))) offer(WEIGHTS.artist);
      if (words(work.title).some((part) => part.startsWith(token))) offer(WEIGHTS.title);
      if (SUBJECT_WORDS[token] && SUBJECT_WORDS[token] === feel.subject) offer(WEIGHTS.feel);
      if (MOOD_WORDS.includes(token) && token === feel.mood) offer(WEIGHTS.feel);
      if (COLOUR_WORDS[token] && COLOUR_WORDS[token] === feel.colour) offer(WEIGHTS.feel);
      const squareMetres = (width * height) / 10000;
      if ((token === 'small' || token === 'little') && squareMetres < SMALL_M2) offer(WEIGHTS.size);
      if ((token === 'large' || token === 'big') && squareMetres > LARGE_M2) offer(WEIGHTS.size);
      if (token === 'wide' && width > height * 1.2) offer(WEIGHTS.size);
      if (token === 'tall' && height > width * 1.1) offer(WEIGHTS.size);
      const number = Number(token);
      if (Number.isFinite(number) && number > 0 && [width, height].some((side) => Math.abs(side - number) <= number * 0.1)) offer(WEIGHTS.size);
      return best;
    }

    /** Works where every word matched, best total first. An empty query finds nothing. */
    search(query, catalog) {
      const tokens = CatalogSearch.tokens(query);
      if (!tokens.length) return [];
      return catalog
        .map((work) => {
          const feel = this.describe(work);
          const parts = tokens.map((token) => this.scoreToken(work, token, feel));
          return { work, score: parts.every((part) => part > 0) ? parts.reduce((sum, part) => sum + part, 0) : 0 };
        })
        .filter((result) => result.score > 0)
        .sort((a, b) => b.score - a.score)
        .map((result) => result.work);
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.CatalogSearch = CatalogSearch;
}());
