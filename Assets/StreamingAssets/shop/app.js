const apiMeta = document.querySelector('meta[name="cinnarios-api"]')?.getAttribute('content') || '';
const API = window.CINNARIOS_PHONE || apiMeta === 'same'
  ? `${location.origin}/v1`
  : `${location.protocol}//${location.hostname}:3001/v1`;
const app = document.querySelector('#app');
const cartCount = document.querySelector('#cart-count');
const session = new window.Cinnarios.Session(API);
// One shared copy of the 500 paintings per visit; fetched outside any page's cancel signal so every page can use it.
const catalog = new window.Cinnarios.CatalogCache(async () => {
  const response = await fetch(`${API}/catalog/artworks`, { credentials: 'include' });
  if (!response.ok) throw new Error('The paintings could not be loaded.');
  return response.json();
});

const sessionKey = 'marketplace.session';

function newId() {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = [...bytes].map((byte) => byte.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

if (!localStorage.getItem(sessionKey)) localStorage.setItem(sessionKey, newId());

function sessionId() {
  return localStorage.getItem(sessionKey);
}

function esc(value) {
  return String(value ?? '').replace(/[&<>"']/g, (character) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
  })[character]);
}

/** National Bank of Rwanda average for 17 Nov 2026: midpoint of the USD buying and selling rates. */
const USD_TO_RWF = 1477.385;

const RW_PROVINCES = {
  'Kigali City': ['Gasabo', 'Kicukiro', 'Nyarugenge'],
  'Northern Province': ['Burera', 'Gakenke', 'Gicumbi', 'Musanze', 'Rulindo'],
  'Southern Province': ['Gisagara', 'Huye', 'Kamonyi', 'Muhanga', 'Nyamagabe', 'Nyanza', 'Nyaruguru', 'Ruhango'],
  'Eastern Province': ['Bugesera', 'Gatsibo', 'Kayonza', 'Kirehe', 'Ngoma', 'Nyagatare', 'Rwamagana'],
  'Western Province': ['Karongi', 'Ngororero', 'Nyabihu', 'Nyamasheke', 'Rubavu', 'Rusizi', 'Rutsiro'],
};

/** Every price shows in Rwandan francs. Prices recorded in dollars are converted at USD_TO_RWF. */
function money(cents, currency = 'USD') {
  const amount = Number(cents) / 100;
  const francs = String(currency).trim() === 'RWF' ? amount : amount * USD_TO_RWF;
  return `RWF ${new Intl.NumberFormat('en-RW', { maximumFractionDigits: 0 }).format(Math.round(francs))}`;
}

function rateNote() {
  return '<p class="note">Shown in francs at the National Bank of Rwanda average for 17 Nov 2026: 1 USD = 1,477.385 RWF. The price is still recorded in dollars.</p>';
}

// Each page visit gets its own signal. Moving to another page cancels the reads still in
// flight for the old one, so a slow page can never finish late and replace the new page.
let visit = new AbortController();

function newVisit() {
  visit.abort();
  visit = new AbortController();
}

async function api(path, options = {}) {
  const reading = !options.method || options.method === 'GET';
  if (reading && path === '/catalog/artworks') {
    const asked = visit;
    const list = await catalog.all();
    // A page the buyer has already left never draws.
    return asked.signal.aborted ? new Promise(() => {}) : list;
  }
  const headers = { ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(options.headers || {}) };
  const token = session.token();
  if (token) headers.Authorization = `Bearer ${token}`;
  const signal = reading ? visit.signal : undefined;
  let response;
  try {
    response = await fetch(API + path, { ...options, headers, credentials: 'include', signal });
  } catch (error) {
    // A read for a page the buyer has left: never settle, so that page never draws.
    if (error.name === 'AbortError') return new Promise(() => {});
    throw error;
  }
  if (response.status === 401 && token && !path.startsWith('/auth/')) {
    // Expired: every request that lands here waits for the same single renewal, then tries once more.
    if (await session.renew()) {
      headers.Authorization = `Bearer ${session.token()}`;
      response = await fetch(API + path, { ...options, headers, credentials: 'include', signal });
    }
  }
  const body = await response.json().catch(() => ({}));
  if (!response.ok) {
    const error = new Error(body.message || 'The request failed.');
    error.code = body.code;
    throw error;
  }
  return body;
}

function signedIn() {
  return session.signedIn();
}

async function refreshCartCount() {
  if (!signedIn()) {
    cartCount.textContent = '';
    return;
  }
  try {
    const cart = await api('/cart');
    const next = cart.items.length ? `(${cart.items.length})` : '';
    if (cartCount.textContent !== next) {
      cartCount.textContent = next;
      if (next && motionOk()) {
        cartCount.animate([{ opacity: 0.2 }, { opacity: 1 }], { duration: 180, easing: 'cubic-bezier(0.16, 1, 0.3, 1)' });
      }
    }
  } catch {
    cartCount.textContent = '';
  }
}

function formError(error) {
  return window.Cinnarios.PageStates.error(error, esc);
}

async function journey(kind, sourceId) {
  if (!signedIn()) return;
  await api('/me/journey', {
    method: 'POST',
    body: JSON.stringify({ kind, sourceId, sessionId: sessionId() }),
  });
}

let painted = false;
let carriedWork = null;

function motionOk() {
  return !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

// If a page takes more than a moment, grey placeholders show until it arrives.
let loadingTimer = 0;

function showLoadingSoon() {
  clearTimeout(loadingTimer);
  loadingTimer = setTimeout(() => {
    app.innerHTML = window.Cinnarios.PageStates.loading();
  }, 180);
}

function place(html, carry, after) {
  clearTimeout(loadingTimer);
  const apply = () => {
    app.innerHTML = html;
    after?.();
  };
  if (!painted || !motionOk() || !document.startViewTransition) {
    apply();
    painted = true;
    return;
  }
  if (!carry) {
    document.querySelectorAll('#app img').forEach((img) => {
      img.style.viewTransitionName = 'none';
    });
  }
  try {
    document.startViewTransition(apply);
  } catch {
    apply();
  }
  painted = true;
}

function listOrigin() {
  const route = location.hash || '#home';
  if (route.startsWith('#search')) return '#search';
  if (route.startsWith('#saved')) return '#saved';
  if (route.startsWith('#home')) return '#home';
  if (
    route.startsWith('#gallery') ||
    route.startsWith('#discover') ||
    route.startsWith('#collection') ||
    route.startsWith('#artist')
  ) return '#gallery';
  return null;
}

function pieceOrigin() {
  const saved = sessionStorage.getItem('marketplace.origin');
  if (saved === '#home' || saved === '#gallery' || saved === '#search' || saved === '#saved') return saved;
  return '#home';
}

function markNav() {
  const route = location.hash || '#home';
  let tab = '#home';
  if (route.startsWith('#work/') || route.startsWith('#try/')) {
    const from = route.split('/')[2];
    tab = from === 'search' ? '#search'
      : from === 'saved' ? '#saved'
      : from === 'home' ? '#home'
      : from === 'discover' ? '#gallery'
      : pieceOrigin();
  }
  else if (route.startsWith('#search')) tab = '#search';
  else if (
    route.startsWith('#gallery') ||
    route.startsWith('#discover') ||
    route.startsWith('#collection') ||
    route.startsWith('#artist')
  ) tab = '#gallery';
  else if (
    route.startsWith('#saved') ||
    route.startsWith('#moodboard') ||
    route.startsWith('#offers') ||
    route.startsWith('#thread') ||
    route.startsWith('#offer/')
  ) tab = '#saved';
  else if (
    route.startsWith('#studio') ||
    route.startsWith('#account') ||
    route.startsWith('#orders') ||
    route.startsWith('#receipt') ||
    route === '#enter' ||
    route === '#join' ||
    route === '#verify' ||
    route === '#forgot'
  ) tab = '#account';
  document.querySelectorAll('[data-tab]').forEach((link) => {
    if (link.getAttribute('href') === tab) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
  const onCart = route === '#cart' || route === '#checkout';
  document.querySelector('#cart-link')?.toggleAttribute('aria-current', onCart);
}

function setPage(mode, title) {
  document.body.dataset.page = mode;
  document.title = title ? `${title} · Cinnarios` : 'Cinnarios';
  let back = document.querySelector('#back');
  if (mode === 'sheet') {
    if (!back) {
      back = document.createElement('a');
      back.id = 'back';
      back.href = '#home';
      back.textContent = 'Back';
      document.querySelector('.bar')?.prepend(back);
    }
  } else if (back) {
    back.remove();
  }
}

document.addEventListener('click', (event) => {
  const link = event.target.closest('a[href^="#"]');
  if (!link) return;
  const href = link.getAttribute('href');
  if (href.startsWith('#work/')) {
    const origin = listOrigin();
    if (origin) sessionStorage.setItem('marketplace.origin', origin);
    carriedWork = href.slice('#work/'.length).split('/')[0];
    const image = link.querySelector('img');
    if (image) image.style.viewTransitionName = `work-${carriedWork}`;
    return;
  }
  carriedWork = null;
});

let libraryReady = false;
let libraryTimer = 0;

function accountIdFromToken(token) {
  try {
    const body = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = body + '='.repeat((4 - (body.length % 4)) % 4);
    const payload = JSON.parse(atob(padded));
    return typeof payload.sub === 'string' ? payload.sub : '';
  } catch {
    return '';
  }
}

/** Points the saved-painting store at the signed-in account, or at the signed-out store. */
function bindLibraryAccount() {
  const id = session.signedIn() ? accountIdFromToken(session.token()) : '';
  if (window.Rooms.accountId() === id) return;
  clearTimeout(libraryTimer);
  window.Rooms.useAccount(id);
  libraryReady = false;
}

function syncLibrary() {
  if (!signedIn()) return;
  const id = accountIdFromToken(session.token());
  if (!id || window.Rooms.accountId() !== id) return;
  clearTimeout(libraryTimer);
  libraryTimer = setTimeout(() => {
    if (!signedIn() || window.Rooms.accountId() !== id) return;
    api('/me/library', { method: 'PUT', body: JSON.stringify(window.Rooms.snapshot()) }).catch(() => {});
  }, 400);
}

async function pullLibrary() {
  const remote = await api('/me/library');
  const local = window.Rooms.snapshot();
  const saved = [...new Set([...(remote.saved || []), ...local.saved])];
  const tries = [...(remote.tries || [])];
  const seen = new Set(tries.map((item) => `${item.sourceId}|${item.at}`));
  for (const item of local.tries) {
    if (!seen.has(`${item.sourceId}|${item.at}`)) tries.push(item);
  }
  window.Rooms.replaceLibrary(saved, tries);
  if (saved.length !== (remote.saved || []).length || tries.length !== (remote.tries || []).length) {
    await api('/me/library', { method: 'PUT', body: JSON.stringify(window.Rooms.snapshot()) });
  }
}

async function ensureLibrary() {
  bindLibraryAccount();
  if (!signedIn() || !window.Rooms.accountId() || libraryReady) return;
  libraryReady = true;
  try {
    await pullLibrary();
  } catch {
    libraryReady = false;
  }
}

async function render() {
  await ensureLibrary();
  newVisit();
  markNav();
  const route = location.hash || '#home';
  showLoadingSoon();
  if (route === '#home') return renderHome();
  if (route.startsWith('#work/')) return renderWork(route.slice('#work/'.length).split('/')[0], route.split('/')[2]);
  if (route.startsWith('#try/')) return renderTry(route.slice('#try/'.length).split('/')[0]);
  if (route === '#cart') return renderCart();
  if (route === '#checkout') return renderCheckout();
  if (route.startsWith('#receipt/')) return renderReceipt(decodeURIComponent(route.slice('#receipt/'.length)));
  if (route === '#saved') return renderSaved();
  if (route === '#try-own') return renderTryOwn();
  if (route === '#account') return renderAccount();
  if (route === '#privacy') return renderPrivacy();
  if (route === '#enter') return renderAuth();
  if (route === '#join') return renderAuth('up');
  if (route === '#verify') return renderVerify();
  if (route === '#forgot' || route === '#reset') return renderForgot();
  if (route === '#search') return renderSearch();
  if (route === '#gallery' || route === '#discover') return renderDiscover();
  if (window.renderRoom?.(route)) return undefined;
  return renderGallery();
}

/** Keeps the access token from a sign-in, or explains why it could not finish. */
function acceptSession(result) {
  if (result.mfaRequired) {
    throw new Error('This account still has the old two-step sign-in turned on, which this version no longer offers.');
  }
  session.keep(result.accessToken);
  if (result.user) sessionStorage.setItem('marketplace.emailVerified', result.user.emailVerifiedAt ? 'yes' : 'no');
  bindLibraryAccount();
}

async function emailConfirmed() {
  if (sessionStorage.getItem('marketplace.emailVerified') === 'yes') return true;
  const me = await api('/me');
  const yes = Boolean(me.emailVerifiedAt);
  sessionStorage.setItem('marketplace.emailVerified', yes ? 'yes' : 'no');
  if (me.email) sessionStorage.setItem('cinnarios.verifyEmail', me.email);
  return yes;
}

/** Puts Google's own sign-in button into a slot, when the server has a Google client id. */
function mountGoogle(slot) {
  api('/auth/providers').then((providers) => {
    if (!providers.googleClientId) return;
    slot.replaceChildren();
    const start = () => {
      const google = window.google;
      if (!google?.accounts?.id) return;
      google.accounts.id.initialize({
        client_id: providers.googleClientId,
        callback: async (response) => {
          try {
            acceptSession(await api('/auth/oauth/google', { method: 'POST', body: JSON.stringify({ idToken: response.credential }) }));
            location.hash = '#account';
          } catch (error) {
            const node = slot.closest('form')?.querySelector('[data-error]');
            if (node) node.textContent = error.message;
          }
        },
      });
      google.accounts.id.renderButton(slot, { theme: 'outline', size: 'large', text: 'continue_with', shape: 'pill', width: Math.min(slot.clientWidth || 320, 400) });
    };
    if (window.google?.accounts?.id) {
      start();
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.onload = start;
    document.head.appendChild(script);
  }).catch(() => { /* Email sign-in still works without Google. */ });
}

function renderAuth(start = 'in') {
  setPage('visit', start === 'up' ? 'Create account' : 'Sign in');
  const page = new window.Cinnarios.SignInPage({
    signIn: async ({ email, password }) => {
      const result = await api('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
      acceptSession(result);
      await refreshCartCount();
      sessionStorage.setItem('cinnarios.verifyEmail', email);
      location.hash = result.user?.emailVerifiedAt ? '#account' : '#verify';
    },
    create: async ({ name, email, password }) => {
      const result = await api('/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) });
      acceptSession(result);
      try {
        await api('/me/collector', { method: 'PATCH', body: JSON.stringify({ displayName: name.trim() }) });
      } catch {
        /* The name can be set later from Account. */
      }
      await refreshCartCount();
      sessionStorage.setItem('cinnarios.verifyEmail', email);
      location.hash = result.user?.emailVerifiedAt ? '#account' : '#verify';
    },
    mountGoogle,
    pictures: async () => (await api('/catalog/artworks')).slice(0, 2).map((work) => work.thumbnailUrl || work.imageUrl),
  }, { esc }, start);
  place(page.html(), false, () => page.bind());
}

async function renderVerify() {
  setPage('visit', 'Confirm your email');
  if (!signedIn()) {
    location.hash = '#enter';
    return;
  }
  if (await emailConfirmed()) {
    location.hash = '#account';
    return;
  }
  const email = sessionStorage.getItem('cinnarios.verifyEmail') || 'your email';
  const page = new window.Cinnarios.VerifyPage(email, {
    verify: async (code) => {
      await api('/auth/email/confirm', { method: 'POST', body: JSON.stringify({ code }) });
      sessionStorage.setItem('marketplace.emailVerified', 'yes');
      sessionStorage.removeItem('cinnarios.verifyEmail');
    },
    cancel: async () => {
      session.forget();
      sessionStorage.removeItem('marketplace.emailVerified');
      location.hash = '#join';
    },
    send: async () => {
      await api('/auth/email/send', { method: 'POST', body: '{}' });
    },
  }, { esc });
  place(page.html(), false, () => page.bind());
}

function renderForgot() {
  setPage('visit', 'Reset password');
  const page = new window.Cinnarios.ResetPage({
    sendCode: async (email) => {
      await api('/auth/password/forgot', { method: 'POST', body: JSON.stringify({ email }) });
    },
    checkCode: (email, code) => api('/auth/password/code', { method: 'POST', body: JSON.stringify({ email, code }) }),
    reset: (email, code, password) => api('/auth/password/reset', { method: 'POST', body: JSON.stringify({ email, code, password }) }),
  }, { esc });
  place(page.html(), false, () => page.bind());
}

async function loadConfidence(sourceId) {
  if (!signedIn()) return null;
  try {
    const report = await api('/me/confidence');
    return sourceId ? report.artworks.find((work) => work.sourceId === sourceId) ?? null : report;
  } catch {
    return null;
  }
}

async function renderGallery() {
  setPage('visit', 'Works');
  try {
    const works = await api('/catalog/artworks');
    place(`<h1>The size the museum recorded.</h1>
      <p class="intro">Public-domain paintings from The Met. The price is a reproduction at that size.</p>
      <div class="grid">${works.map((work) => `
      <a class="work" href="#work/${esc(work.sourceId)}">
        <span class="frame"><img src="${esc(work.thumbnailUrl)}" alt="" loading="lazy" decoding="async" style="${work.sourceId === carriedWork ? `view-transition-name: work-${esc(work.sourceId)}` : ''}" /></span>
        <strong>${esc(work.title)}</strong>
        <span>${esc(work.artist || 'Unknown artist')}</span>
        <span class="measures">${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span>
      </a>`).join('')}</div>`, carriedWork);
  } catch (error) {
    place(formError(error), false);
  }
}

async function openPieceInSpace(sourceId) {
  let apiOrigin = location.origin;
  if (!window.CINNARIOS_PHONE) {
    const host = await fetch('/lan').then((response) => response.json());
    apiOrigin = `http://${host.address}:3001`;
  }
  const cameraHref = `cinnarios-ar://try/${sourceId}?api=${encodeURIComponent(apiOrigin)}`;
  const call = window.Unity && window.Unity.call;
  if (typeof call === 'function') {
    call(cameraHref);
    return;
  }
  window.location.href = cameraHref;
}

function backHref(from) {
  if (from === 'search') return '#search';
  if (from === 'discover') return '#gallery';
  if (from === 'saved') return '#saved';
  if (from === 'home') return '#home';
  return pieceOrigin();
}

function describeFeel(work) {
  return { subject: window.Rooms.subjectName(work), mood: window.Rooms.moodName(work), colour: window.Rooms.colourName(work) };
}

async function renderHome() {
  setPage('visit', 'Home');
  try {
    const catalog = await api('/catalog/artworks');
    const page = new window.Cinnarios.HomePage(catalog, describeFeel, { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderDiscover() {
  setPage('visit', 'Discover');
  try {
    const catalog = await api('/catalog/artworks');
    const picks = new window.Cinnarios.DiscoverySampler(describeFeel).pick(catalog, { count: 18, triedIds: window.Rooms.triedIds() });
    const page = new window.Cinnarios.DiscoverPage(picks, catalog.length, { esc });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderSearch() {
  setPage('visit', 'Search');
  try {
    const catalog = await api('/catalog/artworks');
    const page = new window.Cinnarios.SearchPage(catalog, new window.Cinnarios.CatalogSearch(describeFeel), { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

/** Notes that the buyer opened this painting. Runs after the page is drawn, so the buyer never waits for it. */
async function recordView(sourceId) {
  if (!signedIn()) return;
  try {
    const prior = await loadConfidence(sourceId);
    const seen = new Set(prior?.signals ?? []);
    await journey('artwork_viewed', sourceId);
    if (!seen.has('credit_reviewed')) await journey('credit_reviewed', sourceId);
  } catch {
    /* A missed note never affects the page. */
  }
}

async function renderWork(sourceId, from) {
  carriedWork = sourceId;
  try {
    const list = await api('/catalog/artworks');
    const work = list.find((item) => String(item.sourceId) === String(sourceId)) || await api(`/catalog/artworks/${sourceId}`);
    setPage('visit', work.title);
    const reserved = window.Rooms.isReserved(sourceId);
    const page = new window.Cinnarios.PiecePage({
      work,
      feel: describeFeel(work),
      similar: new window.Cinnarios.SimilarityRanker(describeFeel).rank(work, list, 5),
      from,
      backHref: backHref(from),
      saved: window.Rooms.isSaved(sourceId),
      canAdd: signedIn() && !reserved,
      extra: window.Rooms.workFacts(work) + (signedIn() ? '' : '<p class="piece-note"><a href="#enter">Sign in</a> to keep a cart.</p>'),
    }, { esc, money });
    place(page.html(), sourceId, () => {
      recordView(sourceId);
      window.Rooms.bindWork(work);
      page.bind({
        openCamera: () => openPieceInSpace(sourceId),
        toggleSaved: () => window.Rooms.toggleSaved(sourceId),
        addToCart: async () => {
          if (window.Rooms.isReserved(sourceId)) {
            throw new Error('This print is already reserved in this browser. Release it on this page to add it again.');
          }
          await api('/cart/items', { method: 'POST', body: JSON.stringify({ sourceId, quantity: 1 }) });
          await refreshCartCount();
          location.hash = '#cart';
        },
      });
    });
  } catch (error) {
    setPage('visit', 'Work');
    place(formError(error), false);
  }
}

async function showPhone(sourceId) {
  document.querySelector('#handoff').hidden = false;
  const host = window.CINNARIOS_PHONE ? null : await fetch('/lan').then((response) => response.json());
  const target = window.CINNARIOS_PHONE
    ? `${location.origin}/#try/${sourceId}`
    : `http://${host.address}:3000/#try/${sourceId}`;
  const link = document.querySelector('#phone-link');
  link.href = target;
  link.textContent = target;
  document.querySelector('#phone-qr').src = `/qr.svg?text=${encodeURIComponent(target)}`;
}

async function renderTry(sourceId) {
  carriedWork = sourceId;
  try {
    const work = await api(`/catalog/artworks/${sourceId}`);
    setPage('visit', work.title);
    const current = await loadConfidence(sourceId);
    const host = window.CINNARIOS_PHONE ? null : await fetch('/lan').then((response) => response.json());
    const apiOrigin = window.CINNARIOS_PHONE ? location.origin : `http://${host.address}:3001`;
    const cameraHref = `cinnarios-ar://try/${sourceId}?api=${encodeURIComponent(apiOrigin)}`;
    place(`
      <article class="detail">
        <img src="${esc(work.imageUrl)}" alt="${esc(work.title)}" />
        <div class="facts">
          <h1>${esc(work.title)}</h1>
          <p>${esc(work.artist || 'Unknown artist')}${work.date ? `, ${esc(work.date)}` : ''}</p>
          <p class="measures">${esc(work.widthCm)} × ${esc(work.heightCm)} × ${esc(work.depthCm)} cm</p>
          <p class="price">${esc(money(work.priceAmount, work.currency))}</p>
          ${rateNote()}
          <p>${esc(work.creditLine)}</p>
          <p>${esc(work.mediumDescription)}</p>
          <div class="actions">
            <a class="button" id="open-camera" href="${esc(cameraHref)}">Open in camera</a>
          </div>
          <p class="note">This opens the wall on the phone. The question starts after the painting is locked.</p>
          <section class="block" id="decision" hidden>
            <h2>This try</h2>
            <p id="decision-line"></p>
          </section>
          <p id="try-error" class="error" role="alert"></p>
        </div>
      </article>`, sourceId, () => window.Rooms?.bindTry(work, cameraHref));
  } catch (error) {
    setPage('visit', 'Try on');
    place(formError(error), false);
  }
}

async function renderCart() {
  carriedWork = null;
  setPage('visit', 'Cart');
  if (!signedIn()) {
    place('<section class="cart-page"><h1>Cart</h1><div class="empty-state"><h2>Sign in to keep a cart</h2><p>Your cart stays with your account, on the phone and on the computer.</p><a class="button" href="#enter">Sign in</a></div></section>', false);
    return;
  }
  try {
    const cart = await api('/cart');
    const page = new window.Cinnarios.CartPage(cart, {
      setQuantity: async (id, quantity) => {
        await api(`/cart/items/${id}`, { method: 'PATCH', body: JSON.stringify({ quantity }) });
        await refreshCartCount();
        return api('/cart');
      },
      remove: async (id) => {
        await api(`/cart/items/${id}`, { method: 'DELETE' });
        await refreshCartCount();
        return api('/cart');
      },
    }, { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderCheckout() {
  setPage('visit', 'Check out');
  if (!signedIn()) {
    location.hash = '#enter';
    return;
  }
  try {
    const [loaded, addresses] = await Promise.all([api('/cart'), api('/me/addresses')]);
    const cart = loaded;
    if (!cart.items.length) {
      location.hash = '#cart';
      return;
    }
    const page = new window.Cinnarios.CheckoutPage(cart, addresses[addresses.length - 1] || null, RW_PROVINCES, {
      placeOrder: async (address, tierId) => {
        const saved = await api('/me/addresses', { method: 'POST', body: JSON.stringify(address) });
        const created = await api('/checkout', { method: 'POST', body: JSON.stringify({ addressId: saved.id, sessionId: sessionId() }) });
        window.Cinnarios.Delivery.rememberForOrder(created.number, tierId);
        await refreshCartCount();
        sessionStorage.setItem('marketplace.lastOrder', created.number);
        location.hash = `#receipt/${created.number}`;
      },
    }, { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderReceipt(number) {
  setPage('visit', 'Order placed');
  try {
    const order = (await api('/orders')).find((item) => item.number === number);
    if (!order) throw new Error('That order is not on this account.');
    const page = new window.Cinnarios.OrderPlacedPage(order, window.Cinnarios.Delivery.forOrder(number), { esc, money });
    place(page.html(), false);
  } catch (error) {
    place(formError(error), false);
  }
}

function renderTryOwn() {
  setPage('visit', 'Try your own piece');
  const page = new window.Cinnarios.TryOwnPage({
    onPhone: Boolean(window.CINNARIOS_PHONE),
    // The photo goes only to the phone app's own server, which hands it to the camera.
    openCamera: async (print, photo) => {
      const response = await fetch(`${location.origin}/own-print`, { method: 'POST', headers: { 'Content-Type': 'image/jpeg' }, body: photo });
      if (!response.ok) throw new Error('The camera could not take the photo. Try again.');
      const { id } = await response.json();
      const href = print.link(id, location.origin);
      const call = window.Unity && window.Unity.call;
      if (typeof call === 'function') call(href);
      else window.location.href = href;
    },
  }, { esc });
  place(page.html(), false, () => page.bind());
}

async function renderSaved() {
  setPage('visit', 'Saved');
  try {
    const catalog = await api('/catalog/artworks');
    const page = new window.Cinnarios.SavedPage(catalog, window.Rooms, { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

function renderPrivacy() {
  setPage('visit', 'Privacy');
  place(`<section class="account-page"><h1>Privacy</h1>
    <p>Cinnarios stores the email, password hash, name, delivery address, cart, and orders for the signed-in account. Addresses and the phone number are encrypted. The demo checkout never asks for a card number or a mobile-money PIN, and it does not charge anyone.</p>
    <p>The paintings are public-domain works from The Metropolitan Museum of Art. Signing in on this server is a different account from a sign-in stored only on the phone.</p>
    <p>Signing out leaves the account on the server. Delete account is the only action that removes it, including the cart, orders, saved paintings and wall tries.</p>
    <p><a href="#account">Back to Account</a></p></section>`, false);
}

async function renderAccount() {
  carriedWork = null;
  setPage('visit', 'Account');
  if (!signedIn()) {
    location.hash = '#enter';
    return;
  }
  try {
    const [me, collector, orders, catalog, addresses] = await Promise.all([
      api('/me'), api('/me/collector'), api('/orders'), api('/catalog/artworks'), api('/me/addresses'),
    ]);
    const page = new window.Cinnarios.AccountPage({
      email: me.email,
      displayName: collector.displayName,
      orders,
      catalog,
      addresses,
      tries: window.Rooms.tries(),
      savedIds: window.Rooms.savedIds(),
    }, {
      rename: (displayName) => api('/me/collector', { method: 'PATCH', body: JSON.stringify({ displayName }) }),
      removeAddress: async (id) => {
        await api(`/me/addresses/${id}`, { method: 'DELETE' });
        renderAccount();
      },
      deleteAccount: async () => {
        await api('/me/deletion-requests', { method: 'POST', body: '{}' });
        clearTimeout(libraryTimer);
        window.Rooms.clearLibrary();
        session.forget();
        window.Rooms.useAccount(null);
        libraryReady = false;
        location.hash = '#enter';
      },
      signOut: async () => {
        try {
          await api('/auth/logout', { method: 'POST', body: '{}' });
        } catch {
          /* The local session still ends. */
        }
        clearTimeout(libraryTimer);
        session.forget();
        window.Rooms.useAccount(null);
        libraryReady = false;
        cartCount.textContent = '';
        location.hash = '#enter';
      },
    }, { esc, money });
    place(page.html(), false, () => page.bind());
  } catch (error) {
    place(formError(error), false);
  }
}

async function uploadWallPhoto(file) {
  const headers = {};
  const token = session.token();
  if (token) headers.Authorization = `Bearer ${token}`;
  const body = new FormData();
  body.append('photo', file);
  const response = await fetch(`${API}/me/wall-photos`, { method: 'POST', body, headers, credentials: 'include' });
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(payload.message || 'The photo could not be saved.');
  return payload.url;
}

window.Shop = {
  api, esc, money, rateNote, place, setPage, signedIn, formError, refreshCartCount, sessionId, openPieceInSpace, syncLibrary, uploadWallPhoto,
};

app.addEventListener('click', (event) => {
  if (event.target.closest('[data-retry]')) render();
});

window.addEventListener('hashchange', () => {
  render();
  refreshCartCount();
});
// A new tab or a reload signs back in from the refresh cookie before the first page draws.
session.restore().finally(() => {
  render();
  refreshCartCount();
});
