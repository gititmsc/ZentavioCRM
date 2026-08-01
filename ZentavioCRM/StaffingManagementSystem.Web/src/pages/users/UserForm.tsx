import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { userService, type CreateUserRequest, type ManagedUser } from "@/services/userService";
import { roleService, type Role } from "@/services/roleService";
import { departmentService, type Department } from "@/services/departmentService";
import { territoryService, type Territory } from "@/services/territoryService";
import { UserAvatar } from "@/components/users/UserAvatar";
import { emailPatternRule } from "@/utils/validation";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

// isActive only applies in edit mode (new users are always created active); kept on one
// form type so the same fields/inputs work for both create and edit.
type FormValues = CreateUserRequest & { isActive: boolean };

interface ResetPasswordFormValues {
  newPassword: string;
}

export default function UserForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();
  const { user: currentUser, bumpAvatarVersion, hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.UsersManage);

  const [roles, setRoles] = useState<Role[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [territories, setTerritories] = useState<Territory[]>([]);
  const [managers, setManagers] = useState<ManagedUser[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const [loadedFullName, setLoadedFullName] = useState("");
  const [hasProfilePhoto, setHasProfilePhoto] = useState(false);
  const [photoVersion, setPhotoVersion] = useState(0);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const photoInputRef = useRef<HTMLInputElement>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    defaultValues: {
      employeeCode: "",
      firstName: "",
      lastName: "",
      email: "",
      mobile: null,
      password: "",
      roleId: "",
      departmentId: null,
      reportingManagerId: null,
      territoryId: null,
      isActive: true,
    },
  });

  useEffect(() => {
    (async () => {
      const [roleResult, departmentResult, territoryResult, userResult] = await Promise.all([
        roleService.getAll(),
        departmentService.getAll(),
        territoryService.getAll(),
        userService.getAll(),
      ]);

      if (roleResult.success && roleResult.data) setRoles(roleResult.data);
      if (departmentResult.success && departmentResult.data) setDepartments(departmentResult.data);
      if (territoryResult.success && territoryResult.data) setTerritories(territoryResult.data);
      if (userResult.success && userResult.data) setManagers(userResult.data.filter((u) => u.id !== id));

      if (isEditMode && id) {
        const existing = await userService.getById(id);
        if (existing.success && existing.data) {
          reset({
            employeeCode: existing.data.employeeCode,
            firstName: existing.data.firstName,
            lastName: existing.data.lastName,
            email: existing.data.email,
            mobile: existing.data.mobile,
            password: "",
            roleId: existing.data.roleId,
            departmentId: existing.data.departmentId,
            reportingManagerId: existing.data.reportingManagerId,
            territoryId: existing.data.territoryId,
            isActive: existing.data.isActive,
          });
          setHasProfilePhoto(existing.data.hasProfilePhoto);
          setLoadedFullName(existing.data.fullName);
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const handlePhotoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !id) return;

    setPhotoError(null);
    setIsUploadingPhoto(true);
    const result = await userService.uploadPhoto(id, file);
    setIsUploadingPhoto(false);

    if (!result.success) {
      setPhotoError(result.message || "Unable to upload photo.");
      return;
    }
    setHasProfilePhoto(true);
    setPhotoVersion((v) => v + 1);
    if (currentUser && id === currentUser.id) bumpAvatarVersion();
  };

  const [resetPasswordError, setResetPasswordError] = useState<string | null>(null);
  const [resetPasswordSuccess, setResetPasswordSuccess] = useState<string | null>(null);

  const {
    register: registerResetPassword,
    handleSubmit: handleResetPasswordSubmit,
    reset: resetResetPasswordForm,
    formState: { errors: resetPasswordErrors, isSubmitting: isResettingPassword },
  } = useForm<ResetPasswordFormValues>({ defaultValues: { newPassword: "" } });

  const onAdminResetPassword = async (values: ResetPasswordFormValues) => {
    if (!id) return;

    setResetPasswordError(null);
    setResetPasswordSuccess(null);

    const result = await userService.resetPassword(id, values.newPassword);
    if (!result.success) {
      setResetPasswordError(result.message || "Unable to reset password.");
      return;
    }

    setResetPasswordSuccess("Password reset. The user has been signed out of all devices.");
    resetResetPasswordForm({ newPassword: "" });
  };

  const handleRemovePhoto = async () => {
    if (!id || !window.confirm("Remove this profile photo?")) return;

    setPhotoError(null);
    const result = await userService.deletePhoto(id);
    if (!result.success) {
      setPhotoError(result.message || "Unable to remove photo.");
      return;
    }
    setHasProfilePhoto(false);
    setPhotoVersion((v) => v + 1);
    if (currentUser && id === currentUser.id) bumpAvatarVersion();
  };

  const onSubmit = async (values: FormValues) => {
    setServerError(null);

    const mobile = values.mobile || null;
    const departmentId = values.departmentId || null;
    const reportingManagerId = values.reportingManagerId || null;
    const territoryId = values.territoryId || null;

    const result =
      isEditMode && id
        ? await userService.update(id, {
            firstName: values.firstName,
            lastName: values.lastName,
            mobile,
            roleId: values.roleId,
            departmentId,
            reportingManagerId,
            territoryId,
            isActive: values.isActive,
          })
        : await userService.create({
            employeeCode: values.employeeCode,
            firstName: values.firstName,
            lastName: values.lastName,
            email: values.email,
            mobile,
            password: values.password,
            roleId: values.roleId,
            departmentId,
            reportingManagerId,
            territoryId,
          });

    if (!result.success) {
      setServerError(result.message || "Unable to save user.");
      return;
    }

    navigate("/users");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title={isEditMode ? "Edit User" : "New User"}
        subtitle={isEditMode ? "Update this user's profile, role, and assignment." : "Create a new user account."}
        backTo="/users"
        backLabel="Back to Users"
      />

      {serverError && <div className="alert alert-danger">{serverError}</div>}

      {isEditMode && id && (
        <FormSection icon="bi-camera" title="Profile Photo">
          <div className="d-flex align-items-center gap-3">
            <UserAvatar
              key={`${id}-${photoVersion}`}
              userId={id}
              fullName={loadedFullName || "?"}
              hasProfilePhoto={hasProfilePhoto}
              size={64}
            />
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
        </FormSection>
      )}

      {isEditMode && id && canManage && (
        <FormSection
          icon="bi-key"
          title="Reset Password"
          description="Sets a new password for this user without needing their current one, and signs them out of every device. Use this if a user is locked out or an account may be compromised."
        >
          {resetPasswordError && <div className="alert alert-danger py-2">{resetPasswordError}</div>}
          {resetPasswordSuccess && <div className="alert alert-success py-2">{resetPasswordSuccess}</div>}

          <form onSubmit={handleResetPasswordSubmit(onAdminResetPassword)} noValidate className="row g-2 align-items-end">
            <div className="col-md-6">
              <label className="form-label small">New Password</label>
              <input
                type="password"
                autoComplete="new-password"
                className={`form-control form-control-sm ${resetPasswordErrors.newPassword ? "is-invalid" : ""}`}
                {...registerResetPassword("newPassword", {
                  required: "A new password is required.",
                  minLength: { value: 8, message: "Password must be at least 8 characters." },
                })}
              />
              {resetPasswordErrors.newPassword && (
                <div className="invalid-feedback">{resetPasswordErrors.newPassword.message}</div>
              )}
            </div>
            <div className="col-md-3">
              <button type="submit" className="btn btn-sm btn-outline-danger" disabled={isResettingPassword}>
                {isResettingPassword ? "Resetting..." : "Reset Password"}
              </button>
            </div>
          </form>
        </FormSection>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <FormSection icon="bi-person-badge" title="User Identity" description="Name, contact details, and login credentials.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label">Employee Code</label>
              <input
                className={`form-control ${errors.employeeCode ? "is-invalid" : ""}`}
                disabled={isEditMode}
                {...register("employeeCode", { required: "Employee code is required." })}
              />
              {errors.employeeCode && <div className="invalid-feedback">{errors.employeeCode.message}</div>}
            </div>

            <div className="col-md-4">
              <label className="form-label">First Name</label>
              <input
                className={`form-control ${errors.firstName ? "is-invalid" : ""}`}
                {...register("firstName", { required: "First name is required." })}
              />
              {errors.firstName && <div className="invalid-feedback">{errors.firstName.message}</div>}
            </div>

            <div className="col-md-4">
              <label className="form-label">Last Name</label>
              <input
                className={`form-control ${errors.lastName ? "is-invalid" : ""}`}
                {...register("lastName", { required: "Last name is required." })}
              />
              {errors.lastName && <div className="invalid-feedback">{errors.lastName.message}</div>}
            </div>

            <div className="col-md-6">
              <label className="form-label">Email</label>
              <input
                type="email"
                className={`form-control ${errors.email ? "is-invalid" : ""}`}
                disabled={isEditMode}
                {...register("email", { required: "Email is required.", ...emailPatternRule })}
              />
              {errors.email && <div className="invalid-feedback">{errors.email.message}</div>}
            </div>

            <div className="col-md-6">
              <label className="form-label">Mobile</label>
              <input className="form-control" {...register("mobile")} />
            </div>

            {!isEditMode && (
              <div className="col-md-6">
                <label className="form-label">Temporary Password</label>
                <input
                  type="password"
                  className={`form-control ${errors.password ? "is-invalid" : ""}`}
                  {...register("password", {
                    required: "Password is required.",
                    minLength: { value: 8, message: "Password must be at least 8 characters." },
                  })}
                />
                {errors.password && <div className="invalid-feedback">{errors.password.message}</div>}
              </div>
            )}
          </div>
        </FormSection>

        <FormSection icon="bi-briefcase" title="Role & Assignment" description="Role, department, reporting line, and territory.">
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Role</label>
              <select
                className={`form-select ${errors.roleId ? "is-invalid" : ""}`}
                {...register("roleId", { required: "A role must be selected." })}
              >
                <option value="">Select a role</option>
                {roles.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.name}
                  </option>
                ))}
              </select>
              {errors.roleId && <div className="invalid-feedback">{errors.roleId.message}</div>}
            </div>

            <div className="col-md-6">
              <label className="form-label">Department</label>
              <select className="form-select" {...register("departmentId")}>
                <option value="">None</option>
                {departments.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-6">
              <label className="form-label">Reporting Manager</label>
              <select className="form-select" {...register("reportingManagerId")}>
                <option value="">None</option>
                {managers.map((manager) => (
                  <option key={manager.id} value={manager.id}>
                    {manager.fullName}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-6">
              <label className="form-label">Territory</label>
              <select className="form-select" {...register("territoryId")}>
                <option value="">None</option>
                {territories.map((territory) => (
                  <option key={territory.id} value={territory.id}>
                    {territory.name}
                  </option>
                ))}
              </select>
            </div>

            {isEditMode && (
              <div className="col-md-6 d-flex align-items-end">
                <div className="form-check">
                  <input id="isActive" type="checkbox" className="form-check-input" {...register("isActive")} />
                  <label className="form-check-label" htmlFor="isActive">
                    Active
                  </label>
                </div>
              </div>
            )}
          </div>
        </FormSection>

        <FormActionBar>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/users")}>
            Cancel
          </button>
        </FormActionBar>
      </form>
    </div>
  );
}
