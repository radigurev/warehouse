using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Services;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.Auth.DBModel.Models;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests verifying that the Auth database seeder provisions the four new
/// product-prices permissions introduced in CHG-FEAT-007 and that any role holding
/// <c>sales-orders:create</c> also receives <c>product-prices:read</c> (cascading grant).
/// <para>Covers CHG-FEAT-007 §2.6 RBAC Permissions and §4 Affected SDD-AUTH-001 enhancement.</para>
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
[Category("Integration")]
public sealed class ProductPricePermissionSeedingTests : AuthApiTestBase
{
    /// <summary>
    /// CHG-FEAT-007 §2.6 — the seeder MUST register the four product-prices permissions
    /// and grant all of them to the Admin role (which holds full administrative access).
    /// </summary>
    [Test]
    public async Task SeedPermissions_AdministratorRole_HasAllProductPricePermissions()
    {
        // Arrange
        string[] required = ["read", "create", "update", "delete"];

        // Act
        IReadOnlyList<string> adminActions = await ReadDbContextAsync(async ctx =>
        {
            Role? admin = await ctx.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Name == "Admin" && r.IsSystem);

            return admin is null
                ? (IReadOnlyList<string>)[]
                : admin.RolePermissions
                    .Where(rp => rp.Permission.Resource == "product-prices")
                    .Select(rp => rp.Permission.Action)
                    .ToList();
        });

        // Assert
        adminActions.Should().Contain(required);
    }

    /// <summary>
    /// CHG-FEAT-007 §2.6 — any role that owns <c>sales-orders:create</c> MUST also receive
    /// <c>product-prices:read</c> so sales order creators can preview catalog prices. Verified
    /// by checking that every role granting <c>sales-orders:create</c> also grants
    /// <c>product-prices:read</c>.
    /// </summary>
    [Test]
    public async Task SeedPermissions_SalesOrderCreatorRoles_HaveReadPermission()
    {
        // Arrange
        // (no extra arrange — uses the seeded DB)

        // Act
        bool allCreatorsHaveRead = await ReadDbContextAsync(async ctx =>
        {
            List<Role> rolesWithSoCreate = await ctx.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Where(r => r.RolePermissions.Any(rp =>
                    rp.Permission.Resource == "sales-orders" && rp.Permission.Action == "create"))
                .ToListAsync();

            if (rolesWithSoCreate.Count == 0)
                return true; // vacuously true — no creator role seeded, cascading constraint cannot fail

            return rolesWithSoCreate.All(r =>
                r.RolePermissions.Any(rp =>
                    rp.Permission.Resource == "product-prices" && rp.Permission.Action == "read"));
        });

        // Assert
        allCreatorsHaveRead.Should().BeTrue(
            "every role holding sales-orders:create MUST also hold product-prices:read (CHG-FEAT-007 §2.6 cascading grant).");
    }

    /// <summary>
    /// CHG-FEAT-007 §2.6 — directly verifies the cascading grant logic by creating a synthetic
    /// role that holds only <c>sales-orders:create</c>, re-running the seeder, and asserting
    /// the role now also holds <c>product-prices:read</c>.
    /// </summary>
    [Test]
    public async Task SeedCascadingPermissions_RoleWithSalesOrdersCreate_ReceivesProductPricesRead()
    {
        // Arrange
        int syntheticRoleId = 0;
        await WithDbContextAsync(async ctx =>
        {
            Permission? soCreate = await ctx.Permissions
                .FirstOrDefaultAsync(p => p.Resource == "sales-orders" && p.Action == "create");
            soCreate.Should().NotBeNull("sales-orders:create must be seeded before running cascade test.");

            Role syntheticRole = new()
            {
                Name = "SyntheticSalesOrderCreator",
                Description = "Test-only role used to verify CHG-FEAT-007 §2.6 cascading grant.",
                IsSystem = false
            };
            ctx.Roles.Add(syntheticRole);
            await ctx.SaveChangesAsync();

            ctx.RolePermissions.Add(new RolePermission
            {
                RoleId = syntheticRole.Id,
                PermissionId = soCreate!.Id
            });
            await ctx.SaveChangesAsync();

            syntheticRoleId = syntheticRole.Id;
        });

        // Act
        await WithScopedServiceAsync<DatabaseSeeder>(seeder => seeder.SeedAsync(CancellationToken.None));

        // Assert
        bool hasCascadedPermission = await ReadDbContextAsync(async ctx =>
            await ctx.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => rp.RoleId == syntheticRoleId
                             && rp.Permission.Resource == "product-prices"
                             && rp.Permission.Action == "read"));

        hasCascadedPermission.Should().BeTrue(
            "the cascading grant MUST add product-prices:read to any role holding sales-orders:create.");

        // Cleanup — remove the synthetic role to keep the fixture state consistent for other tests
        await WithDbContextAsync(async ctx =>
        {
            List<RolePermission> rps = await ctx.RolePermissions
                .Where(rp => rp.RoleId == syntheticRoleId)
                .ToListAsync();
            ctx.RolePermissions.RemoveRange(rps);

            Role? role = await ctx.Roles.FirstOrDefaultAsync(r => r.Id == syntheticRoleId);
            if (role is not null)
                ctx.Roles.Remove(role);

            await ctx.SaveChangesAsync();
        });
    }

    /// <summary>
    /// CHG-FEAT-007 §2.6 — the four product-prices permissions (read, create, update, delete)
    /// MUST exist in the Permissions catalog after seeding.
    /// </summary>
    [Test]
    public async Task SeedPermissions_ProductPricePermissions_AreRegistered()
    {
        // Arrange
        string[] expected = ["read", "create", "update", "delete"];

        // Act
        IReadOnlyList<string> registered = await ReadDbContextAsync(async ctx =>
            await ctx.Permissions
                .Where(p => p.Resource == "product-prices")
                .Select(p => p.Action)
                .ToListAsync());

        // Assert
        registered.Should().Contain(expected);
    }
}
