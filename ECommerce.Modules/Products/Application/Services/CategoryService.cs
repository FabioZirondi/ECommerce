using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ECommerce.Modules.Products.Application.DTOs;
using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Shared.Results;
using FluentValidation;

namespace ECommerce.Modules.Products.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>?> ExpandIdsAsync(string? slugOrId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryPathItem>> PathAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
}

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IValidator<CreateCategoryRequest> _createValidator;

    public CategoryService(ICategoryRepository categories, IValidator<CreateCategoryRequest> createValidator)
    {
        _categories = categories;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.ListAsync(cancellationToken);
        return categories
            .Select(CategoryResponse.From)
            .OrderBy(category => category.Name, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), true))
            .ToList();
    }

    public async Task<IReadOnlyList<string>?> ExpandIdsAsync(string? slugOrId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slugOrId))
        {
            return null;
        }

        var categories = await _categories.ListAsync(cancellationToken);
        var current = categories.FirstOrDefault(category =>
            category.Id.Equals(slugOrId, StringComparison.OrdinalIgnoreCase) ||
            category.Slug.Equals(slugOrId, StringComparison.OrdinalIgnoreCase));

        if (current is null)
        {
            return [];
        }

        var children = categories
            .GroupBy(category => category.ParentId ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        var ids = new List<string>();
        var queue = new Queue<string>();
        queue.Enqueue(current.Id);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            ids.Add(id);
            if (!children.TryGetValue(id, out var nested))
            {
                continue;
            }

            foreach (var child in nested)
            {
                queue.Enqueue(child.Id);
            }
        }

        return ids;
    }

    public async Task<IReadOnlyList<CategoryPathItem>> PathAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return [];
        }

        var categories = await _categories.ListAsync(cancellationToken);
        return BuildPath(categories, categoryId);
    }

    public static IReadOnlyList<CategoryPathItem> BuildPath(IReadOnlyList<Category> categories, string categoryId)
    {
        var byId = categories.ToDictionary(category => category.Id, StringComparer.OrdinalIgnoreCase);
        var path = new List<CategoryPathItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentId = categoryId;

        while (!string.IsNullOrWhiteSpace(currentId) && seen.Add(currentId) && byId.TryGetValue(currentId, out var category))
        {
            path.Add(new CategoryPathItem
            {
                Id = category.Id,
                Name = category.Name,
                Slug = string.IsNullOrWhiteSpace(category.Slug) ? category.Id : category.Slug
            });
            currentId = category.ParentId ?? string.Empty;
        }

        path.Reverse();
        return path;
    }

    public async Task<Result<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return Result<CategoryResponse>.Failure("Dados inválidos.", 400, errors);
        }

        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim();
        if (parentId is not null)
        {
            var parent = await _categories.GetByIdAsync(parentId, cancellationToken)
                         ?? await _categories.GetBySlugAsync(parentId, cancellationToken);
            if (parent is null)
            {
                return Result<CategoryResponse>.Failure("Categoria pai não encontrada.", 404);
            }

            parentId = parent.Id;
        }

        var slug = await UniqueSlugAsync(Slugify(request.Name), cancellationToken);
        var category = new Category
        {
            Id = slug,
            Name = request.Name.Trim(),
            Slug = slug,
            ParentId = parentId
        };

        await _categories.AddAsync(category, cancellationToken);
        return Result<CategoryResponse>.Success(CategoryResponse.From(category), 201);
    }

    private async Task<string> UniqueSlugAsync(string slug, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _categories.GetBySlugAsync(candidate, cancellationToken) is not null)
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character is ' ' or '-' or '_')
            {
                builder.Append('-');
            }
        }

        var slug = Regex.Replace(builder.ToString(), "-{2,}", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
