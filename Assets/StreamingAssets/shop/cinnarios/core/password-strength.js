// How strong a new password is, matching the API's rule: at least 12 characters with upper
// and lower case letters and a number. Score 0 to 4 fills the four bars under the field.
(function () {
  const LABELS = Object.freeze(['Too short', 'Weak', 'Fair', 'Good', 'Strong']);

  class PasswordStrength {
    static meetsRule(password) {
      return password.length >= 12 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /\d/.test(password);
    }

    static score(password) {
      if (!password) return 0;
      let score = 0;
      if (password.length >= 8) score += 1;
      if (password.length >= 12) score += 1;
      if (/[a-z]/.test(password) && /[A-Z]/.test(password) && /\d/.test(password)) score += 1;
      if (password.length >= 16 || /[^A-Za-z0-9]/.test(password)) score += 1;
      return PasswordStrength.meetsRule(password) ? Math.max(score, 3) : Math.min(score, 2);
    }

    static label(password) {
      return password ? LABELS[PasswordStrength.score(password)] : '';
    }

    /** What is still missing, as one sentence, or '' when the password is acceptable. */
    static problem(password) {
      if (password.length < 12) return 'Use at least 12 characters.';
      if (!/[a-z]/.test(password) || !/[A-Z]/.test(password)) return 'Mix upper and lower case letters.';
      if (!/\d/.test(password)) return 'Add a number.';
      return '';
    }
  }

  window.Cinnarios = window.Cinnarios || {};
  window.Cinnarios.PasswordStrength = PasswordStrength;
}());
