import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { useAuth } from "@/context/AuthContext";
import { authService } from "@/services/authService";
import "./Login.css";

interface LoginFormValues {
  email: string;
  password: string;
  rememberMe: boolean;
}

const FEATURES = [
  {
    icon: "bi-buildings-fill",
    title: "Full Tenant Lifecycle",
    description: "Provision, suspend, reactivate or stop any tenant — reversible and always audited.",
  },
  {
    icon: "bi-speedometer2",
    title: "Live Plan & Usage",
    description: "See seats, records and storage against plan limits in real time, per tenant.",
  },
  {
    icon: "bi-person-badge-fill",
    title: "Secure Impersonation",
    description: "Sign in as a tenant's admin for support, without ever touching a password.",
  },
];

export default function Login() {
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [searchParams] = useSearchParams();
  const [showPassword, setShowPassword] = useState(false);
  const [serverError, setServerError] = useState<string | null>(
    searchParams.get("reason") === "expired" ? "Your session has expired. Please log in again." : null
  );

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
    setSession(response.data.admin);
    navigate("/dashboard", { replace: true });
  };

  return (
    <div className="login-page">
      <aside className="login-panel">
        <div className="login-panel__content">
          <ZentavioLogo height={36} variant="light" />

          <div className="login-panel__badge">
            <i className="bi bi-shield-fill-check" aria-hidden="true" />
            Platform Admin
          </div>

          <h1 className="login-panel__heading">Run the whole platform from one console.</h1>
          <p className="login-panel__description">
            Provision tenants, monitor plan usage, and support customers — all from a single, secure control
            plane built for ZentavioCRM operators.
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
          <div>Internal Use Only &middot; Version 1.0</div>
          <div>&copy; 2026 ITMusketeers Consultancy Services</div>
        </div>
      </aside>

      <main className="login-content">
        <div className="login-card">
          <ZentavioLogo height={30} className="mb-1" />

          <h2 className="login-card__heading">Platform Admin</h2>
          <p className="login-card__subtitle">Sign in to manage tenants across ZentavioCRM</p>

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
                  aria-invalid={errors.email ? "true" : "false"}
                  {...register("email", { required: "Email is required." })}
                />
              </div>
              {errors.email && <div className="login-field__error">{errors.email.message}</div>}
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
                  aria-invalid={errors.password ? "true" : "false"}
                  {...register("password", { required: "Password is required." })}
                />
                <button
                  type="button"
                  className="login-input-group__toggle"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  <i className={`bi ${showPassword ? "bi-eye-slash-fill" : "bi-eye-fill"}`} aria-hidden="true" />
                </button>
              </div>
              {errors.password && <div className="login-field__error">{errors.password.message}</div>}
            </div>

            <div className="login-row">
              <label className="login-remember" htmlFor="rememberMe">
                <input id="rememberMe" type="checkbox" {...register("rememberMe")} />
                Remember Me
              </label>
            </div>

            <button type="submit" className="login-submit" disabled={isSubmitting} aria-busy={isSubmitting}>
              {isSubmitting && <span className="login-spinner" aria-hidden="true" />}
              {isSubmitting ? "Signing In..." : "Sign In"}
            </button>
          </form>

          <div className="text-center text-muted small mt-3">
            New to ZentavioCRM? <Link to="/signup">Start a free trial</Link>
          </div>
        </div>
      </main>
    </div>
  );
}
