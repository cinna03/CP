// Sign in and Create account. On a computer a blush panel slides across the card to swap
// the two forms; on a phone one form slides away while the other slides in, and the screen
// turns blush for Create account. Signing in, creating the account and Google go through `actions`.
(function () {
  class SignInPage {
    /**
     * @param {{signIn: Function, create: Function, mountGoogle: (slot: HTMLElement) => void, pictures?: () => Promise<string[]>}} actions
     * @param {{esc: Function}} format
     * @param {'in'|'up'} start
     */
    constructor(actions, format, start = 'in') {
      this.actions = actions;
      this.esc = format.esc;
      this.mode = start;
    }

    form(kind) {
      const up = kind === 'up';
      return `
        <form class="auth-form" id="${kind}-form" novalidate>
          <div class="auth-strip" aria-hidden="true"><span></span><span></span></div>
          <h1>${up ? 'Create your account' : 'Sign in'}</h1>
          <p class="auth-lede">${up ? 'Keep every wall you try and every piece you save.' : 'Welcome back to Cinnarios.'}</p>
          ${window.CINNARIOS_PHONE ? '' : '<div class="google-slot" data-google><button type="button" class="google-button" data-google-fallback><span class="google-mark" aria-hidden="true">G</span>Continue with Google</button></div><div class="auth-or"><span></span>or with email<span></span></div>'}
          ${up ? '<label class="sr-only" for="up-name">Your name</label><input id="up-name" name="name" autocomplete="name" placeholder="Your name" required minlength="2" />' : ''}
          <label class="sr-only" for="${kind}-email">Email</label>
          <input id="${kind}-email" name="email" type="email" autocomplete="${up ? 'email' : 'username'}" placeholder="Email" required />
          <div class="password-field">
            <label class="sr-only" for="${kind}-password">Password</label>
            <input id="${kind}-password" name="password" type="password" autocomplete="${up ? 'new-password' : 'current-password'}" placeholder="Password" required />
            <button type="button" class="show-password" data-show>Show</button>
          </div>
          ${up ? '<p class="piece-note">At least 12 characters, with upper and lower case letters and a number.</p>' : ''}
          <p class="error" role="alert" data-error></p>
          <button type="submit">${up ? 'Create account' : 'Sign in'}</button>
          ${up ? '' : '<a class="auth-link" href="#forgot">Forgot your password?</a>'}
          <p class="auth-switch">${up ? 'Already with us?' : 'New to Cinnarios?'} <button type="button" class="text-button" data-switch="${up ? 'in' : 'up'}">${up ? 'Sign in' : 'Create account'}</button></p>
        </form>`;
    }

    html() {
      return `
      <section class="auth ${this.mode === 'up' ? 'on' : ''}">
        <div class="auth-pane auth-in">${this.form('in')}</div>
        <div class="auth-pane auth-up">${this.form('up')}</div>
        <div class="auth-cover" aria-hidden="true"><div class="auth-cover-inner">
          <div class="auth-cover-side auth-cover-left"><span class="auth-cover-art"></span><h2>Already with us?</h2><p>Sign in to see your tries, saved paintings and cart.</p><button type="button" class="quiet" data-switch="in" tabindex="-1">Sign in</button></div>
          <div class="auth-cover-side auth-cover-right"><span class="auth-cover-art"></span><h2>New to Cinnarios?</h2><p>Create an account to keep every wall you try.</p><button type="button" class="quiet" data-switch="up" tabindex="-1">Create account</button></div>
        </div></div>
      </section>`;
    }

    switchTo(mode) {
      this.mode = mode;
      const root = document.querySelector('.auth');
      root.classList.toggle('on', mode === 'up');
      document.body.classList.toggle('auth-blush', mode === 'up');
      root.querySelector(`#${mode}-form input`)?.focus({ preventScroll: true });
    }

    /** Shakes the form and says what is missing. */
    fail(form, message) {
      const note = form.querySelector('[data-error]');
      note.classList.remove('waiting');
      note.textContent = message;
      form.classList.remove('shake');
      void form.offsetWidth;
      form.classList.add('shake');
    }

    async submit(form, up) {
      const data = Object.fromEntries(new FormData(form));
      if (up && String(data.name || '').trim().length < 2) return this.fail(form, 'Add your name.');
      if (!/^\S+@\S+\.\S+$/.test(data.email || '')) return this.fail(form, 'Enter an email like name@example.com.');
      if (!data.password) return this.fail(form, 'Enter your password.');
      if (up) {
        const problem = window.Cinnarios.PasswordStrength.problem(data.password);
        if (problem) return this.fail(form, problem);
      }
      const button = form.querySelector('button[type=submit]');
      const label = button.textContent;
      const note = form.querySelector('[data-error]');
      button.disabled = true;
      button.textContent = up ? 'Creating account…' : 'Signing in…';
      note.classList.remove('waiting');
      const waking = window.setTimeout(() => {
        if (!note.textContent) {
          note.textContent = 'The shop is waking up. The first try after a quiet period can take a minute.';
          note.classList.add('waiting');
        }
      }, 2000);
      try {
        await (up ? this.actions.create(data) : this.actions.signIn(data));
      } catch (err) {
        this.fail(form, err.message);
      } finally {
        window.clearTimeout(waking);
        button.disabled = false;
        button.textContent = label;
      }
      return undefined;
    }

    bind() {
      const root = document.querySelector('.auth');
      document.body.classList.toggle('auth-blush', this.mode === 'up');
      root.addEventListener('click', (event) => {
        const to = event.target.closest('[data-switch]');
        if (to) this.switchTo(to.dataset.switch);
        const google = event.target.closest('[data-google-fallback]');
        if (google) google.closest('form').querySelector('[data-error]').textContent = "Google sign-in isn't set up on this server yet. Use your email for now.";
        const show = event.target.closest('[data-show]');
        if (show) {
          const input = show.parentElement.querySelector('input');
          input.type = input.type === 'password' ? 'text' : 'password';
          show.textContent = input.type === 'password' ? 'Show' : 'Hide';
        }
      });
      ['in', 'up'].forEach((kind) => root.querySelector(`#${kind}-form`).addEventListener('submit', (event) => {
        event.preventDefault();
        this.submit(event.currentTarget, kind === 'up');
      }));
      if (!window.CINNARIOS_PHONE) root.querySelectorAll('[data-google]').forEach((slot) => this.actions.mountGoogle(slot));
      // Two paintings from the catalogue decorate the blush panel and the phone strip.
      this.actions.pictures?.().then((urls) => {
        root.querySelectorAll('.auth-cover-art, .auth-strip span').forEach((node, i) => {
          node.style.backgroundImage = `url("${urls[i % urls.length]}")`;
        });
      }).catch(() => {});
      window.addEventListener('hashchange', () => document.body.classList.remove('auth-blush'), { once: true });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.SignInPage = SignInPage;
}());
