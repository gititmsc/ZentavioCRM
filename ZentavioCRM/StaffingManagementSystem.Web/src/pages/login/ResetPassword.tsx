import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useSearchParams } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { apiClient } from "@/services/apiClient";
import "./Login.css";

interface ResetPasswordFormValues {
  newPassword: string;
  confirmPassword: string;
}

interface ApiResponse {
  success: boolean;
  message: string;
  errors?: string[];
}

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const [serverError, setServerError] = useState<string | null>(null);
  const [succeeded, setSucceeded] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    mode: "onBlur",
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  const onSubmit = async (values: ResetPasswordFormValues) => {
    setServerError(null);

    if (!token) {
      setServerError("This password reset link is invalid.");
      return;
    }

    try {
      const response = await apiClient.post<ApiResponse>("/api/auth/reset-password", {
        token,
        newPassword: values.newPassword,
      });

      if (!response.data.success) {
        setServerError(response.data.message || "Unable to reset your password.");
        return;
      }

      setSucceeded(true);
    } catch (error) {
      const apiMessage = (error as { response?: { data?: ApiResponse } }).response?.data?.message;
      setServerError(apiMessage ?? "Unable to reach the server. Please try again.");
    }
  };

  return (
    <div className="login-page">
      <main className="login-content">
        <div className="login-card">
          <ZentavioLogo height={32} className="mb-1" />

          <h2 className="login-card__heading">Reset Password</h2>
          {!succeeded && <p className="login-card__subtitle">Choose a new password for your account.</p>}

          {serverError && (
            <div className="login-alert" role="alert">
              <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
              <span>{serverError}</span>
            </div>
          )}

          {succeeded ? (
            <>
              <div className="login-alert" style={{ background: "var(--itm-primary)" }} role="status">
                <i className="bi bi-check-circle-fill" aria-hidden="true" />
                <span>Your password has been reset. Please log in with your new password.</span>
              </div>
              <Link className="login-submit" to="/login" style={{ textDecoration: "none" }}>
                Go to Sign In
              </Link>
            </>
          ) : !token ? (
            <div className="login-row" style={{ justifyContent: "center" }}>
              <Link className="login-forgot" to="/forgot-password">
                Request a new reset link
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              <div className="login-field">
                <label htmlFor="newPassword">New Password</label>
                <div className={`login-input-group ${errors.newPassword ? "is-invalid" : ""}`}>
                  <span className="login-input-group__icon">
                    <i className="bi bi-lock-fill" aria-hidden="true" />
                  </span>
                  <input
                    id="newPassword"
                    type="password"
                    autoFocus
                    autoComplete="new-password"
                    placeholder="Enter a new password"
                    aria-label="New password"
                    aria-invalid={errors.newPassword ? "true" : "false"}
                    aria-describedby={errors.newPassword ? "newPassword-error" : undefined}
                    {...register("newPassword", {
                      required: "New password is required.",
                      minLength: { value: 8, message: "Password must be at least 8 characters." },
                    })}
                  />
                </div>
                {errors.newPassword && (
                  <div className="login-field__error" id="newPassword-error">
                    {errors.newPassword.message}
                  </div>
                )}
              </div>

              <div className="login-field">
                <label htmlFor="confirmPassword">Confirm Password</label>
                <div className={`login-input-group ${errors.confirmPassword ? "is-invalid" : ""}`}>
                  <span className="login-input-group__icon">
                    <i className="bi bi-lock-fill" aria-hidden="true" />
                  </span>
                  <input
                    id="confirmPassword"
                    type="password"
                    autoComplete="new-password"
                    placeholder="Re-enter your new password"
                    aria-label="Confirm new password"
                    aria-invalid={errors.confirmPassword ? "true" : "false"}
                    aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
                    {...register("confirmPassword", {
                      required: "Please confirm your new password.",
                      validate: (value) => value === watch("newPassword") || "Passwords do not match.",
                    })}
                  />
                </div>
                {errors.confirmPassword && (
                  <div className="login-field__error" id="confirmPassword-error">
                    {errors.confirmPassword.message}
                  </div>
                )}
              </div>

              <button type="submit" className="login-submit" disabled={isSubmitting} aria-busy={isSubmitting}>
                {isSubmitting && <span className="login-spinner" aria-hidden="true" />}
                {isSubmitting ? "Resetting..." : "Reset Password"}
              </button>
            </form>
          )}

          {!succeeded && (
            <div className="login-row" style={{ justifyContent: "center", marginTop: "1.4rem", marginBottom: 0 }}>
              <Link className="login-forgot" to="/login">
                Back to Sign In
              </Link>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
