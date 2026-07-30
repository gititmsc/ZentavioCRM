/**
 * Shared react-hook-form validation snippets, so every form applies the same rules
 * instead of each screen re-inventing (or forgetting) its own regex/required message.
 */

/** Spread into a register() options object: register("email", { ...emailPatternRule }) */
export const emailPatternRule = {
  pattern: {
    value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    message: "Enter a valid email address.",
  },
};

/** Same as emailPatternRule but also requires a value — for mandatory email fields. */
export const requiredEmailRule = {
  required: "Email address is required.",
  ...emailPatternRule,
};
