import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { apiClient } from "@/services/apiClient";
import { requiredEmailRule } from "@/utils/validation";
import "./Login.css";

interface ForgotPasswordFormValues {
  email: string;
}

interface ApiResponse {
  success: boolean;
  message: string;
}

export default function ForgotPassword() {
  const [serverError, setServerError] = useState<string | null>(null);
  const [submittedMessage, setSubmittedMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    mode: "onBlur",
    defaultValues: { email: "" },
  });

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    setServerError(null);

    try {
      const response = await apiClient.post<ApiResponse>("/api/auth/forgot-password", { email: values.email });
      // The API always returns the same generic message whether or not the email matched an
      // account — that's deliberate (prevents learning which addresses have accounts), so we
      // just display whatever it says.
      setSubmittedMessage(response.data.message || "If an account exists for that email, a reset link has been sent.");
    } catch {
      setServerError("Unable to reach the server. Please try again.");
    }
  };

  return (
    <div className="login-page">
      <main className="login-content">
        <div className="login-card">
          <ZentavioLogo height={32} className="mb-1" />

          <h2 className="login-card__heading">Forgot Password</h2>
          <p className="login-card__subtitle">
            Enter your email address and we&rsquo;ll send you a link to reset your password.
          </p>

          {serverError && (
            <div className="login-alert" role="alert">
              <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
              <span>{serverError}</span>
            </div>
          )}

          {submittedMessage ? (
            <div className="login-alert" style={{ background: "var(--itm-primary)" }} role="status">
              <i className="bi bi-check-circle-fill" aria-hidden="true" />
              <span>{submittedMessage}</span>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              <div className="login-field">
                <label htmlFor="email">Email Address</label>
                <div className={`login-input-group ${errors.email ? "is-invalid" : ""}`}>
                  <span className="login-input-group__icon">
                    <i className="bi bi-envelope-fill" aria-hidden="true" />
                  </span>
                  <input
                    id="email"
                    type="email"
                    autoFocus
                    autoComplete="email"
                    placeholder="Enter your email address"
                    aria-label="Email address"
                    aria-invalid={errors.email ? "true" : "false"}
                    aria-describedby={errors.email ? "email-error" : undefined}
                    {...register("email", requiredEmailRule)}
                  />
                </div>
                {errors.email && (
                  <div className="login-field__error" id="email-error">
                    {errors.email.message}
                  </div>
                )}
              </div>

              <button type="submit" className="login-submit" disabled={isSubmitting} aria-busy={isSubmitting}>
                {isSubmitting && <span className="login-spinner" aria-hidden="true" />}
                {isSubmitting ? "Sending..." : "Send Reset Link"}
              </button>
            </form>
          )}

          <div className="login-row" style={{ justifyContent: "center", marginTop: "1.4rem", marginBottom: 0 }}>
            <Link className="login-forgot" to="/login">
              Back to Sign In
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}
