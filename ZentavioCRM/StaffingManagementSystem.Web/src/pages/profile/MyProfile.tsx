import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useAuth } from "@/context/AuthContext";
import { UserAvatar } from "@/components/users/UserAvatar";
import { userService, type ManagedUser } from "@/services/userService";
import { delegationService, type UserDelegation, type SaveUserDelegationRequest } from "@/services/delegationService";
import { persistRefreshedTokens } from "@/services/authStorage";

/** yyyy-MM-dd for a native <input type="date">. */
function toDateInputValue(value: string): string {
  return value.slice(0, 10);
}

interface DelegationFormValues {
  delegateUserId: string;
  startDate: string;
  endDate: string;
  notes: string;
}

interface ChangePasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function MyProfile() {
  const { user, bumpAvatarVersion } = useAuth();

  const [hasProfilePhoto, setHasProfilePhoto] = useState(false);
  const [photoVersion, setPhotoVersion] = useState(0);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const photoInputRef = useRef<HTMLInputElement>(null);

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [delegations, setDelegations] = useState<UserDelegation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [delegationError, setDelegationError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<DelegationFormValues>({
    defaultValues: { delegateUserId: "", startDate: "", endDate: "", notes: "" },
  });

  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null);

  const {
    register: registerPassword,
    handleSubmit: handlePasswordSubmit,
    watch: watchPassword,
    reset: resetPasswordForm,
    formState: { errors: passwordErrors, isSubmitting: isChangingPassword },
  } = useForm<ChangePasswordFormValues>({
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const onChangePassword = async (values: ChangePasswordFormValues) => {
    if (!user) return;

    setPasswordError(null);
    setPasswordSuccess(null);

    const result = await userService.changePassword(user.id, {
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    });

    if (!result.success || !result.data) {
      setPasswordError(result.message || "Unable to change password.");
      return;
    }

    // Old refresh tokens (all devices, including this one) were just revoked server-side — persist
    // the fresh pair immediately so this session's next silent refresh doesn't fail.
    persistRefreshedTokens(result.data.token, result.data.refreshToken);

    setPasswordSuccess("Your password has been changed.");
    resetPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
  };

  const loadDelegations = async () => {
    const result = await delegationService.getMine();
    if (result.success && result.data) {
      setDelegations(result.data);
    }
  };

  useEffect(() => {
    (async () => {
      if (!user) return;

      const [selfResult, usersResult] = await Promise.all([userService.getById(user.id), userService.getAll()]);

      if (selfResult.success && selfResult.data) {
        setHasProfilePhoto(selfResult.data.hasProfilePhoto);
      }
      if (usersResult.success && usersResult.data) {
        setUsers(usersResult.data.filter((u) => u.id !== user.id));
      }

      await loadDelegations();
      setIsLoading(false);
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id]);

  const handlePhotoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !user) return;

    setPhotoError(null);
    setIsUploadingPhoto(true);
    const result = await userService.uploadPhoto(user.id, file);
    setIsUploadingPhoto(false);

    if (!result.success) {
      setPhotoError(result.message || "Unable to upload photo.");
      return;
    }
    setHasProfilePhoto(true);
    setPhotoVersion((v) => v + 1);
    bumpAvatarVersion();
  };

  const handleRemovePhoto = async () => {
    if (!user || !window.confirm("Remove your profile photo?")) return;

    setPhotoError(null);
    const result = await userService.deletePhoto(user.id);
    if (!result.success) {
      setPhotoError(result.message || "Unable to remove photo.");
      return;
    }
    setHasProfilePhoto(false);
    setPhotoVersion((v) => v + 1);
    bumpAvatarVersion();
  };

  const onCreateDelegation = async (values: DelegationFormValues) => {
    setDelegationError(null);

    const request: SaveUserDelegationRequest = {
      delegateUserId: values.delegateUserId,
      startDateUtc: `${values.startDate}T00:00:00.000Z`,
      endDateUtc: `${values.endDate}T23:59:59.999Z`,
      notes: values.notes || null,
    };

    const result = await delegationService.create(request);
    if (!result.success) {
      setDelegationError(result.message || "Unable to create delegation.");
      return;
    }

    reset({ delegateUserId: "", startDate: "", endDate: "", notes: "" });
    await loadDelegations();
  };

  const handleCancelDelegation = async (delegation: UserDelegation) => {
    if (!window.confirm(`Cancel the delegation to ${delegation.delegateUserName}?`)) return;

    const result = await delegationService.remove(delegation.id);
    if (!result.success) {
      window.alert(result.message || "Unable to cancel delegation.");
      return;
    }
    await loadDelegations();
  };

  if (!user || isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <h1 className="h4 mb-4">My Profile</h1>

      <div className="card shadow-sm border-0 mb-4" style={{ maxWidth: 780 }}>
        <div className="card-body">
          <h2 className="h6 fw-semibold mb-3">Profile Photo</h2>
          <div className="d-flex align-items-center gap-3">
            <UserAvatar key={`${user.id}-${photoVersion}`} userId={user.id} fullName={user.fullName} hasProfilePhoto={hasProfilePhoto} size={72} />
            <div>
              {photoError && <div className="text-danger small mb-1">{photoError}</div>}
              <button
                type="button"
                className="btn btn-sm btn-outline-primary me-2"
                disabled={isUploadingPhoto}
                onClick={() => photoInputRef.current?.click()}
              >
                <i className="bi bi-upload me-1" aria-hidden="true" />
                {isUploadingPhoto ? "Uploading..." : hasProfilePhoto ? "Change Photo" : "Upload Photo"}
              </button>
              {hasProfilePhoto && (
                <button type="button" className="btn btn-sm btn-outline-danger" onClick={handleRemovePhoto}>
                  Remove
                </button>
              )}
              <input
                ref={photoInputRef}
                type="file"
                accept="image/png,image/jpeg,image/gif"
                className="d-none"
                onChange={handlePhotoChange}
              />
            </div>
          </div>
        </div>
      </div>

      <div className="card shadow-sm border-0 mb-4" style={{ maxWidth: 780 }}>
        <div className="card-body">
          <h2 className="h6 fw-semibold mb-3">Change Password</h2>

          {passwordError && <div className="alert alert-danger py-2">{passwordError}</div>}
          {passwordSuccess && <div className="alert alert-success py-2">{passwordSuccess}</div>}

          <form onSubmit={handlePasswordSubmit(onChangePassword)} noValidate className="row g-3">
            <div className="col-md-4">
              <label className="form-label">Current Password</label>
              <input
                type="password"
                autoComplete="current-password"
                className={`form-control ${passwordErrors.currentPassword ? "is-invalid" : ""}`}
                {...registerPassword("currentPassword", { required: "Current password is required." })}
              />
              {passwordErrors.currentPassword && (
                <div className="invalid-feedback">{passwordErrors.currentPassword.message}</div>
              )}
            </div>

            <div className="col-md-4">
              <label className="form-label">New Password</label>
              <input
                type="password"
                autoComplete="new-password"
                className={`form-control ${passwordErrors.newPassword ? "is-invalid" : ""}`}
                {...registerPassword("newPassword", {
                  required: "New password is required.",
                  minLength: { value: 8, message: "Password must be at least 8 characters." },
                })}
              />
              {passwordErrors.newPassword && <div className="invalid-feedback">{passwordErrors.newPassword.message}</div>}
            </div>

            <div className="col-md-4">
              <label className="form-label">Confirm New Password</label>
              <input
                type="password"
                autoComplete="new-password"
                className={`form-control ${passwordErrors.confirmPassword ? "is-invalid" : ""}`}
                {...registerPassword("confirmPassword", {
                  required: "Please confirm your new password.",
                  validate: (value) => value === watchPassword("newPassword") || "Passwords do not match.",
                })}
              />
              {passwordErrors.confirmPassword && (
                <div className="invalid-feedback">{passwordErrors.confirmPassword.message}</div>
              )}
            </div>

            <div className="col-12">
              <button type="submit" className="btn btn-sm btn-primary" disabled={isChangingPassword}>
                {isChangingPassword ? "Changing..." : "Change Password"}
              </button>
            </div>
          </form>
        </div>
      </div>

      <div className="card shadow-sm border-0" style={{ maxWidth: 780 }}>
        <div className="card-body">
          <h2 className="h6 fw-semibold mb-1">Out of Office / Delegation</h2>
          <p className="text-muted small mb-3">
            While active, your delegate will see your assigned Leads, Customers, and Opportunities, and will receive
            your due-date reminders instead of you.
          </p>

          {delegationError && <div className="alert alert-danger py-2">{delegationError}</div>}

          <form onSubmit={handleSubmit(onCreateDelegation)} noValidate className="row g-2 align-items-end mb-4">
            <div className="col-md-4">
              <label className="form-label small">Delegate To</label>
              <select
                className={`form-select form-select-sm ${errors.delegateUserId ? "is-invalid" : ""}`}
                {...register("delegateUserId", { required: "A delegate must be selected." })}
              >
                <option value="">Select a user</option>
                {users.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.fullName}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-3">
              <label className="form-label small">Start Date</label>
              <input
                type="date"
                className={`form-control form-control-sm ${errors.startDate ? "is-invalid" : ""}`}
                {...register("startDate", { required: "Start date is required." })}
              />
            </div>

            <div className="col-md-3">
              <label className="form-label small">End Date</label>
              <input
                type="date"
                className={`form-control form-control-sm ${errors.endDate ? "is-invalid" : ""}`}
                {...register("endDate", { required: "End date is required." })}
              />
            </div>

            <div className="col-md-2">
              <button type="submit" className="btn btn-sm btn-primary w-100" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Add"}
              </button>
            </div>

            <div className="col-12">
              <input className="form-control form-control-sm" placeholder="Notes (optional)" {...register("notes")} />
            </div>
          </form>

          {delegations.length === 0 && <div className="text-muted small">No delegations set up.</div>}

          {delegations.length > 0 && (
            <table className="table table-sm align-middle mb-0">
              <thead>
                <tr>
                  <th>Delegate</th>
                  <th>Start</th>
                  <th>End</th>
                  <th>Status</th>
                  <th>Notes</th>
                  <th className="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {delegations.map((d) => (
                  <tr key={d.id}>
                    <td>{d.delegateUserName}</td>
                    <td>{toDateInputValue(d.startDateUtc)}</td>
                    <td>{toDateInputValue(d.endDateUtc)}</td>
                    <td>
                      <span className={`badge ${d.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                        {d.isActive ? "Active" : "Not active"}
                      </span>
                    </td>
                    <td className="text-muted small">{d.notes ?? <span>&mdash;</span>}</td>
                    <td className="text-end">
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => handleCancelDelegation(d)}
                      >
                        Cancel
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
