# ECommerce

**Projeto - Iniciação.**  
Gerência e Projetos

Marketplace acadêmico (monólito modular) em ASP.NET Core 8 + MongoDB.
A vitrine pública se chama **InovaTech**. Uma conta opera como comprador e anunciante.

## Como subir

O Mongo sobe junto com a API (EphemeralMongo em `127.0.0.1:27017`, dados em `.mongo-data`, banco `ecommerce`). Não é necessário Docker para o fluxo local.

```bash
dotnet run --project ECommerce.API --launch-profile http
cd web
npm install
npm run dev
```

- API / Swagger: http://localhost:5264/swagger
- Frontend: http://localhost:5173

`docker compose up -d` só entra se você quiser um Mongo externo no lugar do processo embutido.

## Fluxo

1. `POST /api/auth/register` (roles Buyer e, se `asSeller`, também Seller)
2. `POST /api/auth/login` → JWT
3. `GET /api/users/me`
4. `GET /api/products` — vitrine (Active e com unidades > 0)
5. `POST /api/products` — publicar anúncio
6. `POST /api/cart/items` → `PATCH` / `DELETE /api/cart/items/{id}` → `POST /api/orders`
7. Checkout paga na hora, baixa o estoque de forma atômica e gera pedido por vendedor
8. Comprador vê **Pedidos**; anunciante vê **Vendas** e **Meus anúncios**
9. Em 0 unidades o anúncio sai da vitrine (`Sold`)

## Solução

- `ECommerce.API` — controllers, JWT, middleware, Swagger
- `ECommerce.Modules` — Users, Products e Orders (domínio, aplicação, repositórios)
- `ECommerce.Infrastructure` — MongoDB e índices
- `ECommerce.Shared` — Result, exceções, extensões
- `web` — React (Vite): cadastro, vitrine, anúncios, carrinho e pedidos
