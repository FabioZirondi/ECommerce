using ECommerce.Modules.Products.Application.DTOs;
using ECommerce.Modules.Products.Application.Search;
using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Shared.Results;
using FluentValidation;

namespace ECommerce.Modules.Products.Application.Services;

public interface IProductService
{
    Task<Result<PaginatedResult<ProductResponse>>> ListAsync(
        int page,
        int pageSize,
        string? category,
        string? query = null,
        string? nearCity = null,
        string? nearState = null,
        CancellationToken cancellationToken = default);
    Task<Result<SearchSuggestResponse>> SuggestAsync(
        string? query,
        string? nearCity = null,
        string? nearState = null,
        CancellationToken cancellationToken = default);
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
        string? query = null,
        string? nearCity = null,
        string? nearState = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var categoryIds = await _categoryService.ExpandIdsAsync(category, cancellationToken);
        var tree = await _categories.ListAsync(cancellationToken);
        var term = query?.Trim();

        if (string.IsNullOrWhiteSpace(term))
        {
            var total = await _products.CountAsync(categoryIds, cancellationToken: cancellationToken);
            var items = await _products.ListAsync((page - 1) * pageSize, pageSize, categoryIds, cancellationToken: cancellationToken);

            return Result<PaginatedResult<ProductResponse>>.Success(new PaginatedResult<ProductResponse>
            {
                Items = RankWithoutQuery(items, tree, nearCity, nearState),
                Page = page,
                PageSize = pageSize,
                Total = total
            });
        }

        var pool = await _products.ListAsync(0, 200, categoryIds, cancellationToken: cancellationToken);
        var ranked = Rank(pool, tree, term, nearCity, nearState)
            .Where(item => item.Match.HasText)
            .ToList();

        return Result<PaginatedResult<ProductResponse>>.Success(new PaginatedResult<ProductResponse>
        {
            Items = ranked
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => item.Response)
                .ToList(),
            Page = page,
            PageSize = pageSize,
            Total = ranked.Count
        });
    }

    public async Task<Result<SearchSuggestResponse>> SuggestAsync(
        string? query,
        string? nearCity = null,
        string? nearState = null,
        CancellationToken cancellationToken = default)
    {
        var term = query?.Trim() ?? string.Empty;
        if (term.Length < 2)
        {
            return Result<SearchSuggestResponse>.Success(new SearchSuggestResponse());
        }

        var tree = await _categories.ListAsync(cancellationToken);
        var pool = await _products.ListAsync(0, 200, cancellationToken: cancellationToken);
        var ranked = Rank(pool, tree, term, nearCity, nearState).ToList();

        var products = ranked
            .Where(item => item.Match.HasText)
            .Take(6)
            .Select(item => ToSuggest(item.Product, item.Match.Hint))
            .ToList();
        var selectedIds = products.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);

        var places = pool
            .Where(product => !string.IsNullOrWhiteSpace(product.City))
            .GroupBy(product => CatalogSearch.PlaceKey(product.City, product.State))
            .Select(group =>
            {
                var sample = group.First();
                var city = sample.City.Trim();
                var state = sample.State.Trim().ToUpperInvariant();
                var score = CatalogSearch.ScoreField($"{city} {state}", term);
                return new
                {
                    City = city,
                    State = state,
                    Count = group.Count(),
                    Score = score,
                    Key = CatalogSearch.PlaceKey(city, state)
                };
            })
            .Where(place => place.Score > 0)
            .OrderByDescending(place => place.Score)
            .ThenByDescending(place => place.Count)
            .Take(4)
            .ToList();

        var nearbyKeys = products
            .Select(item => CatalogSearch.PlaceKey(item.City, item.State))
            .Concat(places.Select(place => place.Key))
            .Where(key => key.Length > 1)
            .ToHashSet(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(nearCity))
        {
            nearbyKeys.Add(CatalogSearch.PlaceKey(nearCity, nearState));
        }

        var nearby = ranked
            .Where(item =>
                !selectedIds.Contains(item.Product.Id)
                && nearbyKeys.Contains(CatalogSearch.PlaceKey(item.Product.City, item.Product.State)))
            .Take(4)
            .Select(item => ToSuggest(
                item.Product,
                item.Match.Hint
                ?? (CatalogSearch.SamePlace(item.Product.City, item.Product.State, nearCity, nearState)
                    ? "Na sua cidade"
                    : "Nesta cidade")))
            .ToList();

        var categories = tree
            .Select(category => (Category: category, Score: CatalogSearch.ScoreField(category.Name, term)))
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Category.Name)
            .Take(3)
            .Select(item => new CategorySuggestItem
            {
                Id = item.Category.Id,
                Name = item.Category.Name,
                Slug = string.IsNullOrWhiteSpace(item.Category.Slug) ? item.Category.Id : item.Category.Slug
            })
            .ToList();

        return Result<SearchSuggestResponse>.Success(new SearchSuggestResponse
        {
            Products = products,
            Places = places.Select(place => new PlaceSuggestItem
            {
                City = place.City,
                State = place.State,
                Count = place.Count,
                Label = CatalogSearch.PlaceLabel(place.City, place.State)
            }).ToList(),
            Nearby = nearby,
            Categories = categories
        });
    }

    public async Task<Result<PaginatedResult<ProductResponse>>> ListMineAsync(
        string sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 50);

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

        var categoryId = request.CategoryId.Trim();
        var category = await _categories.GetByIdAsync(categoryId, cancellationToken)
                       ?? await _categories.GetBySlugAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result<ProductResponse>.Failure("Categoria não encontrada.", 400);
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
            CategoryId = category.Id,
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

    private static IReadOnlyList<ProductResponse> RankWithoutQuery(
        IReadOnlyList<Product> items,
        IReadOnlyList<Category> tree,
        string? nearCity,
        string? nearState)
    {
        if (string.IsNullOrWhiteSpace(nearCity) && string.IsNullOrWhiteSpace(nearState))
        {
            return items.Select(product => ProductResponse.From(
                product,
                CategoryService.BuildPath(tree, product.CategoryId),
                includePreciseAddress: false)).ToList();
        }

        return items
            .Select(product =>
            {
                var path = CategoryService.BuildPath(tree, product.CategoryId);
                var geo = CatalogSearch.SamePlace(product.City, product.State, nearCity, nearState)
                    ? 2
                    : CatalogSearch.SameState(product.State, nearState) ? 1 : 0;
                return (product, path, geo);
            })
            .OrderByDescending(item => item.geo)
            .ThenByDescending(item => item.product.CreatedAt)
            .Select(item => ProductResponse.From(item.product, item.path, includePreciseAddress: false))
            .ToList();
    }

    private static IEnumerable<(Product Product, ProductResponse Response, CatalogSearch.Match Match)> Rank(
        IEnumerable<Product> products,
        IReadOnlyList<Category> tree,
        string query,
        string? nearCity,
        string? nearState)
        => products
            .Select(product =>
            {
                var path = CategoryService.BuildPath(tree, product.CategoryId);
                var categoryName = path.LastOrDefault()?.Name ?? string.Empty;
                var match = CatalogSearch.ScoreProduct(product, categoryName, query, nearCity, nearState);
                return (
                    Product: product,
                    Response: ProductResponse.From(product, path, includePreciseAddress: false),
                    Match: match);
            })
            .OrderByDescending(item => item.Match.Total)
            .ThenByDescending(item => item.Product.CreatedAt);

    private static ProductSuggestItem ToSuggest(Product product, string? hint)
        => new()
        {
            Id = product.Id,
            Title = product.Title,
            City = product.City,
            State = product.State,
            Price = product.Price,
            Image = product.Images.FirstOrDefault(image => !string.IsNullOrWhiteSpace(image)),
            Hint = hint
        };

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
