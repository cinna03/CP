const API = window.CINNARIOS_PHONE
  ? `${location.origin}/v1`
  : `${location.protocol}//${location.hostname}:3001/v1`;
const app = document.querySelector('#app');
const cartCount = document.querySelector('#cart-count');

const sessionKey = 'marketplace.session';
const tokenKey = 'marketplace.access';

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

const currencyKey = 'marketplace.currency';
/** National Bank of Rwanda average for 17 Nov 2026: midpoint of the USD buying and selling rates. */
const USD_TO_RWF = 1477.385;

const RW_PROVINCES = {
  'Kigali City': ['Gasabo', 'Kicukiro', 'Nyarugenge'],
  'Northern Province': ['Burera', 'Gakenke', 'Gicumbi', 'Musanze', 'Rulindo'],
  'Southern Province': ['Gisagara', 'Huye', 'Kamonyi', 'Muhanga', 'Nyamagabe', 'Nyanza', 'Nyaruguru', 'Ruhango'],
  'Eastern Province': ['Bugesera', 'Gatsibo', 'Kayonza', 'Kirehe', 'Ngoma', 'Nyagatare', 'Rwamagana'],
  'Western Province': ['Karongi', 'Ngororero', 'Nyabihu', 'Nyamasheke', 'Rubavu', 'Rusizi', 'Rutsiro'],
};

function displayCurrency() {
  return localStorage.getItem(currencyKey) === 'USD' ? 'USD' : 'RWF';
}

function syncCurrency() {
  const current = displayCurrency();
  document.querySelectorAll('[data-currency]').forEach((button) => {
    button.setAttribute('aria-pressed', String(button.dataset.currency === current));
  });
}

function money(cents, currency = 'USD') {
  const code = String(currency).trim();
  const amount = Number(cents) / 100;
  if (displayCurrency() === 'RWF' && code === 'USD') {
    const francs = Math.round(amount * USD_TO_RWF);
    return `RWF ${new Intl.NumberFormat('en-RW', { maximumFractionDigits: 0 }).format(francs)}`;
  }
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: code }).format(amount);
}

function rateNote() {
  if (displayCurrency() !== 'RWF') return '';
  return '<p class="note">Shown in francs at the National Bank of Rwanda average for 17 Nov 2026: 1 USD = 1,477.385 RWF. The price is still recorded in dollars.</p>';
}

async function api(path, options = {}) {
  const headers = { ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(options.headers || {}) };
  const token = sessionStorage.getItem(tokenKey);
  if (token) headers.Authorization = `Bearer ${token}`;
  let response = await fetch(API + path, { ...options, headers, credentials: 'include' });
  if (response.status === 401 && token && !path.startsWith('/auth/')) {
    const refreshed = await fetch(API + '/auth/refresh', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: '{}',
    });
    if (refreshed.ok) {
      const data = await refreshed.json();
      sessionStorage.setItem(tokenKey, data.accessToken);
      headers.Authorization = `Bearer ${data.accessToken}`;
      response = await fetch(API + path, { ...options, headers, credentials: 'include' });
    } else {
      sessionStorage.removeItem(tokenKey);
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
  return Boolean(sessionStorage.getItem(tokenKey));
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
  return `<p class="error">${error.message}</p>`;
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

function place(html, carry, after) {
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

function markNav() {
  const route = location.hash || '#gallery';
  let tab = '#gallery';
  if (route.startsWith('#search') || route.startsWith('#collection')) tab = '#search';
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
    route === '#forgot'
  ) tab = '#account';
  document.querySelectorAll('[data-tab]').forEach((link) => {
    if (link.getAttribute('href') === tab) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
}

function setPage(mode, title) {
  document.body.dataset.page = mode;
  document.title = title ? `${title} · Marketplace` : 'Marketplace';
  let back = document.querySelector('#back');
  if (mode === 'sheet') {
    if (!back) {
      back = document.createElement('a');
      back.id = 'back';
      back.href = '#gallery';
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
    carriedWork = href.slice('#work/'.length);
    const image = link.querySelector('img');
    if (image) image.style.viewTransitionName = `work-${carriedWork}`;
    return;
  }
  carriedWork = null;
});

async function render() {
  syncCurrency();
  markNav();
  const route = location.hash || '#gallery';
  if (!painted) app.innerHTML = '<p class="loading">Opening the works.</p>';
  if (route.startsWith('#work/')) return renderWork(route.slice('#work/'.length).split('/')[0]);
  if (route.startsWith('#try/')) return renderTry(route.slice('#try/'.length).split('/')[0]);
  if (route === '#cart') return renderCart();
  if (route === '#account') return renderAccount();
  if (route === '#enter') return renderAuth();
  if (route === '#forgot') return renderForgot();
  if (route === '#reset') return renderReset();
  if ((route === '#gallery' || route === '#discover') && window.renderDiscover) return window.renderDiscover();
  if (window.renderRoom?.(route)) return undefined;
  return renderGallery();
}

function acceptSession(result) {
  if (result.mfaRequired) {
    sessionStorage.setItem('marketplace.mfa', result.mfaToken);
    const mfaForm = document.querySelector('#mfa-form');
    if (mfaForm) {
      mfaForm.hidden = false;
      mfaForm.dataset.token = result.mfaToken;
      return;
    }
    location.hash = '#enter';
    return;
  }
  sessionStorage.removeItem('marketplace.mfa');
  sessionStorage.setItem(tokenKey, result.accessToken);
  location.hash = '#gallery';
}

function renderAuth() {
  setPage('sheet', 'Sign in');
  place(`
    <h1>Come in.</h1>
    <p>Create an account or sign in. The profile, cart, and confidence record stay with this account.</p>
    <form id="auth-form">
      <label for="email">Email</label>
      <input id="email" name="email" type="email" autocomplete="username" required />
      <label for="password">Password</label>
      <input id="password" name="password" type="password" autocomplete="new-password" minlength="12" required />
      <p class="note">At least 12 characters, with upper and lower case letters and a number.</p>
      <button type="submit" data-mode="register">Create account</button>
      <button type="submit" class="quiet" data-mode="login">Sign in</button>
      <p id="auth-error" class="error"></p>
      <p><a href="#forgot">Forgot password</a></p>
    </form>
    <div id="providers"></div>
    <form id="mfa-form" hidden>
      <label for="code">Authentication code</label>
      <input id="code" name="code" inputmode="numeric" autocomplete="one-time-code" required />
      <button type="submit">Confirm code</button>
    </form>`, false, () => {
  const form = document.querySelector('#auth-form');
  form.addEventListener('click', (event) => {
    const button = event.target.closest('button');
    if (button) form.dataset.mode = button.dataset.mode;
  });
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const data = new FormData(form);
    const mode = form.dataset.mode || 'register';
    try {
      const result = await api(mode === 'login' ? '/auth/login' : '/auth/register', {
        method: 'POST',
        body: JSON.stringify({ email: data.get('email'), password: data.get('password') }),
      });
      acceptSession(result);
    } catch (error) {
      document.querySelector('#auth-error').textContent = error.message;
    }
  });
  document.querySelector('#mfa-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    const mfaForm = event.currentTarget;
    try {
      const result = await api('/auth/mfa/challenge', {
        method: 'POST',
        body: JSON.stringify({ mfaToken: mfaForm.dataset.token, code: new FormData(mfaForm).get('code') }),
      });
      acceptSession(result);
    } catch (error) {
      document.querySelector('#auth-error').textContent = error.message;
    }
  });
  loadProviders();
  });
}

function renderForgot() {
  setPage('sheet', 'Reset password');
  place(`
    <h1>Reset the password.</h1>
    <p>Enter the account email. This local server shows the reset link here, because mail is only written to the API log.</p>
    <form id="forgot-form">
      <label for="email">Email</label>
      <input id="email" name="email" type="email" autocomplete="username" required />
      <button type="submit">Send reset link</button>
      <p id="forgot-result"></p>
    </form>`, false, () => {
    document.querySelector('#forgot-form').addEventListener('submit', async (event) => {
      event.preventDefault();
      const result = document.querySelector('#forgot-result');
      result.className = '';
      result.textContent = '';
      try {
        const body = await api('/auth/password/forgot', {
          method: 'POST',
          body: JSON.stringify({ email: new FormData(event.currentTarget).get('email') }),
        });
        result.textContent = body.message;
        if (body.resetUrl) {
          const link = document.createElement('a');
          link.href = body.resetUrl;
          link.textContent = 'Open the reset link';
          result.append(document.createElement('br'), link);
        }
      } catch (error) {
        result.className = 'error';
        result.textContent = error.message;
      }
    });
  });
}

function renderReset() {
  setPage('sheet', 'New password');
  const token = sessionStorage.getItem('marketplace.reset') || '';
  place(`
    <h1>Choose a new password.</h1>
    <form id="reset-form">
      <label for="password">New password</label>
      <input id="password" name="password" type="password" autocomplete="new-password" minlength="12" required />
      <p class="note">At least 12 characters, with upper and lower case letters and a number.</p>
      <button type="submit" ${token ? '' : 'disabled'}>Update password</button>
      <p id="reset-result" class="${token ? '' : 'error'}">${token ? '' : 'This reset link is missing its token.'}</p>
    </form>`, false, () => {
    document.querySelector('#reset-form').addEventListener('submit', async (event) => {
      event.preventDefault();
      const result = document.querySelector('#reset-result');
      try {
        await api('/auth/password/reset', {
          method: 'POST',
          body: JSON.stringify({ token, password: new FormData(event.currentTarget).get('password') }),
        });
        sessionStorage.removeItem('marketplace.reset');
        result.className = '';
        result.textContent = 'Password updated.';
        const link = document.createElement('a');
        link.href = '#enter';
        link.textContent = 'Sign in';
        result.append(document.createElement('br'), link);
      } catch (error) {
        result.className = 'error';
        result.textContent = error.message;
      }
    });
  });
}

function loadProviders() {
  const slot = document.querySelector('#providers');
  if (!slot) return;
  api('/auth/providers').then((providers) => {
    if (!providers.googleClientId && !providers.appleClientId) {
      slot.innerHTML = '<p class="note">Google and Apple sign-in run once a client id is set for this server.</p>';
      return;
    }
    if (providers.googleClientId) slot.insertAdjacentHTML('beforeend', '<div id="google-button"></div>');
    if (providers.appleClientId) slot.insertAdjacentHTML('beforeend', '<button type="button" id="apple-button" class="quiet">Continue with Apple</button>');
    if (providers.googleClientId) mountGoogle(providers.googleClientId);
    if (providers.appleClientId) mountApple(providers.appleClientId);
  }).catch((error) => {
    slot.innerHTML = `<p class="error">${esc(error.message)}</p>`;
  });
}

function mountGoogle(clientId) {
  const start = () => {
    const google = window.google;
    if (!google?.accounts?.id) return;
    google.accounts.id.initialize({
      client_id: clientId,
      callback: async (response) => {
        try {
          acceptSession(await api('/auth/oauth/google', {
            method: 'POST',
            body: JSON.stringify({ idToken: response.credential }),
          }));
        } catch (error) {
          const node = document.querySelector('#auth-error');
          if (node) node.textContent = error.message;
        }
      },
    });
    google.accounts.id.renderButton(document.querySelector('#google-button'), {
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'pill',
    });
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
}

function mountApple(clientId) {
  const start = () => {
    const apple = window.AppleID;
    if (!apple?.auth) return;
    apple.auth.init({
      clientId,
      scope: 'name email',
      redirectURI: location.origin,
      usePopup: true,
    });
    document.querySelector('#apple-button').addEventListener('click', async () => {
      try {
        const response = await apple.auth.signIn();
        acceptSession(await api('/auth/oauth/apple', {
          method: 'POST',
          body: JSON.stringify({ idToken: response.authorization.id_token }),
        }));
      } catch (error) {
        const node = document.querySelector('#auth-error');
        if (node) node.textContent = error.message || 'Apple sign-in was cancelled.';
      }
    });
  };
  if (window.AppleID?.auth) {
    start();
    return;
  }
  const script = document.createElement('script');
  script.src = 'https://appleid.cdn-apple.com/appleauth/static/jsapi/appleid/1/en_US/appleid.auth.js';
  script.async = true;
  script.onload = start;
  document.head.appendChild(script);
}

function confidenceSentence(report) {
  if (!report) return 'This first look starts uncertain.';
  if (report.becameConfident) return 'You started uncertain. This work is now one you can stand behind.';
  const checked = [];
  if (report.signals.includes('credit_reviewed')) checked.push('the museum credit');
  if (report.signals.includes('dimensions_checked')) checked.push('the recorded size');
  if (report.signals.includes('viewed_in_room')) checked.push('the scale against a door');
  const soFar = checked.length ? ` So far you have checked ${checked.join(', ').replace(/, ([^,]*)$/, ' and $1')}.` : '';
  return `Still uncertain.${soFar}`;
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

async function renderWork(sourceId) {
  carriedWork = sourceId;
  try {
    const work = await api(`/catalog/artworks/${sourceId}`);
    setPage('visit', work.title);
    const prior = await loadConfidence(sourceId);
    const seen = new Set(prior?.signals ?? []);
    if (signedIn()) {
      try {
        await journey('artwork_viewed', sourceId);
        if (!seen.has('credit_reviewed')) await journey('credit_reviewed', sourceId);
      } catch {
        /* A stale sign-in should not hide the painting. */
      }
    }
    const current = await loadConfidence(sourceId);
    place(`
      <article class="detail">
        <div class="viewer"><img id="zoom-image" src="${esc(work.imageUrl)}" alt="${esc(work.title)}" style="view-transition-name: work-${esc(sourceId)}" /></div>
        <div class="facts">
          <h1>${esc(work.title)}</h1>
          <p>${esc(work.artist || 'Unknown artist')}${work.date ? `, ${esc(work.date)}` : ''}</p>
          <p class="measures">${esc(work.widthCm)} × ${esc(work.heightCm)} × ${esc(work.depthCm)} cm</p>
          <p class="price">${esc(money(work.priceAmount, work.currency))}</p>
          ${rateNote()}
          <p>A reproduction at this size. Framing, a crate, and shipping are not part of this price.</p>
          <p>${esc(work.creditLine)}</p>
          <p>${esc(work.mediumDescription)}</p>
          <p>Shop guides for comparing: colour ${esc(window.Rooms?.colourName?.(work) || '')}, mood ${esc(window.Rooms?.moodName?.(work) || '')}, subject ${esc(window.Rooms?.subjectName?.(work) || '')}. These sort the next pictures. They are not a measurement of the paint.</p>
          <p id="confidence-line">${esc(signedIn() ? confidenceSentence(current) : 'Sign in to keep this try with the account.')}</p>
          ${window.Rooms?.workFacts(work) || ''}
          <div class="actions">
            <a class="button" href="#try/${esc(sourceId)}">View in your room</a>
            <button id="add" ${signedIn() && !window.Rooms?.isReserved(sourceId) ? '' : 'disabled'}>${window.Rooms?.isReserved(sourceId) ? 'Reserved in this browser' : 'Add to cart'}</button>
            <button type="button" id="save-work" class="quiet">${window.Rooms?.isSaved(sourceId) ? 'Saved' : 'Save'}</button>
          </div>
          ${signedIn() ? '' : '<p class="note"><a href="#enter">Sign in</a> to keep a cart.</p>'}
          <p id="work-error" class="error" role="alert"></p>
        </div>
      </article>`, sourceId, () => {
    window.Rooms?.bindWork(work);
    document.querySelector('#add').addEventListener('click', async () => {
      const button = document.querySelector('#add');
      if (window.Rooms?.isReserved(sourceId)) {
        document.querySelector('#work-error').textContent = 'This browser already reserved that print in a sandbox order. Release it on this page to add it again.';
        return;
      }
      button.disabled = true;
      button.textContent = 'Adding…';
      try {
        await api('/cart/items', { method: 'POST', body: JSON.stringify({ sourceId, quantity: 1 }) });
        await refreshCartCount();
        location.hash = '#cart';
      } catch (error) {
        button.disabled = !window.Rooms?.isReserved(sourceId) && signedIn();
        button.textContent = 'Add to cart';
        document.querySelector('#work-error').textContent = error.message;
      }
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

function wallTries(catalog) {
  let tries = [];
  try {
    tries = JSON.parse(localStorage.getItem('marketplace.rooms') || '{}').tries || [];
  } catch {
    tries = [];
  }
  if (!tries.length) return '<p>No wall tries yet. Open a painting in the camera and come back.</p>';
  const byId = new Map(catalog.map((work) => [work.sourceId, work]));
  return `<div class="suggest">${tries.map((item) => {
    const work = byId.get(item.sourceId);
    return work ? workLink(work, `#try/${esc(work.sourceId)}`) : '';
  }).join('')}</div>`;
}

function suggestionSets(catalog, confidence) {
  const seen = new Set((confidence?.artworks || []).map((work) => work.sourceId));
  const seenArtists = new Set(catalog.filter((work) => seen.has(work.sourceId) && work.artist).map((work) => work.artist));
  const pieces = catalog.filter((work) => !seen.has(work.sourceId) && seenArtists.has(work.artist)).slice(0, 4);
  const used = new Set(seenArtists);
  const artists = [];
  for (const work of catalog) {
    if (seen.has(work.sourceId) || !work.artist || used.has(work.artist)) continue;
    artists.push(work);
    used.add(work.artist);
    if (artists.length === 3) break;
  }
  return { pieces, artists, seenArtists: [...seenArtists] };
}

function workLink(work, href) {
  return `<a class="work" href="${href}">
    <span class="frame"><img src="${esc(work.thumbnailUrl)}" alt="" /></span>
    <strong>${esc(work.title)}</strong>
    <span>${esc(work.artist || 'Unknown artist')}</span>
    <span class="measures">${esc(work.widthCm)} × ${esc(work.heightCm)} cm</span>
  </a>`;
}

async function renderTry(sourceId) {
  carriedWork = sourceId;
  try {
    const work = await api(`/catalog/artworks/${sourceId}`);
    setPage('visit', work.title);
    const current = await loadConfidence(sourceId);
    const host = window.CINNARIOS_PHONE ? null : await fetch('/lan').then((response) => response.json());
    const apiOrigin = window.CINNARIOS_PHONE ? location.origin : `http://${host.address}:3001`;
    const cameraHref = `cinnarios://try/${sourceId}?api=${encodeURIComponent(apiOrigin)}`;
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

function orderLabel(status) {
  if (status === 'awaiting_payment') return 'Unpaid';
  if (status === 'paid') return 'Paid';
  return status;
}

async function renderCart() {
  carriedWork = null;
  setPage('sheet', 'Cart');
  if (!signedIn()) {
    place(`<h1>Your cart</h1><p><a href="#enter">Sign in</a> to keep works here.</p>`, false);
    return;
  }
  try {
    const cart = await api('/cart');
    const addresses = await api('/me/addresses');
    const items = cart.items.length
      ? `<div id="cart-lines" class="cart-lines">${cart.items.map((item) => `<div class="line">
          <img src="${esc(item.thumbnailUrl)}" alt="" width="72" height="72" />
          <div>
            <p><strong>${esc(item.title)}</strong></p>
            <p>${esc(item.widthCm)} × ${esc(item.heightCm)} cm reproduction · ${esc(money(item.lineAmount, item.currency))}</p>
            <p class="qty"><button type="button" class="quiet" data-id="${esc(item.id)}" data-quantity="${item.quantity}" data-delta="-1">${item.quantity === 1 ? 'Remove' : 'Fewer'}</button><span>${esc(item.quantity)}</span><button type="button" class="quiet" data-id="${esc(item.id)}" data-quantity="${item.quantity}" data-delta="1" ${item.quantity >= 10 ? 'disabled' : ''}>More</button></p>
          </div>
        </div>`).join('')}</div>
        <p class="total">Total ${esc(money(cart.totalAmount, cart.currency))}</p>
        ${rateNote()}`
      : '<p>Nothing here yet. <a href="#gallery">Choose a work.</a></p>';
    const addressOptions = addresses.map((address) => `<option value="${esc(address.id)}">${esc(address.recipientName)}, ${esc(address.city)}${address.region ? `, ${esc(address.region)}` : ''}</option>`).join('');
    const provinceOptions = Object.keys(RW_PROVINCES).map((province) => `<option value="${esc(province)}">${esc(province)}</option>`).join('');
    const checkout = cart.items.length
      ? `<section class="block">
          <h2>Where it should arrive</h2>
          <p>Delivery in this version is inside Rwanda. A rider uses the district, the sector, and a landmark.</p>
          <form id="address-form">
            <label for="recipientName">Name</label>
            <input id="recipientName" name="recipientName" required />
            <label for="phone">Phone</label>
            <input id="phone" name="phone" type="tel" inputmode="tel" autocomplete="tel" required pattern="(\\+?250|0)7[0-9]{8}" />
            <p class="note">A mobile number a rider can call, such as 0788123456.</p>
            <label for="region">Province</label>
            <select id="region" name="region" required>
              <option value="">Choose a province</option>
              ${provinceOptions}
            </select>
            <label for="city">District</label>
            <select id="city" name="city" required>
              <option value="">Choose a district</option>
            </select>
            <label for="line2">Sector</label>
            <input id="line2" name="line2" required />
            <label for="line1">Street or landmark</label>
            <input id="line1" name="line1" required />
            <button type="submit">Save address</button>
          </form>
          <form id="checkout-form">
            <label for="addressId">Ship to</label>
            <select id="addressId" name="addressId" ${addressOptions ? '' : 'disabled'}>${addressOptions || '<option>Save an address first</option>'}</select>
            <button type="submit" ${addressOptions ? '' : 'disabled'}>Proceed to checkout</button>
            <p class="note">The next step checks a card or mobile-money number as a sandbox. The order stays unpaid, and nothing is charged.</p>
          </form>
        </section>`
      : '';
    place(`<h1>Your cart</h1>
      ${items}
      ${checkout}
      <p id="cart-error" class="error" role="alert"></p>`, false, () => {
    document.querySelector('#cart-lines')?.addEventListener('click', async (event) => {
      const button = event.target.closest('button[data-id]');
      if (!button) return;
      const next = Number(button.dataset.quantity) + Number(button.dataset.delta);
      try {
        if (next < 1) await api(`/cart/items/${button.dataset.id}`, { method: 'DELETE' });
        else await api(`/cart/items/${button.dataset.id}`, { method: 'PATCH', body: JSON.stringify({ quantity: next }) });
        renderCart();
      } catch (error) {
        document.querySelector('#cart-error').textContent = error.message;
      }
    });
    const province = document.querySelector('#region');
    const district = document.querySelector('#city');
    const fillDistricts = () => {
      const names = RW_PROVINCES[province.value] || [];
      district.innerHTML = `<option value="">Choose a district</option>${names.map((name) => `<option value="${esc(name)}">${esc(name)}</option>`).join('')}`;
    };
    province?.addEventListener('change', fillDistricts);
    document.querySelector('#address-form')?.addEventListener('submit', async (event) => {
      event.preventDefault();
      const data = Object.fromEntries(new FormData(event.currentTarget));
      data.countryCode = 'RW';
      data.postalCode = 'N/A';
      try {
        await api('/me/addresses', { method: 'POST', body: JSON.stringify(data) });
        renderCart();
      } catch (error) {
        document.querySelector('#cart-error').textContent = error.message;
      }
    });
    document.querySelector('#checkout-form')?.addEventListener('submit', async (event) => {
      event.preventDefault();
      const addressId = new FormData(event.currentTarget).get('addressId');
      sessionStorage.setItem('marketplace.checkoutAddress', String(addressId));
      sessionStorage.setItem('marketplace.checkoutStep', 'ship');
      location.hash = '#checkout';
    });
    });
  } catch (error) {
    place(formError(error), false);
  }
}

async function renderAccount() {
  carriedWork = null;
  setPage('sheet', 'Account');
  if (!signedIn()) {
    place(`<h1>Account</h1><p><a href="#enter">Sign in</a> to see your profile and confidence.</p>`, false);
    return;
  }
  try {
    const [me, collector, confidence, orders, catalog] = await Promise.all([
      api('/me'),
      api('/me/collector'),
      api('/me/confidence'),
      api('/orders'),
      api('/catalog/artworks'),
    ]);
    let artist = null;
    try {
      artist = await api('/me/artist');
    } catch (error) {
      if (error.code !== 'NOT_FOUND') throw error;
    }
    const titles = new Map(catalog.map((work) => [work.sourceId, work.title]));
    const last = sessionStorage.getItem('marketplace.lastOrder');
    const confident = confidence.summary.worksBecameConfident;
    const uncertain = confidence.summary.worksStillUncertain;
    const confidenceLine = confident
      ? `${confident} you can stand behind. ${uncertain} ${uncertain === 1 ? 'is' : 'are'} still uncertain.`
      : `${uncertain} still uncertain. None you can stand behind yet.`;
    const works = confidence.artworks.length
      ? confidence.artworks.map((work) => `<div class="opened"><p><a href="#work/${esc(work.sourceId)}">${esc(titles.get(work.sourceId) || work.sourceId)}</a></p><p>${esc(confidenceSentence(work))}</p><button type="button" class="quiet again" data-work="${esc(work.sourceId)}">Open it again</button></div>`).join('')
      : '<p>No paintings opened yet.</p>';
    const ideas = suggestionSets(catalog, confidence);
    const forYou = ideas.pieces.length
      ? `<div class="suggest">${ideas.pieces.map((work) => workLink(work, `#work/${esc(work.sourceId)}`)).join('')}</div>`
      : '<p>Open a painting and this visit will follow the artists you actually look at.</p>';
    const otherArtists = ideas.artists.length
      ? `<div class="suggest">${ideas.artists.map((work) => workLink(work, `#work/${esc(work.sourceId)}`)).join('')}</div>`
      : '';
    const lookedAt = ideas.seenArtists.length
      ? `<p>You have looked at ${esc(ideas.seenArtists.join(', '))}.</p>`
      : '';
    place(`<div class="account-head"><h1>${esc(collector.displayName)}</h1><button type="button" id="sign-out" class="quiet">Sign out</button></div>
      <p>${esc(me.email)}</p>
      ${last ? `<p>Order ${esc(last)} is placed and unpaid. Payment is outside this version, so nothing was charged.</p>` : ''}
      <section class="block">
        <h2>For you</h2>
        ${lookedAt}
        ${forYou}
        ${otherArtists ? `<h2>Artists you have not opened</h2>${otherArtists}` : ''}
      </section>
      <section class="block">
        <h2>Your name</h2>
        <p>This is the name on the account. A shipping address is asked when you place an order.</p>
        <form id="collector-form">
          <label for="displayName">Name</label>
          <input id="displayName" name="displayName" required minlength="2" value="${esc(collector.displayName)}" />
          <button type="submit">Save name</button>
          <p id="collector-result"></p>
        </form>
      </section>
      <section class="block">
        <h2>Sign-in</h2>
        <p><a href="#forgot">Send a reset link</a> for this password.</p>
        ${me.mfaEnabled
          ? '<p>Two-factor is on. The next sign-in asks for a code from your authenticator, or one recovery code.</p>'
          : `<p>Two-factor is off. Setting it up shows a secret, then asks for one code from an authenticator app.</p><button type="button" id="mfa-start">Set up two-factor</button>`}
        <div id="mfa-enroll"></div>
      </section>
      <section class="block">
        <h2>Orders</h2>
        <p>When a piece is on its way, the notice stays on this page. It does not open the camera.</p>
        ${rateNote()}
        ${orders.length ? orders.map((order) => `<p>${esc(order.number)} · ${esc(orderLabel(order.status))} · ${esc(money(order.totalAmount, order.currency))}</p>`).join('') : '<p>No orders yet. Nothing is on its way.</p>'}
      </section>
      <section class="block">
        <h2>Tries on your wall</h2>
        <p>Each painting you put on the wall is kept here. Open one to hang it again in the same kind of visit.</p>
        ${wallTries(catalog)}
      </section>
      <section class="block">
        <h2>Paintings you have opened</h2>
        <p>${confidenceLine} Opening one again counts as coming back to it.</p>
        ${works}
      </section>
      `, false, () => {
      bindAccount();
    });
  } catch (error) {
    place(formError(error), false);
  }
}

function profileBody(form) {
  const data = Object.fromEntries(new FormData(form));
  const body = {};
  if (data.displayName) body.displayName = data.displayName;
  if (data.bio) body.bio = data.bio;
  if (data.city) body.city = data.city;
  if (data.countryCode) body.countryCode = data.countryCode.toUpperCase();
  if (data.website) body.website = data.website;
  return body;
}

function bindAccount() {
  document.querySelector('#collector-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    const result = document.querySelector('#collector-result');
    const data = Object.fromEntries(new FormData(event.currentTarget));
    try {
      await api('/me/collector', {
        method: 'PATCH',
        body: JSON.stringify({ displayName: data.displayName }),
      });
      document.querySelector('h1').textContent = data.displayName;
      result.className = '';
      result.textContent = 'Name saved.';
    } catch (error) {
      result.className = 'error';
      result.textContent = error.message;
    }
  });
  document.querySelector('#mfa-start')?.addEventListener('click', async () => {
    const box = document.querySelector('#mfa-enroll');
    const start = document.querySelector('#mfa-start');
    start.disabled = true;
    try {
      const enrollment = await api('/auth/mfa/enroll', { method: 'POST' });
      start.hidden = true;
      box.innerHTML = `
        <p>Add this secret to an authenticator app. Write the recovery codes down before you confirm. Each code works once.</p>
        <p class="secret">${esc(enrollment.secret)}</p>
        <p class="secret">${enrollment.recoveryCodes.map((code) => esc(code)).join('<br />')}</p>
        <form id="mfa-confirm">
          <label for="mfaCode">Code from the app</label>
          <input id="mfaCode" name="code" inputmode="numeric" autocomplete="one-time-code" required />
          <button type="submit">Confirm two-factor</button>
        </form>
        <p id="mfa-error" class="error"></p>`;
      document.querySelector('#mfa-confirm').addEventListener('submit', async (confirmEvent) => {
        confirmEvent.preventDefault();
        try {
          await api('/auth/mfa/confirm', {
            method: 'POST',
            body: JSON.stringify({ code: new FormData(confirmEvent.currentTarget).get('code') }),
          });
          box.innerHTML = '<p>Two-factor is on. The next sign-in asks for a code.</p>';
        } catch (error) {
          document.querySelector('#mfa-error').textContent = error.message;
        }
      });
    } catch (error) {
      start.disabled = false;
      box.innerHTML = `<p class="error">${esc(error.message)}</p>`;
    }
  });
  document.querySelectorAll('.again').forEach((button) => {
    button.addEventListener('click', () => {
      localStorage.setItem(sessionKey, newId());
      location.hash = `#work/${button.dataset.work}`;
    });
  });
  document.querySelector('#sign-out').addEventListener('click', async () => {
    try {
      await api('/auth/logout', { method: 'POST', body: '{}' });
    } catch {
      /* The local session still ends. */
    }
    sessionStorage.removeItem(tokenKey);
    cartCount.textContent = '';
    location.hash = '#enter';
  });
}

if (location.pathname === '/reset-password') {
  const token = new URLSearchParams(location.search).get('token');
  if (token) sessionStorage.setItem('marketplace.reset', token);
  history.replaceState(null, '', '/#reset');
}

document.querySelector('.currency').addEventListener('click', (event) => {
  const button = event.target.closest('[data-currency]');
  if (!button) return;
  localStorage.setItem(currencyKey, button.dataset.currency);
  render();
});

window.Shop = {
  api, esc, money, rateNote, place, setPage, signedIn, formError, refreshCartCount, sessionId, workLink,
};

window.addEventListener('hashchange', () => {
  render();
  refreshCartCount();
});
render();
refreshCartCount();
