<p align="center">
  <img src="frontend/public/branding/logo.svg" alt="API Testing Packet" width="430">
</p>

<p align="center">
  <a href="#english">English</a> •
  <a href="#türkçe">Türkçe</a>
</p>

---

# English

# API Testing Packet

API Testing Packet is a self-hosted API workbench for building HTTP requests, chaining response values, checking assertions, running ordered scenarios, and executing the same versioned packets in CI. It combines a focused React developer interface with a .NET execution engine shared by the web API and CLI.

> **Status:** Usable V1 foundation. The core execution workflow, authenticated workspaces, collections, environments, scenarios, history, native import/export, bounded OpenAPI 3 import, CLI reporting, containers, and automated tests are implemented. See [Current limitations](#current-limitations) for honest boundaries.

## Screenshots

Screenshots will be added after the first tagged release. The application currently includes a polished responsive dark/light workbench rather than placeholder mockups.

## Features

- Email/password accounts with BCrypt hashing, short-lived JWTs, HttpOnly rotating hashed refresh tokens, revocation, and rate-limited auth endpoints.
- Per-user collections, nested folders, request duplication, environments, scenarios, execution history, search, pagination, and dashboard summaries.
- HTTP methods, query parameters, headers, JSON/text/form/multipart fields, bearer/basic/API-key authentication, timeouts, and variable interpolation.
- One shared execution core for manual sends, sequential scenarios, and CLI runs.
- Status, duration, header, JSON path/value/type, array length, format, required-field, and recursive sensitive-field assertions.
- JSON/header extraction into runtime variables for subsequent steps.
- Sanitized immutable history snapshots, response size limits, scheme restrictions, and configurable private-network policy.
- Versioned `.atp.json` import/export and OpenAPI 3 JSON/YAML import with explicit warnings.
- Console, JSON, and JUnit CLI output with CI-friendly exit codes.

## Architecture

The backend is a layered modular monolith with the primary flow:

`API -> Service -> Repository -> Core`

- `Core` owns entities, DTOs, exceptions, and interfaces.
- `Repository` owns EF Core and PostgreSQL.
- `Service` owns business logic and the shared request-execution engine.
- `API` is a thin HTTP host.

The CLI consumes the same `Service` execution engine.

PostgreSQL stores identities and queryable ownership metadata relationally, while flexible request definitions and sanitized snapshots use JSONB.

See [architecture](docs/architecture.md) and [execution engine](docs/execution-engine.md).

## Technology

- .NET 10, ASP.NET Core, EF Core, Npgsql/PostgreSQL, JWT, BCrypt, OpenAPI
- React 19, strict TypeScript, Vite, React Router, TanStack Query, Zustand, React Hook Form, Zod, PrimeReact
- xUnit, Vitest, React Testing Library, Docker Compose, GitHub Actions

## Requirements

- Docker 29+ and Docker Compose (recommended), or
- .NET SDK 10, Node.js 24, npm 11, and PostgreSQL 16+

## Docker Quick Start

```bash
cp .env.example .env
# Replace POSTGRES_PASSWORD and JWT_SIGNING_KEY in .env
docker compose up --build
```

Open `http://localhost:3000`.

Compose explicitly sets `ApplyMigrations=true`, waits for PostgreSQL, applies EF migrations, and then starts the API. Do not reuse the example secrets for a shared deployment.

## Manual Development

Start PostgreSQL, then:

```bash
dotnet restore backend/ApiTestingPacket.slnx

dotnet ef database update \
  --project backend/src/ApiTestingPacket.Repository \
  --startup-project backend/src/ApiTestingPacket.API

dotnet run --project backend/src/ApiTestingPacket.API

cd frontend
npm ci
npm run dev
```

The Vite development server proxies `/api` and `/health` to the API launch URL at `http://localhost:5115`, so registration and refresh cookies remain same-origin.

Set `VITE_API_URL` only when intentionally using a different API origin.

### Configuration

Environment variables use ASP.NET Core's `__` separator:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL connection string |
| `Jwt__SigningKey` | At least 32 bytes; use 64 random characters in deployments |
| `Jwt__Issuer`, `Jwt__Audience` | JWT validation scope |
| `OutboundRequests__AllowPrivateNetworks` | Allows loopback/private targets; `false` in production by default |
| `OutboundRequests__MaxResponseBytes` | Maximum captured response, default 5 MiB |
| `AllowedOrigins__0` | Trusted frontend origin |
| `ApplyMigrations` | Explicitly apply pending migrations at startup |
| `VITE_API_URL` | Browser API base URL; blank uses same origin |

## CLI

Run directly from source:

```bash
dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --environment demo

dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --report junit --output reports/results.xml
```

Or create and install a local tool package:

```bash
dotnet pack backend/src/ApiTestingPacket.Cli -c Release -o artifacts

dotnet tool install --global --add-source artifacts ApiTestingPacket.Cli

api-testing-packet run examples/httpbin-workflow.atp.json
```

Exit codes:

- `0` — Every request/assertion passed.
- `1` — Execution/test failure.
- `2` — Invalid CLI input or packet data.

Private/localhost targets require the explicit `--allow-private` flag.

## Variables and Assertions

Use:

```text
{{baseUrl}}/users/{{userId}}
```

in URLs, headers, auth values, and bodies.

Precedence:

`Packet defaults -> Selected environment -> Runtime/extracted values`

A missing variable fails before sending.

Assertions are structured definitions in packets.

Example:

```json
{
  "kind": "JsonValue",
  "operator": "Equals",
  "path": "user.id",
  "expected": "42",
  "name": "Correct user"
}
```

See [assertions](docs/assertions.md) and [environments](docs/environments.md).

## Packet, Scenarios, and Imports

The schema is versioned with:

```json
"schemaVersion": 1
```

Scenarios run in order and extracted values override environment values for later steps.

Export omits secret environment values by default.

See [packet format](docs/packet-format.md) and the [example packet](examples/httpbin-workflow.atp.json).

OpenAPI import accepts OpenAPI 3 JSON/YAML.

It imports:

- Servers
- Supported HTTP operations
- Query/header parameters
- JSON body examples

It reports unsupported parameter/body/security constructs. Imports are parsed before the collection is persisted.

See [OpenAPI import](docs/openapi-import.md).

## CI/CD

This repository runs backend and frontend quality gates in `.github/workflows/ci.yml`.

Another repository can run a packet with:

```yaml
- uses: actions/setup-dotnet@v5
  with:
    dotnet-version: '10.0.x'

- run: dotnet tool install --global ApiTestingPacket.Cli

- run: api-testing-packet run tests/api.atp.json --environment ci --report junit --output results.xml
```

Pin the tool version in production workflows.

## Testing

```bash
dotnet format backend/ApiTestingPacket.slnx --verify-no-changes
dotnet build backend/ApiTestingPacket.slnx --configuration Release
dotnet test backend/ApiTestingPacket.slnx --configuration Release

cd frontend
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

Repository tests do not call public APIs. The example packet uses a public demo service only for opt-in manual runs.

## Security

Production denies private, loopback, link-local, and unspecified destination addresses by default and only permits HTTP(S).

DNS results are checked before connecting, redirects are disabled, responses and timeouts are bounded, and sensitive snapshots are redacted.

This reduces but cannot eliminate all SSRF/DNS-rebinding risk in every network topology; deploy the backend with egress controls.

The sensitive-field assertion is a helper, not a security scanner.

Read [SECURITY.md](SECURITY.md) and [security design](docs/security.md).

## Repository Structure

```text
backend/    Core, Repository, Service, thin ASP.NET Core API, CLI, migrations, tests
frontend/   React/Vite application and component tests
docs/       Architecture and user/developer documentation
examples/   Versioned test packets
.github/    CI, Dependabot, issue and pull-request templates
```

## Current Limitations

- Multipart V1 supports text fields; interactive binary file streaming is not yet exposed in the React editor.
- OpenAPI import intentionally omits callbacks, links, polymorphic schema generation, and automatic OAuth flows.
- Scenario authoring is currently packet/API driven; the UI lists and runs stored definitions but does not yet provide drag-and-drop composition or an active stop control.
- Advanced request auth, assertion, and extraction authoring is currently packet/API driven; the workspace tabs preserve editor state and display results but do not yet expose every configuration as a visual form.
- Redirects are disabled rather than revalidated hop-by-hop.
- HTML reporting is not included; console, JSON, and JUnit are supported.
- Secret environment values are masked conceptually and excluded from exports, but at-rest application-level encryption is not included; secure the PostgreSQL volume and deployment.

## Branding

The original packet-and-terminal logo is included as SVG artwork in `frontend/public/branding`.

It is used in:

- Application shell
- Authentication screen
- Favicon
- README

It is covered by the repository's MIT license and needs no third-party attribution.

See [branding guidance](docs/branding.md).

## Contributing, Roadmap, and License

Read [CONTRIBUTING.md](CONTRIBUTING.md).

Near-term roadmap items:

- Scenario visual editor
- Safe streamed file upload
- Redirect revalidation
- Richer OpenAPI examples

API Testing Packet is available under the [MIT License](LICENSE).

---

# Türkçe

# API Testing Packet

API Testing Packet; HTTP istekleri oluşturmak, response değerlerini sonraki isteklere aktarmak, assertion kontrolleri yapmak, sıralı senaryolar çalıştırmak ve aynı versiyonlanmış test paketlerini CI ortamlarında yürütmek için geliştirilmiş self-hosted bir API çalışma ortamıdır.

Odaklı bir React geliştirici arayüzünü, Web API ve CLI tarafından ortak kullanılan .NET tabanlı bir execution engine ile birleştirir.

> **Durum:** Kullanılabilir V1 temeli. Temel execution akışı, kimlik doğrulamalı çalışma alanları, koleksiyonlar, environment'lar, senaryolar, geçmiş kayıtları, native import/export, sınırlandırılmış OpenAPI 3 import desteği, CLI raporlama, container desteği ve otomatik testler uygulanmıştır. Ayrıntılar için [Mevcut Sınırlamalar](#mevcut-sınırlamalar-1) bölümüne bakabilirsiniz.

## Ekran Görüntüleri

Ekran görüntüleri ilk etiketlenmiş sürümden sonra eklenecektir. Uygulama şu anda placeholder tasarımlar yerine responsive, dark/light tema destekli ve kullanıma hazır bir çalışma arayüzü içermektedir.

## Özellikler

- BCrypt parola hashleme, kısa ömürlü JWT, HttpOnly rotating hashed refresh token, token iptali ve rate-limit uygulanmış authentication endpointleriyle e-posta/parola hesap sistemi.
- Kullanıcı bazında koleksiyonlar, iç içe klasörler, request çoğaltma, environment'lar, senaryolar, çalışma geçmişi, arama, pagination ve dashboard özetleri.
- HTTP metodları, query parametreleri, header'lar, JSON/text/form/multipart alanları, Bearer/Basic/API Key authentication, timeout ve variable interpolation.
- Manuel request'ler, sıralı senaryolar ve CLI çalıştırmaları için ortak execution engine.
- Status, süre, header, JSON path/value/type, array uzunluğu, format, required-field ve recursive sensitive-field assertion'ları.
- Sonraki adımlarda kullanılmak üzere JSON/header değerlerinin runtime değişkenlerine çıkarılması.
- Temizlenmiş immutable history snapshot'ları, response boyutu limitleri, scheme kısıtlamaları ve yapılandırılabilir private-network politikası.
- Versiyonlanmış `.atp.json` import/export ve açık uyarılarla OpenAPI 3 JSON/YAML import desteği.
- CI uyumlu exit code'larla Console, JSON ve JUnit CLI çıktıları.

## Mimari

Backend, aşağıdaki temel akışa sahip katmanlı bir modular monolith mimarisidir:

`API -> Service -> Repository -> Core`

- `Core`: Entity'leri, DTO'ları, exception'ları ve interface'leri içerir.
- `Repository`: EF Core ve PostgreSQL veri erişim katmanını içerir.
- `Service`: İş mantığını ve ortak request execution engine'i içerir.
- `API`: İnce bir HTTP host katmanıdır.

CLI da aynı `Service` execution engine'ini kullanır.

PostgreSQL, kullanıcı kimliklerini ve sorgulanabilir ownership metadata'larını ilişkisel olarak saklarken esnek request tanımları ve temizlenmiş snapshot'lar JSONB olarak tutulur.

Detaylar için [mimari](docs/architecture.md) ve [execution engine](docs/execution-engine.md) dokümanlarına bakabilirsiniz.

## Teknolojiler

- .NET 10, ASP.NET Core, EF Core, Npgsql/PostgreSQL, JWT, BCrypt, OpenAPI
- React 19, strict TypeScript, Vite, React Router, TanStack Query, Zustand, React Hook Form, Zod, PrimeReact
- xUnit, Vitest, React Testing Library, Docker Compose, GitHub Actions

## Gereksinimler

- Docker 29+ ve Docker Compose (önerilen), veya
- .NET SDK 10, Node.js 24, npm 11 ve PostgreSQL 16+

## Docker ile Hızlı Başlangıç

```bash
cp .env.example .env
# .env içerisindeki POSTGRES_PASSWORD ve JWT_SIGNING_KEY değerlerini değiştirin
docker compose up --build
```

Ardından `http://localhost:3000` adresini açın.

Compose açıkça `ApplyMigrations=true` değerini ayarlar, PostgreSQL'in hazır olmasını bekler, EF migration'larını uygular ve ardından API'yi başlatır.

Örnek secret değerlerini ortak veya production ortamında kullanmayın.

## Manuel Geliştirme

PostgreSQL'i başlatın, ardından:

```bash
dotnet restore backend/ApiTestingPacket.slnx

dotnet ef database update \
  --project backend/src/ApiTestingPacket.Repository \
  --startup-project backend/src/ApiTestingPacket.API

dotnet run --project backend/src/ApiTestingPacket.API

cd frontend
npm ci
npm run dev
```

Vite development server, `/api` ve `/health` isteklerini API'nin `http://localhost:5115` adresine proxy eder.

Böylece registration ve refresh cookie'leri same-origin olarak çalışmaya devam eder.

`VITE_API_URL` değerini yalnızca bilinçli olarak farklı bir API origin kullanmak istediğinizde ayarlayın.

### Yapılandırma

Environment variable'lar ASP.NET Core'un `__` ayırıcısını kullanır:

| Değişken | Amaç |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL bağlantı bilgisi |
| `Jwt__SigningKey` | En az 32 byte; deployment ortamlarında 64 rastgele karakter önerilir |
| `Jwt__Issuer`, `Jwt__Audience` | JWT doğrulama kapsamı |
| `OutboundRequests__AllowPrivateNetworks` | Loopback/private hedeflere izin verir; production'da varsayılan `false` |
| `OutboundRequests__MaxResponseBytes` | Maksimum yakalanan response boyutu; varsayılan 5 MiB |
| `AllowedOrigins__0` | Güvenilen frontend origin'i |
| `ApplyMigrations` | Bekleyen migration'ları startup sırasında uygular |
| `VITE_API_URL` | Browser API base URL'i; boş bırakılırsa same-origin kullanılır |

## CLI

Doğrudan kaynak kod üzerinden:

```bash
dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --environment demo

dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --report junit --output reports/results.xml
```

Local tool paketi oluşturup yüklemek için:

```bash
dotnet pack backend/src/ApiTestingPacket.Cli -c Release -o artifacts

dotnet tool install --global --add-source artifacts ApiTestingPacket.Cli

api-testing-packet run examples/httpbin-workflow.atp.json
```

Exit code'lar:

- `0` — Tüm request ve assertion'lar başarılı.
- `1` — Execution/test başarısız.
- `2` — Geçersiz CLI girdisi veya packet verisi.

Private/localhost hedefleri için `--allow-private` flag'i açıkça kullanılmalıdır.

## Değişkenler ve Assertion'lar

URL, header, authentication değerleri ve body içerisinde:

```text
{{baseUrl}}/users/{{userId}}
```

kullanılabilir.

Öncelik sırası:

`Packet defaults -> Seçilen environment -> Runtime/extracted values`

Eksik bir değişken varsa request gönderilmeden önce işlem başarısız olur.

Assertion'lar packet içerisinde yapılandırılmış tanımlar olarak tutulur.

Örnek:

```json
{
  "kind": "JsonValue",
  "operator": "Equals",
  "path": "user.id",
  "expected": "42",
  "name": "Correct user"
}
```

Detaylar için [assertions](docs/assertions.md) ve [environments](docs/environments.md) dokümanlarına bakabilirsiniz.

## Packet, Senaryolar ve Import

Schema:

```json
"schemaVersion": 1
```

ile versiyonlanır.

Senaryolar sıralı olarak çalışır ve çıkarılan değerler sonraki adımlarda environment değerlerinin üzerine yazılır.

Export işlemleri varsayılan olarak secret environment değerlerini dışarı aktarmaz.

Detaylar için [packet formatı](docs/packet-format.md) ve [örnek packet](examples/httpbin-workflow.atp.json) dosyasına bakabilirsiniz.

OpenAPI import özelliği OpenAPI 3 JSON/YAML formatlarını kabul eder.

Şunları içe aktarabilir:

- Server tanımları
- Desteklenen HTTP operasyonları
- Query/header parametreleri
- JSON body örnekleri

Desteklenmeyen parameter/body/security yapıları için uyarılar oluşturulur.

Import edilen içerik collection veritabanına kaydedilmeden önce parse edilir.

Detaylar için [OpenAPI import](docs/openapi-import.md) dokümanına bakabilirsiniz.

## CI/CD

Repository, `.github/workflows/ci.yml` içerisinde backend ve frontend kalite kontrollerini çalıştırır.

Başka bir repository packet çalıştırmak için:

```yaml
- uses: actions/setup-dotnet@v5
  with:
    dotnet-version: '10.0.x'

- run: dotnet tool install --global ApiTestingPacket.Cli

- run: api-testing-packet run tests/api.atp.json --environment ci --report junit --output results.xml
```

Production workflow'larında tool sürümünü sabitleyin.

## Test

```bash
dotnet format backend/ApiTestingPacket.slnx --verify-no-changes
dotnet build backend/ApiTestingPacket.slnx --configuration Release
dotnet test backend/ApiTestingPacket.slnx --configuration Release

cd frontend
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

Repository testleri public API'lere istek göndermez.

Örnek packet yalnızca isteğe bağlı manuel çalıştırmalar için public bir demo servisi kullanır.

## Güvenlik

Production ortamında private, loopback, link-local ve unspecified destination adresleri varsayılan olarak engellenir ve yalnızca HTTP(S) protokollerine izin verilir.

DNS sonuçları bağlantı kurulmadan önce kontrol edilir, redirect'ler devre dışı bırakılır, response boyutları ve timeout süreleri sınırlandırılır ve hassas snapshot verileri maskelenir.

Bu önlemler SSRF/DNS-rebinding risklerini azaltır ancak her ağ topolojisinde tamamen ortadan kaldırmayı garanti etmez.

Backend'in egress kontrolleriyle birlikte deploy edilmesi önerilir.

Sensitive-field assertion yardımcı bir kontroldür; tam kapsamlı bir güvenlik tarayıcısı değildir.

Detaylar için [SECURITY.md](SECURITY.md) ve [güvenlik tasarımı](docs/security.md) dokümanlarını okuyabilirsiniz.

## Repository Yapısı

```text
backend/    Core, Repository, Service, ince ASP.NET Core API, CLI, migration'lar, testler
frontend/   React/Vite uygulaması ve component testleri
docs/       Mimari ve kullanıcı/geliştirici dokümantasyonu
examples/   Versiyonlanmış test packet'ları
.github/    CI, Dependabot, issue ve pull-request template'leri
```

## Mevcut Sınırlamalar

- Multipart V1 text field'larını destekler; interactive binary file streaming henüz React editöründe sunulmamaktadır.
- OpenAPI import; callback'leri, link'leri, polymorphic schema generation'ı ve otomatik OAuth flow'larını kapsam dışı bırakmaktadır.
- Senaryo oluşturma şu anda packet/API üzerinden yapılmaktadır. UI kayıtlı tanımları listeler ve çalıştırır ancak henüz drag-and-drop composition veya aktif stop kontrolü sunmaz.
- Gelişmiş request auth, assertion ve extraction tanımları şu anda packet/API üzerinden yapılmaktadır. Workspace tab'ları editor state'ini korur ve sonuçları gösterir ancak her yapılandırmayı henüz görsel form olarak sunmaz.
- Redirect'ler hop-by-hop yeniden doğrulanmak yerine devre dışı bırakılmıştır.
- HTML raporlama bulunmamaktadır; Console, JSON ve JUnit desteklenmektedir.
- Secret environment değerleri maskelenir ve export işlemlerinden çıkarılır ancak application-level at-rest encryption henüz bulunmamaktadır. PostgreSQL volume ve deployment ortamı ayrıca güvenli hale getirilmelidir.

## Marka ve Logo

Orijinal packet-and-terminal logosu SVG olarak `frontend/public/branding` klasöründe bulunmaktadır.

Logo:

- Uygulama shell'inde
- Authentication ekranında
- Favicon'da
- README'de

kullanılmaktadır.

Logo repository'nin MIT lisansı kapsamındadır ve üçüncü taraf attribution gerektirmez.

Detaylar için [branding rehberi](docs/branding.md) dokümanına bakabilirsiniz.

## Katkıda Bulunma, Roadmap ve Lisans

Katkıda bulunmak için [CONTRIBUTING.md](CONTRIBUTING.md) dosyasını okuyabilirsiniz.

Yakın dönem roadmap:

- Görsel senaryo editörü
- Güvenli streamed file upload
- Redirect revalidation
- Daha gelişmiş OpenAPI örnek desteği

API Testing Packet, [MIT Lisansı](LICENSE) altında sunulmaktadır.
