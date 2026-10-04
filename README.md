


<!--
# Plataforma de E-commerce — Módulo 1: Fundamentos do Back-end

Catálogo de produtos em ASP.NET Core + EF Core + PostgreSQL, rodando em Docker.
Este é o primeiro serviço da plataforma. Todos os módulos seguintes do roadmap constroem em cima dele.

---

## Como rodar

### Opção A — tudo em Docker (mais rápido)

```bash
docker compose up --build
```

API em `http://localhost:5080`, documentação em `http://localhost:5080/scalar/v1`.

### Opção B — banco em Docker, API local (melhor para estudar/debugar)

```bash
# 1. sobe só o PostgreSQL
docker compose up -d postgres

# 2. instala a ferramenta de migrations (uma vez por máquina)
dotnet tool install --global dotnet-ef

# 3. cria a primeira migration — ainda não existe nenhuma no repo, de propósito
dotnet ef migrations add InitialCreate --project src/Catalog.Api --output-dir Infrastructure/Persistence/Migrations

# 4. roda
dotnet run --project src/Catalog.Api
```

A migration é aplicada automaticamente no start (só em `Development`) e o banco é populado com 2 categorias e 3 produtos.

Use o arquivo `src/Catalog.Api/Catalog.Api.http` para disparar as requisições direto do VS Code (extensão REST Client) ou do Rider/Visual Studio.

---

## Se der erro de versão

O projeto está em **.NET 10**. Confira com `dotnet --list-sdks`. Se você estiver no .NET 9:

1. `Directory.Build.props` → `<TargetFramework>net9.0</TargetFramework>`
2. `Catalog.Api.csproj` → troque todos os `Version="10.0.0"` por `9.0.*` (`dotnet restore` resolve)
3. `src/Catalog.Api/Dockerfile` → `sdk:9.0` e `aspnet:9.0`

Se o `dotnet build ECommerce.slnx` reclamar do formato da solution, seu SDK é anterior ao 9.0.200 — compile o projeto direto: `dotnet build src/Catalog.Api`.

Os números de versão dos pacotes NuGet podem estar defasados. Se o restore falhar, rode:

```bash
dotnet list src/Catalog.Api package --outdated
```

---

## O que tem aqui e por quê

```
src/Catalog.Api/
├── Domain/                 entidades e regras de negócio — não conhece EF, HTTP nem JSON
├── Contracts/              DTOs de entrada e saída da API
├── Features/               endpoints agrupados por assunto (vertical slice)
├── Infrastructure/         DbContext, mapeamentos, migrations
├── Common/                 tratamento de erro, validação, mapeamento
└── Program.cs              composição: configuração, DI, pipeline
```

### Decisões que valem entender

**Minimal APIs em vez de Controllers.** Menos cerimônia, mesma capacidade. Endpoints ficam em classes estáticas de extensão (`MapProductEndpoints`), agrupados por feature. Um `MapGroup` por recurso permite aplicar autenticação, rate limit e versionamento no grupo inteiro depois.

**Entidade rica, não anêmica.** `Product` tem setters privados. Preço só muda por `ChangePrice`, que valida. Isso é o embrião do DDD (Módulo 4): se qualquer camada puder escrever `product.Price = -10`, a regra não existe de fato.

**DTO ≠ entidade.** `CreateProductRequest` e `ProductResponse` são separados de `Product`. Expor a entidade acopla seu contrato público ao seu banco: renomear uma coluna quebraria o cliente da API. Além disso evita over-posting (o cliente mandar `IsActive: true` num campo que não deveria controlar).

**Guid v7 como chave primária.** `Guid.CreateVersion7()` é ordenado no tempo. `Guid.NewGuid()` é aleatório e fragmenta o índice B-tree do PostgreSQL a cada insert — problema real quando a tabela cresce.

**`decimal` com precisão explícita.** Dinheiro nunca é `double`. E sem `HasPrecision(18,2)` o provider escolhe por você — arredondamento vira bug de fatura.

**`AsNoTracking()` em leitura, tracking em escrita.** O change tracker do EF existe para detectar alterações. Em `GET` ele só gasta memória. Repare que no `UpdateProduct` não existe `db.Update()`: a entidade rastreada já sabe o que mudou.

**Projeção com `Select` em vez de `Include`.** `Include` traz todas as colunas da entidade e da relação. `Select` gera um `SELECT` só com o que a resposta precisa. Menos I/O, menos alocação.

**Paginação com ordenação determinística.** `OrderByDescending(CreatedAtUtc).ThenBy(Id)`. Sem desempate, o PostgreSQL pode devolver a mesma linha em duas páginas — bug clássico e difícil de reproduzir.

**Erros em formato ProblemDetails (RFC 9457).** Um `IExceptionHandler` global traduz exceção em resposta HTTP. Sem isso, cada endpoint vira um `try/catch` e o formato de erro diverge entre rotas. Mensagem de erro interno fica no log, não na resposta.

**Soft delete.** `DELETE /products/{id}` desativa em vez de apagar. Pedidos antigos referenciam o produto; apagar a linha quebraria o histórico.

**Migration não roda em produção pelo processo da API.** Aqui roda só em `Development` por conveniência. Com N réplicas subindo juntas você teria N migrations concorrentes. Em produção isso vira passo de pipeline (Módulo 14).

**Log estruturado.** `logger.LogInformation("Produto {ProductId} criado", id)` — não `$"Produto {id} criado"`. No primeiro caso `ProductId` vira campo pesquisável no Grafana/Loki (Módulo 12). No segundo vira texto.

---

## Endpoints

| Método | Rota | O quê |
|---|---|---|
| GET | `/health` | Health check (inclui conectividade com o banco) |
| GET | `/scalar/v1` | Documentação interativa (só em Development) |
| GET | `/api/v1/categories` | Lista categorias |
| GET | `/api/v1/categories/{id}` | Detalhe |
| POST | `/api/v1/categories` | Cria categoria |
| GET | `/api/v1/products` | Lista com `search`, `categoryId`, `onlyActive`, `page`, `pageSize` |
| GET | `/api/v1/products/{id}` | Detalhe |
| POST | `/api/v1/products` | Cria produto |
| PUT | `/api/v1/products/{id}` | Atualiza dados |
| PATCH | `/api/v1/products/{id}/stock` | Define estoque |
| DELETE | `/api/v1/products/{id}` | Desativa (soft delete) |

Códigos usados: `200`, `201`, `204`, `400` (regra de negócio), `404`, `409` (SKU/slug duplicado), `422` (categoria inexistente), `500`.

---

## Checklist do Módulo 1

Só siga para o Módulo 2 quando conseguir fazer tudo isso **sem consultar o código**:

- [ ] Explicar a diferença entre `AddScoped`, `AddTransient` e `AddSingleton` e por que `DbContext` é Scoped
- [ ] Descrever a ordem de precedência da Configuration (env var > user-secrets > appsettings)
- [ ] Criar uma migration, ler o arquivo gerado e reverter com `dotnet ef database update <MigrationAnterior>`
- [ ] Explicar o que `AsNoTracking()` economiza e quando ele quebraria seu código
- [ ] Ler o SQL gerado por uma query LINQ no log e apontar se ele usa índice
- [ ] Explicar por que o Dockerfile copia o `.csproj` antes do resto do código
- [ ] Dizer o que muda entre `400`, `409` e `422` nas respostas acima

---

## Exercícios (faça antes do Módulo 2)

1. **Filtro por faixa de preço.** Adicione `minPrice` e `maxPrice` ao `GET /products`. Valide que `minPrice <= maxPrice` e devolva `400` quando não for.
2. **Endpoint de baixa de estoque.** `POST /products/{id}/stock/decrease` usando `Product.DecreaseStock`. Repare que a regra já existe no domínio — o endpoint só orquestra.
3. **Concorrência otimista.** Dois `PUT` simultâneos no mesmo produto: o último vence e sobrescreve o primeiro silenciosamente. Resolva com o `xmin` do PostgreSQL (`UseXminAsConcurrencyToken`) e devolva `409` no conflito.
4. **User secrets.** Tire a senha do `appsettings.json` e coloque em `dotnet user-secrets`. Entenda por que credencial em arquivo versionado é incidente de segurança.
5. **Índice de verdade.** Rode `EXPLAIN ANALYZE` na query de listagem com filtro de categoria, com e sem o índice de `CategoryId`. Compare os planos.
6. **Git.** Faça commits separados por assunto (domínio / persistência / endpoints / docker), com mensagens no padrão Conventional Commits.

---

## Dívidas técnicas propositais

Estas coisas estão erradas de propósito — cada uma é resolvida num módulo específico. Não conserte agora:

| O que | Onde resolve |
|---|---|
| Endpoints falam direto com `DbContext`, sem camada de aplicação | Módulo 2 (Clean Architecture, Repository) |
| Validação com DataAnnotations, regras espalhadas | Módulo 2 (FluentValidation) |
| Regra de negócio sinaliza erro via exceção | Módulo 2 (Result Pattern) |
| `Price` + `Currency` como campos soltos | Módulo 2/4 (Value Object `Money`) |
| Nenhum teste automatizado | Módulo 3 (xUnit, Testcontainers) |
| API aberta, sem autenticação | Módulo 5 (JWT, Keycloak) |
| Busca com `ILIKE %texto%` não usa índice | Módulo 6 (Full Text Search, GIN) |
| Sem cache | Módulo 6 (Redis, Cache Aside) |
| Migration aplicada pelo processo da API | Módulo 14 (pipeline) |
| Senha do banco em texto plano no compose | Módulo 14 (Secrets) |

---

## Próximo passo

Módulo 2 — Código Profissional: separar em camadas (Domain / Application / Infrastructure / Api), introduzir Repository + Unit of Work, FluentValidation, Result Pattern e o Value Object `Money`. O código atual vira o "antes" da refatoração.
