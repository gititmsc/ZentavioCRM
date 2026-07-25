import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { userService, type CreateUserRequest, type ManagedUser } from "@/services/userService";
import { roleService, type Role } from "@/services/roleService";
import { departmentService, type Department } from "@/services/departmentService";

// isActive only applies in edit mode (new users are always created active); kept on one
// form type so the same fields/inputs work for both create and edit.
type FormValues = CreateUserRequest & { isActive: boolean };

export default function UserForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [roles, setRoles] = useState<Role[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [managers, setManagers] = useState<ManagedUser[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
      isActive: true,
    },
  });

  useEffect(() => {
    (async () => {
      const [roleResult, departmentResult, userResult] = await Promise.all([
        roleService.getAll(),
        departmentService.getAll(),
        userService.getAll(),
      ]);

      if (roleResult.success && roleResult.data) setRoles(roleResult.data);
      if (departmentResult.success && departmentResult.data) setDepartments(departmentResult.data);
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
            isActive: existing.data.isActive,
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: FormValues) => {
    setServerError(null);

    const mobile = values.mobile || null;
    const departmentId = values.departmentId || null;
    const reportingManagerId = values.reportingManagerId || null;

    const result =
      isEditMode && id
        ? await userService.update(id, {
            firstName: values.firstName,
            lastName: values.lastName,
            mobile,
            roleId: values.roleId,
            departmentId,
            reportingManagerId,
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
      <h1 className="h4 mb-4">{isEditMode ? "Edit User" : "New User"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 720 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
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
                <input className="form-control" {...register("lastName")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Email</label>
                <input
                  type="email"
                  className={`form-control ${errors.email ? "is-invalid" : ""}`}
                  disabled={isEditMode}
                  {...register("email", { required: "Email is required." })}
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

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </button>
              <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/users")}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
