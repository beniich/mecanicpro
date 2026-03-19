# MecaPro — SaaS Mécanique Professionnelle

MecaPro est une solution complète (API .NET 8 + Blazor WASM) destinée aux garages automobiles. Elle gère l'intégralité du cycle de vie client, depuis le diagnostic jusqu'à la facturation, en passant par la gestion des stocks, la fidélité, et les abonnements SaaS.

## 🚀 Architecture Globale

La solution respecte la Clean Architecture et le pattern CQRS avec MediatR :
1. **Domain Layer** (Core) : Entités métier (Vehicle, Customer, Diagnostic), Value Objects (Money, VIN, LicensePlate), Domain Events.
2. **Application Layer** : CQRS (Commands/Queries), FluentValidation, Pipeline Behaviors (Log, Cache, Transac).
3. **Infrastructure Layer** : EF Core, SQL Server, Redis, Stripe, Azure Blob, SendGrid, SignalR, Background Jobs (Hangfire), SkiaSharp (PDF).
4. **API Layer** : Minimal APIs (Carter), Rate Limiting, RBAC, JWT.
5. **Blazor WASM** : Frontend Web.

## ⚙️ Prérequis

- **.NET 8 SDK**
- **SQL Server 2022** (ou Docker)
- **Redis 7** (ou Docker)
- Docker Desktop (optionnel pour exécution via `docker-compose`)

## 🛠️ Démarrage Rapide (Local)

1. Lancer les services (SQL Server + Redis + Seq) via Docker :
   ```bash
   docker-compose up -d sqlserver redis seq
   ```

2. Configurer les User Secrets pour l'API :
   ```bash
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/MecaPro.API
   dotnet user-secrets set "Jwt:PrivateKeyPem" "-----BEGIN RSA..." --project src/MecaPro.API
   ```

3. Lancer les migrations et exécuter l'API :
   ```bash
   cd src/MecaPro.API
   dotnet ef database update --project ../MecaPro.Infrastructure
   dotnet run
   ```
   L'API sera disponible sur `http://localhost:5000`. Le Swagger UI est remplacé par **Scalar** via `/scalar/v1`.

## 📦 Fonctionnalités Principales (Les 15 Phases Couvertes)

- **Auth & Sécu** : JWT Asymétrique (RS256), Rotation Refresh Tokens, TOTP 2FA, RBAC.
- **CRM & Fidélité** : Profil Client 360°, Tiers Bronze→Platinum, points automatiques suite aux révisions.
- **Abonnements & Stripe** : Plans SaaS (Starter/Pro), Webhooks Stripe, Checkout Session, Customer Portal.
- **Révisions & Diagnostics** : Codes OBD, génération de QR Codes par véhicule (QRCoder), workflow de réparation complet.
- **E-Commerce & Stocks** : Catalogue de pièces, panier Redis, webhook payment_intent Stripe, decrement de stock transactionnel.
- **Facturation** : Génération automatique de PDF 100% C# (SkiaSharp), numérotation séquentielle garantie, TVA.
- **SignalR & Notifs** : Chat temps réel (mécanicien ↔ client) spécifique par véhicule, Hub notifications, envois SMS/Email (Twilio/SendGrid).
- **Background Jobs** : Hangfire (Rappels de Révision automatiques, Alertes de stock faible, Retards de paiement / Dunning).
- **Observabilité** : Serilog + Seq, OpenTelemetry (Metrics & Tracing), Health Checks UI.

## 🚢 Déploiement Production (CI/CD GitHub + Serveurs)

1. Vous trouverez le workflow de CI/CD dans `.github/workflows/main.yml`.
2. Le Dockerfile `MecaPro/Dockerfile` utilise un build multi-stage.
3. Le fichier `docker-compose.yml` prépare les bases SQL Server, Redis et Seq.
4. L'API tourne via Kestrel. Sur votre nom de domaine, configurez Nginx en Reverse Proxy 80/443 pointing to 5000.
5. Utiliser le script `deploy.sh` inclus.
