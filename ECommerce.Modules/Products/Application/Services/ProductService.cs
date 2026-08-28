using ECommerce.Modules.Products.Application.DTOs;
using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Shared.Results;
using FluentValidation;

namespace ECommerce.Modules.Products.Application.Services;

public interface IProductService
{
    Task<Result<PaginatedResult<ProductResponse>>> ListAsync(int page, int pageSize, string? category, CancellationToken cancellationToken = default);
    Task<Result<PaginatedResult<ProductResponse>>> ListMineAsync(string sellerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> CreateAsync(string sellerId, string sellerName, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> UpdateAsync(string sellerId, string id, UpdateProductRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        ICategoryService categoryService,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
    {
        _products = products;
        _categories = categories;
        _categoryService = categoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginatedResult<ProductResponse>>> ListAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 20 : pageSize;

        var categoryIds = await _categoryService.ExpandIdsAsync(category, cancellationToken);
        var tree = await _categories.ListAsync(cancellationToken);
        var total = await _products.CountAsync(categoryIds, cancellationToken: cancellationToken);
        var items = await _products.ListAsync((page - 1) * pageSize, pageSize, categoryIds, cancellationToken: cancellationToken);

        return Result<PaginatedResult<ProductResponse>>.Success(new PaginatedResult<ProductResponse>
        {
            Items = items.Select(product => ProductResponse.From(
                product,
                CategoryService.BuildPath(tree, product.CategoryId),
                includePreciseAddress: false)).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    public async Task<Result<PaginatedResult<ProductResponse>>> ListMineAsync(
        string sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 20 : pageSize;

        var tree = await _categories.ListAsync(cancellationToken);
        var total = await _products.CountAsync(sellerId: sellerId, activeOnly: false, cancellationToken: cancellationToken);
        var items = await _products.ListAsync(
            (page - 1) * pageSize,
            pageSize,
            sellerId: sellerId,
            activeOnly: false,
            cancellationToken: cancellationToken);

        return Result<PaginatedResult<ProductResponse>>.Success(new PaginatedResult<ProductResponse>
        {
            Items = items.Select(product => ProductResponse.From(product, CategoryService.BuildPath(tree, product.CategoryId))).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<ProductResponse>.Failure("Anúncio não encontrado.", 404);
        }

        var tree = await _categories.ListAsync(cancellationToken);
        return Result<ProductResponse>.Success(
            ProductResponse.From(product, CategoryService.BuildPath(tree, product.CategoryId)));
    }

    public async Task<Result<ProductResponse>> CreateAsync(
        string sellerId,
        string sellerName,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ProductResponse>.Failure("Dados inválidos.", 400, ToErrors(validation));
        }

        if (!Enum.TryParse<ProductCondition>(request.Condition, ignoreCase: true, out var condition))
        {
            condition = ProductCondition.Used;
        }

        var product = new Product
        {
            SellerId = sellerId,
            SellerName = sellerName,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            Condition = condition,
            City = request.City.Trim(),
            State = request.State.Trim().ToUpperInvariant(),
            Neighborhood = request.Neighborhood.Trim(),
            Street = request.Street.Trim(),
            ZipCode = request.ZipCode.Trim(),
            Images = SanitizeImages(request.Images),
            AvailableUnits = request.AvailableUnits < 1 ? 1 : request.AvailableUnits
        };

        await _products.AddAsync(product, cancellationToken);
        var tree = await _categories.ListAsync(cancellationToken);
        return Result<ProductResponse>.Success(ProductResponse.From(product, CategoryService.BuildPath(tree, product.CategoryId)), 201);
    }

    public async Task<Result<ProductResponse>> UpdateAsync(
        string sellerId,
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ProductResponse>.Failure("Dados inválidos.", 400, ToErrors(validation));
        }

        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Result<ProductResponse>.Failure("Anúncio não encontrado.", 404);
        }

        if (!string.Equals(product.SellerId, sellerId, StringComparison.Ordinal))
        {
            return Result<ProductResponse>.Failure("Você só pode alterar os seus anúncios.", 403);
        }

        if (request.AvailableUnits.HasValue)
        {
            product.AvailableUnits = Math.Max(0, request.AvailableUnits.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status)
            && status is ProductStatus.Active or ProductStatus.Closed)
        {
            product.Status = status;
        }

        if (product.AvailableUnits <= 0)
        {
            product.AvailableUnits = 0;
            if (product.Status != ProductStatus.Closed)
            {
                product.Status = ProductStatus.Sold;
            }
        }
        else if (product.Status == ProductStatus.Sold)
        {
            product.Status = ProductStatus.Active;
        }

        await _products.ReplaceAsync(product, cancellationToken);
        var tree = await _categories.ListAsync(cancellationToken);
        return Result<ProductResponse>.Success(ProductResponse.From(product, CategoryService.BuildPath(tree, product.CategoryId)));
    }

    private static IReadOnlyDictionary<string, string[]> ToErrors(FluentValidation.Results.ValidationResult validation)
        => validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

    private static List<string> SanitizeImages(IEnumerable<string>? images)
        => (images ?? [])
            .Where(IsSafeImageUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

    private static bool IsSafeImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return !url.Contains("..", StringComparison.Ordinal);
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
