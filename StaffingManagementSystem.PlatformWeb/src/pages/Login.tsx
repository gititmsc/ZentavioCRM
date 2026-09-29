import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { platformAuthService } from "@/services/platformAuthService";
import { usePlatformAuth } from "@/context/PlatformAuthContext";

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
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="card shadow-sm border-0" style={{ width: 380 }}>
        <div className="card-body p-4">
          <h1 className="h4 mb-1">ZentavioCRM</h1>
          <p className="text-muted mb-4">Platform Admin</p>

          {error && <div className="alert alert-danger py-2">{error}</div>}

          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label className="form-label">Email</label>
              <input
                type="email"
                className="form-control"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
              />
            </div>
            <div className="mb-3">
              <label className="form-label">Password</label>
              <input
                type="password"
                className="form-control"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <button type="submit" className="btn btn-primary w-100" disabled={isSubmitting}>
              {isSubmitting ? "Signing in..." : "Sign In"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
