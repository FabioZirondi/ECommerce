# InovaTech / ECommerce — contexto do projeto

Marketplace acadêmico (Projeto - Iniciação / Gerência e Projetos).
Monólito modular em ASP.NET Core 8 + MongoDB local + React (Vite).
Vitrine pública: **InovaTech**. Uma conta compra e vende.

Fale com o usuário em português. Não faça commit/push no git sem ele pedir.

## Como subir (local)

Pré-requisitos: .NET 8 SDK e Node.js 18+.

```bash
dotnet run --project ECommerce.API --launch-profile http
cd web
npm install
npm run dev
```

- API / Swagger: http://localhost:5264/swagger
- Frontend: http://localhost:5173 (proxy `/api` e `/health` e `/uploads` para 5264)

O Mongo **não** é Docker no fluxo padrão. A API sobe EphemeralMongo na porta **27017**, pasta **`.mongo-data`** na raiz, banco **`ecommerce`**.

## Onde estão os dados (portabilidade)

Git **não** leva cadastros nem fotos. Ficam fora do repositório:

| O quê | Pasta |
| --- | --- |
| Usuários, anúncios, categorias, carrinhos, pedidos | `.mongo-data/` |
| Fotos enviadas dos anúncios | `ECommerce.API/wwwroot/uploads/` |
| JWT da sessão no browser | `localStorage` (`ecommerce.token` / `ecommerce.user`) — não transfere |

Levar para outro PC **Windows**, com a API parada:

1. Neste PC: `powershell -File scripts/exportar-ambiente.ps1`
2. Copiar o código **e** `portabilidade/inovatech-dados.zip`
3. No outro PC: colar o projeto, depois `powershell -File scripts/importar-ambiente.ps1`
4. Subir API e `web` como acima e **entrar de novo** (e-mail/senha iguais; o token do browser antigo não vale)

Não copie `.mongo-data` com o Mongo ligado (porta 27017). O script recusa se a porta estiver em uso.

Seed de categorias é idempotente. Produtos/usuários já existentes não são apagados na subida.

## Arquitetura

```
ECommerce.API            controllers, JWT, CORS, Swagger, uploads, LocalMongo
ECommerce.Modules        Users, Products, Orders (domínio + aplicação + repositórios)
ECommerce.Infrastructure Mongo, índices
ECommerce.Shared         Result, exceções, claims
web                      SPA React
```

Sem microserviços, MediatR, fila ou gateway de pagamento. Checkout é síncrono e marca o pedido como pago na hora.

## Regras de negócio

- Registro: `asSeller` true → roles Buyer+Seller; senão só Buyer. Qualquer autenticado pode anunciar.
- Vitrine `GET /api/products`: `Active` **e** `availableUnits > 0`. Endereço preciso não vai na listagem pública.
- Carrinho: DTO (`CartResponse`). Não compra o próprio anúncio. Quantidade ≤ estoque. `PATCH`/`DELETE /api/cart/items/{productId}`.
- Checkout `POST /api/orders`: um pedido por vendedor; baixa estoque com `FindOneAndUpdate` atômico e devolve unidades se falhar.
- `availableUnits == 0` → status `Sold`, some da vitrine; continua em Meus anúncios.
- Anunciante pode `Closed`/`Active` e alterar unidades.
- Pedidos do comprador: `/orders`. Vendas: `/sales`.
- JWT em `appsettings.json` (`Jwt:Secret`, ≥ 32 chars). O mesmo secret precisa ir no outro PC (já vai no código).
- CORS: `http://localhost:5173` e `127.0.0.1:5173`. Rate limit em auth/checkout.

## API principal

- `POST /api/auth/register` `POST /api/auth/login` `GET /api/users/me`
- `GET/POST /api/products` `GET /api/products/mine` `PATCH /api/products/{id}` `GET /api/categories`
- `GET /api/cart` `POST /api/cart/items` `PATCH/DELETE /api/cart/items/{productId}`
- `POST /api/orders` `GET /api/orders` `GET /api/orders/sales`
- `POST /api/uploads` (jpeg/png, magic bytes)
- `GET /health` → `{ status: "ok" }`

## Frontend (`web/src`)

Rotas: `/` catálogo, `/products/:id`, `/login`, `/register`, `/sell`, `/listings`, `/cart`, `/orders`, `/sales`.
Token no `localStorage`. Quantidade no anúncio e no carrinho via stepper.

## Fora do recorte

Sem CPF/CNPJ implementado, sem pagamento externo, sem papel Admin na UI.
`docker-compose.yml` é opcional (Mongo externo). Não é o caminho local padrão.
