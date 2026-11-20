// The Discover page: the 3D gallery of 18 different pieces, a button to browse the whole
// catalogue, and the band for trying a print the buyer already owns.
(function () {
  class DiscoverPage {
    /**
     * @param {object[]} works the picked pieces, in order
     * @param {number} total   catalogue size, for the Browse button
     * @param {{esc: Function}} format
     */
    constructor(works, total, format) {
      this.works = works;
      this.total = total;
      this.esc = format.esc;
    }

    html() {
      const icons = window.Cinnarios.icons;
      const ticks = this.works.map(() => '<span data-tick></span>').join('');
      return `
      <section class="discover-stage" tabindex="0" aria-label="Discover gallery. Scroll, drag or use the arrow keys to browse; press a painting to open it.">
        <div class="discover-track"></div>
        <div class="discover-intro">
          <h1>Discover</h1>
          <p>No idea where to start? Here are ${this.works.length} pieces, each one different from the last.</p>
        </div>
        <div class="discover-controls">
          <button type="button" class="round-button" data-prev aria-label="Previous piece">${icons.back()}</button>
          <div class="discover-ticks" aria-hidden="true">${ticks}</div>
          <button type="button" class="round-button" data-next aria-label="Next piece">${icons.next()}</button>
        </div>
        <a class="button discover-browse" href="#search" data-browse>Browse all ${this.esc(this.total)} pieces${icons.next()}</a>
      </section>
      ${DiscoverPage.tryOwnBand()}`;
    }

    /** Shared with the homepage: invites a buyer to try a print they already own. */
    static tryOwnBand() {
      const icons = window.Cinnarios.icons;
      return `
      <section class="try-band">
        <div>
          <p class="eyebrow">Try your own piece</p>
          <h2>Already have a print? See it here first.</h2>
          <p>Upload a photo of it, enter its size in centimetres, and hang it on your wall in the camera.</p>
        </div>
        <a class="button" href="#try-own">${icons.upload()}Upload a photo</a>
      </section>`;
    }

    bind() {
      const stage = document.querySelector('.discover-stage');
      document.querySelector('[data-browse]').addEventListener('click', () => {
        try { sessionStorage.removeItem('cinnarios.search'); } catch { /* nothing kept */ }
      });
      this.gallery = new window.Cinnarios.DiscoverGallery(stage, stage.querySelector('.discover-track'), this.works, (work) => {
        location.hash = `#work/${work.sourceId}/discover`;
      });
      this.gallery.start();
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.DiscoverPage = DiscoverPage;
}());
