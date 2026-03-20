# Architecture de MecaPro

MecaPro suit une architecture en couches (Clean Architecture / Onion Architecture) pour assurer la séparation des préoccupations et la testabilité.

## 🏗️ Structure des Couches

### 1. Domain (`MecaPro.Domain`)
- **Responsabilité :** Contient la logique métier pure, les entités, les objets de valeur (Value Objects) et les interfaces de repository.
- **Éléments clés :** `Customer`, `Vehicle`, `Address`, `Money`, `IUnitOfWork`.
- **Dépendances :** Aucune.

### 2. Application (`MecaPro.Application`)
- **Responsabilité :** Orchestre les cas d'utilisation (Use Cases) via MediatR.
- **Éléments clés :** Commandes, Requêtes, Handlers, DTOs, Validations (FluentValidation).
- **Dépendances :** Domain.

### 3. Infrastructure (`MecaPro.Infrastructure`)
- **Responsabilité :** Implémente les détails techniques (Base de données, Identité, Services externes).
- **Éléments clés :** 
  - `AppDbContext` (EF Core + SQL Server)
  - `JwtTokenService` (Authentification)
  - `CrmService`, `PaymentService` (Stripe), `EmailService`.
- **Dépendances :** Domain, Application.

### 4. Présentation / API (`MecaPro.API` & `MecaPro.Blazor`)
- **API :** Points de terminaison Minimal APIs (via Carter) exposant les services.
- **Blazor :** Frontend WebAssembly interactif pour les garagistes et clients.
- **Dépendances :** Infrastructure, Application, Domain.

## 🛠️ Stack Technique
- **Backend :** .NET 9, EF Core, MediatR, Carter, Serilog.
- **Frontend :** Blazor WebAssembly, MudBlazor (UI).
- **Infrastructure :** Docker, SQL Server, Redis, Seq (Logs).
- **Communication :** REST API, SignalR (Notifications temps réel).
