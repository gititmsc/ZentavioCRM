using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Users;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IUserService"/>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<IReadOnlyList<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(Map).ToList();
        }

        public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user is null
                ? ApiResponse<UserDto>.FailureResponse("User not found.")
                : ApiResponse<UserDto>.SuccessResponse(Map(user));
        }

        public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _userRepository.EmailExistsAsync(email))
            {
                return ApiResponse<UserDto>.FailureResponse("A user with this email already exists.", ["Email already in use."]);
            }

            if (await _userRepository.EmployeeCodeExistsAsync(request.EmployeeCode))
            {
                return ApiResponse<UserDto>.FailureResponse("A user with this employee code already exists.", ["Employee code already in use."]);
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId);
            if (role is null)
            {
                return ApiResponse<UserDto>.FailureResponse("Selected role does not exist.");
            }

            var user = new User
            {
                EmployeeCode = request.EmployeeCode.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Mobile = request.Mobile,
                PasswordHash = _passwordHasher.Hash(request.Password),
                RoleId = request.RoleId,
                DepartmentId = request.DepartmentId,
                ReportingManagerId = request.ReportingManagerId,
                TerritoryId = request.TerritoryId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _userRepository.AddAsync(user);

            var created = await _userRepository.GetByIdAsync(user.Id);
            return ApiResponse<UserDto>.SuccessResponse(Map(created!), "User created.");
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found.");
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId);
            if (role is null)
            {
                return ApiResponse<UserDto>.FailureResponse("Selected role does not exist.");
            }

            if (request.ReportingManagerId == id)
            {
                return ApiResponse<UserDto>.FailureResponse("A user cannot report to themself.", ["A user cannot report to themself."]);
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Mobile = request.Mobile;
            user.RoleId = request.RoleId;
            user.DepartmentId = request.DepartmentId;
            user.ReportingManagerId = request.ReportingManagerId;
            user.TerritoryId = request.TerritoryId;
            user.IsActive = request.IsActive;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            var updated = await _userRepository.GetByIdAsync(id);
            return ApiResponse<UserDto>.SuccessResponse(Map(updated!), "User updated.");
        }

        public async Task<ApiResponse<UserDto>> UploadPhotoAsync(Guid id, string contentType, byte[] content)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found.");
            }

            user.ProfilePhotoContent = content;
            user.ProfilePhotoContentType = contentType;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return ApiResponse<UserDto>.SuccessResponse(Map(user), "Profile photo updated.");
        }

        public async Task<(byte[] Content, string ContentType)?> DownloadPhotoAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user?.ProfilePhotoContent is null || user.ProfilePhotoContent.Length == 0)
            {
                return null;
            }

            return (user.ProfilePhotoContent, user.ProfilePhotoContentType ?? "application/octet-stream");
        }

        public async Task<ApiResponse<bool>> DeletePhotoAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
            {
                return ApiResponse<bool>.FailureResponse("User not found.");
            }

            user.ProfilePhotoContent = null;
            user.ProfilePhotoContentType = null;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true, "Profile photo removed.");
        }

        private static UserDto Map(User user) => new()
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email,
            Mobile = user.Mobile,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            ReportingManagerId = user.ReportingManagerId,
            ReportingManagerName = user.ReportingManager?.FullName,
            TerritoryId = user.TerritoryId,
            TerritoryName = user.Territory?.Name,
            HasProfilePhoto = user.ProfilePhotoContent is not null && user.ProfilePhotoContent.Length > 0,
            IsActive = user.IsActive,
            LastLoginAtUtc = user.LastLoginAtUtc,
        };
    }
}
