import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { platformAuthService } from "@/services/platformAuthService";
import { usePlatformAuth } from "@/context/PlatformAuthContext";
import "./Login.css";

export default function Login() {
  const navigate = useNavigate();
  const { setSession } = usePlatformAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    const result = await platformAuthService.login({ email, password });
    setIsSubmitting(false);

    if (!result.success || !result.data) {
      setError(result.message || "Invalid email or password.");
      return;
    }

    setSession(result.data.admin);
    navigate("/");
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <ZentavioLogo height={32} />
        <h1 className="login-card__heading">Platform Admin</h1>
        <p className="login-card__subtitle">Sign in to manage ZentavioCRM tenants</p>

        {error && (
          <div className="login-alert" role="alert">
            <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="login-field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              autoFocus
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="login-field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button type="submit" className="login-submit" disabled={isSubmitting}>
            {isSubmitting ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <div className="login-footer">&copy; 2026 ITMusketeers Consultancy Services</div>
      </div>
    </div>
  );
}
