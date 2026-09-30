import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { signupService } from "@/services/signupService";
import "@/pages/login/Login.css";

interface SignupFormValues {
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
}

const FEATURES = [
  {
    icon: "bi-lightning-charge-fill",
    title: "Up and Running in Minutes",
    description: "No sales call, no setup fee — your own ZentavioCRM workspace, ready immediately.",
  },
  {
    icon: "bi-gift-fill",
    title: "14-Day Free Trial",
    description: "Full access to try ZentavioCRM with your team before deciding on a plan.",
  },
  {
    icon: "bi-shield-lock-fill",
    title: "Your Own Isolated Database",
    description: "Every company gets a dedicated database — your data is never mixed with anyone else's.",
  },
];

export default function Signup() {
  const [showPassword, setShowPassword] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [success, setSuccess] = useState<{ subdomain: string } | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SignupFormValues>({
    mode: "onBlur",
    defaultValues: { companyName: "", subdomain: "", adminFirstName: "", adminLastName: "", adminEmail: "", adminPassword: "" },
  });

  const onSubmit = async (values: SignupFormValues) => {
    setServerError(null);

    const response = await signupService.signup({
      companyName: values.companyName.trim(),
      subdomain: values.subdomain.trim().toLowerCase(),
      adminFirstName: values.adminFirstName.trim(),
      adminLastName: values.adminLastName.trim() || undefined,
      adminEmail: values.adminEmail.trim(),
      adminPassword: values.adminPassword,
    });

    if (!response.success || !response.data) {
      setServerError(response.message || "Could not create your account. Please try again.");
      return;
    }

    setSuccess(response.data);
  };

  if (success) {
    return (
      <div className="login-page">
        <main className="login-content" style={{ width: "100%" }}>
          <div className="login-card text-center">
            <ZentavioLogo height={30} className="mb-3" />
            <i className="bi bi-check-circle-fill text-success" style={{ fontSize: "2.5rem" }} aria-hidden="true" />
            <h2 className="login-card__heading mt-3">You're all set!</h2>
            <p className="login-card__subtitle">
              Your ZentavioCRM workspace at <strong>{success.subdomain}.zentaviocrm.com</strong> is ready. Sign in there with
              the email and password you just chose to start your 14-day trial.
            </p>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="login-page">
      <aside className="login-panel">
        <div className="login-panel__content">
          <ZentavioLogo height={36} variant="light" />

          <div className="login-panel__badge">
            <i className="bi bi-rocket-takeoff-fill" aria-hidden="true" />
            Start Free Trial
          </div>

          <h1 className="login-panel__heading">Bring your sales team onto ZentavioCRM.</h1>
          <p className="login-panel__description">
            Create your own workspace in minutes — leads, customers, quotes and orders, all in one place.
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

          <h2 className="login-card__heading">Create Your Workspace</h2>
          <p className="login-card__subtitle">Start your 14-day free trial — no credit card required</p>

          {serverError && (
            <div className="login-alert" role="alert">
              <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
              <span>{serverError}</span>
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="login-field">
              <label htmlFor="companyName">Company Name</label>
              <div className={`login-input-group ${errors.companyName ? "is-invalid" : ""}`}>
                <span className="login-input-group__icon">
                  <i className="bi bi-building-fill" aria-hidden="true" />
                </span>
                <input
                  id="companyName"
                  autoFocus
                  placeholder="Acme Inc."
                  aria-invalid={errors.companyName ? "true" : "false"}
                  {...register("companyName", { required: "Company name is required." })}
                />
              </div>
              {errors.companyName && <div className="login-field__error">{errors.companyName.message}</div>}
            </div>

            <div className="login-field">
              <label htmlFor="subdomain">Subdomain</label>
              <div className={`login-input-group ${errors.subdomain ? "is-invalid" : ""}`}>
                <span className="login-input-group__icon">
                  <i className="bi bi-globe2" aria-hidden="true" />
                </span>
                <input
                  id="subdomain"
                  placeholder="acme"
                  aria-invalid={errors.subdomain ? "true" : "false"}
                  {...register("subdomain", {
                    required: "Subdomain is required.",
                    pattern: {
                      value: /^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$/,
                      message: "Lowercase letters, digits and hyphens only.",
                    },
                  })}
                />
              </div>
              {errors.subdomain && <div className="login-field__error">{errors.subdomain.message}</div>}
            </div>

            <div className="row g-2">
              <div className="col-6">
                <div className="login-field">
                  <label htmlFor="adminFirstName">First Name</label>
                  <div className={`login-input-group ${errors.adminFirstName ? "is-invalid" : ""}`}>
                    <input
                      id="adminFirstName"
                      placeholder="Jane"
                      aria-invalid={errors.adminFirstName ? "true" : "false"}
                      {...register("adminFirstName", { required: "Required." })}
                    />
                  </div>
                  {errors.adminFirstName && <div className="login-field__error">{errors.adminFirstName.message}</div>}
                </div>
              </div>
              <div className="col-6">
                <div className="login-field">
                  <label htmlFor="adminLastName">Last Name</label>
                  <div className="login-input-group">
                    <input id="adminLastName" placeholder="Doe" {...register("adminLastName")} />
                  </div>
                </div>
              </div>
            </div>

            <div className="login-field">
              <label htmlFor="adminEmail">Email Address</label>
              <div className={`login-input-group ${errors.adminEmail ? "is-invalid" : ""}`}>
                <span className="login-input-group__icon">
                  <i className="bi bi-envelope-fill" aria-hidden="true" />
                </span>
                <input
                  id="adminEmail"
                  type="email"
                  autoComplete="email"
                  placeholder="jane@acme.com"
                  aria-invalid={errors.adminEmail ? "true" : "false"}
                  {...register("adminEmail", { required: "Email is required." })}
                />
              </div>
              {errors.adminEmail && <div className="login-field__error">{errors.adminEmail.message}</div>}
            </div>

            <div className="login-field">
              <label htmlFor="adminPassword">Password</label>
              <div className={`login-input-group ${errors.adminPassword ? "is-invalid" : ""}`}>
                <span className="login-input-group__icon">
                  <i className="bi bi-lock-fill" aria-hidden="true" />
                </span>
                <input
                  id="adminPassword"
                  type={showPassword ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="At least 8 characters"
                  aria-invalid={errors.adminPassword ? "true" : "false"}
                  {...register("adminPassword", {
                    required: "Password is required.",
                    minLength: { value: 8, message: "At least 8 characters." },
                  })}
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
              {errors.adminPassword && <div className="login-field__error">{errors.adminPassword.message}</div>}
            </div>

            <button type="submit" className="login-submit" disabled={isSubmitting} aria-busy={isSubmitting}>
              {isSubmitting && <span className="login-spinner" aria-hidden="true" />}
              {isSubmitting ? "Creating your workspace..." : "Start Free Trial"}
            </button>
          </form>

          <div className="text-center text-muted small mt-3">
            Already have an account? <Link to="/login">Sign in</Link>
          </div>
        </div>
      </main>
    </div>
  );
}
