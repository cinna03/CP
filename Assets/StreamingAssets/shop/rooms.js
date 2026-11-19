const roomKey = 'marketplace.rooms';
const colourGuide = {
  436535: 'gold',
  436532: 'blue',
  436528: 'green',
  436524: 'earth',
  437881: 'red',
  435809: 'dark',
  437394: 'pale',
  436575: 'gold',
  12127: 'earth',
  11417: 'blue',
  10065: 'red',
};
// Shop filing for comparison. Subject comes from the title. Mood follows the colour guide.
// Neither is a measurement of the paint or a story about what the picture means.
const subjectGuide = {
  437394: 'figure',
  436528: 'flowers',
  12127: 'figure',
  10065: 'figure',
  436532: 'figure',
  436524: 'flowers',
  435809: 'landscape',
  436575: 'landscape',
  11417: 'figure',
  436535: 'landscape',
  437881: 'figure',
};
const moodGuide = {
  gold: 'bright',
  blue: 'cool',
  green: 'fresh',
  red: 'warm',
  earth: 'grounded',
  pale: 'quiet',
  dark: 'deep',
};
const swatches = [
  ['gold', '#c4a15a'],
  ['blue', '#3d5c78'],
  ['green', '#3f5c45'],
  ['red', '#8c3b24'],
  ['earth', '#7a6248'],
  ['pale', '#d9d0c2'],
  ['dark', '#1c1915'],
];
const frames = { none: 0, wood: 4500, linen: 7500 };
const shipTiers = {
  kigali: { label: 'Kigali rider', cents: 8000 },
  province: { label: 'Another province', cents: 15000 },
};

function blankRooms() {
  return {
    saved: [],
    follows: [],
    boards: [],
    offers: [],
    inventory: [],
    methods: [],
    notices: { orders: true, offers: true },
    role: 'collector',
    reserved: {},
    advisory: [],
    payout: null,
    filters: { medium: '', min: '', max: '', colour: '', minW: '', minH: '', orientation: '' },
    concerns: {},
    feedback: {},
    tries: [],
  };
}

function rooms() {
  try {
    return { ...blankRooms(), ...JSON.parse(localStorage.getItem(roomKey) || '{}') };
  } catch {
    return blankRooms();
  }
}

function keep(next) {
  localStorage.setItem(roomKey, JSON.stringify(next));
}

function shop() {
  return window.Shop;
}

function colourOf(work) {
  const filed = window.SHOP_GUIDES && window.SHOP_GUIDES.colour && window.SHOP_GUIDES.colour[work.sourceId];
  return filed || colourGuide[work.sourceId] || 'earth';
}

function subjectOf(work) {
  const filed = window.SHOP_GUIDES && window.SHOP_GUIDES.subject && window.SHOP_GUIDES.subject[work.sourceId];
  return filed || subjectGuide[work.sourceId] || 'picture';
}

function moodOf(work) {
  return moodGuide[colourOf(work)] || 'quiet';
}

function orientationOf(work) {
  const width = Number(work.widthCm);
  const height = Number(work.heightCm);
  if (Math.abs(width - height) / Math.max(width, height) < 0.05) return 'square';
  return width > height ? 'landscape' : 'portrait';
}

function collectionsFrom(works) {
  const groups = [
    { id: 'picks', title: 'Curator picks', text: 'A short wall of works already in the shop.', works: works.slice(0, 4) },
    { id: 'wide', title: 'Wider than tall', text: 'Works whose museum width is greater than the height.', works: works.filter((work) => orientationOf(work) === 'landscape') },
    { id: 'upright', title: 'Taller than wide', text: 'Works whose museum height is greater than the width.', works: works.filter((work) => orientationOf(work) === 'portrait') },
  ];
  const byArtist = new Map();
  for (const work of works) {
    if (!work.artist) continue;
    const list = byArtist.get(work.artist) || [];
    list.push(work);
    byArtist.set(work.artist, list);
  }
  for (const [artist, list] of byArtist) {
    if (list.length < 2) continue;
    groups.push({
      id: `artist-${artist}`,
      title: artist,
      text: 'Every work by this name in the current catalogue.',
      works: list,
    });
  }
  return groups.filter((group) => group.works.length);
}

function findCollection(works, id) {
  return collectionsFrom(works).find((group) => group.id === id);
}

function passes(work, filters) {
  const price = Number(work.priceAmount) / 100;
  if (filters.medium && work.medium !== filters.medium) return false;
  if (filters.colour && colourOf(work) !== filters.colour) return false;
  if (filters.orientation && orientationOf(work) !== filters.orientation) return false;
  if (filters.min && price < Number(filters.min)) return false;
  if (filters.max && price > Number(filters.max)) return false;
  if (filters.minW && Number(work.widthCm) < Number(filters.minW)) return false;
  if (filters.minH && Number(work.heightCm) < Number(filters.minH)) return false;
  return true;
}

function luhn(value) {
  const digits = value.replace(/\D/g, '');
  if (digits.length < 13 || digits.length > 19) return false;
  let sum = 0;
  let alt = false;
  for (let index = digits.length - 1; index >= 0; index -= 1) {
    let digit = Number(digits[index]);
    if (alt) {
      digit *= 2;
      if (digit > 9) digit -= 9;
    }
    sum += digit;
    alt = !alt;
  }
  return sum % 10 === 0;
}

function momoOk(value) {
  return /^(\+?250|0)7(2|3|8|9)\d{7}$/.test(value.replace(/\s/g, ''));
}

function expiryOk(value) {
  const match = /^(\d{2})\s*\/\s*(\d{2})$/.exec(value.trim());
  if (!match) return false;
  const month = Number(match[1]);
  const year = 2000 + Number(match[2]);
  if (month < 1 || month > 12) return false;
  const end = new Date(year, month, 1);
  return end > new Date();
}

function rememberScroll(hash) {
  const y = sessionStorage.getItem(`marketplace.scroll:${hash}`);
  if (y) requestAnimationFrame(() => window.scrollTo(0, Number(y)));
}

let previousHash = location.hash;
window.addEventListener('hashchange', () => {
  sessionStorage.setItem(`marketplace.scroll:${previousHash || '#gallery'}`, String(window.scrollY));
  previousHash = location.hash;
});

function card(work) {
  const { esc } = shop();
  const state = rooms();
  const mark = state.reserved[work.sourceId] ? '<span class="badge">Reserved in this browser</span>' : '';
  return `<a class="work" href="#work/${esc(work.sourceId)}">
    <span class="frame"><img src="${esc(work.thumbnailUrl)}" alt="" loading="lazy" decoding="async" /></span>
    ${mark}
    <strong>${esc(work.title)}</strong>
    <span>${esc(work.artist || 'Unknown artist')}</span>
    <span class="measures">${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span>
  </a>`;
}

function areaOf(work) {
  return Number(work.widthCm) * Number(work.heightCm);
}

function answerFromRecord(work, text, catalog) {
  const { money } = shop();
  const asked = String(text || '').toLowerCase();
  let concern = '';
  if (/size|big|small|large|centimet|cm\b|tall|wide|scale|dimension/.test(asked)) concern = 'size';
  else if (/light|lamp|dark|bright|sun|shadow/.test(asked)) concern = 'light';
  else if (/colou?r|tone|palette/.test(asked)) concern = 'colour';
  else if (/fit|wall|room|space|suit|hang/.test(asked)) concern = 'fit';
  else if (/mean|story|about|who|artist|credit|history|represent|made of|medium/.test(asked)) concern = 'meaning';
  else if (/price|cost|franc|dollar|rwf|cheap|expensive/.test(asked)) concern = 'price';
  let preference = '';
  if (/too big|too large|smaller|won't fit|will not fit/.test(asked)) preference = 'smaller';
  else if (/too small|larger|bigger/.test(asked)) preference = 'larger';
  const area = (areaOf(work) / 10000).toFixed(2);
  const replies = {
    size: `The museum record is ${work.widthCm} cm wide, ${work.heightCm} cm high, and ${work.depthCm} cm deep, about ${area} square metres. The wall preview uses those centimetres.`,
    light: 'The museum record does not say how this paint reads under a lamp. The wall preview is where you judge that in the room.',
    colour: `The shop files a colour guide of “${colourOf(work)}”, a mood guide of “${moodOf(work)}”, and a subject guide of “${subjectOf(work)}”. Those guides sort the next pictures. They are not a measurement of the paint.`,
    fit: `A clear wall needs at least ${work.widthCm} by ${work.heightCm} cm. The preview is upright, at those centimetres, and walking does not move it after you place it.`,
    meaning: `${work.artist || 'The artist'}${work.date ? `, ${work.date}` : ''}. ${work.mediumDescription}. ${work.creditLine} That is the account this shop can give. It does not add a story the museum record does not contain.`,
    price: `${money(work.priceAmount, work.currency)} is the reproduction at this size. Framing, a crate, and shipping are not included.`,
  };
  return {
    concern,
    preference,
    reply: replies[concern] || 'Ask about the size, the light, the colour guide, whether it fits a wall, what the museum says it is, or the price.',
    next: rankAfter(catalog, work, concern || 'fit', preference),
  };
}

function rankAfter(catalog, work, concern, preference) {
  const rest = catalog.filter((item) => item.sourceId !== work.sourceId);
  const score = (item) => {
    let value = 0;
    if (item.artist && item.artist === work.artist) value += 3;
    if (orientationOf(item) === orientationOf(work)) value += 2;
    if (subjectOf(item) === subjectOf(work)) value += concern === 'meaning' || concern === 'fit' ? 4 : 2;
    if (moodOf(item) === moodOf(work)) value += concern === 'colour' ? 4 : 2;
    if (concern === 'colour' && colourOf(item) === colourOf(work)) value += 4;
    const difference = areaOf(item) - areaOf(work);
    if (concern === 'size' || concern === 'fit') {
      if (preference === 'smaller' && difference < 0) value += 6;
      else if (preference === 'larger' && difference > 0) value += 6;
      else if (!preference || preference === 'kept') value += 3 / (1 + Math.abs(difference) / Math.max(areaOf(work), 1));
    }
    if (concern === 'price') {
      const gap = Math.abs(Number(item.priceAmount) - Number(work.priceAmount));
      if (gap <= 20000) value += 4;
    }
    if (concern === 'meaning' && item.artist === work.artist) value += 2;
    return value;
  };
  return rest.sort((left, right) => score(right) - score(left)).slice(0, 3);
}

function guideBox() {
  return `<section class="block" id="guide">
    <h2>Ask from this record</h2>
    <p>The reply uses the museum size, credit, medium, and price, plus the shop’s colour, mood, and subject guides. It will not invent a story about the picture.</p>
    <section class="block" id="decision" hidden>
      <h2>Does this still feel uncertain?</h2>
      <p id="decision-line"></p>
      <div class="actions">
        <button type="button" id="still-uncertain">It still feels uncertain</button>
        <button type="button" id="can-stay">This one can stay</button>
      </div>
    </section>
    <form id="ask-form">
      <label for="ask">Your question</label>
      <input id="ask" name="ask" required placeholder="Will this fit, or what does it cost?" />
      <button type="submit">Ask</button>
    </form>
    <div class="actions">
      <button type="button" data-pref="kept">This size can stay</button>
      <button type="button" data-pref="smaller">Too large for the wall</button>
      <button type="button" data-pref="larger">Too small for the wall</button>
    </div>
    <p id="ask-reply"></p>
    <div id="ask-next" class="suggest"></div>
  </section>`;
}

function showWallOutcome(work, outcome, image) {
  const box = document.querySelector('#decision');
  const line = document.querySelector('#decision-line');
  if (!box || !line) return;
  box.hidden = false;
  line.replaceChildren();
  if (image && /^[\w.-]+$/.test(image)) {
    const picture = document.createElement('img');
    picture.src = `${location.origin}/wall-tries/${image}`;
    picture.alt = 'The view saved from the wall';
    line.append(picture);
  }
  const summary = outcome === 'satisfied'
    ? `${work.title} was the one. The session ended there, and the saved view stays with this try.`
    : outcome === 'declined'
      ? `${work.title} was not wanted. The session ended there.`
      : `${work.title} was still under review. The paintings swapped into that spot are kept with this try.`;
  line.append(summary);
  document.querySelector('#still-uncertain')?.setAttribute('hidden', '');
  document.querySelector('#can-stay')?.setAttribute('hidden', '');
}

function showDecision(work) {
  const box = document.querySelector('#decision');
  const line = document.querySelector('#decision-line');
  if (!box || !line) return;
  box.hidden = false;
  line.textContent = `${work.title} was on your wall at ${work.widthCm} × ${work.heightCm} cm. The tries from this visit are kept on the account.`;
}

async function answerDecision(work, choice, hrefFor) {
  const catalog = await shop().api('/catalog/artworks');
  const preference = choice === 'kept' ? 'kept' : (rooms().feedback[work.sourceId] || '');
  const concern = choice === 'kept' ? 'size' : (rooms().concerns[work.sourceId] || 'fit');
  const result = answerFromRecord(work, concern, catalog);
  result.preference = preference || result.preference;
  result.concern = concern;
  result.next = rankAfter(catalog, work, concern, result.preference);
  result.reply = choice === 'kept'
    ? `${work.title} can stay at ${work.widthCm} × ${work.heightCm} cm. The next pictures are close to that size and share its shop guides.`
    : `${work.title} still feels uncertain. These three are the next to try in the same spot. Each one keeps its own centimetres.`;
  showGuideResult(work, result, hrefFor);
  sessionStorage.removeItem('marketplace.askDecision');
}

async function noteWallTry(report) {
  const current = String(report?.current || '');
  const tried = [...new Set([...(report?.tried || []), current].filter(Boolean))];
  const state = rooms();
  const known = new Set((state.tries || []).map((item) => item.sourceId));
  const now = new Date().toISOString();
  state.tries = state.tries || [];
  const fresh = [];
  for (const sourceId of tried) {
    if (known.has(sourceId)) continue;
    state.tries.push({ sourceId, at: now });
    fresh.push(sourceId);
  }
  keep(state);
  const outcome = report?.outcome || '';
  if (!outcome && current) sessionStorage.setItem('marketplace.askDecision', current);
  if (outcome) sessionStorage.removeItem('marketplace.askDecision');
  const send = shop()?.api;
  for (const sourceId of fresh) {
    if (!send) break;
    send('/me/journey', {
      method: 'POST',
      body: JSON.stringify({ kind: 'viewed_in_room', sourceId, sessionId: shop().sessionId() }),
    }).catch(() => {});
  }
  try {
    const catalog = await shop().api('/catalog/artworks');
    const work = catalog.find((item) => item.sourceId === current);
    if (!work) return;
    if (outcome) showWallOutcome(work, outcome, report?.image || '');
    else showDecision(work);
  } catch {
    /* The question appears the next time this painting is open. */
  }
}

function rememberGuide(work, concern, preference) {
  const state = rooms();
  if (concern) state.concerns[work.sourceId] = concern;
  if (preference) state.feedback[work.sourceId] = preference;
  keep(state);
}

function showGuideResult(work, result, hrefFor) {
  const { esc } = shop();
  document.querySelector('#ask-reply').textContent = result.reply;
  document.querySelector('#ask-next').innerHTML = result.next.map((item) => `<a class="work" href="${esc(hrefFor(item.sourceId))}">
    <span class="frame"><img src="${esc(item.thumbnailUrl)}" alt="" /></span>
    <strong>${esc(item.title)}</strong>
    <span>${esc(item.artist || 'Unknown artist')}</span>
    <span class="measures">${esc(item.widthCm)} × ${esc(item.heightCm)} cm</span>
  </a>`).join('');
  rememberGuide(work, result.concern, result.preference);
}

async function bindGuide(work) {
  const form = document.querySelector('#ask-form');
  if (!form) return;
  const hrefFor = (sourceId) => (location.hash.startsWith('#try/') ? `#try/${sourceId}` : `#work/${sourceId}`);
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const catalog = await shop().api('/catalog/artworks');
    const result = answerFromRecord(work, new FormData(form).get('ask'), catalog);
    showGuideResult(work, result, hrefFor);
    if (result.concern === 'size') shop().api?.('/me/journey', { method: 'POST', body: JSON.stringify({ kind: 'dimensions_checked', sourceId: work.sourceId, sessionId: shop().sessionId() }) }).catch(() => {});
  });
  document.querySelector('#still-uncertain')?.addEventListener('click', () => answerDecision(work, 'uncertain', hrefFor));
  document.querySelector('#can-stay')?.addEventListener('click', () => answerDecision(work, 'kept', hrefFor));
  const pending = sessionStorage.getItem('marketplace.askDecision');
  if (pending === work.sourceId) showDecision(work);
  document.querySelector('#guide').addEventListener('click', async (event) => {
    const button = event.target.closest('[data-pref]');
    if (!button) return;
    const catalog = await shop().api('/catalog/artworks');
    const preference = button.dataset.pref;
    const spoken = preference === 'smaller' ? 'too large' : preference === 'larger' ? 'too small' : 'size';
    const result = answerFromRecord(work, spoken, catalog);
    result.preference = preference === 'kept' ? 'kept' : result.preference;
    if (preference === 'kept') result.reply = `The record stays ${work.widthCm} × ${work.heightCm} cm. The next pictures are close to that size.`;
    showGuideResult(work, result, hrefFor);
  });
}

window.Rooms = {
  isSaved(sourceId) {
    return rooms().saved.includes(sourceId);
  },
  isReserved(sourceId) {
    return Boolean(rooms().reserved[sourceId]);
  },
  workFacts(work) {
    const { esc, money } = shop();
    const state = rooms();
    const framing = sessionStorage.getItem(`marketplace.frame:${work.sourceId}`) || 'none';
    const reserved = state.reserved[work.sourceId];
    return `${reserved ? `<p class="badge">Reserved in this browser on order ${esc(reserved.orderNumber)}. The museum print stays in the catalogue for the camera. <button type="button" id="release" class="quiet">Release reservation</button></p>` : ''}
      <div class="actions">
        <button type="button" id="zoom-in" class="quiet">Closer</button>
        <button type="button" id="zoom-out" class="quiet">Farther</button>
      </div>`;
  },
  bindWork(work) {
    const image = document.querySelector('#zoom-image');
    let scale = 1;
    const paint = () => {
      image.style.transform = `scale(${scale})`;
    };
    document.querySelector('#zoom-in')?.addEventListener('click', () => {
      scale = Math.min(3, scale + 0.25);
      paint();
    });
    document.querySelector('#zoom-out')?.addEventListener('click', () => {
      scale = Math.max(1, scale - 0.25);
      paint();
    });
    image?.addEventListener('wheel', (event) => {
      event.preventDefault();
      scale = Math.min(3, Math.max(1, scale + (event.deltaY < 0 ? 0.12 : -0.12)));
      paint();
    }, { passive: false });
    document.querySelector('#frame')?.addEventListener('change', (event) => {
      sessionStorage.setItem(`marketplace.frame:${work.sourceId}`, event.target.value);
      const extra = frames[event.target.value] || 0;
      document.querySelector('#frame-note').textContent = extra
        ? `Guide ${shop().money(extra)} for that frame. The recorded work price does not change.`
        : 'The frame figure is a guide. It is not added to the recorded price.';
    });
    document.querySelector('#save-work')?.addEventListener('click', () => {
      const state = rooms();
      state.saved = state.saved.includes(work.sourceId)
        ? state.saved.filter((id) => id !== work.sourceId)
        : [...state.saved, work.sourceId];
      keep(state);
      document.querySelector('#save-work').textContent = state.saved.includes(work.sourceId) ? 'Saved' : 'Save';
    });
    document.querySelector('#release')?.addEventListener('click', () => {
      const state = rooms();
      delete state.reserved[work.sourceId];
      keep(state);
      location.reload();
    });
    bindGuide(work);
  },
  bindTry(work, cameraHref) {
    const payload = {
      artworkId: work.sourceId,
      imageUrl: work.imageUrl,
      widthCm: work.widthCm,
      heightCm: work.heightCm,
      depthCm: work.depthCm,
      api: new URL(cameraHref).searchParams.get('api'),
    };
    const node = document.querySelector('#ar-payload');
    if (node) node.textContent = JSON.stringify(payload, null, 2);
    document.querySelector('#open-camera')?.addEventListener('click', (event) => {
      const call = window.Unity && window.Unity.call;
      if (typeof call !== 'function') return;
      event.preventDefault();
      call(cameraHref);
    });
    document.querySelector('#share-try')?.addEventListener('click', async () => {
      const url = location.href;
      try {
        if (navigator.share) await navigator.share({ title: work.title, url });
        else {
          await navigator.clipboard.writeText(url);
          document.querySelector('#try-error').textContent = 'The try link is on the clipboard. The wall picture is taken in the camera, not on this page.';
        }
      } catch (error) {
        if (error?.name !== 'AbortError') document.querySelector('#try-error').textContent = 'The link could not be shared from this browser.';
      }
    });
    document.querySelector('#try-add')?.addEventListener('click', async () => {
      const button = document.querySelector('#try-add');
      button.disabled = true;
      try {
        await shop().api('/cart/items', { method: 'POST', body: JSON.stringify({ sourceId: work.sourceId, quantity: 1 }) });
        await shop().refreshCartCount();
        location.hash = '#cart';
      } catch (error) {
        button.disabled = false;
        document.querySelector('#try-error').textContent = error.message;
      }
    });
    bindGuide(work);
  },
  guideBox,
  noteWallTry,
  colourName: colourOf,
  moodName: moodOf,
  subjectName: subjectOf,
};

async function renderDiscover() {
  const { api, esc, place, setPage, money } = shop();
  setPage('visit', 'Discover');
  try {
    const works = await api('/catalog/artworks');
    const hero = works[0];
    const artists = [...new Map(works.filter((work) => work.artist).map((work) => [work.artist, work])).values()].slice(0, 8);
    const mediums = [...new Set(works.map((work) => work.medium).filter(Boolean))];
    place(`
      <h1>Discover</h1>
      <p class="intro">Public-domain paintings from The Met, at the centimetres the museum recorded.</p>
      <a class="hero" href="#work/${esc(hero.sourceId)}">
        <img src="${esc(hero.imageUrl)}" alt="${esc(hero.title)}" />
        <strong>${esc(hero.title)}</strong>
        <span>${esc(hero.artist || 'Unknown artist')} · ${esc(money(hero.priceAmount, hero.currency))}</span>
      </a>
      <section class="block">
        <h2>Curator picks</h2>
        <div class="carousel">${works.slice(0, 6).map(card).join('')}</div>
      </section>
      <section class="block">
        <h2>Artists in the catalogue</h2>
        <div class="carousel">${artists.map((work) => `<a class="pill-card" href="#artist/${encodeURIComponent(work.artist)}"><strong>${esc(work.artist)}</strong><span>Open the portfolio</span></a>`).join('')}</div>
      </section>
      <section class="block">
        <h2>Medium</h2>
        <div class="pills">${mediums.map((medium) => `<button type="button" class="quiet medium-pill" data-medium="${esc(medium)}">${esc(medium)}</button>`).join('')}</div>
      </section>
      <section class="block">
        <h2><a href="#collections">Collections</a></h2>
        <div class="carousel">${collectionsFrom(works).slice(0, 3).map((group) => `<a class="pill-card" href="#collection/${encodeURIComponent(group.id)}"><strong>${esc(group.title)}</strong><span>${group.works.length} works</span></a>`).join('')}</div>
      </section>
      <div class="grid">${works.map(card).join('')}</div>`, false, () => {
      document.querySelectorAll('.medium-pill').forEach((button) => {
        button.addEventListener('click', () => {
          const state = rooms();
          state.filters = { ...blankRooms().filters, medium: button.dataset.medium };
          keep(state);
          location.hash = '#search';
        });
      });
      rememberScroll('#gallery');
    });
  } catch (error) {
    place(shop().formError(error), false);
  }
}

async function renderSearch() {
  const { api, esc, place, setPage } = shop();
  setPage('visit', 'Search');
  const state = rooms();
  try {
    const works = await api('/catalog/artworks');
    const mediums = [...new Set(works.map((work) => work.medium).filter(Boolean))];
    const shown = works.filter((work) => passes(work, state.filters));
    const filters = state.filters;
    place(`
      <h1>Search</h1>
      <p class="intro">${shown.length} of ${works.length} works. Colour swatches are a shop guide, not a reading of the paint.</p>
      <button type="button" id="open-filters">Filters</button>
      <dialog id="filters" class="sheet-dialog">
        <form method="dialog" id="filter-form">
          <h2>Filters</h2>
          <label for="medium">Medium</label>
          <select id="medium" name="medium">
            <option value="">Any</option>
            ${mediums.map((medium) => `<option ${filters.medium === medium ? 'selected' : ''}>${esc(medium)}</option>`).join('')}
          </select>
          <label for="min">Minimum price, USD</label>
          <input id="min" name="min" inputmode="decimal" value="${esc(filters.min)}" />
          <label for="max">Maximum price, USD</label>
          <input id="max" name="max" inputmode="decimal" value="${esc(filters.max)}" />
          <p>Colour</p>
          <div class="swatches">${swatches.map(([name, hex]) => `<button type="button" class="swatch ${filters.colour === name ? 'on' : ''}" data-colour="${name}" style="background:${hex}" aria-pressed="${filters.colour === name}" aria-label="${name}"></button>`).join('')}<button type="button" class="quiet" id="clear-colour">Any colour</button></div>
          <label for="minW">Minimum width, cm</label>
          <input id="minW" name="minW" inputmode="decimal" value="${esc(filters.minW)}" />
          <label for="minH">Minimum height, cm</label>
          <input id="minH" name="minH" inputmode="decimal" value="${esc(filters.minH)}" />
          <label for="orientation">Orientation</label>
          <select id="orientation" name="orientation">
            ${['', 'landscape', 'portrait', 'square'].map((value) => `<option value="${value}" ${filters.orientation === value ? 'selected' : ''}>${value || 'Any'}</option>`).join('')}
          </select>
          <div class="actions">
            <button type="submit">Show these works</button>
            <button type="button" id="reset-filters" class="quiet">Clear</button>
            <button type="button" id="close-filters" class="quiet">Close</button>
          </div>
        </form>
      </dialog>
      <div class="masonry">${shown.map(card).join('') || '<p>Nothing matches these filters. Clear them and look again.</p>'}</div>`, false, () => {
      const dialog = document.querySelector('#filters');
      document.querySelector('#open-filters').addEventListener('click', () => dialog.showModal());
      document.querySelector('#close-filters').addEventListener('click', () => dialog.close());
      document.querySelector('#clear-colour').addEventListener('click', () => {
        document.querySelectorAll('.swatch').forEach((button) => button.setAttribute('aria-pressed', 'false'));
        dialog.dataset.colour = '';
      });
      document.querySelectorAll('.swatch').forEach((button) => {
        button.addEventListener('click', () => {
          document.querySelectorAll('.swatch').forEach((other) => other.setAttribute('aria-pressed', 'false'));
          button.setAttribute('aria-pressed', 'true');
          dialog.dataset.colour = button.dataset.colour;
        });
      });
      dialog.dataset.colour = filters.colour || '';
      document.querySelector('#reset-filters').addEventListener('click', () => {
        const next = rooms();
        next.filters = blankRooms().filters;
        keep(next);
        renderSearch();
      });
      document.querySelector('#filter-form').addEventListener('submit', () => {
        const data = Object.fromEntries(new FormData(document.querySelector('#filter-form')));
        const next = rooms();
        next.filters = { ...data, colour: dialog.dataset.colour || '' };
        keep(next);
        renderSearch();
      });
      rememberScroll('#search');
    });
  } catch (error) {
    place(shop().formError(error), false);
  }
}

async function renderCollections() {
  const { api, esc, place, setPage } = shop();
  setPage('visit', 'Collections');
  const works = await api('/catalog/artworks');
  const state = rooms();
  place(`<h1>Collections</h1>
    <div class="stack">${collectionsFrom(works).map((group) => `<a class="line-link" href="#collection/${encodeURIComponent(group.id)}"><strong>${esc(group.title)}</strong><span>${group.works.length} works</span></a>`).join('')}</div>`);
}

async function renderCollection(id) {
  const { api, esc, place, setPage } = shop();
  const works = await api('/catalog/artworks');
  const group = findCollection(works, decodeURIComponent(id));
  if (!group) {
    setPage('sheet', 'Collection');
    place('<h1>That collection is not in the shop.</h1><p><a href="#collections">See the collections.</a></p>', false);
    return;
  }
  setPage('visit', group.title);
  place(`<h1>${esc(group.title)}</h1>
    <p>${esc(group.text)}</p>
    <div class="grid">${group.works.map(card).join('')}</div>`);
}

async function renderArtist(name) {
  const { api, esc, place, setPage } = shop();
  const artist = decodeURIComponent(name);
  const works = await api('/catalog/artworks');
  const own = works.filter((work) => work.artist === artist);
  setPage('visit', artist);
  place(`<h1>${esc(artist)}</h1>
    <p>${own.length} museum ${own.length === 1 ? 'work' : 'works'} in this catalogue.</p>
    <div class="grid">${own.map(card).join('') || '<p>None in this catalogue.</p>'}</div>`);
}

async function renderSaved() {
  const { api, esc, place, setPage } = shop();
  setPage('visit', 'Saved');
  const state = rooms();
  const works = await api('/catalog/artworks');
  const saved = works.filter((work) => state.saved.includes(work.sourceId));
  place(`<h1>Saved</h1>
    ${saved.length ? `<div class="grid">${saved.map(card).join('')}</div>` : '<p>Nothing saved yet. Open a work and choose Save.</p>'}`);
}

async function renderBoard(id) {
  const { api, esc, place, setPage } = shop();
  const state = rooms();
  const board = state.boards.find((item) => item.id === id);
  setPage('sheet', 'Moodboard');
  if (!board) {
    place('<h1>That board is gone.</h1><p><a href="#saved">Back to saved.</a></p>', false);
    return;
  }
  const works = await api('/catalog/artworks');
  const chosen = works.filter((work) => board.sourceIds.includes(work.sourceId));
  const rest = works.filter((work) => !board.sourceIds.includes(work.sourceId));
  place(`<h1>${esc(board.name)}</h1>
    <div class="grid">${chosen.map(card).join('') || '<p>This board is empty.</p>'}</div>
    <form id="add-board"><label for="piece">Add a work</label>
      <select id="piece" name="sourceId">${rest.map((work) => `<option value="${esc(work.sourceId)}">${esc(work.title)}</option>`).join('')}</select>
      <button type="submit" ${rest.length ? '' : 'disabled'}>Add</button>
    </form>`, false, () => {
    document.querySelector('#add-board')?.addEventListener('submit', (event) => {
      event.preventDefault();
      const next = rooms();
      const current = next.boards.find((item) => item.id === id);
      current.sourceIds.push(new FormData(event.currentTarget).get('sourceId'));
      keep(next);
      renderBoard(id);
    });
  });
}

async function renderOffer(sourceId) {
  const { api, esc, place, setPage, money, signedIn } = shop();
  const work = await api(`/catalog/artworks/${sourceId}`);
  setPage('sheet', 'Offer');
  if (!signedIn()) {
    place(`<h1>Make an offer</h1><p><a href="#enter">Sign in</a> before proposing a price for ${esc(work.title)}.</p>`, false);
    return;
  }
  place(`<h1>Offer on ${esc(work.title)}</h1>
    <p>The listed price is ${esc(money(work.priceAmount, work.currency))}. An offer stays in this browser until you accept, decline, or hear a counter. It does not charge anyone.</p>
    <form id="offer-form">
      <label for="amount">Your price, USD</label>
      <input id="amount" name="amount" inputmode="decimal" required />
      <label class="check"><input type="checkbox" name="terms" required /> I understand this is a sandbox proposal. The shop does not take payment and the artist does not receive a payout.</label>
      <button type="submit">Submit offer</button>
      <p id="offer-error" class="error" role="alert"></p>
    </form>`, false, () => {
    document.querySelector('#offer-form').addEventListener('submit', (event) => {
      event.preventDefault();
      const amount = Number(new FormData(event.currentTarget).get('amount'));
      const error = document.querySelector('#offer-error');
      if (!Number.isFinite(amount) || amount <= 0) {
        error.textContent = 'Enter a price above zero.';
        return;
      }
      const state = rooms();
      const offer = {
        id: crypto.randomUUID?.() || String(Date.now()),
        sourceId,
        title: work.title,
        amountCents: Math.round(amount * 100),
        status: 'pending',
        messages: [{ from: 'you', text: `Proposed ${amount.toFixed(2)} USD.`, at: new Date().toISOString() }],
      };
      state.offers.unshift(offer);
      keep(state);
      location.hash = `#thread/${offer.id}`;
    });
  });
}

function renderOffers() {
  const { esc, place, setPage, money } = shop();
  setPage('sheet', 'Offers');
  const state = rooms();
  place(`<h1>Offers</h1>
    ${state.offers.length ? `<div class="stack">${state.offers.map((offer) => `<a class="line-link" href="#thread/${esc(offer.id)}"><strong>${esc(offer.title)}</strong><span>${esc(offer.status)} · ${esc(money(offer.amountCents))}</span></a>`).join('')}</div>` : '<p>No offers yet. Propose one from a work.</p>'}`);
}

function renderThread(id) {
  const { esc, place, setPage, money } = shop();
  const state = rooms();
  const offer = state.offers.find((item) => item.id === id);
  setPage('sheet', 'Offer');
  if (!offer) {
    place('<h1>That offer is not on this device.</h1><p><a href="#offers">Back to offers.</a></p>', false);
    return;
  }
  place(`<h1>${esc(offer.title)}</h1>
    <p class="badge">${esc(offer.status)}</p>
    <p>Current figure ${esc(money(offer.amountCents))}.</p>
    <div class="thread">${offer.messages.map((message) => `<p><strong>${esc(message.from)}</strong> ${esc(message.text)}</p>`).join('')}</div>
    <div class="actions">
      ${offer.status === 'pending' || offer.status === 'countered' ? '<button type="button" id="counter">Hear a counter</button>' : ''}
      ${offer.status === 'countered' ? '<button type="button" id="accept">Accept counter</button><button type="button" id="decline" class="quiet">Decline</button>' : ''}
      ${offer.status === 'accepted' ? `<a class="button" href="#work/${esc(offer.sourceId)}">Add it from the work</a>` : ''}
    </div>`, false, () => {
    const update = (mutate) => {
      const next = rooms();
      const current = next.offers.find((item) => item.id === id);
      mutate(current);
      keep(next);
      renderThread(id);
    };
    document.querySelector('#counter')?.addEventListener('click', () => {
      update((current) => {
        const counter = Math.round(current.amountCents * 1.08);
        current.amountCents = counter;
        current.status = 'countered';
        current.messages.push({ from: 'studio', text: `Countered at ${(counter / 100).toFixed(2)} USD. This reply is simulated on this device.`, at: new Date().toISOString() });
      });
    });
    document.querySelector('#accept')?.addEventListener('click', () => {
      update((current) => {
        current.status = 'accepted';
        current.messages.push({ from: 'you', text: 'Accepted the counter. Nothing was charged.', at: new Date().toISOString() });
      });
    });
    document.querySelector('#decline')?.addEventListener('click', () => {
      update((current) => {
        current.status = 'declined';
        current.messages.push({ from: 'you', text: 'Declined the counter.', at: new Date().toISOString() });
      });
    });
  });
}

function renderStudio() {
  location.replace('#account');
}

function renderInventory() {
  const { esc, place, setPage, money } = shop();
  setPage('sheet', 'Inventory');
  const items = rooms().inventory;
  const view = sessionStorage.getItem('marketplace.inventoryView') || 'all';
  const shown = view === 'all' ? items : items.filter((item) => item.status === view);
  place(`<h1>Inventory</h1>
    <p>These works live on this device. Publishing one does not put it in the museum catalogue or on the camera.</p>
    <div class="pills">
      ${['all', 'draft', 'published', 'sold'].map((status) => `<button type="button" class="quiet" data-view="${status}" aria-pressed="${view === status}">${status}</button>`).join('')}
    </div>
    <div class="stack">${shown.map((item) => `<article class="line-link"><strong>${esc(item.title)}</strong><span class="badge">${esc(item.status)}</span><span>${esc(item.widthCm)} × ${esc(item.heightCm)} × ${esc(item.depthCm)} cm · ${esc(money(item.priceCents))}</span>
      <span class="actions">${item.status === 'draft' ? `<button type="button" data-publish="${esc(item.id)}">Publish on this device</button>` : ''}${item.status === 'published' ? `<button type="button" data-sell="${esc(item.id)}" class="quiet">Mark simulated sale</button>` : ''}</span></article>`).join('') || '<p>No works in this view. <a href="#upload">Upload one.</a></p>'}</div>`, false, () => {
    document.querySelectorAll('[data-view]').forEach((button) => {
      button.addEventListener('click', () => {
        sessionStorage.setItem('marketplace.inventoryView', button.dataset.view);
        renderInventory();
      });
    });
    document.querySelectorAll('[data-publish]').forEach((button) => {
      button.addEventListener('click', () => {
        const next = rooms();
        next.inventory.find((item) => item.id === button.dataset.publish).status = 'published';
        keep(next);
        renderInventory();
      });
    });
    document.querySelectorAll('[data-sell]').forEach((button) => {
      button.addEventListener('click', () => {
        const next = rooms();
        next.inventory.find((item) => item.id === button.dataset.sell).status = 'sold';
        keep(next);
        renderInventory();
      });
    });
  });
}

function renderUpload() {
  const { esc, place, setPage } = shop();
  setPage('sheet', 'Upload');
  const step = sessionStorage.getItem('marketplace.uploadStep') || '1';
  place(`<h1>Upload a work</h1>
    <p>Step ${esc(step)} of 3. This draft stays on this device.</p>
    <form id="upload-form">
      <div ${step === '1' ? '' : 'hidden'}>
        <label for="file">Picture</label>
        <input id="file" name="file" type="file" accept="image/*" ${step === '1' ? 'required' : ''} />
        <img id="preview" alt="" hidden />
      </div>
      <div ${step === '2' ? '' : 'hidden'}>
        <label for="title">Title</label><input id="title" name="title" required value="${esc(sessionStorage.getItem('marketplace.draftTitle') || '')}" />
        <label for="artist">Artist name</label><input id="artist" name="artist" required value="${esc(sessionStorage.getItem('marketplace.draftArtist') || '')}" />
        <label for="medium">Medium</label><input id="medium" name="medium" required />
        <label for="widthCm">Width, cm</label><input id="widthCm" name="widthCm" inputmode="decimal" required />
        <label for="heightCm">Height, cm</label><input id="heightCm" name="heightCm" inputmode="decimal" required />
        <label for="depthCm">Depth, cm</label><input id="depthCm" name="depthCm" inputmode="decimal" required value="2" />
        <label for="price">Price, USD</label><input id="price" name="price" inputmode="decimal" required />
      </div>
      <div ${step === '3' ? '' : 'hidden'}>
        <h2>On a 300 cm wall</h2>
        <div class="wall-preview" id="wall-preview"></div>
        <p id="scale-note" class="note"></p>
      </div>
      <button type="submit">${step === '3' ? 'Save draft' : 'Continue'}</button>
      <p id="upload-error" class="error" role="alert"></p>
    </form>`, false, () => {
    const file = document.querySelector('#file');
    file?.addEventListener('change', () => {
      const chosen = file.files?.[0];
      if (!chosen) return;
      if (chosen.size > 1500000) {
        document.querySelector('#upload-error').textContent = 'Choose a picture under 1.5 MB.';
        file.value = '';
        return;
      }
      const reader = new FileReader();
      reader.onload = () => {
        sessionStorage.setItem('marketplace.draftImage', reader.result);
        const img = document.querySelector('#preview');
        img.src = reader.result;
        img.hidden = false;
      };
      reader.readAsDataURL(chosen);
    });
    if (step === '3') {
      const width = Number(sessionStorage.getItem('marketplace.draftW') || 0);
      const height = Number(sessionStorage.getItem('marketplace.draftH') || 0);
      const px = Math.max(12, (width / 300) * 280);
      const py = width ? px * (height / width) : 12;
      document.querySelector('#wall-preview').innerHTML = `<span style="width:${px}px;height:${py}px"></span>`;
      document.querySelector('#scale-note').textContent = width
        ? `At ${width} cm wide, the work takes ${Math.round((width / 300) * 100)}% of a 300 cm wall. The camera is what hangs it in a room.`
        : 'Go back and enter the centimetres.';
    }
    document.querySelector('#upload-form').addEventListener('submit', (event) => {
      event.preventDefault();
      const data = Object.fromEntries(new FormData(event.currentTarget));
      if (step === '1') {
        if (!sessionStorage.getItem('marketplace.draftImage')) {
          document.querySelector('#upload-error').textContent = 'Choose a picture first.';
          return;
        }
        sessionStorage.setItem('marketplace.uploadStep', '2');
        renderUpload();
        return;
      }
      if (step === '2') {
        sessionStorage.setItem('marketplace.draftTitle', data.title);
        sessionStorage.setItem('marketplace.draftArtist', data.artist);
        sessionStorage.setItem('marketplace.draftMedium', data.medium);
        sessionStorage.setItem('marketplace.draftW', data.widthCm);
        sessionStorage.setItem('marketplace.draftH', data.heightCm);
        sessionStorage.setItem('marketplace.draftD', data.depthCm);
        sessionStorage.setItem('marketplace.draftPrice', data.price);
        sessionStorage.setItem('marketplace.uploadStep', '3');
        renderUpload();
        return;
      }
      const next = rooms();
      next.inventory.unshift({
        id: crypto.randomUUID?.() || String(Date.now()),
        title: sessionStorage.getItem('marketplace.draftTitle'),
        artist: sessionStorage.getItem('marketplace.draftArtist'),
        medium: sessionStorage.getItem('marketplace.draftMedium'),
        widthCm: Number(sessionStorage.getItem('marketplace.draftW')),
        heightCm: Number(sessionStorage.getItem('marketplace.draftH')),
        depthCm: Number(sessionStorage.getItem('marketplace.draftD')),
        priceCents: Math.round(Number(sessionStorage.getItem('marketplace.draftPrice')) * 100),
        image: sessionStorage.getItem('marketplace.draftImage'),
        status: 'draft',
      });
      keep(next);
      ['marketplace.uploadStep', 'marketplace.draftImage', 'marketplace.draftTitle', 'marketplace.draftArtist', 'marketplace.draftMedium', 'marketplace.draftW', 'marketplace.draftH', 'marketplace.draftD', 'marketplace.draftPrice'].forEach((key) => sessionStorage.removeItem(key));
      location.hash = '#inventory';
    });
  });
}

function renderLedger() {
  const { esc, place, setPage, money } = shop();
  setPage('sheet', 'Ledger');
  const state = rooms();
  const sold = state.inventory.filter((item) => item.status === 'sold');
  const gross = sold.reduce((sum, item) => sum + item.priceCents, 0);
  const fee = Math.round(gross * 0.15);
  place(`<h1>Ledger</h1>
    <p class="badge">Sandbox. Nothing is paid out.</p>
    <p class="total">${esc(money(gross - fee))}</p>
    <p>Artist net after a 15% platform fee of ${esc(money(fee))}, on ${esc(money(gross))} of simulated sales.</p>
    <form id="payout">
      <label for="destination">Payout destination</label>
      <select id="destination" name="kind"><option value="bank">Bank</option><option value="momo">Mobile money</option></select>
      <label for="label">Name on the account, or the phone</label>
      <input id="label" name="label" required value="${esc(state.payout?.label || '')}" />
      <button type="submit">Save destination</button>
    </form>
    <p class="note">${state.payout ? `Saved on this device: ${esc(state.payout.kind)}, ${esc(state.payout.label)}.` : 'No destination yet.'}</p>`, false, () => {
    document.querySelector('#payout').addEventListener('submit', (event) => {
      event.preventDefault();
      const data = Object.fromEntries(new FormData(event.currentTarget));
      const next = rooms();
      next.payout = data;
      keep(next);
      renderLedger();
    });
  });
}

async function renderOrders() {
  const { api, esc, place, setPage, money, signedIn, formError } = shop();
  setPage('sheet', 'Orders');
  if (!signedIn()) {
    place('<h1>Orders</h1><p><a href="#enter">Sign in</a> to see them.</p>', false);
    return;
  }
  try {
    const orders = await api('/orders');
    place(`<h1>Orders</h1>
      <p>An arrival notice stays on this page. It does not open the camera. Payment is still outside this version.</p>
      ${orders.length ? `<div class="stack">${orders.map((order) => `<a class="line-link" href="#receipt/${esc(order.number)}"><strong>${esc(order.number)}</strong><span>${order.status === 'paid' ? 'Paid' : 'Unpaid'} · ${esc(money(order.totalAmount, order.currency))}</span></a>`).join('')}</div>` : '<p>No orders yet.</p>'}`);
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderSettings() {
  const { api, esc, place, setPage, signedIn, formError } = shop();
  setPage('sheet', 'Settings');
  if (!signedIn()) {
    place('<h1>Settings</h1><p><a href="#enter">Sign in</a> to see saved addresses.</p>', false);
    return;
  }
  try {
    const addresses = await api('/me/addresses');
    const state = rooms();
    place(`<h1>Settings</h1>
      <section class="block"><h2>Addresses</h2>
        ${addresses.length ? `<div class="stack">${addresses.map((address) => `<p><strong>${esc(address.recipientName)}</strong><br>${esc(address.line1)}${address.line2 ? `, ${esc(address.line2)}` : ''}<br>${esc(address.city)}${address.region ? `, ${esc(address.region)}` : ''}</p>`).join('')}</div>` : '<p>Save one from the cart.</p>'}
        <p><a href="#cart">Edit delivery in the cart</a></p>
      </section>
      <section class="block"><h2>Payment methods</h2>
        <p>These are the last digits or numbers you typed in the sandbox. The full card number is not stored.</p>
        ${state.methods.length ? `<div class="stack">${state.methods.map((method) => `<p>${esc(method.kind === 'card' ? 'Card' : 'Mobile money')} · ${esc(method.label)}</p>`).join('')}</div>` : '<p>None yet. Finish a sandbox checkout to keep one.</p>'}
      </section>
      <section class="block"><h2>Notices</h2>
        <p>These switches stay on this device. No message is sent.</p>
        <label class="check"><input type="checkbox" id="notice-orders" ${state.notices.orders ? 'checked' : ''} /> Order notices</label>
        <label class="check"><input type="checkbox" id="notice-offers" ${state.notices.offers ? 'checked' : ''} /> Offer replies</label>
      </section>`, false, () => {
      const saveNotice = () => {
        const next = rooms();
        next.notices = {
          orders: document.querySelector('#notice-orders').checked,
          offers: document.querySelector('#notice-offers').checked,
        };
        keep(next);
      };
      document.querySelector('#notice-orders').addEventListener('change', saveNotice);
      document.querySelector('#notice-offers').addEventListener('change', saveNotice);
    });
  } catch (error) {
    place(formError(error), false);
  }
}

function renderAdvisory() {
  const { esc, place, setPage, signedIn } = shop();
  setPage('sheet', 'Advisory');
  if (!signedIn()) {
    place('<h1>Art advisory</h1><p><a href="#enter">Sign in</a> to keep a request with the account on this device.</p>', false);
    return;
  }
  const state = rooms();
  place(`<h1>Art advisory</h1>
    <p>Tell us about the wall. The photo stays in this browser. It is not a picture from the camera.</p>
    <form id="advice">
      <label for="room">Which wall</label>
      <input id="room" name="room" required />
      <label for="note">What you want to live with</label>
      <textarea id="note" name="note" required></textarea>
      <label for="wall">Photo of the wall</label>
      <input id="wall" name="wall" type="file" accept="image/*" />
      <button type="submit">Save request</button>
      <p id="advice-error" class="error" role="alert"></p>
    </form>
    <div class="stack">${state.advisory.map((item) => `<article><strong>${esc(item.room)}</strong><p>${esc(item.note)}</p>${item.photo ? `<img src="${item.photo}" alt="Wall for ${esc(item.room)}" />` : ''}</article>`).join('')}</div>`, false, () => {
    document.querySelector('#advice').addEventListener('submit', (event) => {
      event.preventDefault();
      const data = new FormData(event.currentTarget);
      const file = data.get('wall');
      const finish = (photo) => {
        const next = rooms();
        next.advisory.unshift({ id: String(Date.now()), room: data.get('room'), note: data.get('note'), photo });
        keep(next);
        renderAdvisory();
      };
      if (file && file.size) {
        if (file.size > 1500000) {
          document.querySelector('#advice-error').textContent = 'Choose a photo under 1.5 MB.';
          return;
        }
        const reader = new FileReader();
        reader.onload = () => finish(reader.result);
        reader.readAsDataURL(file);
        return;
      }
      finish('');
    });
  });
}

function shake(form, message) {
  form.classList.remove('shake');
  void form.offsetWidth;
  form.classList.add('shake');
  const error = form.querySelector('.error');
  if (error) error.textContent = message;
}

async function renderCheckout() {
  const { api, esc, place, setPage, money, signedIn, formError, rateNote, sessionId } = shop();
  setPage('sheet', 'Checkout');
  if (!signedIn()) {
    place('<h1>Checkout</h1><p><a href="#enter">Sign in</a> to place the sandbox order.</p>', false);
    return;
  }
  let cart;
  let addresses;
  try {
    [cart, addresses] = await Promise.all([api('/cart'), api('/me/addresses')]);
  } catch (error) {
    place(formError(error), false);
    return;
  }
  if (!cart.items.length) {
    place('<h1>Checkout</h1><p>The cart is empty. <a href="#gallery">Choose a work.</a></p>', false);
    return;
  }
  const crowded = cart.items.find((item) => item.quantity > 1);
  if (crowded) {
    place(`<h1>Checkout</h1><p>${esc(crowded.title)} is in the cart more than once. This sandbox treats each work as a single print. <a href="#cart">Lower it to one.</a></p>`, false);
    return;
  }
  const addressId = sessionStorage.getItem('marketplace.checkoutAddress');
  const address = addresses.find((item) => item.id === addressId);
  if (!address) {
    place('<h1>Checkout</h1><p>Choose a delivery address in the cart first. <a href="#cart">Back to the cart.</a></p>', false);
    return;
  }
  const tier = sessionStorage.getItem('marketplace.shipTier') || 'kigali';
  place(`<h1>Checkout</h1>
    <p class="badge">Sandbox / verification mode. No funds deducted.</p>
    <p>${cart.items.map((item) => esc(item.title)).join(', ')}</p>
    <p class="total">${esc(money(cart.totalAmount, cart.currency))}</p>
    ${rateNote()}
    <p>Ship to ${esc(address.recipientName)}, ${esc(address.city)}${address.region ? `, ${esc(address.region)}` : ''}.</p>
    <form id="pay-form">
      <label for="tier">Shipping guide</label>
      <select id="tier" name="tier">
        <option value="kigali" ${tier === 'kigali' ? 'selected' : ''}>Kigali rider, guide ${esc(money(shipTiers.kigali.cents))}</option>
        <option value="province" ${tier === 'province' ? 'selected' : ''}>Another province, guide ${esc(money(shipTiers.province.cents))}</option>
      </select>
      <p class="note">The guide is not added to the recorded order.</p>
      <div class="segment" role="group" aria-label="Payment check">
        <button type="button" id="tab-card" aria-pressed="true">Card</button>
        <button type="button" id="tab-momo" aria-pressed="false">Mobile money</button>
      </div>
      <div id="card-fields">
        <label for="pan">Card number</label>
        <input id="pan" name="pan" inputmode="numeric" autocomplete="off" />
        <label for="exp">Expiry</label>
        <input id="exp" name="exp" placeholder="MM/YY" autocomplete="off" />
        <label for="cvv">Security code</label>
        <input id="cvv" name="cvv" inputmode="numeric" autocomplete="off" />
      </div>
      <div id="momo-fields" hidden>
        <label for="msisdn">Mobile money number</label>
        <input id="msisdn" name="msisdn" inputmode="tel" placeholder="0788123456" />
        <p class="note">MTN 078 or 079, Airtel 072 or 073.</p>
      </div>
      <button type="submit" id="authorize">Verify and place unpaid order</button>
      <p class="error" role="alert"></p>
    </form>
    <dialog id="momo-dialog" class="sheet-dialog">
      <h2>Approve the prompt</h2>
      <p>Approve ${esc(money(0))} verification prompt on your handset. This check moves no money. The order total above is recorded and stays unpaid.</p>
      <div class="actions">
        <button type="button" id="momo-yes">Approve</button>
        <button type="button" id="momo-no" class="quiet">Deny</button>
      </div>
    </dialog>
    <p id="authorizing" class="settle" hidden>Checking the sandbox…</p>`, false, () => {
    let method = 'card';
    const form = document.querySelector('#pay-form');
    document.querySelector('#tab-card').addEventListener('click', () => {
      method = 'card';
      document.querySelector('#tab-card').setAttribute('aria-pressed', 'true');
      document.querySelector('#tab-momo').setAttribute('aria-pressed', 'false');
      document.querySelector('#card-fields').hidden = false;
      document.querySelector('#momo-fields').hidden = true;
    });
    document.querySelector('#tab-momo').addEventListener('click', () => {
      method = 'momo';
      document.querySelector('#tab-momo').setAttribute('aria-pressed', 'true');
      document.querySelector('#tab-card').setAttribute('aria-pressed', 'false');
      document.querySelector('#card-fields').hidden = true;
      document.querySelector('#momo-fields').hidden = false;
    });
    const placeOrder = async (label) => {
      document.querySelector('#authorizing').hidden = false;
      document.querySelector('#authorize').disabled = true;
      const wait = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 1500;
      await new Promise((resolve) => setTimeout(resolve, wait));
      try {
        const order = await api('/checkout', { method: 'POST', body: JSON.stringify({ addressId, sessionId: sessionId() }) });
        const next = rooms();
        for (const item of cart.items) next.reserved[item.sourceId] = { orderNumber: order.number, at: new Date().toISOString() };
        next.methods = [{ kind: method, label }, ...next.methods.filter((item) => item.label !== label)].slice(0, 4);
        keep(next);
        const meta = JSON.parse(sessionStorage.getItem('marketplace.receipts') || '{}');
        meta[order.number] = { method, label, tier: document.querySelector('#tier').value };
        sessionStorage.setItem('marketplace.receipts', JSON.stringify(meta));
        sessionStorage.setItem('marketplace.lastOrder', order.number);
        await shop().refreshCartCount();
        location.hash = `#receipt/${order.number}`;
      } catch (error) {
        document.querySelector('#authorizing').hidden = true;
        document.querySelector('#authorize').disabled = false;
        shake(form, error.message);
      }
    };
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      sessionStorage.setItem('marketplace.shipTier', document.querySelector('#tier').value);
      if (method === 'card') {
        const pan = document.querySelector('#pan').value;
        const digits = pan.replace(/\D/g, '');
        if (!luhn(pan)) {
          shake(form, 'That card number does not pass the check. Try the number again.');
          return;
        }
        if (!expiryOk(document.querySelector('#exp').value)) {
          shake(form, 'That expiry is missing or already past. Use a future MM/YY.');
          return;
        }
        const cvv = document.querySelector('#cvv').value.replace(/\D/g, '');
        const needed = digits.length === 15 ? 4 : 3;
        if (cvv.length !== needed) {
          shake(form, needed === 4 ? 'A 15-digit card needs a 4-digit security code.' : 'The security code needs 3 digits.');
          return;
        }
        await placeOrder(`•••• ${digits.slice(-4)}`);
        return;
      }
      const phone = document.querySelector('#msisdn').value;
      if (!momoOk(phone)) {
        shake(form, 'That mobile number does not match a Rwanda MoMo prefix. Use 072, 073, 078, or 079.');
        return;
      }
      const dialog = document.querySelector('#momo-dialog');
      dialog.showModal();
      document.querySelector('#momo-yes').onclick = async () => {
        dialog.close();
        await placeOrder(phone.replace(/\s/g, ''));
      };
      document.querySelector('#momo-no').onclick = () => {
        dialog.close();
        shake(form, 'The handset prompt was denied. Nothing was reserved.');
      };
    });
  });
}

async function renderReceipt(number) {
  const { api, esc, place, setPage, money, signedIn, formError } = shop();
  setPage('sheet', 'Receipt');
  if (!signedIn()) {
    place('<h1>Receipt</h1><p><a href="#enter">Sign in</a> to open it.</p>', false);
    return;
  }
  try {
    const orders = await api('/orders');
    const order = orders.find((item) => item.number === number);
    if (!order) {
      place('<h1>That order is not on this account.</h1>', false);
      return;
    }
    const meta = JSON.parse(sessionStorage.getItem('marketplace.receipts') || '{}')[number];
    const guide = meta ? shipTiers[meta.tier] : null;
    place(`<article class="receipt">
      <p class="badge">Sandbox / verification mode. No funds deducted.</p>
      <h1>${esc(order.number)}</h1>
      <p>${order.status === 'paid' ? 'Paid' : 'Unpaid'} · ${esc(money(order.totalAmount, order.currency))}</p>
      ${shop().rateNote()}
      <p>${meta ? `${meta.method === 'card' ? 'Card' : 'Mobile money'} ${esc(meta.label)}` : 'Payment method was not recorded in this browser.'}</p>
      ${guide ? `<p>Shipping guide ${esc(guide.label)}, ${esc(money(guide.cents))}. Not charged.</p>` : ''}
      <div class="stack">${order.items.map((item) => `<p><strong>${esc(item.title)}</strong><br>${esc(item.widthCm)} × ${esc(item.heightCm)} × ${esc(item.depthCm)} cm · Reserved in this browser, simulated.</p>`).join('')}</div>
      <ol class="timeline">
        <li>Order recorded. Nothing was deducted.</li>
        <li>The print is reserved in this browser only. The catalogue and the camera still have it.</li>
        <li>An arrival notice, when one exists, stays on the orders page and does not open the camera.</li>
      </ol>
      <p><a href="#orders">All orders</a></p>
    </article>`);
  } catch (error) {
    place(formError(error), false);
  }
}

window.renderDiscover = renderDiscover;
window.renderRoom = (route) => {
  if (route === '#search') {
    renderSearch();
    return true;
  }
  if (route === '#collections') {
    renderCollections();
    return true;
  }
  if (route.startsWith('#collection/')) {
    renderCollection(route.slice('#collection/'.length));
    return true;
  }
  if (route.startsWith('#artist/')) {
    renderArtist(route.slice('#artist/'.length));
    return true;
  }
  if (route === '#saved') {
    renderSaved();
    return true;
  }
  if (
    route.startsWith('#moodboard/') ||
    route.startsWith('#offer/') ||
    route === '#offers' ||
    route.startsWith('#thread/') ||
    route === '#inventory' ||
    route === '#upload' ||
    route === '#ledger' ||
    route === '#settings' ||
    route === '#advisory'
  ) {
    location.replace('#gallery');
    return true;
  }
  if (route === '#studio') {
    renderStudio();
    return true;
  }
  if (route === '#orders') {
    renderOrders();
    return true;
  }
  if (route === '#checkout') {
    renderCheckout();
    return true;
  }
  if (route.startsWith('#receipt/')) {
    renderReceipt(route.slice('#receipt/'.length));
    return true;
  }
  return false;
};
