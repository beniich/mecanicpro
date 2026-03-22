# MecaPro — Plan de Migration vers les Microservices

## 🏗️ Architecture actuelle (Monolithe Modulaire)

```
MecaPro.API  ──►  MecaPro.Application  ──►  MecaPro.Domain
           u       MecaPro.Infrastructure
```

Le monolithe modulaire actuel est **intentionnel** : c'est la base idéale
avant de décomposer en microservices, car les **frontièr
es de domaine** sont
déjà claires grâce à la modularisation.

---

## 🚀 Feuille de Route — Microservices

### **Phase 1 : Préparer l'infrastructure partagée**

Avant de couper quoi que ce soit, mettre en place :

```yaml
Services partagés:
  - API Gateway:        Ocelot ou YARP (proxy + routing + rate limiting centralisé)
  - Service Discovery:  Consul ou Kubernetes DNS
  - Message Bus:        RabbitMQ + MassTransit (déjà en dépendance ✅)
  - Distributed Cache:  Redis (déjà configuré ✅)
  - Centralized Logging: Seq + Serilog (déjà configuré ✅)
  - Auth Server:         IdentityServer6 ou Keycloak (séparé du monolithe)
```

### **Phase 2 : Extraire les services par ordre de priorité**

```
Priorité 1 — Auth Service (critique, toujours séparé)
  Port: 5100
  Responsabilité: JWT, Refresh Token, 2FA, Roles
  DB:   auth_db (SQL Server, tables AspNetUsers/RefreshTokens)
  →  Tous les autres services valident le JWT contre ce service

Priorité 2 — Notification Service (sans état, facile à isoler)
  Port: 5200
  Responsabilité: Email (SendGrid), SMS (Twilio), Push, In-App
  DB:   Aucune (stateless, consomme des events MassTransit)
  Consomme: CustomerCreated, RevisionCompleted, InvoiceGenerated

Priorité 3 — Billing / Invoice Service
  Port: 5300
  Responsabilité: Factures, PDF, Paiements Stripe, Abonnements
  DB:   billing_db (Invoice, Order, Subscription, SubscriptionPlan)
  Publie: InvoiceGenerated, PaymentSucceeded, PaymentFailed

Priorité 4 — Operations Service
  Port: 5400
  Responsabilité: Vehicles, Diagnostics, Revisions
  DB:   operations_db
  Publie: RevisionCompleted, DiagnosticCreated

Priorité 5 — CRM Service
  Port: 5500
  Responsabilité: Customers, Loyalty, Customer360
  DB:   crm_db (Customer, LoyaltyTransaction)
  Publie: CustomerCreated, LoyaltyPointsAwarded

Priorité 6 — Inventory Service
  Port: 5600
  Responsabilité: Parts, Stock, Orders
  DB:   inventory_db (Part, Order, OrderItem)
  Publie: StockLow, OrderCreated
```

### **Phase 3 : Pattern de communication**

```
Synchrone (HTTP/gRPC):
  ├── API Gateway → each microservice (user-facing requests)
  ├── Auth validation (JWT introspection)
  └── Health checks

Asynchrone (RabbitMQ events via MassTransit):
  ├── CustomerCreated  → Notification Service (welcome email)
  ├── RevisionCompleted → Notification Service + Billing Service
  ├── InvoiceGenerated  → Notification Service (send PDF)
  ├── PaymentSucceeded  → CRM Service (update loyalty)
  └── StockLow          → Notification Service (alert garage owner)
```

### **Phase 4 : Isolation des données (Database per Service)**

```sql
-- AVANT: un seul AppDbContext avec toutes les tables
-- APRÈS: chaque service a sa propre base de données

auth_db:        AspNetUsers, AspNetRoles, RefreshTokens
crm_db:         Customers, LoyaltyTransactions, Subscriptions
operations_db:  Vehicles, Diagnostics, Revisions, RevisionTasks
inventory_db:   Parts, Orders, OrderItems
billing_db:     Invoices, Payments
notification_db: Notifications, AuditLogs (ou Seq)
```

### **Phase 5 : API Gateway avec YARP**

```csharp
// MecaPro.Gateway / Program.cs
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// appsettings.json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route":   { "ClusterId": "auth-cluster",   "Match": { "Path": "/api/v1/auth/{**catch-all}" } },
      "crm-route":    { "ClusterId": "crm-cluster",    "Match": { "Path": "/api/v1/crm/{**catch-all}" } },
      "ops-route":    { "ClusterId": "ops-cluster",    "Match": { "Path": "/api/v1/operations/{**catch-all}" } },
      "billing-route":{ "ClusterId": "billing-cluster","Match": { "Path": "/api/v1/billing/{**catch-all}" } }
    },
    "Clusters": {
      "auth-cluster":   { "Destinations": { "d1": { "Address": "http://auth-service:5100" } } },
      "crm-cluster":    { "Destinations": { "d1": { "Address": "http://crm-service:5500" } } },
      "ops-cluster":    { "Destinations": { "d1": { "Address": "http://ops-service:5400" } } },
      "billing-cluster":{ "Destinations": { "d1": { "Address": "http://billing-service:5300" } } }
    }
  }
}
```

### **Phase 6 : Docker Compose pour le développement local**

Fichier `docker-compose.microservices.yml` à créer :

```yaml
version: '3.9'
services:
  gateway:
    build: ./MecaPro.Gateway
    ports: ["5000:80"]
    depends_on: [auth-service, crm-service, ops-service]

  auth-service:
    build: ./MecaPro.Auth
    environment:
      - ConnectionStrings__DefaultConnection=Server=sql;Database=auth_db;...
    ports: ["5100:80"]

  crm-service:
    build: ./MecaPro.CRM
    ports: ["5500:80"]

  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]

  redis:
    image: redis:alpine
    ports: ["6379:6379"]

  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStr0ngPassword!"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
```

---

## 🔐 Sécurité dans l'architecture Microservices

### JWT Validation inter-services
Chaque microservice valide le JWT lui-même (clé publique RSA partagée).
Aucun appel réseau pour valider un token → performance maximale.

```csharp
// Dans chaque microservice
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.Authority = "http://auth-service:5100";  // Discovery endpoint
        // ou configurer manuellement avec la clé publique partagée
    });
```

### Secrets management
- **Développement**: `dotnet user-secrets`
- **Production**: Azure Key Vault ou HashiCorp Vault
- **CI/CD**: GitHub Actions Secrets → injected as env vars

### mTLS entre services (optionnel, étape avancée)
Mutual TLS pour garantir que seul le gateway peut appeler les services backend.

---

## 📊 État actuel vs Objectif

| Aspect               | Maintenant (✅ en place)              | Objectif Microservices        |
|----------------------|---------------------------------------|-------------------------------|
| Auth                 | JWT RS256 + Refresh Rotation          | Auth Service dédié            |
| Multi-tenancy        | GarageId dans JWT + TenantGuard       | Tenant claim + API Gateway    |
| Rate Limiting        | Per-IP (auth) / Per-user (API)        | Centralisé au Gateway         |
| Audit Log            | Middleware SQL                        | Event stream (Seq/ELK)        |
| Communication        | In-process (MediatR)                  | RabbitMQ (MassTransit)        |
| Database             | 1 SQL Server shared                   | 1 DB par service              |
| Déploiement          | 1 process                             | Kubernetes (Helm charts)      |
