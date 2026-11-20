// "Try your own piece": a photo of the buyer's print, its real size, an optional name, and a
// preview in true proportions. Open in camera hands it to the phone app through `actions`.
(function () {
  const PREVIEW_PX = 300;

  class TryOwnPage {
    /**
     * @param {{openCamera: (print: OwnPrint, photo: Blob) => Promise<void>, onPhone: boolean}} actions
     * @param {{esc: Function}} format
     */
    constructor(actions, format) {
      this.actions = actions;
      this.esc = format.esc;
      this.print = new window.Cinnarios.OwnPrint();
      this.photoBlob = null;
    }

    html() {
      const icons = window.Cinnarios.icons;
      return `
      <section class="try-own">
        <p class="eyebrow">Try your own piece</p>
        <h1>Already have a print? See it on your wall first.</h1>
        <div class="try-own-layout">
          <form id="own-form" class="checkout-form" novalidate>
            <div class="field">
              <p class="field-title">1. A photo of your print</p>
              <label class="photo-drop" for="own-photo">${icons.upload()}<span id="photo-label">Choose a photo</span><small>Straight on, in good light, the whole print in frame.</small></label>
              <input id="own-photo" type="file" accept="image/*" class="sr-only" />
            </div>
            <div class="field">
              <p class="field-title">2. Its real size</p>
              <div class="field-pair">
                <div><label for="own-w">Width, cm</label><input id="own-w" inputmode="decimal" placeholder="50" /></div>
                <div><label for="own-h">Height, cm</label><input id="own-h" inputmode="decimal" placeholder="70" /></div>
              </div>
              <p class="piece-note">Measure the print itself, edge to edge. The camera hangs it at exactly this size.</p>
            </div>
            <div class="field">
              <label for="own-name" class="field-title">3. A name, if you like</label>
              <input id="own-name" maxlength="80" placeholder="Grandma's harbour" />
            </div>
            <p id="own-error" class="error" role="alert"></p>
            <button type="submit" class="cart-checkout" id="own-open">${icons.camera()}Open in camera</button>
          </form>
          <div class="own-side">
            <div class="own-preview">
              <div class="own-frame" id="own-frame"><span>Your photo shows here</span></div>
              <span class="tag tag-blush" id="own-size">Add the size</span>
              <span class="own-caption">Your own print</span>
            </div>
            <div class="own-notes">
              <p><strong>Not from our collection.</strong> We show your print at the size you enter, and you can ask the decision assistant about it on the wall. Because we didn't make or measure it, we can't stand behind its size, colour or condition, and it can't be bought here.</p>
              <p><strong>This helps us grow the collection.</strong> We note that an own print was tried, its size and how the decision went. If many people bring their own pieces, it tells us what the collection is missing.</p>
            </div>
          </div>
        </div>
        <div id="own-done" class="order-placed" hidden></div>
      </section>`;
    }

    /** Keeps the preview frame in the print's real proportions inside a 300 px box. */
    paintPreview() {
      const frame = document.querySelector('#own-frame');
      const print = this.print;
      if (print.hasSize()) {
        const scale = Math.min(PREVIEW_PX / Number(print.widthCm), PREVIEW_PX / Number(print.heightCm));
        frame.style.width = `${Math.round(Number(print.widthCm) * scale)}px`;
        frame.style.height = `${Math.round(Number(print.heightCm) * scale)}px`;
      } else {
        frame.style.width = '';
        frame.style.height = '';
      }
      document.querySelector('#own-size').textContent = print.sizeLabel();
    }

    async choosePhoto(file) {
      const error = document.querySelector('#own-error');
      const problem = window.Cinnarios.OwnPrint.checkFile(file);
      if (problem) {
        error.textContent = problem;
        return;
      }
      error.textContent = '';
      try {
        this.photoBlob = await window.Cinnarios.OwnPrint.shrink(file);
      } catch (err) {
        error.textContent = err.message;
        return;
      }
      if (this.print.photo) URL.revokeObjectURL(this.print.photo);
      this.print.photo = URL.createObjectURL(this.photoBlob);
      document.querySelector('#own-frame').innerHTML = `<img src="${this.print.photo}" alt="Your print" />`;
      document.querySelector('#photo-label').textContent = 'Change the photo';
    }

    showDone() {
      const esc = this.esc;
      const done = document.querySelector('#own-done');
      document.querySelector('.try-own-layout').hidden = true;
      done.hidden = false;
      done.innerHTML = this.actions.onPhone
        ? `<h2>Opening the camera</h2><p>Find a wall, tap to hang your print at ${esc(this.print.sizeLabel())}, then lock it. The decision assistant opens once it is on the wall.</p><button type="button" class="quiet" id="own-again">Change the photo or size</button>`
        : `<h2>Open this on your phone</h2><p>The camera is in the Cinnarios app on your phone. Open Try your own piece there to hang your print at ${esc(this.print.sizeLabel())}.</p><button type="button" class="quiet" id="own-again">Back</button>`;
      done.querySelector('#own-again').addEventListener('click', () => {
        done.hidden = true;
        document.querySelector('.try-own-layout').hidden = false;
      });
    }

    bind() {
      const form = document.querySelector('#own-form');
      const error = document.querySelector('#own-error');
      document.querySelector('#own-photo').addEventListener('change', (event) => this.choosePhoto(event.target.files[0]));
      const sizes = [['#own-w', 'widthCm'], ['#own-h', 'heightCm']];
      sizes.forEach(([selector, key]) => document.querySelector(selector).addEventListener('input', (event) => {
        this.print[key] = event.target.value.replace(',', '.');
        error.textContent = '';
        this.paintPreview();
      }));
      document.querySelector('#own-name').addEventListener('input', (event) => { this.print.name = event.target.value; });
      form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const problem = this.print.problem();
        if (problem) {
          error.textContent = problem;
          form.classList.remove('shake');
          void form.offsetWidth;
          form.classList.add('shake');
          return;
        }
        try {
          if (this.actions.onPhone) await this.actions.openCamera(this.print, this.photoBlob);
          this.showDone();
        } catch (err) {
          error.textContent = err.message;
        }
      });
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.TryOwnPage = TryOwnPage;
}());
