# ECommerce

**Projeto - Iniciação.**

Marketplace acadêmico (modular monolith) em ASP.NET Core 8 + MongoDB.
Três contextos: Users, Products (anúncios) e Orders.

## Como subir

```bash
docker compose up -d
dotnet run --project ECommerce.API --launch-profile http
cd web
npm install
npm run dev
```

- API / Swagger: http://localhost:5264/swagger
- Frontend: http://localhost:5173


## Fluxo de identidade (já implementado)

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. `GET /api/users/me` com header `Authorization: Bearer {token}`

```json
{
  "name": "Ana Silva",
  "email": "ana@email.com",
  "password": "senha1234",
  "asSeller": true
}
```

## Solução

- `ECommerce.API` — controllers, JWT, middleware, Swagger
- `ECommerce.Modules` — domínio, aplicação e repositórios por contexto
- `ECommerce.Infrastructure` — MongoDB
- `ECommerce.Shared` — Result, exceções, extensões
- `web` — React (Vite) para exercitar cadastro, anúncios, carrinho e pedidos

Catálogo, checkout com baixa de estoque, pedidos do comprador e vendas do anunciante já fazem parte deste recorte.
