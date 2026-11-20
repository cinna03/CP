// Confirm your email once, right after creating an account with an email and password.
// The 6-digit code is sent to that inbox. Google sign-up does not use this screen.
(function () {
  class VerifyPage {
    /**
     * @param {string} email
     * @param {{send: () => Promise<void>, verify: (code: string) => Promise<void>}} actions
     * @param {{esc: Function}} format
     */
    constructor(email, actions, format) {
      this.email = email;
      this.actions = actions;
      this.esc = format.esc;
    }

    html() {
      return `
      <section class="auth-card"><div id="verify-step" class="step-in">
        <div class="auth-badge">${window.Cinnarios.icons.mail()}</div>
        <h1>Confirm your email</h1>
        <p class="auth-lede">Enter the 6-digit code for <strong>${this.esc(this.email)}</strong>. You only do this once.</p>
        <div id="verify-code"></div>
        <p class="error" id="verify-error" role="alert"></p>
        <p class="auth-foot"><button type="button" class="text-button" id="verify-cancel">Use a different email</button></p>
      </div></section>`;
    }

    async bind() {
      new window.Cinnarios.CodeInput(document.querySelector('#verify-code'), {
        check: (code) => this.actions.verify(code),
        resend: () => this.actions.send(),
        done: () => {
          try { sessionStorage.removeItem('cinnarios.codeSentFor'); } catch { /* nothing kept */ }
          document.querySelector('#verify-step').outerHTML = `<div class="step-in">
            <div class="done-badge">${window.Cinnarios.icons.check()}</div>
            <h1>Email confirmed</h1>
            <p class="auth-lede">Your account is ready. Your tries, saved paintings and cart will be kept here.</p>
            <a class="button auth-wide" href="#account">Go to your account</a></div>`;
        },
      }).mount();
      document.querySelector('#verify-cancel')?.addEventListener('click', () => {
        try { sessionStorage.removeItem('cinnarios.codeSentFor'); } catch { /* nothing kept */ }
        this.actions.cancel?.();
      });
      const note = document.querySelector('#verify-code .code-message');
      let already = false;
      try { already = sessionStorage.getItem('cinnarios.codeSentFor') === this.email; } catch { already = false; }
      if (already) {
        if (note) note.textContent = 'Enter the code from your email. It expires in 15 minutes.';
        return;
      }
      if (note) note.textContent = 'Sending a code…';
      try {
        await this.actions.send();
        try { sessionStorage.setItem('cinnarios.codeSentFor', this.email); } catch { /* private mode */ }
        if (note) note.textContent = 'A code was sent. Check your inbox and your spam folder.';
      } catch (err) {
        if (note) {
          note.classList.add('error');
          note.textContent = err.message || 'The code could not be sent.';
        }
      }
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.VerifyPage = VerifyPage;
}());
