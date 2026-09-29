import { useForm } from "react-hook-form";
import { platformAdminService } from "@/services/platformAdminService";

interface FormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

interface ChangePasswordModalProps {
  onClose: () => void;
}

export function ChangePasswordModal({ onClose }: ChangePasswordModalProps) {
  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors, isSubmitting, isSubmitSuccessful },
  } = useForm<FormValues>({ defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" } });

  const onSubmit = async (values: FormValues) => {
    if (values.newPassword !== values.confirmPassword) {
      setError("confirmPassword", { message: "Passwords do not match." });
      return;
    }

    const response = await platformAdminService.changeMyPassword({
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    });

    if (!response.success) {
      setError("root", { message: response.message || "Could not change password." });
      return;
    }
  };

  return (
    <div className="modal d-block" tabIndex={-1} style={{ background: "rgba(15, 23, 42, 0.45)" }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content" style={{ borderRadius: "var(--itm-radius-card)" }}>
          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="modal-header">
              <h5 className="modal-title d-flex align-items-center gap-2">
                <i className="bi bi-key-fill text-primary" aria-hidden="true" />
                Change Password
              </h5>
              <button type="button" className="btn-close" onClick={onClose} aria-label="Close" />
            </div>

            <div className="modal-body">
              {isSubmitSuccessful ? (
                <div className="alert alert-success d-flex align-items-center gap-2 mb-0">
                  <i className="bi bi-check-circle-fill" />
                  Password changed successfully.
                </div>
              ) : (
                <>
                  {errors.root && <div className="alert alert-danger py-2">{errors.root.message}</div>}

                  <div className="mb-3">
                    <label className="form-label">Current Password</label>
                    <input
                      type="password"
                      className={`form-control ${errors.currentPassword ? "is-invalid" : ""}`}
                      {...register("currentPassword", { required: "Current password is required." })}
                    />
                    {errors.currentPassword && <div className="invalid-feedback">{errors.currentPassword.message}</div>}
                  </div>

                  <div className="mb-3">
                    <label className="form-label">New Password</label>
                    <input
                      type="password"
                      className={`form-control ${errors.newPassword ? "is-invalid" : ""}`}
                      {...register("newPassword", {
                        required: "New password is required.",
                        minLength: { value: 8, message: "At least 8 characters." },
                      })}
                    />
                    {errors.newPassword && <div className="invalid-feedback">{errors.newPassword.message}</div>}
                  </div>

                  <div className="mb-1">
                    <label className="form-label">Confirm New Password</label>
                    <input
                      type="password"
                      className={`form-control ${errors.confirmPassword ? "is-invalid" : ""}`}
                      {...register("confirmPassword", {
                        required: "Please confirm your new password.",
                        validate: (value) => value === watch("newPassword") || "Passwords do not match.",
                      })}
                    />
                    {errors.confirmPassword && <div className="invalid-feedback">{errors.confirmPassword.message}</div>}
                  </div>
                </>
              )}
            </div>

            <div className="modal-footer">
              {isSubmitSuccessful ? (
                <button type="button" className="btn btn-primary" onClick={onClose}>
                  Done
                </button>
              ) : (
                <>
                  <button type="button" className="btn btn-outline-secondary" onClick={onClose} disabled={isSubmitting}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                    {isSubmitting ? "Saving..." : "Change Password"}
                  </button>
                </>
              )}
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
