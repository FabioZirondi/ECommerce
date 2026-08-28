using ECommerce.Modules.Products.Domain.Entities;
using MongoDB.Driver;

namespace ECommerce.Modules.Products.Infrastructure;

public static class CatalogSeed
{
    public static async Task EnsureAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var categories = database.GetCollection<Category>("categories");
        var products = database.GetCollection<Product>("products");

        var existing = await categories.Find(FilterDefinition<Category>.Empty).ToListAsync(cancellationToken);
        var existingIds = existing.Select(category => category.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = BuildTree().Where(category => !existingIds.Contains(category.Id)).ToList();
        if (missing.Count > 0)
        {
            await categories.InsertManyAsync(missing, cancellationToken: cancellationToken);
        }

        foreach (var (title, categoryId) in ProductCategories)
        {
            var filter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq(product => product.Title, title),
                Builders<Product>.Filter.Or(
                    Builders<Product>.Filter.Eq(product => product.CategoryId, string.Empty),
                    Builders<Product>.Filter.Eq(product => product.CategoryId, null!)));

            await products.UpdateManyAsync(
                filter,
                Builders<Product>.Update.Set(product => product.CategoryId, categoryId),
                cancellationToken: cancellationToken);
        }

        await products.UpdateManyAsync(
            Builders<Product>.Filter.Or(
                Builders<Product>.Filter.Eq(product => product.State, string.Empty),
                Builders<Product>.Filter.Eq(product => product.State, null!)),
            Builders<Product>.Update.Set(product => product.State, "SP"),
            cancellationToken: cancellationToken);

        await products.UpdateManyAsync(
            Builders<Product>.Filter.Exists(product => product.AvailableUnits, false),
            Builders<Product>.Update.Set(product => product.AvailableUnits, 1),
            cancellationToken: cancellationToken);

        if (await products.CountDocumentsAsync(
                Builders<Product>.Filter.Eq(product => product.Title, "Cooler RGB 120mm"),
                cancellationToken: cancellationToken) == 0)
        {
            var seller = await products.Find(FilterDefinition<Product>.Empty).FirstOrDefaultAsync(cancellationToken);
            if (seller is not null)
            {
                await products.InsertOneAsync(new Product
                {
                    SellerId = seller.SellerId,
                    SellerName = seller.SellerName,
                    Title = "Cooler RGB 120mm",
                    Description = "Ventoinha para gabinete, RGB, rolamento silencioso.",
                    Price = 59.90m,
                    CategoryId = "cooler",
                    Condition = ProductCondition.New,
                    City = seller.City,
                    AvailableUnits = 1,
                    Status = ProductStatus.Active
                }, cancellationToken: cancellationToken);
            }
        }
    }

    private static List<Category> BuildTree() =>
    [
        Cat("informatica", "Informática"),
        Cat("computador", "Computador", "informatica"),
        Cat("gabinete", "Gabinete", "computador"),
        Cat("cooler", "Cooler", "gabinete"),
        Cat("notebook", "Notebook", "informatica"),
        Cat("perifericos", "Periféricos", "informatica"),
        Cat("teclado", "Teclado", "perifericos"),
        Cat("monitor", "Monitor", "perifericos"),
        Cat("fone", "Fone", "perifericos"),
        Cat("cadeira-gamer", "Cadeira gamer", "informatica"),
        Cat("celulares", "Celulares"),
        Cat("smartphones", "Smartphones", "celulares"),
        Cat("games", "Games"),
        Cat("consoles", "Consoles", "games"),
        Cat("casa", "Casa e móveis"),
        Cat("moveis", "Móveis", "casa"),
        Cat("eletrodomesticos", "Eletrodomésticos", "casa"),
        Cat("esportes", "Esportes"),
        Cat("bicicletas", "Bicicletas", "esportes"),
        Cat("calcados", "Calçados", "esportes"),
        Cat("papelaria", "Papelaria"),
        Cat("veiculos", "Veículos"),
        Cat("carros", "Carros", "veiculos"),
        Cat("motos", "Motos", "veiculos"),
        Cat("moda", "Moda e beleza"),
        Cat("roupas", "Roupas", "moda"),
        Cat("beleza", "Beleza", "moda"),
        Cat("livros", "Livros e revistas"),
        Cat("ferramentas", "Ferramentas")
    ];

    private static readonly (string Title, string CategoryId)[] ProductCategories =
    [
        ("iPhone 13 128GB", "smartphones"),
        ("Bicicleta Caloi Aro 29", "bicicletas"),
        ("Sofá 3 lugares cinza", "moveis"),
        ("Notebook Dell Inspiron i5", "notebook"),
        ("Tênis Nike Revolution 41", "calcados"),
        ("Geladeira Brastemp 375L", "eletrodomesticos"),
        ("PlayStation 5 Slim", "consoles"),
        ("Mesa de jantar 6 cadeiras", "moveis"),
        ("Caderno", "papelaria"),
        ("Fone JBL Tune 510BT", "fone"),
        ("Monitor LG 24 UltraGear", "monitor"),
        ("Cafeteira Nespresso Inissia", "eletrodomesticos"),
        ("Guarda-roupa 6 portas", "moveis"),
        ("Air Fryer 4L Mondial", "eletrodomesticos"),
        ("Teclado mecânico Redragon", "teclado"),
        ("Cadeira gamer reclinável", "cadeira-gamer")
    ];

    private static Category Cat(string id, string name, string? parentId = null) => new()
    {
        Id = id,
        Name = name,
        Slug = id,
        ParentId = parentId
    };
}
