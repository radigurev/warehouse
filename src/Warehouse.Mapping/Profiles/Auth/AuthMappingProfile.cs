using AutoMapper;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;

namespace Warehouse.Mapping.Profiles.Auth;

/// <summary>
/// AutoMapper profile for mapping auth entities to DTOs.
/// <para>See <see cref="User"/>, <see cref="Role"/>, <see cref="Permission"/>, <see cref="UserActionLog"/>.</para>
/// </summary>
public sealed class AuthMappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations for all auth entity-to-DTO pairs.
    /// </summary>
    public AuthMappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<User, UserDetailDto>()
            .ForMember(
                dest => dest.Roles,
                opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role)));

        CreateMap<Role, RoleDto>();

        CreateMap<Role, RoleDetailDto>()
            .ForMember(
                dest => dest.Permissions,
                opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));

        CreateMap<Permission, PermissionDto>();

        CreateMap<UserActionLog, AuditLogDto>();
    }
}
