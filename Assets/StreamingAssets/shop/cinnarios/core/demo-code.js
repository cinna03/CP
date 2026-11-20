// Placeholder for the emailed 6-digit codes. This version sends no email, so, as in the
// design mockups, the code screens accept one fixed demo code and show it on the screen.
// Swapping in real emailed codes later only means replacing check() with an API call.
(function () {
  const CODE = '482916';

  class DemoCode {
    static get value() {
      return CODE;
    }

    /** Shown under the code boxes, as in the mockup. */
    static note() {
      return 'In this demo the code is 482 916.';
    }

    static async check(code) {
      if (code !== CODE) throw new Error("That code doesn't match. Check it and try again.");
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.DemoCode = DemoCode;
}());
