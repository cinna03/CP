// Reset password in four steps: the email, the 6-digit code sent to it, a new password with
// a strength bar, then done. Each step replaces the last inside one card. The API calls are
// passed in as actions.
(function () {
  class ResetPage {
    /**
     * @param {{sendCode: (email: string) => Promise<void>, checkCode: Function, reset: Function}} actions
     * @param {{esc: Function}} format
     */
    constructor(actions, format) {
      this.actions = actions;
      this.esc = format.esc;
      this.email = '';
      this.code = '';
    }

    html() {
      return '<section class="auth-card"><div id="reset-step"></div></section>';
    }

    show(markup) {
      const step = document.querySelector('#reset-step');
      step.innerHTML = `<div class="step-in">${markup}</div>`;
    }

    emailStep() {
      const icons = window.Cinnarios.icons;
      this.show(`
        <div class="auth-badge">${icons.lock()}</div>
        <h1>Reset your password</h1>
        <p class="auth-lede">Enter the email on your account and we'll send you a 6-digit code.</p>
        <hr class="dashed" />
        <form id="reset-email" novalidate>
          <label for="reset-address" class="field-title">Email</label>
          <div class="icon-field">${icons.mail()}<input id="reset-address" type="email" autocomplete="email" placeholder="name@example.com" value="${this.esc(this.email)}" /></div>
          <p class="error" role="alert"></p>
          <button type="submit">Send code</button>
        </form>
        <p class="auth-foot">Remembered it? <a href="#enter">Sign in</a></p>`);
      const form = document.querySelector('#reset-email');
      form.querySelector('input').focus();
      form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const email = form.querySelector('input').value.trim();
        if (!/^\S+@\S+\.\S+$/.test(email)) {
          form.querySelector('.error').textContent = 'Enter an email like name@example.com.';
          return;
        }
        try {
          this.email = email;
          await this.actions.sendCode(email);
          this.codeStep();
        } catch (err) {
          form.querySelector('.error').textContent = err.message;
        }
      });
    }

    codeStep() {
      this.show(`
        <div class="auth-badge">${window.Cinnarios.icons.mail()}</div>
        <h1>Check your email</h1>
        <p class="auth-lede">We sent a 6-digit code to <strong>${this.esc(this.email)}</strong>.</p>
        <div id="reset-code"></div>
        <p class="auth-foot"><button type="button" class="text-button" id="other-email">Use another email</button></p>`);
      document.querySelector('#other-email').addEventListener('click', () => this.emailStep());
      new window.Cinnarios.CodeInput(document.querySelector('#reset-code'), {
        check: async (code) => {
          await this.actions.checkCode(this.email, code);
          this.code = code;
        },
        resend: () => this.actions.sendCode(this.email),
        done: () => this.passwordStep(),
      }).mount();
    }

    passwordStep() {
      const Strength = window.Cinnarios.PasswordStrength;
      this.show(`
        <div class="auth-badge">${window.Cinnarios.icons.lock()}</div>
        <h1>Choose a new password</h1>
        <p class="auth-lede">At least 12 characters, with upper and lower case letters and a number.</p>
        <form id="reset-password" novalidate>
          <label for="new-password" class="sr-only">New password</label>
          <input id="new-password" type="password" autocomplete="new-password" placeholder="New password" />
          <div class="strength" aria-hidden="true"><span></span><span></span><span></span><span></span><em></em></div>
          <label for="new-password-2" class="sr-only">Repeat the new password</label>
          <input id="new-password-2" type="password" autocomplete="new-password" placeholder="Repeat it" />
          <p class="error" role="alert"></p>
          <button type="submit">Save password</button>
        </form>`);
      const form = document.querySelector('#reset-password');
      const first = form.querySelector('#new-password');
      first.focus();
      first.addEventListener('input', () => {
        const score = Strength.score(first.value);
        form.querySelectorAll('.strength span').forEach((bar, i) => { bar.dataset.on = String(i < score); });
        form.querySelector('.strength').dataset.score = String(score);
        form.querySelector('.strength em').textContent = Strength.label(first.value);
      });
      form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const error = form.querySelector('.error');
        const password = first.value;
        const problem = Strength.problem(password) || (password !== form.querySelector('#new-password-2').value ? 'The two passwords are different.' : '');
        if (problem) {
          error.textContent = problem;
          form.classList.remove('shake');
          void form.offsetWidth;
          form.classList.add('shake');
          return;
        }
        try {
          await this.actions.reset(this.email, this.code, password);
          this.doneStep();
        } catch (err) {
          error.textContent = err.message;
        }
      });
    }

    doneStep() {
      this.show(`
        <div class="done-badge">${window.Cinnarios.icons.check()}</div>
        <h1>Password updated</h1>
        <p class="auth-lede">Every other device has been signed out, to keep your account safe. Sign in with your new password.</p>
        <a class="button auth-wide" href="#enter">Sign in</a>`);
    }

    bind() {
      this.emailStep();
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.ResetPage = ResetPage;
}());
