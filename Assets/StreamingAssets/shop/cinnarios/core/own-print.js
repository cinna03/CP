// A print the buyer already owns, to try on a wall. It is shown at the size they enter and
// can't be bought. This file checks the photo and the size, shrinks the photo for the phone,
// and builds the camera link. The photo is only handed to the camera on this phone; it is
// never sent to the shop's API.
(function () {
  const MIN_CM = 5;
  const MAX_CM = 600;
  const MAX_PHOTO_BYTES = 25 * 1024 * 1024;
  const MAX_EDGE_PX = 2048;
  const LINK_PREFIX = 'cinnarios://own/';

  class OwnPrint {
    constructor({ photo = null, widthCm = '', heightCm = '', name = '' } = {}) {
      this.photo = photo;
      this.widthCm = widthCm;
      this.heightCm = heightCm;
      this.name = name;
    }

    static sizeOk(value) {
      const number = Number(value);
      return Number.isFinite(number) && number >= MIN_CM && number <= MAX_CM;
    }

    hasSize() {
      return OwnPrint.sizeOk(this.widthCm) && OwnPrint.sizeOk(this.heightCm);
    }

    sizeLabel() {
      return this.hasSize() ? `${Number(this.widthCm)} × ${Number(this.heightCm)} cm` : 'Add the size';
    }

    /** The first thing missing before the camera can open, as a sentence, or '' when ready. */
    problem() {
      if (!this.photo) return 'Add a photo of your print first.';
      if (!this.hasSize()) return `Enter the width and height in centimetres, between ${MIN_CM} and ${MAX_CM}.`;
      return '';
    }

    /** Rejects files that are not pictures or are too large to handle on a phone. */
    static checkFile(file) {
      if (!file || !/^image\//.test(file.type)) return 'Choose a photo (JPEG, PNG or HEIC).';
      if (file.size > MAX_PHOTO_BYTES) return 'That photo is over 25 MB. Choose a smaller one.';
      return '';
    }

    /** Draws the photo no larger than 2048 px on its longest edge and returns it as a JPEG. */
    static async shrink(file) {
      const bitmap = await createImageBitmap(file);
      const scale = Math.min(1, MAX_EDGE_PX / Math.max(bitmap.width, bitmap.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(bitmap.width * scale);
      canvas.height = Math.round(bitmap.height * scale);
      canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height);
      bitmap.close?.();
      return new Promise((resolve, reject) => {
        canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('The photo could not be read.'))), 'image/jpeg', 0.88);
      });
    }

    /** cinnarios://own/<photoId>?w=..&h=..&name=..&api=.. — what the phone app opens. */
    link(photoId, apiOrigin) {
      const query = new URLSearchParams({ w: String(Number(this.widthCm)), h: String(Number(this.heightCm)), api: apiOrigin });
      if (this.name.trim()) query.set('name', this.name.trim().slice(0, 80));
      return `${LINK_PREFIX}${encodeURIComponent(photoId)}?${query.toString()}`;
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.OwnPrint = OwnPrint;
}());
