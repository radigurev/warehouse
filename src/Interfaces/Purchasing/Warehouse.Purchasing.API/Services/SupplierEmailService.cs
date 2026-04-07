using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Services;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements CRUD operations for supplier emails with primary-flag and uniqueness management.
/// <para>See <see cref="ISupplierEmailService"/>.</para>
/// </summary>
public sealed class SupplierEmailService : BasePurchasingEntityService, ISupplierEmailService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierEmailService(PurchasingDbContext context, IMapper mapper) : base(context, mapper) { }

    /// <inheritdoc />
    public async Task<Result<SupplierEmailDto>> CreateAsync(int supplierId, CreateSupplierEmailRequest request, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<SupplierEmailDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        bool duplicate = await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId && e.EmailAddress == request.EmailAddress, cancellationToken).ConfigureAwait(false);
        if (duplicate) return Result<SupplierEmailDto>.Failure("DUPLICATE_SUPPLIER_EMAIL", "This supplier already has this email address.", 409);

        bool isFirst = !await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);

        SupplierEmail email = new() { SupplierId = supplierId, EmailType = request.EmailType, EmailAddress = request.EmailAddress, IsPrimary = isFirst, CreatedAtUtc = DateTime.UtcNow };
        Context.SupplierEmails.Add(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierEmail, SupplierEmailDto>(email);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SupplierEmailDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<IReadOnlyList<SupplierEmailDto>>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        List<SupplierEmail> emails = await Context.SupplierEmails.AsNoTracking().Where(e => e.SupplierId == supplierId).OrderBy(e => e.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return MapListToResult<SupplierEmail, SupplierEmailDto>(emails);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierEmailDto>> UpdateAsync(int supplierId, int emailId, UpdateSupplierEmailRequest request, CancellationToken cancellationToken)
    {
        SupplierEmail? email = await Context.SupplierEmails.FirstOrDefaultAsync(e => e.Id == emailId && e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (email is null) return Result<SupplierEmailDto>.Failure("EMAIL_NOT_FOUND", "Supplier email not found.", 404);

        bool duplicate = await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId && e.EmailAddress == request.EmailAddress && e.Id != emailId, cancellationToken).ConfigureAwait(false);
        if (duplicate) return Result<SupplierEmailDto>.Failure("DUPLICATE_SUPPLIER_EMAIL", "This supplier already has this email address.", 409);

        email.EmailType = request.EmailType; email.EmailAddress = request.EmailAddress; email.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsPrimary && !email.IsPrimary)
            await PrimaryFlagHelper.UnsetOthersAsync(Context.SupplierEmails, e => e.SupplierId == supplierId && e.IsPrimary, emailId, e => e.IsPrimary = false, cancellationToken).ConfigureAwait(false);
        email.IsPrimary = request.IsPrimary;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierEmail, SupplierEmailDto>(email);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int supplierId, int emailId, CancellationToken cancellationToken)
    {
        SupplierEmail? email = await Context.SupplierEmails.FirstOrDefaultAsync(e => e.Id == emailId && e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (email is null) return Result.Failure("EMAIL_NOT_FOUND", "Supplier email not found.", 404);

        bool wasPrimary = email.IsPrimary;
        Context.SupplierEmails.Remove(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
        {
            await PrimaryFlagHelper.PromoteNextAsync(Context.SupplierEmails, e => e.SupplierId == supplierId, e => e.CreatedAtUtc, e => e.IsPrimary = true, cancellationToken).ConfigureAwait(false);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return Result.Success();
    }

    private async Task<Result?> ValidateSupplierExistsAsync(int supplierId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Suppliers.AnyAsync(s => s.Id == supplierId && !s.IsDeleted, cancellationToken).ConfigureAwait(false);
        return exists ? null : Result.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);
    }
}
