#:package MongoDB.Driver@3.11.1

using MongoDB.Bson;
using MongoDB.Driver;

var sellerId = "07607ff0937d48d086e371dacbf989f7";
var sellerName = "fabio";

var catalog = new (string Title, string Description, decimal Price, string Condition, string City)[]
{
    ("iPhone 13 128GB", "Aparelho em ótimo estado, bateria 87%, com caixa e cabo.", 2190m, "Used", "São Paulo"),
    ("Bicicleta Caloi Aro 29", "Uso urbano, revisada, pneus novos e freio a disco.", 890m, "Used", "Campinas"),
    ("Sofá 3 lugares cinza", "Tecido suede, pouco uso, sem rasgo. Retirada no local.", 1250m, "Used", "Ribeirão Preto"),
    ("Notebook Dell Inspiron i5", "8GB RAM, SSD 256GB, ideal para faculdade e trabalho.", 2400m, "Used", "Matão"),
    ("Tênis Nike Revolution 41", "Novo, na caixa, nunca usado. Número 41.", 189.90m, "New", "Araraquara"),
    ("Geladeira Brastemp 375L", "Frost free, funcionando perfeitamente. Entrega combinada.", 1980m, "Used", "São Carlos"),
    ("PlayStation 5 Slim", "Com um controle e três jogos. Sem marcas profundas.", 3200m, "Used", "São Paulo"),
    ("Mesa de jantar 6 cadeiras", "Madeira maciça, bem conservada. Comprador retira.", 750m, "Used", "Jaú"),
    ("Fone JBL Tune 510BT", "Lacrado, nota fiscal, Bluetooth e 40h de bateria.", 149.90m, "New", "Matão"),
    ("Monitor LG 24 UltraGear", "Full HD, 75Hz, HDMI e VGA. Na caixa.", 620m, "New", "Campinas"),
    ("Cafeteira Nespresso Inissia", "Funcionando, com 20 cápsulas de brinde.", 280m, "Used", "Araraquara"),
    ("Guarda-roupa 6 portas", "Branco, com espelho. Desmontado para transporte.", 1100m, "Used", "Ribeirão Preto"),
    ("Air Fryer 4L Mondial", "Pouco uso, limpa, com manual.", 220m, "Used", "Matão"),
    ("Teclado mecânico Redragon", "Switch blue, RGB, novo lacrado.", 259.90m, "New", "São Paulo"),
    ("Cadeira gamer reclinável", "Ajuste de altura e lombar. Uso em home office.", 640m, "Used", "Campinas")
};

var client = new MongoClient("mongodb://127.0.0.1:27017");
var products = client.GetDatabase("ecommerce").GetCollection<BsonDocument>("products");

var now = DateTime.UtcNow;
var documents = catalog.Select((item, index) => new BsonDocument
{
    { "_id", Guid.NewGuid().ToString("N") },
    { "sellerId", sellerId },
    { "sellerName", sellerName },
    { "title", item.Title },
    { "description", item.Description },
    { "price", item.Price },
    { "categoryId", "" },
    { "condition", item.Condition },
    { "city", item.City },
    { "images", new BsonArray() },
    { "status", "Active" },
    { "createdAt", now.AddMinutes(-index) }
}).ToList();

await products.InsertManyAsync(documents);
Console.WriteLine($"inserted={documents.Count}");
