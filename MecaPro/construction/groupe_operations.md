# 🏭 GROUPE OPÉRATIONNEL — MECAPRO
> **Document de Référence Technique**
> Dernière mise à jour : 20 Mars 2026
> Version : 2.0.0 — Phase Opérationnelle Complète

Ce document recense et détaille **chaque opération technique** du projet MecaPro,
organisée par couche architecturale (Clean Architecture). Pour chaque opération :
son rôle, sa définition, les fichiers impliqués, les déclencheurs, les critères
de validation et le statut actuel.

---

## 🗺️ CARTE DES COUCHES OPÉRATIONNELLES

```
┌─────────────────────────────────────────────────────────────┐
│  O5 — BLAZOR (Frontend WebAssembly)                         │
│       Pages · Services · ApiClient · DTO Sync               │
├─────────────────────────────────────────────────────────────┤
│  O4 — API (Carter Minimal APIs)                             │
│       Endpoints · Auth JWT · Modules Carter                 │
├─────────────────────────────────────────────────────────────┤
│  O3 — APPLICATION (CQRS + MediatR)                          │
│       Commands · Queries · Handlers · DTOs · Pipelines      │
├─────────────────────────────────────────────────────────────┤
│  O2 — INFRASTRUCTURE (EF Core + SQL Server)                 │
│       DbContext · Repositories · Migrations · Seeding       │
├─────────────────────────────────────────────────────────────┤
│  O1 — DOMAINE (Pure Business Logic)                         │
│       Entities · Value Objects · Interfaces · Enums         │
└─────────────────────────────────────────────────────────────┘
         O6 — CONFIGURATION (Docker · Redis · JWT · CORS)
         O7 — DÉPLOIEMENT (CI/CD · Migrations · Tests)
```

---

## 📦 O1 — COUCHE DOMAINE (`MecaPro.Domain`)

> **Rôle Général :** Contient la logique métier pure. Ne dépend d'aucune autre couche.
> Toute règle business est encodée ici. Aucun framework externe autorisé.

---

### O1.1 — Entités Racines (Aggregates)

- 🔴 **Rôle :** Représenter les objets métier principaux avec identité propre et cycle de vie.
- 🔵 **Définition :** Les `AggregateRoot<TId>` sont les entités racines qui centralisent la cohérence
  du modèle. Elles émettent des `DomainEvents` et protègent leurs invariants via des méthodes.
- 📁 **Fichiers :** `src/MecaPro.Domain/Domain.cs`
- 🧱 **Aggregates existants :**
  - `Customer` — Dossier client (loyauté, véhicules, contact, B2B)
  - `Vehicle` — Véhicule avec QR code, statuts, diagnostics
  - `Revision` — Intervention avec tâches (`RevisionTask`) et pièces posées (`RevisionPart`)
  - `Diagnostic` — Code OBD-III + sévérité + résolution
  - `Part` — Pièce de stock avec alertes de niveau
  - `Order` — Bon de commande client avec lignes
  - `Subscription` — Abonnement SaaS garage (Stripe)
- ⚡ **Déclencheurs :** Toute modification de règle métier nécessite une modification ici *en premier*
- ✅ **Critères de Validation :**
  - [ ] Aucune dépendance sur EF Core, MediatR ou tout framework
  - [ ] Chaque aggregate expose uniquement des méthodes publiques (pas de setter direct)
  - [ ] Les invariants sont vérifiés dans les constructeurs et méthodes
- 📊 **Statut :** ✅ FAIT (Customer, Vehicle, Revision, Diagnostic, Part, Order, Subscription)

---

### O1.2 — Value Objects

- 🔴 **Rôle :** Représenter des concepts métier sans identité propre (immuables, comparés par valeur).
- 🔵 **Définition :** Les Value Objects encapsulent la validation et le formatage d'un concept.
  Un `Email` ne peut pas être créé sans passer la validation `@`. Un `Money` garantit la devise.
- 📁 **Fichiers :** `src/MecaPro.Domain/Domain.cs`
- 🧱 **Value Objects existants :**
  - `FullName` — Prénom + Nom validés (non vides)
  - `Email` — Format email validé, normalisé en minuscule
  - `Phone` — Numéro nettoyé (sans espaces)
  - `Money` — Montant + Devise EUR (conversion en centimes possible)
  - `LicensePlate` — Immatriculation normalisée en majuscules
  - `VIN` — NIV 17 caractères validé
  - `Address` — Rue, Ville, Code Postal, Pays
- ⚡ **Déclencheurs :** Utilisés à chaque création d'entité via les factories `Create()`
- ✅ **Critères de Validation :**
  - [ ] Toujours créés via une méthode `Create()` statique
  - [ ] Immuables après création (pas de setters publics)
  - [ ] Lèvent des `ArgumentException` explicites en cas de données invalides
- 📊 **Statut :** ✅ FAIT

---

### O1.3 — Interfaces de Repository

- 🔴 **Rôle :** Définir le contrat d'accès aux données sans couplage à l'implémentation.
- 🔵 **Définition :** Les interfaces `IXxxRepository` définissent ce que la couche Application
  peut demander à la persistance. L'Infrastructure implémente ces contrats.
  Ce pattern respecte le principe d'inversion de dépendance (DIP).
- 📁 **Fichiers :** `src/MecaPro.Domain/Domain.cs`
- 🧱 **Interfaces existantes :**
  - `IRepository<T, TId>` — Interface générique (GetById, GetAll, Add, Update, Remove)
  - `ICustomerRepository` — + GetByEmail, GetWithVehicles, GetPaged
  - `IVehicleRepository` — + GetByQrToken, GetByLicensePlate, GetByCustomerId
  - `IRevisionRepository` — + GetWithDetails, GetByVehicleId, GetPagedByVehicle, GetPaged
  - `IPartRepository` — + GetByCategory, GetByReference
  - `IOrderRepository` — Interface générique héritée
  - `ISubscriptionRepository` — Interface générique héritée
  - `IInvoiceRepository` — GetByCustomerId, GetById
  - `IUnitOfWork` — `SaveChangesAsync()` — orchestrateur de transaction
- ✅ **Critères de Validation :**
  - [ ] Chaque interface correspond à un aggregate existant
  - [ ] Pas de méthodes retournant des types EF Core ou SQL (IQueryable interdit)
  - [ ] `IUnitOfWork` est toujours utilisé à la fin d'un handler de Command
- 📊 **Statut :** ✅ FAIT

---

### O1.4 — Enumerations et Exceptions de Domaine

- 🔴 **Rôle :** Typer les états des entités et formaliser les erreurs métier.
- 🔵 **Définition :** Les enums représentent les états possibles d'un aggregate (ex. `RevisionStatus`).
  Les exceptions de domaine (`DomainException`, `NotFoundException`) permettent de distinguer
  une erreur métier d'une erreur technique.
- 📁 **Fichiers :** `src/MecaPro.Domain/Domain.cs`
- 🧱 **Enums existants :**
  - `VehicleStatus` — Active, InRepair, Idle, Retired
  - `DiagnosticSeverity` — Info, Minor, Major, Critical
  - `DiagnosticStatus` — Detected, InAnalysis, Resolved, Ignored
  - `RevisionStatus` — Scheduled, InProgress, Completed, Cancelled
  - `OrderStatus` — Draft, Pending, Paid, Shipped, Cancelled
  - `CustomerSegment` — Standard, Silver, Gold, Platinum, VIP
  - `SubscriptionStatus` — Active, Trialing, PastDue, Cancelled
  - `ContactChannel` — Email, SMS, Phone, WhatsApp
- 📊 **Statut :** ✅ FAIT

---

## 🗄️ O2 — COUCHE INFRASTRUCTURE (`MecaPro.Infrastructure`)

> **Rôle Général :** Implémente les détails techniques. Connaît EF Core, SQL Server,
> Redis, et les services externes. Convertit les abstractions du Domain en concret.

---

### O2.1 — DbContext (AppDbContext)

- 🔴 **Rôle :** Point d'entrée unique vers la base de données via Entity Framework Core.
- 🔵 **Définition :** `AppDbContext` hérite de `IdentityDbContext<AppUser>` et déclare tous
  les `DbSet<T>` correspondant aux aggregates. C'est ici que les configurations EF sont
  appliquées via `IEntityTypeConfiguration<T>`.
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs`
- 🧱 **DbSets déclarés :**
  - `Customers`, `Vehicles`, `Diagnostics`, `Revisions`
  - `RevisionTasks`, `RevisionParts`
  - `Parts`, `Orders`, `OrderItems`
  - `Subscriptions`, `Invoices`, `OutboxMessages`
- ⚡ **Déclencheurs :** Toute nouvelle entité Domain nécessite l'ajout d'un DbSet + une configuration
- ✅ **Critères de Validation :**
  - [ ] Chaque DbSet correspond à une entité Domain
  - [ ] `OnModelCreating` applique toutes les configurations via `ApplyConfigurationsFromAssembly`
  - [ ] Aucun mapping SQL en dur dans AppDbContext (tout dans les classes de configuration)
- 📊 **Statut :** ✅ FAIT

---

### O2.2 — Configurations EF Core

- 🔴 **Rôle :** Définir le mapping objet-relationnel : colonnes, contraintes, relations, Value Objects.
- 🔵 **Définition :** Les classes `XxxConfiguration : IEntityTypeConfiguration<T>` définissent
  comment une entité est persistée. Les Value Objects sont mappés avec `OwnsOne()` ou `OwnsMany()`.
  Les relations entre entités sont déclarées ici (HasMany, HasOne, WithMany).
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs`
- 🧱 **Configurations existantes :**
  - `CustomerConfiguration` — OwnsOne(Name, Email, Phone, Address), OwnsOne(Loyalty) via JSON
  - `VehicleConfiguration` — OwnsOne(LicensePlate, VIN)
  - `RevisionConfiguration` — HasMany(Tasks), HasMany(Parts), OwnsOne(EstimatedCost)
- ⚡ **Déclencheurs :** Modification d'un Value Object ou ajout d'une relation dans le Domain
- ✅ **Critères de Validation :**
  - [ ] Chaque Value Object est mappé avec `OwnsOne` ou `ToJson`
  - [ ] Les colonnes `NOT NULL` aient des contraintes correspondantes dans EF
  - [ ] Les suppressions en cascade sont explicitement définies
- 📊 **Statut :** ✅ FAIT (partiel — à compléter pour Order, Part, Subscription)

---

### O2.3 — Repositories (Implémentations)

- 🔴 **Rôle :** Implémenter les contrats définis dans le Domain avec EF Core.
- 🔵 **Définition :** Chaque `XxxRepository` hérite d'un `BaseRepository<T, TId>` générique
  et surcharge les méthodes spécifiques. Il utilise directement `_context.Set<T>()` ou les
  DbSets typés pour les requêtes complexes avec `Include()`, `Where()`, `OrderBy()`.
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs`
- 🧱 **Repositories existants :**
  - `CustomerRepository` — GetByEmail, GetWithVehicles (Include), GetPaged (search full-text)
  - `VehicleRepository` — GetByQrToken, GetByLicensePlate, GetByCustomerId
  - `RevisionRepository` — GetWithDetailsAsync (Include Tasks + Parts), GetByVehicleId, GetPaged
  - `PartRepository` — GetByCategory, GetByReference
  - `OrderRepository` — CRUD générique
  - `SubscriptionRepository` — CRUD générique
- ⚡ **Dépendances :** `AppDbContext` (injecté via DI)
- ✅ **Critères de Validation :**
  - [ ] Toujours injecter `AppDbContext` (jamais `new DbContext()`)
  - [ ] Les queries paginées utilisent `Skip().Take()` côté SGBD (pas en mémoire)
  - [ ] Les `Include()` sont chargés explicitement (pas de lazy loading)
- 📊 **Statut :** ✅ FAIT

---

### O2.4 — Unit of Work

- 🔴 **Rôle :** Orchestrer la sauvegarde des changements + dispatch des Domain Events.
- 🔵 **Définition :** `UnitOfWork` wrappe `DbContext.SaveChangesAsync()` et, après la sauvegarde,
  publie tous les `DomainEvents` accumulés dans les aggregates en mémoire via `IMediator.Publish()`.
  C'est le mécanisme de découplage entre la persistance et les effets de bord (emails, notifications).
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs`
- ⚡ **Déclencheurs :** Appelé à la fin de chaque Command Handler pour valider une transaction
- ✅ **Critères de Validation :**
  - [ ] Toujours `await uow.SaveChangesAsync(ct)` dans les handlers de Command
  - [ ] Les Domain Events sont effacés après publication (`ClearDomainEvents`)
  - [ ] En cas d'exception, la transaction est rollback implicitement
- 📊 **Statut :** ✅ FAIT

---

### O2.5 — Migrations EF Core

- 🔴 **Rôle :** Versionner l'état de la base de données de manière reproductible.
- 🔵 **Définition :** Les migrations EF Core tracent chaque modification de schéma dans un fichier
  C# versionné. Lors du démarrage de l'API, `db.Database.MigrateAsync()` applique automatiquement
  toutes les migrations en attente en production (via le Seeder).
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Migrations/`
- ⚡ **Commandes :**
  ```bash
  # Créer une migration
  dotnet ef migrations add NomDeLaMigration --project src/MecaPro.Infrastructure --startup-project src/MecaPro.API
  # Appliquer
  dotnet ef database update --project src/MecaPro.Infrastructure --startup-project src/MecaPro.API
  ```
- ✅ **Critères de Validation :**
  - [ ] Chaque ajout de colonne/table génère une nouvelle migration (jamais modifier une migration existante)
  - [ ] Les migrations sont testées en local avant commit
  - [ ] Aucun `DropColumn` sans sauvegarde de données préalable
- 📊 **Statut :** ✅ FAIT (migration initiale appliquée)

---

### O2.6 — Seeding de Base de Données

- 🔴 **Rôle :** Peupler la base de données avec des données initiales réalistes pour les démonstrations.
- 🔵 **Définition :** `DatabaseSeeder` est appelé au démarrage de l'API (dans `Program.cs`).
  Il crée les rôles (SuperAdmin, Mechanic, Client), un utilisateur admin par défaut,
  et des données de démonstration (clients, véhicules, révisions, diagnostics, pièces).
  Il est idempotent : vérifie `if (await db.Customers.AnyAsync()) return` avant l'insertion.
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs` — classe `DatabaseSeeder`
- 🧱 **Données seedées :**
  - **Rôles :** SuperAdmin, GarageOwner, Mechanic, Client
  - **Users :** `admin@mecapro.com` (Admin@MecaPro123!), `mechanic@mecapro.com`
  - **Clients :** Marc Dupont, Sophie Martin
  - **Véhicules (4) :** Peugeot 308, Renault Clio, Volkswagen Golf, Toyota Yaris
  - **Révisions (3) :** Vidange planifiée, Freins en cours, Distribution programmée — avec tâches et pièces
  - **Diagnostics (2) :** P0301 (Major), P0420 (Minor)
  - **Stock (7 pièces) :** Filtres, Freinage, Allumage, Électrique, Suspension, Transmission, Pneumatiques
  - **Factures (2)** : INV-2025-0001 (Payée), INV-2025-0002 (Émise)
- ✅ **Critères de Validation :**
  - [ ] Le seeder est idempotent (plusieurs runs ne créent pas de doublons)
  - [ ] Les mots de passe dev respectent la politique Identity (majuscule, chiffre, spécial)
  - [ ] Les données seedées utilisent uniquement les factories du Domain (ex. `Customer.Create()`)
- 📊 **Statut :** ✅ FAIT

---

### O2.7 — Identity & JWT (Authentication)

- 🔴 **Rôle :** Gérer l'inscription, la connexion, et l'émission des tokens JWT.
- 🔵 **Définition :** `AppUser : IdentityUser` étend l'utilisateur ASP.NET Identity avec des
  propriétés métier (FirstName, LastName, GarageId, IsActive). `JwtTokenService` génère les
  tokens avec les claims (UserId, Email, Role, GarageId) et une durée de vie configurable.
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs`, `src/MecaPro.API/Program.cs`
- 🧱 **Claims injectés dans le JWT :**
  - `sub` (UserId), `email`, `role`, `garageId`, `jti` (token ID unique)
- ⚡ **Endpoints d'Auth :** `POST /api/v1/auth/login`, `POST /api/v1/auth/register`
- ✅ **Critères de Validation :**
  - [ ] Le secret JWT est dans `appsettings.json` (jamais en dur dans le code)
  - [ ] Les tokens expirent (actuellement 24h)
  - [ ] `IsActive = false` bloque la connexion même avec mot de passe correct
- 📊 **Statut :** ✅ FAIT

---

## ⚡ O3 — COUCHE APPLICATION (`MecaPro.Application`)

> **Rôle Général :** Orchestre les cas d'utilisation via le pattern CQRS + MediatR.
> Contient la logique applicative : validation, transformation de données (DTOs),
> dispatch vers les repositories. Ne connaît ni EF Core ni HTTP.

---

### O3.1 — Pattern Result<T>

- 🔴 **Rôle :** Uniformiser la gestion des succès et erreurs dans tous les handlers.
- 🔵 **Définition :** `Result<T>` est un wrapper générique remplaçant les exceptions pour les
  erreurs métier attendues. `Result<T>.Success(value)` ou `Result<T>.Failure("message")`.
  Les handlers retournent toujours un `Result<T>`, jamais directement `T` ou une exception brute.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- ⚡ **Utilisation :** Chaque handler retourne `Result<T>`. L'API transforme en HTTP 200/400/404.
- ✅ **Critères de Validation :**
  - [ ] Aucun handler ne `throw` directement (sauf erreurs systèmes imprévues)
  - [ ] `Result.Failure` doit contenir un message lisible pour l'utilisateur (en français)
- 📊 **Statut :** ✅ FAIT

---

### O3.2 — Pipeline de Comportement (Behaviors MediatR)

- 🔴 **Rôle :** Ajouter des comportements transversaux (logging, validation, cache) autour des handlers.
- 🔵 **Définition :** Les `IPipelineBehavior<TRequest, TResponse>` s'exécutent dans l'ordre
  avant et après chaque handler MediatR (comme des middlewares). Ils sont enregistrés dans
  `Program.cs` via `AddMediatR(cfg => cfg.AddBehavior<>())`.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- 🧱 **Pipelines existants :**
  - `LoggingBehavior` — Log le nom de chaque request avant/après exécution
  - `ValidationBehavior` — Exécute tous les `IValidator<TRequest>` FluentValidation avant le handler
  - `CachingBehavior` — Met en cache les résultats des queries qui implémentent `ICacheableRequest`
- ⚡ **Ordre d'exécution :** `Logging → Validation → Caching → Handler`
- ✅ **Critères de Validation :**
  - [ ] Les erreurs de validation (`ValidationException`) sont catchées dans l'API et retournent HTTP 400
  - [ ] Le cache est invalidé quand des données sont mutées (Command handlers)
- 📊 **Statut :** ✅ FAIT

---

### O3.3 — DTOs (Data Transfer Objects)

- 🔴 **Rôle :** Définir les contrats de données entre couches (Application ↔ API ↔ Blazor).
- 🔵 **Définition :** Les DTOs sont des `record` C# immuables. Ils ne contiennent aucune logique
  métier. Un DTO représente exactement ce qu'une vue ou un endpoint doit recevoir/retourner.
  La source de vérité unique se trouve dans `CQRS.cs` (Application) et est **dupliquée**
  par synchronisation dans `BlazorServices.cs` (Frontend).
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- 🧱 **DTOs existants :**

  | DTO | Contenu | Usage |
  |-----|---------|-------|
  | `CustomerDto` | Id, Nom, Email, Phone, Segment, Points | Liste clients, cartes CRM |
  | `CustomerDetailDto` | + Adresse, Véhicules, LoyaltyHistory, Revisions | Fiche complète client |
  | `VehicleDto` | Id, Plaque, Marque, Modèle, Statut | Cartes véhicule |
  | `VehicleDetailDto` | + VIN, FuelType, Color, QrCode, CustomerName | Fiche véhicule |
  | `RevisionDto` | Id, Type, Date, Statut, Coût estimé | Historiques, plannings |
  | `RevisionDetailDto` | + Tasks, Parts, ActualCost, Notes | Fiche Intervention |
  | `RevisionTaskDto` | Id, Description, Minutes estimés/réels, IsCompleted | Checklist technicien |
  | `RevisionPartDto` | Id, NomPièce, Quantité, Prix unitaire, Total | Facturation pièces |
  | `DiagnosticDto` | Id, Code OBD, Sévérité, Statut | Dashboard technique |
  | `PartDto` | Id, Référence, Nom, Catégorie, Prix, Stock, IsLowStock | Inventaire |
  | `InvoiceDto` | Id, Numéro, Montant, Date, Statut, URL PDF | Facturation |
  | `WorkshopScheduleDto` | Date, Liste AppointmentDto | Planning journalier |
  | `AppointmentDto` | Id, Titre, Statut, Start, Durée, Ressource | Créneau planning |
  | `DashboardStatsDto` | VehiclesEnCours, Diagnostics, Clients, RévisionsDuJour | KPIs |
  | `LoyaltyTransactionDto` | Points, Raison, Date | Historique fidélité |
  | `PagedResult<T>` | Items, Total, Page, PageSize, HasNext, HasPrevious | Pagination universelle |

- ✅ **Critères de Validation :**
  - [ ] Chaque DTO modifié dans `CQRS.cs` doit être **synchronisé** dans `BlazorServices.cs`
  - [ ] Pas de propriétés de navivation EF dans les DTOs
  - [ ] Les DTOs sont des `record` (immuables par défaut en C#)
- 📊 **Statut :** ✅ FAIT

---

### O3.4 — Extensions de Mapping

- 🔴 **Rôle :** Convertir les entités Domain en DTOs sans dépendance AutoMapper.
- 🔵 **Définition :** `MappingExtensions` est une classe statique contenant des méthodes
  d'extension `ToDto()` et `ToDetailDto()` sur les entités Domain. Ces méthodes
  transforment l'entité en son DTO correspondant sans aucune réflexion ou configuration secondaire.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs` — classe `MappingExtensions`
- ⚡ **Utilisation :** `var dto = entity.ToDto()` dans les handlers
- ✅ **Critères de Validation :**
  - [ ] Chaque mapping est explicite (pas de magic strings)
  - [ ] Les collections sont mappées avec `.Select(x => x.ToDto()).ToList()`
  - [ ] Les propriétés nullable sont gérées avec `?.` (pas de NullReferenceException)
- 📊 **Statut :** ✅ FAIT

---

### O3.5 — Commandes (Commands CQRS)

- 🔴 **Rôle :** Représenter les intentions de mutation de l'état du système.
- 🔵 **Définition :** Une `Command` est un `record` implémentant `IRequest<Result<T>>`.
  Elle exprime **l'intention** d'une action (créer, modifier, supprimer).
  Elle ne contient que des données d'entrée. Le handler décide de la logique.
  **Règle :** Une Command ne doit jamais lire sans écrire.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- 🧱 **Commands existantes :**

  | Command | Rôle |
  |---------|------|
  | `CreateCustomerCommand` | Créer un nouveau dossier client |
  | `UpdateCustomerCommand` | Modifier les informations de contact d'un client |
  | `AddLoyaltyPointsCommand` | Ajouter des points fidélité avec raison |
  | `CreateVehicleCommand` | Enregistrer un nouveau véhicule lié à un client |
  | `UpdateRevisionStatusCommand` | Changer le statut d'une intervention (Scheduled→InProgress→Completed) |
  | `AdjustStockCommand` | Modifier le stock d'une pièce (+/-) |

- ✅ **Critères de Validation :**
  - [ ] Le handler d'une Command appelle toujours `uow.SaveChangesAsync()` à la fin
  - [ ] Les données sont validées avec FluentValidation (via le pipeline)
  - [ ] Une Command ne fait qu'une seule chose (Single Responsibility)
- 📊 **Statut :** ✅ FAIT

---

### O3.6 — Queries (Queries CQRS)

- 🔴 **Rôle :** Représenter les demandes de lecture de données.
- 🔵 **Définition :** Une `Query` est un `record` implémentant `IRequest<Result<T>>`.
  Elle ne modifie jamais l'état du système (lecture seule).
  Les queries peuvent être cachées via `ICacheableRequest`.
  **Règle :** Une Query ne doit jamais écrire.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- 🧱 **Queries existantes :**

  | Query | Rôle |
  |-------|------|
  | `GetCustomersPagedQuery` | Récupérer les clients avec pagination et recherche |
  | `GetCustomerByIdQuery` | Charger tous les détails d'un client + véhicules + révisions |
  | `GetVehiclesByCustomerQuery` | Lister les véhicules d'un client |
  | `GetRevisionDetailQuery` | Charger une intervention avec tâches et pièces |
  | `GetWorkshopScheduleQuery` | Planning de la semaine (DateRange) groupé par resource |
  | `GetPartsPagedQuery` | Catalogue paginé avec filtres catégorie et recherche |
  | `GetPartByReferenceQuery` | Trouver une pièce par son code de référence |
  | `GetPartCategoriesQuery` | Lister toutes les catégories de pièces distinctes |

- ✅ **Critères de Validation :**
  - [ ] Aucune modification de la base dans un Query Handler
  - [ ] Les queries coûteuses implémentent `ICacheableRequest`
  - [ ] Les paramètres de tri et pagination sont dans la Query (pas dans le Handler)
- 📊 **Statut :** ✅ FAIT

---

### O3.7 — Handlers CQRS

- 🔴 **Rôle :** Exécuter la logique applicative pour chaque Command ou Query.
- 🔵 **Définition :** Les Handlers implémentent `IRequestHandler<TRequest, TResponse>`.
  Ils sont la seule couche autorisée à appeler les Repositories (via les interfaces Domain).
  Un Handler = un cas d'utilisation. Il orchestre : validation métier → appel repository
  → transformation en DTO → retour Result.
- 📁 **Fichiers :** `src/MecaPro.Application/CQRS.cs`
- 🧱 **Handlers existants :**

  | Handler | Type | Rôle |
  |---------|------|------|
  | `CreateCustomerHandler` | Command | Vérifier unicité email → créer client → sauvegarder |
  | `UpdateCustomerHandler` | Command | Charger client → appliquer changements → sauvegarder |
  | `GetCustomersPagedHandler` | Query | Déléguer au repo paginé → mapper DTOs |
  | `GetCustomerByIdHandler` | Query | Charger client + toutes ses révisions → DTO complet |
  | `AddLoyaltyPointsHandler` | Command | Charger client → ajouter points → sauvegarder |
  | `CreateVehicleHandler` | Command | Créer véhicule avec LicensePlate VO → sauvegarder |
  | `GetVehiclesByCustomerHandler` | Query | Déléguer au repo → mapper DTOs |
  | `GetRevisionDetailHandler` | Query | Charger révision avec tâches et pièces → DTO détail |
  | `UpdateRevisionStatusHandler` | Command | Charger révision → SetStatus → sauvegarder |
  | `GetWorkshopScheduleHandler` | Query | Filtrer révisions par date → grouper par pont → DTO |
  | `GetPartsPagedHandler` | Query | Filtrer catalogue → paginer → mapper DTOs |
  | `AdjustStockHandler` | Command | Charger pièce → AdjustStock(delta) → sauvegarder |
  | `GetPartCategoriesHandler` | Query | Extraire catégories distinctes du catalogue |

- ✅ **Critères de Validation :**
  - [ ] Un Handler par Command/Query (pas de handlers "fourre-tout")
  - [ ] En cas de ressource introuvable → `Result.Failure("...introuvable")`
  - [ ] Pas d'accès direct à DbContext dans les handlers (passer par les repos)
- 📊 **Statut :** ✅ FAIT

---

## 🌐 O4 — COUCHE API (`MecaPro.API`)

> **Rôle Général :** Exposer les fonctionnalités de l'Application via HTTP REST.
> Utilise Carter (Minimal APIs) pour définir des modules de routes groupées.
> Gère l'authentification JWT, le versioning `/api/v1/` et le format des réponses.

---

### O4.1 — Modules Carter (ICarterModule)

- 🔴 **Rôle :** Regrouper les endpoints par domaine métier dans des classes dédiées.
- 🔵 **Définition :** Chaque `ICarterModule` définit un préfixe de route (`/api/v1/xxx`)
  et enregistre ses endpoints via `AddRoutes(IEndpointRouteBuilder)`.
  Carter remplace les Controllers dans l'approche Minimal API modern de .NET.
- 📁 **Fichiers :** `src/MecaPro.API/Endpoints/Endpoints.cs`
- 🧱 **Modules existants :**

  | Module | Préfixe | Endpoints exposés |
  |--------|---------|-------------------|
  | `AuthModule` | `/api/v1/auth` | POST /login, POST /register |
  | `CustomerModule` | `/api/v1/customers` | GET (paginé), GET /{id}, POST, PUT /{id}, POST /{id}/loyalty |
  | `VehicleModule` | `/api/v1/vehicles` | GET /{id}, GET /by-customer/{cid}, POST |
  | `RevisionModule` | `/api/v1/revisions` | GET (schedule), GET /{id}, POST /{id}/status |
  | `StockModule` | `/api/v1/parts` | GET (paginé+filtres), GET /categories, POST /{id}/stock |
  | `DashboardModule` | `/api/v1/dashboard` | GET /stats |

- ✅ **Critères de Validation :**
  - [ ] Chaque module est sur un préfixe versioned `/api/v1/`
  - [ ] Les routes sont RESTful (GET=lecture, POST=création, PUT=modification)
  - [ ] Les modules sont enregistrés dans `Program.cs` via `app.MapCarter()`
- 📊 **Statut :** ✅ FAIT (Auth, Customer, Vehicle, Revision, Stock, Dashboard)

---

### O4.2 — Authentification et Autorisation JWT

- 🔴 **Rôle :** Protéger les endpoints avec des tokens JWT et des politiques de rôles.
- 🔵 **Définition :** Les endpoints sécurisés utilisent `.RequireAuthorization()` ou des
  politiques de rôles `.RequireAuthorization(p => p.RequireRole("Mechanic"))`.
  L'API valide le token JWT dans le middleware `UseAuthentication()` avant d'atteindre les routes.
- 📁 **Fichiers :** `src/MecaPro.API/Endpoints/Endpoints.cs`, `src/MecaPro.API/Program.cs`
- 🧱 **Politiques :**
  - `RequireAuthorization()` — Tout endpoint nécessite un token valide
  - `RequireAuthorization(p => p.RequireRole("Mechanic"))` — Réservé aux techniciens
  - `AllowAnonymous()` — Login et Register seulement
- ✅ **Critères de Validation :**
  - [ ] Tous les endpoints métier sont protégés (pas de bypass)
  - [ ] Le Blazor frontend passe le token dans le header `Authorization: Bearer {token}`
  - [ ] Un token expiré retourne HTTP 401 (pas 500)
- 📊 **Statut :** ✅ FAIT

---

### O4.3 — Gestion des Erreurs HTTP

- 🔴 **Rôle :** Transformer les `Result<T>` applicatifs en codes HTTP appropriés.
- 🔵 **Définition :** Chaque endpoint suit le pattern : si `result.IsSuccess → Results.Ok(result.Value)`,
  sinon `Results.BadRequest(result.Error)` ou `Results.NotFound()`.
  Un middleware global `UseExceptionHandler` capture les exceptions non attendues et retourne HTTP 500.
- 📁 **Fichiers :** `src/MecaPro.API/Endpoints/Endpoints.cs`, `src/MecaPro.API/Program.cs`
- 🧱 **Convention :**
  ```
  200 OK         — Succès, retourne le DTO
  201 Created    — Ressource créée
  400 BadRequest — Erreur métier (Result.Failure)
  401 Unauthorized — Token manquant ou invalide
  403 Forbidden  — Rôle insuffisant
  404 NotFound   — Ressource inexistante
  500 Internal   — Erreur système non attendue
  ```
- 📊 **Statut :** ✅ FAIT

---

### O4.4 — CORS (Cross-Origin Resource Sharing)

- 🔴 **Rôle :** Autoriser le frontend Blazor (port 5200) à appeler l'API (port 5001).
- 🔵 **Définition :** Une politique CORS est configurée dans `Program.cs` pour accepter
  les requêtes provenant des origines connues. En développement : `AllowAnyOrigin`.
  En production : origines explicites.
- 📁 **Fichiers :** `src/MecaPro.API/Program.cs`
- ⚡ **Configuration dev :** `AllowAnyOrigin + AllowAnyMethod + AllowAnyHeader`
- 📊 **Statut :** ✅ FAIT

---

## 🖥️ O5 — COUCHE BLAZOR (`MecaPro.Blazor`)

> **Rôle Général :** Interface utilisateur WebAssembly (WASM) pour les garagistes.
> Consomme l'API via `ApiClient`. Affiche les données via des composants Blazor avec
> le design néo-futuriste industriel standardisé.

---

### O5.1 — ApiClient (Couche d'Accès HTTP)

- 🔴 **Rôle :** Centraliser tous les appels HTTP vers l'API.
- 🔵 **Définition :** `ApiClient` est un service singleton qui wraps `HttpClient`.
  Il injecte automatiquement le token JWT depuis le `LocalStorage` et désérialise via `System.Text.Json`.
  Méthodes standards : `GetAsync<T>()`, `PostAsync<T>()`, `PutAsync<T>()`, `DeleteAsync()`.
- 📁 **Fichiers :** `src/MecaPro.Blazor/Services/BlazorServices.cs`
- ⚡ **Utilisation :**
  ```csharp
  var result = await Api.GetAsync<CustomerDetailDto>($"/api/v1/customers/{Id}");
  var dto = await Api.PostAsync<CustomerDto>("/api/v1/customers", command);
  ```
- ✅ **Critères de Validation :**
  - [ ] Jamais d'`HttpClient` raw dans les pages — toujours passer par `ApiClient`
  - [ ] Le token JWT est injecté automatiquement dans `Authorization: Bearer`
  - [ ] Retourne `null` en cas d'erreur (à gérer dans la page avec état d'erreur)
- 📊 **Statut :** ✅ FAIT

---

### O5.2 — Synchronisation DTOs Blazor

- 🔴 **Rôle :** Dupliquer les DTOs de l'Application dans le frontend Blazor.
- 🔵 **Définition :** Blazor WASM ne peut pas référencer directement `MecaPro.Application`
  (raison de taille et de compilation WASM). Les DTOs sont recréés en tant que `record` C#
  identiques dans `BlazorServices.cs`. Toute modification de DTO côté Application
  **doit être synchronisée** dans ce fichier.
- 📁 **Fichiers :** `src/MecaPro.Blazor/Services/BlazorServices.cs`
- ⚡ **Règle de Synchronisation :** Si `CQRS.cs` change un DTO → `BlazorServices.cs` doit être mis à jour
- ✅ **Critères de Validation :**
  - [ ] Les propriétés sont identiques (nom, type, ordre) entre Application et Blazor
  - [ ] Aucune propriété de navigation ajoutée côté Blazor
- 📊 **Statut :** ✅ FAIT

---

### O5.3 — Pages Fonctionnelles (Modules Actifs)

- 🔴 **Rôle :** Afficher les données et permettre les interactions utilisateur.
- 🔵 **Définition :** Chaque page Blazor correspond à un module métier. Elle :
  1. Déclare `@page "/route"` et `@attribute [Authorize]`
  2. Charge les données dans `OnInitializedAsync()` via `ApiClient`
  3. Gère les états : `loading`, `error`, données null
  4. Affiche avec le design néo-futuriste industriel standardisé (voir `ui_standards.md`)
- 📁 **Fichiers :** `src/MecaPro.Blazor/Pages/`
- 🧱 **Pages Fonctionnelles (données réelles, non mockées) :**

  | Page | Route | Fonctionnalités |
  |------|-------|----------------|
  | `ClientsList.razor` | `/clients` | Liste paginée CRM + recherche temps réel |
  | `FicheClient.razor` | `/clients/fiche-client/{id}` | Dossier complet : véhicules + timeline interventions |
  | `OnboardingClient.razor` | `/clients/onboarding` | Wizard 3 étapes : Identité → Véhicule → Init |
  | `AddVehicle.razor` | `/clients/add-vehicle/{customerId}` | Formulaire ajout véhicule + redirect dossier |
  | `DetailIntervention.razor` | `/clients/detail-intervention/{revisionId}` | Fiche technicien : statut stepper + tâches + pièces |
  | `PlanningAtelier.razor` | `/planning/planning-atelier` | Grille hebdomadaire par pont + navigation semaines |
  | `InventaireStock.razor` | `/stock/inventaire-stock` | Catalogue + filtres catégorie + ajustement stock +/- |
  | `Dashboard.razor` | `/` | KPIs temps réel : véhicules, interventions, clients |

- 📊 **Statut :** ✅ FAIT (8 pages fonctionnelles avec données API réelles)

---

### O5.4 — Layout et Navigation

- 🔴 **Rôle :** Structurer l'interface et gérer la navigation inter-pages.
- 🔵 **Définition :** `MainLayout.razor` définit la structure générale avec sidebar.
  `EmptyLayout.razor` est utilisé pour les pages full-screen (planning, inventaire).
  `App.razor` gère l'authentification globale avec `AuthorizeRouteView`.
- 📁 **Fichiers :** `src/MecaPro.Blazor/Layout/`, `src/MecaPro.Blazor/App.razor`
- 🧱 **Layouts :**
  - `MainLayout` — Sidebar navigation + header + contenu
  - `EmptyLayout` — Page full-screen sans sidebar (planning, atelier)
- 📊 **Statut :** ✅ FAIT

---

### O5.5 — Standards UI Néo-Futuristes

- 🔴 **Rôle :** Assurer la cohérence visuelle sur l'ensemble de l'application.
- 🔵 **Définition :** Toutes les pages respectent le langage de design industriel défini :
  - Fond : `bg-[#111318]` (noir profond)
  - Accent : `text-primary` (`#f5a623` orange industriel)
  - Typographie : `font-black italic uppercase tracking-tighter` pour les titres
  - Cartes : `rounded-[40px] border border-white/5 bg-surface-container`
  - Boutons CTA : `bg-primary text-on-primary shadow-primary/20`
  - Police technique : JetBrains Mono pour les données
- 📁 **Fichiers :** `construction/ui_standards.md`, `src/MecaPro.Blazor/wwwroot/css/`
- ✅ **Règle :** Aucune couleur arbitraire — utiliser uniquement les variables CSS définies
- 📊 **Statut :** ✅ FAIT

---

## ⚙️ O6 — CONFIGURATION & INFRASTRUCTURE

> **Rôle Général :** Configurer l'environnement d'exécution : services, ports,
> conteneurs Docker, variables d'environnement.

---

### O6.1 — Program.cs (API — Point d'Entrée)

- 🔴 **Rôle :** Démarrer l'application API et configurer tous les services.
- 🔵 **Définition :** `Program.cs` dans `MecaPro.API` enregistre dans l'ordre :
  1. DbContext (EF Core + SQL Server)
  2. Identity (ASP.NET Core Identity)
  3. JWT Authentication & Authorization
  4. MediatR (tous les handlers)
  5. Repositories (DI)
  6. Carter (routing automatique)
  7. CORS
  8. Redis (Cache distribué)
  9. Serilog (Logging structuré)
  10. Swagger (Documentation auto)
  11. Seeder (Données initiales)
- 📁 **Fichiers :** `src/MecaPro.API/Program.cs`
- 📊 **Statut :** ✅ FAIT

---

### O6.2 — Docker Compose

- 🔴 **Rôle :** Orchestrer les conteneurs de services (SQL Server, Redis, Seq).
- 🔵 **Définition :** `docker-compose.yml` démarre :
  - `sql-server` — SQL Server 2022 sur port 1433 avec `MecaProDb`
  - `redis` — Redis 7 sur port 6379 (cache distribué)
  - `seq` — Interface de logs structurés Serilog sur port 5341
- 📁 **Fichiers :** `docker-compose.yml` (racine du projet)
- ⚡ **Commandes :**
  ```bash
  docker-compose up -d          # Démarrer les services
  docker-compose down           # Arrêter
  docker-compose logs sql-server # Voir les logs SQL
  ```
- 📊 **Statut :** ✅ FAIT

---

### O6.3 — appsettings.json

- 🔴 **Rôle :** Centraliser la configuration sensible de l'application.
- 🔵 **Définition :** Contient les chaînes de connexion, les secrets JWT, les URLs de services.
  Ne jamais versionner les secrets en production (utiliser Azure Key Vault ou variables d'env CI/CD).
- 📁 **Fichiers :** `src/MecaPro.API/appsettings.json`, `src/MecaPro.Blazor/wwwroot/appsettings.json`
- 🧱 **Clés importantes :**
  - `ConnectionStrings:DefaultConnection` — SQL Server
  - `JwtSettings:Secret` — Clé de signature JWT (32+ caractères)
  - `JwtSettings:Issuer`, `JwtSettings:Audience`
  - `ApiBaseUrl` — URL de l'API pour le Blazor frontend
- 📊 **Statut :** ✅ FAIT

---

## 🚀 O7 — CYCLES DE DÉPLOIEMENT

> **Rôle Général :** Assurer la qualité, la fiabilité et la livraison continue du code.

---

### O7.1 — Tests Unitaires

- 🔴 **Rôle :** Valider le comportement des handlers et du Domain de manière isolée.
- 🔵 **Définition :** Les tests unitaires couvrent :
  - Les Value Objects (validation des règles, cas limites)
  - Les Domain Methods (ex. `Customer.AddLoyaltyPoints()`)
  - Les Handlers CQRS (avec repositories mockés via Moq)
- 📁 **Fichiers :** `tests/MecaPro.Tests/`
- ⚡ **Framework :** xUnit + Moq + FluentAssertions
- ⚡ **Commande :** `dotnet test`
- 📊 **Statut :** ⚠️ EN COURS (tests Domain présents, handlers à compléter)

---

### O7.2 — Tests d'Intégration

- 🔴 **Rôle :** Tester les handlers avec une vraie base de données (InMemory ou SQL Test).
- 🔵 **Définition :** Tests end-to-end des handlers avec `WebApplicationFactory<Program>`
  pour tester les endpoints HTTP dans un environnement proche de la production.
  `EF Core InMemory Provider` pour isoler les tests de données.
- 📁 **Fichiers :** `tests/MecaPro.IntegrationTests/`
- 📊 **Statut :** 🔴 À FAIRE

---

### O7.3 — CI/CD GitHub Actions

- 🔴 **Rôle :** Automatiser le build, les tests et le déploiement à chaque push.
- 🔵 **Définition :** Pipeline GitHub Actions qui :
  1. Checkout du code
  2. Setup .NET 9
  3. `dotnet restore` + `dotnet build`
  4. `dotnet test` (tous les projets de test)
  5. Build Docker + push vers registry
  6. Déploiement automatique en staging
- 📁 **Fichiers :** `.github/workflows/ci.yml`
- 📊 **Statut :** 🔴 À FAIRE

---

### O7.4 — Migrations en Production

- 🔴 **Rôle :** Appliquer les changements de schéma en production sans interruption de service.
- 🔵 **Définition :** En production, `db.Database.MigrateAsync()` est appelé au démarrage
  via le `DatabaseSeeder`. Pour les changements majeurs, utiliser un script SQL pré-validé.
- 📁 **Fichiers :** `src/MecaPro.Infrastructure/Persistence/Database.cs` — `SeedAsync()`
- ⚡ **Règle :** Jamais de `EnsureDeleted()` en production (données perdues)
- 📊 **Statut :** ✅ FAIT (auto-migration au démarrage)

---

## 📊 TABLEAU DE BORD D'ÉTAT (GLOBAL)

| Couche | Module | Statut | Priorité |
|--------|--------|--------|----------|
| O1 — Domaine | Aggregates (Customer, Vehicle, Revision...) | ✅ FAIT | — |
| O1 — Domaine | Value Objects | ✅ FAIT | — |
| O1 — Domaine | Interfaces Repositories | ✅ FAIT | — |
| O2 — Infrastructure | AppDbContext + DbSets | ✅ FAIT | — |
| O2 — Infrastructure | EF Configurations | ✅ FAIT (partiel) | 🟡 MOYENNE |
| O2 — Infrastructure | Repositories | ✅ FAIT | — |
| O2 — Infrastructure | Unit of Work | ✅ FAIT | — |
| O2 — Infrastructure | Migrations | ✅ FAIT | — |
| O2 — Infrastructure | Database Seeding | ✅ FAIT | — |
| O2 — Infrastructure | Identity + JWT | ✅ FAIT | — |
| O3 — Application | Result Pattern | ✅ FAIT | — |
| O3 — Application | Pipeline Behaviors | ✅ FAIT | — |
| O3 — Application | DTOs (15 types) | ✅ FAIT | — |
| O3 — Application | Mapping Extensions | ✅ FAIT | — |
| O3 — Application | Commands (6) | ✅ FAIT | — |
| O3 — Application | Queries (8) | ✅ FAIT | — |
| O3 — Application | Handlers (13) | ✅ FAIT | — |
| O4 — API | Carter Modules (6) | ✅ FAIT | — |
| O4 — API | Auth JWT | ✅ FAIT | — |
| O4 — API | CORS | ✅ FAIT | — |
| O5 — Blazor | ApiClient | ✅ FAIT | — |
| O5 — Blazor | DTO Sync | ✅ FAIT | — |
| O5 — Blazor | Pages fonctionnelles (8) | ✅ FAIT | — |
| O5 — Blazor | Layout + Navigation | ✅ FAIT | — |
| O5 — Blazor | UI Standards | ✅ FAIT | — |
| O6 — Config | Program.cs API | ✅ FAIT | — |
| O6 — Config | Docker Compose | ✅ FAIT | — |
| O6 — Config | appsettings.json | ✅ FAIT | — |
| O7 — Deploy | Tests Unitaires Domain | ⚠️ EN COURS | 🔴 HAUTE |
| O7 — Deploy | Tests d'Intégration | 🔴 À FAIRE | 🔴 HAUTE |
| O7 — Deploy | CI/CD GitHub Actions | 🔴 À FAIRE | 🟡 MOYENNE |
| O7 — Deploy | Migrations Production | ✅ FAIT | — |

---

## 🔄 FLUX D'UNE OPÉRATION TYPE (Exemple : Charger une Fiche Client)

```
[1] Blazor FicheClient.razor
    └── OnInitializedAsync()
        └── await Api.GetAsync<CustomerDetailDto>("/api/v1/customers/{Id}")
                                    │
[2] HTTP GET → API → Carter CustomerModule
    └── mediator.Send(new GetCustomerByIdQuery(id))
                                    │
[3] MediatR Pipeline
    ├── LoggingBehavior → [LOG] Handling GetCustomerByIdQuery
    ├── ValidationBehavior → (pas de validateur défini pour les Queries)
    └── CachingBehavior → (pas de cache défini)
                                    │
[4] GetCustomerByIdHandler
    ├── customers.GetWithVehiclesAsync(id) → CustomerRepository → EF Include
    ├── pour chaque vehicle: revisions.GetByVehicleIdAsync(v.Id)
    └── customer.ToDetailDto(revs) → CustomerDetailDto
                                    │
[5] Return Result<CustomerDetailDto>.Success(dto)
    └── API → Results.Ok(dto) [HTTP 200]
                                    │
[6] Blazor → Désérialise CustomerDetailDto
    └── StateHasChanged() → Rendu de la Fiche Client
```

---

## 🗂️ LIENS VERS DOCUMENTS CONNEXES

| Document | Contenu |
|----------|---------|
| `construction/plan_fonctionnalisation.md` | Plan complet des 89 modules à fonctionnaliser |
| `construction/architecture.md` | Architecture Clean Architecture, Stack technique |
| `construction/ui_standards.md` | Standards design néo-futuriste industriel |
| `construction/plan.md` | Plan d'exécution et suivi des tâches |
| `construction/tests.md` | Plan de tests unitaires et intégration |
| `construction/audit.md` | Journal des audits de qualité |
| `construction/walkthrough.md` | Guide de démarrage rapide pour nouveaux devs |

---

*Document généré le 20 Mars 2026 — MecaPro Atelier Intelligence Platform v2.0*
*Architecture : Clean Architecture · DDD · CQRS · MediatR · EF Core · Carter · Blazor WASM*
