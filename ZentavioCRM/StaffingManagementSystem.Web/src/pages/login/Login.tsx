import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { ITMLogo } from "@/components/brand/ITMLogo";
import { useAuth } from "@/context/AuthContext";
import { authService } from "@/services/authService";
import { LoginPanelPattern } from "@/pages/login/LoginPanelPattern";
import { requiredEmailRule } from "@/utils/validation";
import "./Login.css";

interface LoginFormValues {
  email: string;
  password: string;
  rememberMe: boolean;
}

const FEATURES = [
  {
    icon: "bi-funnel-fill",
    title: "Lead-to-Customer Pipeline",
    description: "Track every lead from first contact through qualification, conversion and beyond.",
  },
  {
    icon: "bi-building",
    title: "Unified Customer Records",
    description: "Contacts, addresses, activities and history — all in one place for every account.",
  },
  {
    icon: "bi-graph-up-arrow",
    title: "Reports & Analytics",
    description: "Configurable dashboards and role-based access for teams of any size.",
  },
];

export default function Login() {
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [showPassword, setShowPassword] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    mode: "onBlur",
    defaultValues: { email: "", password: "", rememberMe: false },
  });

  const onSubmit = async (values: LoginFormValues) => {
    setServerError(null);

    const response = await authService.login({
      email: values.email,
      password: values.password,
      rememberMe: values.rememberMe,
    });

    if (!response.success || !response.data) {
      setServerError(response.message || "Unable to sign in. Please try again.");
      return;
    }

    authService.persistSession(response.data, values.rememberMe);
    setSession(response.data.user);
    navigate("/dashboard", { replace: true });
  };

  return (
    <div className="login-page">
      {/* Left promotional panel */}
      <aside className="login-panel">
        <div className="login-panel__pattern">
          <LoginPanelPattern />
        </div>

        <div className="login-panel__content">
          <ITMLogo height={40} variant="light" />

          <h1 className="login-panel__heading">ZentavioCRM</h1>
          <p className="login-panel__subtitle">{"Manage Customers.\nAccelerate Sales.\nGrow Every Relationship."}</p>
          <p className="login-panel__description">
            A configurable CRM platform that helps growing businesses manage leads, customers and their complete
            sales pipeline in one place.
          </p>
        </div>

        <div className="login-panel__features">
          {FEATURES.map((feature) => (
            <div className="login-panel__feature" key={feature.title}>
              <div className="login-panel__feature-icon">
                <i className={`bi ${feature.icon}`} aria-hidden="true" />
              </div>
              <div>
                <div className="login-panel__feature-title">{feature.title}</div>
                <div className="login-panel__feature-desc">{feature.description}</div>
              </div>
            </div>
          ))}
        </div>

        <div className="login-panel__footer">
          <div>Version 1.0</div>
          <div>&copy; 2026 ITMusketeers Consultancy Services</div>
        </div>
      </aside>

      {/* Right login panel */}
      <main className="login-content">
        <div className="login-card">
          <ITMLogo height={32} className="mb-1" />

          <h2 className="login-card__heading">Welcome Back</h2>
          <p className="login-card__subtitle">Sign in to continue to ZentavioCRM</p>

          {serverError && (
            <div className="login-alert" role="alert">
              <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
              <span>{serverError}</span>
            </div>
          )}

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

            <div className="login-field">
              <label htmlFor="password">Password</label>
              <div className={`login-input-group ${errors.password ? "is-invalid" : ""}`}>
                <span className="login-input-group__icon">
                  <i className="bi bi-lock-fill" aria-hidden="true" />
                </span>
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  placeholder="Enter your password"
                  aria-label="Password"
                  aria-invalid={errors.password ? "true" : "false"}
                  aria-describedby={errors.password ? "password-error" : undefined}
                  {...register("password", {
                    required: "Password is required.",
                    minLength: { value: 8, message: "Password must be at least 8 characters." },
                  })}
                />
                <button
                  type="button"
                  className="login-input-group__toggle"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  aria-pressed={showPassword}
                  tabIndex={0}
                >
                  <i className={`bi ${showPassword ? "bi-eye-slash-fill" : "bi-eye-fill"}`} aria-hidden="true" />
                </button>
              </div>
              {errors.password && (
                <div className="login-field__error" id="password-error">
                  {errors.password.message}
                </div>
              )}
            </div>

            <div className="login-row">
              <label className="login-remember" htmlFor="rememberMe">
                <input id="rememberMe" type="checkbox" {...register("rememberMe")} />
                Remember Me
              </label>
              <a className="login-forgot" href="/forgot-password">
                Forgot Password?
              </a>
            </div>

            <button type="submit" className="login-submit" disabled={isSubmitting} aria-busy={isSubmitting}>
              {isSubmitting && <span className="login-spinner" aria-hidden="true" />}
              {isSubmitting ? "Signing In..." : "Sign In"}
            </button>
          </form>

          <div className="login-divider">OR</div>

          <div className="login-sso">
            <button type="button" className="login-sso__btn" disabled aria-disabled="true">
              <i className="bi bi-microsoft" aria-hidden="true" />
              Microsoft
            </button>
            <button type="button" className="login-sso__btn" disabled aria-disabled="true">
              <i className="bi bi-google" aria-hidden="true" />
              Google
            </button>
          </div>

          <a href="/user-manual.html" target="_blank" rel="noopener noreferrer" className="login-manual-link">
            <i className="bi bi-book-half" aria-hidden="true" />
            User Manual
          </a>
        </div>
      </main>
    </div>
  );
}
