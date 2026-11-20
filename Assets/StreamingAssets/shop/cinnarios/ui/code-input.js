// The 6-digit code boxes, Apple style. One hidden input does the typing (so paste and
// autofill from Mail or Messages work); six boxes show it. Digits spring in, the next box
// lifts with a blush ring and a blinking caret, a wrong code wiggles and clears, and a right
// code folds the boxes into a check. A small state machine keeps the steps in order:
// idle → checking → ok, or idle → checking → error → idle.
(function () {
  const LENGTH = 6;
  const RESEND_SECONDS = 30;

  class CodeInput {
    /**
     * @param {HTMLElement} root  an empty element to draw into
     * @param {{check: (code: string) => Promise<void>, resend: () => Promise<void>, done: () => void}} actions
     */
    constructor(root, actions) {
      this.root = root;
      this.actions = actions;
      this.state = 'idle';
      this.attempt = 0;
      this.resendIn = RESEND_SECONDS;
    }

    mount() {
      this.root.innerHTML = `
        <div class="code-wrap">
          <div class="code-row">${Array.from({ length: LENGTH }, () => '<span class="code-box"><span class="code-digit"></span><span class="code-caret"></span></span>').join('')}</div>
          <label for="code-entry" class="sr-only">6-digit code</label>
          <input id="code-entry" class="code-entry" inputmode="numeric" autocomplete="one-time-code" maxlength="${LENGTH}" />
          <div class="code-done" aria-hidden="true">${window.Cinnarios.icons.check()}</div>
          <span class="spinner" hidden></span>
        </div>
        <p class="code-message" role="status"></p>
        <p class="code-resend"></p>`;
      this.input = this.root.querySelector('#code-entry');
      this.boxes = [...this.root.querySelectorAll('.code-box')];
      this.input.addEventListener('input', () => this.typed());
      this.input.addEventListener('focus', () => this.draw());
      this.input.addEventListener('blur', () => this.draw());
      this.root.querySelector('.code-row').addEventListener('click', () => this.input.focus());
      this.startTimer();
      this.draw();
      this.input.focus();
    }

    typed() {
      if (this.state !== 'idle') return;
      this.input.value = this.input.value.replace(/\D/g, '').slice(0, LENGTH);
      this.root.querySelector('.code-message').textContent = '';
      this.draw();
      if (this.input.value.length === LENGTH) this.submit(this.input.value);
    }

    async submit(code) {
      this.set('checking');
      try {
        await this.actions.check(code);
        this.set('ok');
        clearInterval(this.timer);
        setTimeout(() => this.actions.done(), 650);
      } catch (err) {
        this.attempt += 1;
        this.root.querySelector('.code-message').textContent = err.message;
        this.set('error');
        setTimeout(() => {
          this.input.value = '';
          this.set('idle');
          this.input.focus();
        }, 700);
      }
    }

    set(state) {
      this.state = state;
      this.draw();
    }

    draw() {
      const value = this.input.value;
      const focused = document.activeElement === this.input;
      const wrap = this.root.querySelector('.code-wrap');
      wrap.dataset.state = this.state;
      // Alternating animation names restart the wiggle on every wrong code.
      wrap.style.animationName = this.state === 'error' ? (this.attempt % 2 ? 'shakeA' : 'shakeB') : 'none';
      this.root.querySelector('.spinner').hidden = this.state !== 'checking';
      this.boxes.forEach((box, i) => {
        box.querySelector('.code-digit').textContent = value[i] || '';
        box.classList.toggle('filled', i < value.length);
        box.classList.toggle('active', focused && this.state === 'idle' && i === Math.min(value.length, LENGTH - 1) && value.length < LENGTH);
      });
    }

    startTimer() {
      clearInterval(this.timer);
      this.resendIn = RESEND_SECONDS;
      this.drawResend();
      this.timer = setInterval(() => {
        if (!this.root.isConnected) return clearInterval(this.timer);
        this.resendIn -= 1;
        this.drawResend();
        if (this.resendIn <= 0) clearInterval(this.timer);
        return undefined;
      }, 1000);
    }

    drawResend() {
      const line = this.root.querySelector('.code-resend');
      if (this.resendIn > 0) {
        line.innerHTML = `Send a new code in <span class="tick tick-${this.resendIn % 2 ? 'a' : 'b'}">0:${String(this.resendIn).padStart(2, '0')}</span>`;
        return;
      }
      line.innerHTML = '<button type="button" class="text-button" id="code-resend">Send a new code</button>';
      line.querySelector('#code-resend').addEventListener('click', async () => {
        const note = this.root.querySelector('.code-message');
        const button = line.querySelector('#code-resend');
        button.disabled = true;
        button.textContent = 'Sending…';
        note.classList.remove('error');
        note.textContent = 'Sending a new code…';
        try {
          await this.actions.resend();
          note.textContent = 'A new code was sent. Check your inbox and your spam folder.';
          this.input.value = '';
          this.set('idle');
          this.startTimer();
          this.input.focus();
        } catch (err) {
          note.classList.add('error');
          note.textContent = err.message || 'The code could not be sent.';
          button.disabled = false;
          button.textContent = 'Send a new code';
        }
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.CodeInput = CodeInput;
}());
