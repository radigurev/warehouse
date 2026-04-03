using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.DBModel;
using Warehouse.DBModel.Models.Auth;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements user management operations.
/// <para>See <see cref="IUserService"/>.</para>
/// </summary>
public sealed class UserService : IUserService
{
    private readonly WarehouseDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public UserService(
        WarehouseDbContext context,
        IPasswordHasher passwordHasher,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<UserDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result<UserDetailDto>.Failure("USER_NOT_FOUND", "User not found.", 404);

        UserDetailDto dto = _mapper.Map<UserDetailDto>(user);
        return Result<UserDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<UserDto>>> GetPaginatedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        IQueryable<User> query = _context.Users.AsNoTracking();

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u => u.Username.Contains(searchTerm) || u.Email.Contains(searchTerm));

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<User> items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UserDto> dtos = _mapper.Map<IReadOnlyList<UserDto>>(items);

        PaginatedResponse<UserDto> response = new()
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<UserDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<CreateUserResponse>> CreateAsync(
        CreateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.IsActive, cancellationToken).ConfigureAwait(false))
            return Result<CreateUserResponse>.Failure("DUPLICATE_USERNAME", "A user with this username already exists.", 409);

        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.IsActive, cancellationToken).ConfigureAwait(false))
            return Result<CreateUserResponse>.Failure("DUPLICATE_EMAIL", "A user with this email already exists.", 409);

        string plainPassword = string.IsNullOrWhiteSpace(request.Password)
            ? GeneratePassword()
            : request.Password;

        User user = new()
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(plainPassword),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(user.Id, "CreateUser", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);

        // TODO: Send generated password to user via email when email service is available.
        CreateUserResponse response = new()
        {
            Id = user.Id,
            Username = user.Username,
            GeneratedPassword = plainPassword
        };

        return Result<CreateUserResponse>.Success(response);
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        const string all = upper + lower + digits + special;

        byte[] randomBytes = new byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);

        char[] password = new char[12];
        password[0] = upper[randomBytes[0] % upper.Length];
        password[1] = lower[randomBytes[1] % lower.Length];
        password[2] = digits[randomBytes[2] % digits.Length];
        password[3] = special[randomBytes[3] % special.Length];

        for (int i = 4; i < 12; i++)
            password[i] = all[randomBytes[i] % all.Length];

        System.Random.Shared.Shuffle(password);
        return new string(password);
    }

    /// <inheritdoc />
    public async Task<Result<UserDetailDto>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result<UserDetailDto>.Failure("USER_NOT_FOUND", "User not found.", 404);

        if (user.Email != request.Email &&
            await _context.Users.AnyAsync(u => u.Email == request.Email && u.IsActive && u.Id != id, cancellationToken).ConfigureAwait(false))
            return Result<UserDetailDto>.Failure("DUPLICATE_EMAIL", "A user with this email already exists.", 409);

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(id, "UpdateUser", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);

        UserDetailDto dto = _mapper.Map<UserDetailDto>(user);
        return Result<UserDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, string? ipAddress, CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.", 404);

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(id, "DeactivateUser", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(
        int id,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.", 404);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("INVALID_CURRENT_PASSWORD", "Current password is incorrect.", 400);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(id, "PasswordChange", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(int userId, CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result<IReadOnlyList<RoleDto>>.Failure("USER_NOT_FOUND", "User not found.", 404);

        IReadOnlyList<RoleDto> roles = user.UserRoles
            .Select(ur => _mapper.Map<RoleDto>(ur.Role))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(roles);
    }

    /// <inheritdoc />
    public async Task<Result> AssignRolesAsync(
        int userId,
        AssignRolesRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.", 404);

        foreach (int roleId in request.RoleIds)
        {
            bool roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId, cancellationToken).ConfigureAwait(false);
            if (!roleExists)
                return Result.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

            bool alreadyAssigned = user.UserRoles.Any(ur => ur.RoleId == roleId);
            if (!alreadyAssigned)
                _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(userId, "AssignRoles", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRoleAsync(
        int userId,
        int roleId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User? user = await GetUserWithRolesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.", 404);

        UserRole? userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(userId, "RemoveRole", "users", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<User?> GetUserWithRolesAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }
}
