# 🛠️ PLAN GLOBAL DE FONCTIONNALISATION (ALL MODULES)
> **MecaPro** · Audit complet : 22/03/2026
> **Objectif :** Transformer le Frontend Blazor statique/Mock en une Dashboard réactive entièrement câblée à l'API Backend.

---

## 📅 1. PLANNING ATELIER & OPÉRATIONS

### État de l'API Backend : **🟡 Partiel**
- endpoints `/api/v1/revisions` et `/api/v1/revisions/schedule` **sont créés** dans `OperationsEndpoints.cs`.
- Les handlers MediatR (`GetRevisionsQuery`, `GetWorkshopScheduleQuery`) **existent déjà**.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Planning Atelier** | `PlanningAtelier.razor` | Connecter aux appels `GET /api/v1/revisions/schedule` pour afficher le calendrier interactif. Ajouter le Drag & Drop pour modifier les dates. |
| **Interventions** | `RevisionsPlannings.razor` | Câbler la grille sur `GET /api/v1/revisions`. Permettre le changement de statut (En attente → En cours) via `POST /api/v1/revisions/{id}/status`. |

---

## 🤖 2. DIAGNOSTIC IA & EXPERTISE

### État de l'API Backend : **🟡 Partiel**
- Endpoint `/api/v1/diagnostics` **existe** (ajout manuel de diagnostic).
- L'architecture **AI Sub-Agent** vient d'être créée dans `/sub-agent/agent/` mais n'est pas encore connectée à un contrôleur C#.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Diagnostic IA** | `DiagnosticAvance.razor` | Remplacer la logique d'attente "Timeout" par un appel réel au Server-Sent Events (SSE) du futur contrôleur IA (`/api/v1/ai-agent/stream`). |
| **Scan Dégâts (Photos)** | `PhotoExpertiseIA.razor` | Créer un endpoint `POST /api/v1/diagnostics/vision` dans le backend. Envoyer les images uploadées à l'API pour analyse via l'agent "Diagnostic". |
| **Décodeur VIN** | `VinDecoderPro.razor` | Créer un endpoint `GET /api/v1/vehicles/decode-vin/{vin}` qui utilisera une API externe via l'orchestrateur IA pour rapatrier le modèle et l'année. |

---

## 💰 3. FINANCE & STATS (FACTURATION)

### État de l'API Backend : **🟠 Basique**
- `BillingEndpoints.cs` contient seulement `GET /api/v1/billing/invoices` pour récupérer les factures.
- Manque la **Génération PDF** et l'**Intégration Stripe** détaillée.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Archives Factures** | `ArchivesFactures.razor` | Remplacer les fausses factures par l'appel `GET /api/v1/billing/invoices`. Permettre le téléchargement des PDF. |
| **Paiement Sécurisé** | `PaiementSecurise.razor` | Initialiser le SDK Stripe au démarrage. Créer un `PaymentIntent` via `POST /api/v1/billing/checkout` au moment de payer. |
| **Analytique & Revenus** | `AnalytiqueStats.razor` | Créer un nouveau endpoint `GET /api/v1/dashboard/finance-metrics` pour afficher les graphiques en aires (Chart.js ou MudBlazor) avec les vrais CA. |

---

## 📦 4. STOCK & LOGISTIQUE

### État de l'API Backend : **🟢 Quasi Prêt**
- `StockEndpoints.cs` **est complet** : Pagination sur `/api/v1/parts`, filtrage par `/api/v1/parts/categories`, et mise à jour des quantités.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Catalogue & Promo** | `CataloguePromo.razor` | Remplacer la grille par un appel à `GET /api/v1/parts` de l'inventaire actuel. Connecter la barre de recherche au paramètre `search=` de l'API. |
| **Suivi Livraisons** | `SuiviLivraisonsV2.razor` | Créer les endpoints manquants pour la table `Orders` (Commandes fournisseurs). Gérer le statut des commandes (Expédié / Livré) pour mettre à jour la quantité dans `Parts`. |
| **Commandes** | `CommandesV1.razor` | Implémenter le formulaire de passation de commande fournisseur (POST `/api/v1/inventory/orders`). |

---

## 👥 5. RH & SÉCURITÉ

### État de l'API Backend : **🔴 Très basique**
- Pas d'API claire pour la gestion des employés (Mécaniciens, Réceptionnistes) car l'Auth a été isolée dans le service Auth.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Planning Equipes** | `PlanningEquipes.razor` | Nécessite un proxy vers le service d'Auth pour lister tous les utilisateurs de type `Mechanic` de ce garage. Créer un endpoint d'assignation de tâches aux mécaniciens. |
| **Congés & Formations** | `PlanningConges.razor` | Créer une table `EmployeeAbsence` (Vacances/Maladies) et les CRUD endpoints. Ce qui permettra d'aviser si un mécanicien est disponible dans la `WorkshopScheduleDto`. |

---

## 📢 6. MARKETING & SUPPORT

### État de l'API Backend : **🔴 Nouveau module**
- N'existe que des tables basiques `ChatMessage` (non exposées) et `CustomerSatisfaction` manquante.

### Pages Frontend à câbler :
| Page | Fichier | Actions Requises |
|---|---|---|
| **Support IA (Chat)**| `SupportInterne.razor` | Connecter au Hub SignalR pour le Tchat interne, ou intégrer l'Agent Orchestrateur pour répondre aux questions des employés sur la documentation garage. |
| **Analyse Satisfaction**| `AnalyseSatisfaction.razor`| Câbler les statistiques NPS et Avis Google. Créer un service de Webhook pour capter les notes automatiquement. |

---

## 🎯 ORDRE D'ATTAQUE STRATÉGIQUE (ROADMAP)

Pour garantir des résultats visibles immédiatement sans tout casser, voici l'ordre dans lequel je peux effectuer la fonctionnalisation :

🚀 **Sprint 1 — PLANNING & INTERVENTIONS (Atelier)**
1. Lier `PlanningAtelier.razor` et `RevisionsPlannings.razor` aux APIs existantes.
2. S'assurer que lorsqu'on clôture une Révision, cela déduit automatiquement les pièces de l'Inventaire (Stock).

🚀 **Sprint 2 — STOCK & CATALOGUE LOGISTIQUE**
1. Lier `CataloguePromo.razor` pour afficher les vraies pièces (Filtre à Huile, Pneus).
2. Ajouter le bouton "Ajuster le stock" fonctionnel (connexion à `/api/v1/parts/{id}/stock`).

🚀 **### Sprint 3 : IA & DIAGNOSTICS (EN COURS 🟢)
- [x] Créer le microservice Node.js `sub-agent` (Port 3001).
- [x] Implémenter le proxy C# `AiEngineEndpoints`.
- [x] Lier `DiagnosticAvance.razor` via `AiStreamService` (SSE).
- [x] Mode Mock opérationnel si `ANTHROPIC_API_KEY` manquante.
- [ ] Connecter les outils réels (DB, Stock) aux agents IA.

### Sprint 4 : Finance & Stripe Integration ✅
- [x] **Facturation PDF** : Génération de factures premium avec Scriban (Template HTML).
- [x] **Stripe Checkout** : Intégration de `Stripe.net` pour les paiements sécurisés.
- [x] **Webhooks** : Gestion du callback pour validation automatique des paiements.
- [x] **Interface Archives** : Visualisation et paiement direct depuis le portail client.

### Sprint 5 : Advanced AI Tooling (Liaison des outils) ✅
- [x] **Tool Executor** : Connexion réelle du sous-agent Node.js aux APIs .NET (Gateway/Monolith).
- [x] **Mapping Dynamique** : Traduction des appels d'outils IA (get_customer_360, query_database) vers les ressources C#.
- [x] **Sécurité Interne** : Authentification par token pour les communications inter-services.

### Sprint 6 : Optimisation & Scalability
- [ ] **Redis Performance** : Caching des résultats de diagnostic fréquents.
- [ ] **Vision AI** : Analyse d'images de pièces défectueuses via GPT-4o-vision / Claude 3.5.
- [ ] **Déploiement K8s** : Préparation des manifestes pour mise en production scale-out.

Ce plan est désormais la référence. Si cela vous convient, **nous pouvons lancer le Sprint 1 (Planning Atelier) dès maintenant**.
