# 🚀 PLAN DE FONCTIONNALISATION — MecaPro (89 Modules)
> Dernière mise à jour : 20 Mars 2026
> Objectif : Éliminer tous les mocks et connecter chaque module à une vraie API, vraies données DB et vraie logique métier.

---

## 📐 PRINCIPES DIRECTEURS

| Principe | Règle |
|---|---|
| **Architecture** | Tout backend passe par CQRS + MediatR (Queries/Commands) |
| **Données** | EF Core → SQL Server via Repository Pattern |
| **Temps réel** | SignalR pour IoT, alertes, monitoring |
| **Auth** | JWT Bearer, Rôles Claims sur chaque endpoint |
| **API** | Carter Modules, versioning `/api/v1/` |
| **Blazor** | `ApiClient.GetAsync<T>()` pour tous les appels HTTP |
| **Export** | PdfSharp ou QuestPDF pour PDF, EPPlus pour Excel |

---

## ⚡ PHASE 1 — SOCLE CRM CLIENTS/VEHICULES (Sprint 1 — 5 jours)

> **Priorité critique.** Les clients et véhicules sont la colonne vertébrale de tout le CRM.

### 1.1 Fiche Client (`/clients/fiche-client`)
**Backend à créer :**
- `GetCustomerByIdQuery(Guid id)` → `CustomerFullDto` (nom, contacts, historique, tags)
- `UpdateCustomerCommand(...)` → validation FluentValidation
- `GetCustomerTimelineQuery(Guid id)` → liste chronologique de toutes les interactions

**Frontend :**
- Charger `CustomerFullDto` au `OnInitializedAsync`
- Sections éditables inline (MudTextField en mode `ReadOnly` basculant)
- Timeline verticale (interventions, factures, devis) triée par date descendante

**DB :** Table `Customers` existante — ajouter colonne `Tags`, `PreferredChannelContact`, `LoyaltyTier`

---

### 1.2 Fiche Intervention (`/clients/detail-intervention`)
**Backend :**
- `GetRevisionDetailQuery(Guid revisionId)` → `RevisionDetailDto` avec lignes de travaux, techniciens, pièces posées, codes défauts
- `UpdateRevisionStatusCommand(Guid, RevisionStatus)`

**Frontend :**
- Stepper de statut : Réservé → En cours → Terminé → Livré
- Section pièces posées avec coût unitaire et temps main d'œuvre

---

### 1.3 Programme Fidélité (`/clients/detail-fidelite`, `/clients/programme-fidelite`)
**Backend :**
- `GetLoyaltyAccountQuery(Guid customerId)` → points actuels, tier, historique points, prochaine récompense
- `AwardLoyaltyPointsCommand(Guid customerId, int points, string reason)`

**DB :** Nouvelle table `LoyaltyTransactions(Id, CustomerId, Points, Reason, Date)`

**Frontend :**
- Jauge de progression vers le prochain palier (Bronze/Silver/Gold/Platinum)
- Historique des points en tableau paginé

---

### 1.4 Onboarding Client (`/clients/onboarding`)
**Backend :**
- Étapes wizard : `CreateCustomerDraftCommand` → `AddVehicleToCustomerCommand` → `FinalizeOnboardingCommand`
- Validation de chaque étape avec FluentValidation

**Frontend :**
- Stepper multi-étapes : (1) Info personnelles → (2) Ajout véhicule → (3) Préférences → (4) Résumé + Signature numérique
- Auto-complétion de la marque/modèle sur la plaque EN → API décodage VIN

---

### 1.5 Ventes Occasion & Flotte (`/clients/ventes-occasion`, `gestion-flotte`, `portail-flotte-b2b`, `profil-flotte-b2b`, `pret-vehicule`)
**DB :** Table `Vehicles` → ajouter `Status` (ENUM: Perso, VO_En_Vente, Prêt, Flotte_B2B), `B2BClientId`

**Backend :**
- `GetUsedVehiclesForSaleQuery` → liste des VO disponibles avec photos, prix, km
- `CreateVehicleLoanCommand` → création d'un prêt de véhicule avec contrat PDF
- `GetB2BFleetQuery(Guid b2bClientId)` → flotte dédiée au client professionnel

---

## 🔧 PHASE 2 — ATELIER & PLANNING (Sprint 2 — 6 jours)

### 2.1 Planning Atelier (`/atelier/planning`)
**Backend :**
- `GetWorkshopScheduleQuery(DateRange)` → créneaux, technicien assigné, type intervention
- `CreateAppointmentCommand(...)` → nouvelle réservation avec validation de disponibilité
- `MoveAppointmentCommand(Guid, DateTime)` → drag & drop côté Blazor

**Frontend :**
- Calendrier Gantt par technicien (librairie MudBlazor scheduler ou roulette de jours custom)
- Code couleur par type de travaux (Révision=vert, Pneus=orange, Moteur=rouge)

### 2.2 Planning Équipes, Congés & Formations (`/atelier/planning-equipes`, `planning-conges`, `atelier/formations`)
**Backend :**
- `GetTeamScheduleQuery(Week)` → disponibilités vs absences
- `RequestLeaveCommand(Guid employeeId, DateRange, LeaveType)` → soumission congé
- `ApproveLeaveCommand(Guid requestId, bool approved)` → validation manager

**DB :** Table `LeaveRequests(Id, EmployeeId, Start, End, Type, Status, ManagerNote)`

### 2.3 Journal Tâches (`/atelier/journal-taches`)
**Backend :**
- `GetTaskJournalQuery(DateRange, Guid? technicianId)` → toutes les tâches pointées
- `StartTaskTimerCommand(Guid taskId, Guid technicianId)` → pointage début
- `StopTaskTimerCommand(Guid taskId)` → calcul temps réel passé

**Frontend :** Horloge chronométrique animée en temps réel (SignalR ou Timer Blazor)

### 2.4 Plan 2D Atelier (`/atelier/plan-2d`)
**Frontend :**
- Carte SVG interactive des emplacements (boxes 1..N)
- Code couleur de chaque box (libre/occupé/réservé)
- `GetWorkshopLayoutQuery` → positions véhicules en cours

### 2.5 Monitoring IoT (`/atelier/monitoring-iot`)
**Backend :**
- Hub SignalR : `WorkshopHub.cs` → push OBD metrics toutes les 5s
- `GetDeviceStatusesQuery` → état de chaque borne OBD connectée

**Frontend :**
- Tuiles machines avec jauges animées (température, pression, courant)
- Alertes push si valeur hors plage (via `INotificationService`)

### 2.6 Maintenance Préventive (`/atelier/maintenance-preventive`)
**Backend :**
- `GetUpcomingMaintenancesQuery` → liste d'interventions planifiées par IA selon kilométrage
- `SnoozeMaintenanceAlertCommand(Guid vehicleId, int days)`

### 2.7 Reconditionnement Moteur (`/atelier/reconditionnement-moteur`)
**Backend :**
- `CreateEngineReconditioningJobCommand` → ouverture d'une fiche de chantier moteur
- `GetReconditioningJobQuery(Guid id)` → suivi des étapes de remise en état

---

## 🤖 PHASE 3 — DIAGNOSTIC IA & TECHNIQUE (Sprint 3 — 5 jours)

### 3.1 Diagnostic Avancé (`/diagnostic/avancer`)
**Backend :**
- Intégration OBD-III : Parse codes Pxx, Bxx, Cxx, Uxx → `DiagnosticCodeDto(Code, Severity, Description, Recommandation)`
- `RunAiDiagnosticCommand(Guid vehicleId, string[] obdCodes)` → appel OpenAI GPT-4 avec prompt spécialisé auto

**Frontend :**
- Scanner de codes OBD (input manuel ou via Bluetooth WASM API si permission)
- Affichage hiérarchique : Critique → Avertissement → Information
- Bouton "Générer rapport PDF"

### 3.2 Diagnostic Batterie EV (`/diagnostic/batterie-ev`)
**Backend :**
- `GetBatteryHealthQuery(Guid vehicleId)` → SOH%, SOC%, cycles charge, température cellules
- `GetChargingHistoryQuery(Guid vehicleId, DateRange)`

**Frontend :** Graphiques D3.js (ou Chart.js via JS interop) : courbe santé batterie vs temps

### 3.3 Décodeur VIN Pro (`/diagnostic/vin-pro`)
**Backend :**
- `DecodeVinQuery(string vin)` → appel API externe NHTSA/DEKRA + cache Redis
- `GetRecallsByVinQuery(string vin)` → rappels constructeur officiels

### 3.4 Expertise Photo IA (`/diagnostic/expertise-photo`)
**Backend :**
- Upload photo → Azure Blob Storage → appel Azure Computer Vision ou OpenAI Vision
- `AnalyzeVehicleDamageCommand(Guid vehicleId, string blobUrl)` → `DamageAssessmentDto(Severity, AffectedParts[], EstimatedCost)`

**Frontend :** Drag & Drop photo upload avec prévisualisation + overlay de zones détectées

### 3.5 Rapport Diagnostic (`/atelier/rapport-diagnostic`)
**Backend :**
- `GenerateDiagnosticReportCommand(Guid revisionId)` → PDF avec logo garage, codes OBD, recommandations, signature technicien
- Utilise **QuestPDF** : header + section par système (moteur, freinage, électrique...)

### 3.6 Analytique Prédictive IA (`/diagnostic/analytique-ia`)
**Backend :**
- `GetPredictiveInsightsQuery` → agrégation ML.NET sur données historiques de pannes
- Prédictions : "Véhicule IMT-442-AZ : risque panne alternateur dans ~3 mois"

---

## 💰 PHASE 4 — FINANCE, STOCK & LOGISTIQUE (Sprint 4 — 7 jours)

### 4.1 Archives Factures + Factures Abonnements
**Backend :**
- `GetInvoicesPagedQuery(filter, dateRange, status)` → `PagedResult<InvoiceDto>`
- `GetSubscriptionInvoicesQuery(Guid garageId)` → abonnement SaaS MecaPro

**Frontend :** Table triable/filtrable avec export CSV et PDF par ligne

### 4.2 Paiement Sécurisé (`/paiement-securise`)
**Backend :**
- Intégration **Stripe** (déjà initialisé dans Services.cs)
- `CreatePaymentIntentCommand(decimal amount, Guid invoiceId)` → retourne `clientSecret`
- Webhook `/api/v1/stripe/webhook` → marque facture "Payée" sur succès

**Frontend :** Composant Stripe Elements (JS interop) pour saisie CB sécurisée

### 4.3 Export Comptable (`/export-comptable`)
**Backend :**
- `ExportAccountingDataCommand(DateRange, ExportFormat)` → CSV ou XML CIEL/SAGE format comptable
- **EPPlus** pour Excel avec feuilles séparées : Achats, Ventes, TVA

### 4.4 Analytique Stats & Prévisions (`/analytique-stats`, `/previsions-revenu`)
**Backend :**
- `GetBusinessKpisQuery(DateRange)` → CA, marge, ticket moyen, taux retour client
- `GetRevenueForecastQuery(int nextMonths)` → régression linéaire sur données historiques

**Frontend :** Charts interactifs via **Blazor-ApexCharts** : Camemberts, courbes, barres empilées

### 4.5 Journal Ventes + Rapports Annuels
**Backend :**
- `GetSalesJournalQuery(DateRange)` → chaque ligne de caisse avec TVA
- `GenerateAnnualReportCommand(int year)` → PDF rapport de gestion complet

### 4.6 Comparatif Devis (1, 2, 3)
**Backend :**
- `CompareQuoteOptionsQuery(Guid vehicleId)` → liste devis alternatifs (OEM, compatible, reconditionné)
- Affichage côte-à-côte des 3 options — le moins cher mis en surbrillance

### 4.7 Inventaire Stock (`/stock/inventaire`)
**Backend :**
- `GetStockPagedQuery(search, category, stockFilter)` → `PagedResult<PartDto>`
- `AdjustStockCommand(Guid partId, int delta, string reason)` → inventaire physique
- Alertes auto si `QuantityInStock < MinimumThreshold`

### 4.8 Commandes Fournisseurs + Détail Commande
**Backend :**
- `GetSupplierOrdersQuery(status, dateRange)` → commandes en cours/reçues/annulées
- `CreateSupplierOrderCommand(List<OrderLineDto>)` → génération bon de commande PDF

### 4.9 Suivi Livraisons + Gestion Retours
**Backend :**
- `GetDeliveriesQuery` → pièces commandées avec statut transporteur
- Intégration webhook transporteur (La Poste / Chronopost) → mise à jour automatique
- `CreateReturnCommand(Guid deliveryId, string reason)` → RMA

### 4.10 Comparatif Carburants + Suivi Énergie
**Backend :**
- `GetFuelPricesQuery` → scraping / API carbu.com pour prix régionaux au jour J
- `GetEnergyConsumptionQuery(DateRange)` → électricité, gaz, fluides atelier

---

## 👥 PHASE 5 — RH, SÉCURITÉ & MARKETING (Sprint 5 — 6 jours)

### 5.1 Gestion Employés + Équipes Staff + Matrice Compétences
**Backend :**
- `GetEmployeesPagedQuery` → `EmployeeDto(Id, Name, Role, Skills[], Certifications[], Schedule)`
- `UpdateEmployeeSkillsCommand(Guid, SkillDto[])` → mise à jour compétences
- `GetSkillMatrixQuery` → tableau croisé Employé × Compétence avec niveau 1-5

**DB :** Tables `Employees`, `Skills`, `EmployeeSkills(EmployeeId, SkillId, Level, ValidUntil)`

### 5.2 Audit Sécurité + Journal Audit + Archives Audits
**Backend :**
- `GetAuditLogsQuery(DateRange, action, userId)` → chaque action utilisateur loguée
- Le `LoggingBehavior` MediatR déjà en place — enrichir avec niveau criticité
- `GenerateAuditReportCommand(DateRange)` → export PDF

### 5.3 Gestion Déchets + Suivi Conformité + Historique/Rapport Pollution
**Backend :**
- `LogWasteDisposalCommand(WasteType, Quantity, DisposalCompany, BsdNumber)` → saisie bordereau de suivi déchets
- `GetWasteHistoryQuery(DateRange)` → historique conforme aux normes ICPE
- `GenerateBilanCarboneCommand(int year)` → PDF bilan carbone + indicateurs GES

### 5.4 Documents Légaux
**Backend :**
- `GetLegalDocumentsQuery` → liste GED (Contrats de travail, kbis, assurances, URSSAF)
- `UploadLegalDocumentCommand(file, category, expiryDate)` → Azure Blob + métadonnées DB
- Alertes auto 30j avant expiration d'un document

### 5.5 Analyse Satisfaction + Sondage
**Backend :**
- SMS/Email automatique 48h après livraison → lien sondage unique (token)
- `SubmitSurveyResponseCommand(token, answers)` → stockage anonymisé
- `GetSatisfactionDashboardQuery` → NPS moyen, verbatims, tendances

### 5.6 Notifications Config
**Backend :**
- `GetNotificationSettingsQuery(Guid garageId)` → quels événements déclenchent quelle notif (SMS, email, push)
- `UpdateNotificationSettingsCommand(...)` → granularité par type d'événement

### 5.7 Config Imprimante + Export
**Backend :**
- `GetPrinterConfigQuery` → liste imprimantes réseau configurées (IP, format papier)
- `PrintDocumentCommand(Guid documentId, Guid printerId)` → envoi direct via PrintService

### 5.8 Gestion Sinistres
**Backend :**
- `CreateClaimCommand(Guid vehicleId, ClaimType, ExpertId, InsuranceRef)` → dossier sinistre
- `GetClaimsQuery` → suivi état dossiers (Déclaré, Expert Mandaté, Indemnisé, Clôturé)
- Intégration signature électronique (DocuSign ou YOUSIGN API)

### 5.9 Chat Temps Réel (`/chat`)
**Backend :**
- `ChatHub.cs` (SignalR) → canaux : général, par équipe, par véhicule
- `SendMessageCommand` → persistence en DB + push SignalR
- `GetMessageHistoryQuery(channel, before, limit)` → pagination curseur

---

## 📊 RÉSUMÉ PRIORISATION

| Phase | Module | Valeur Métier | Complexité | Sprint |
|---|---|---|---|---|
| 1 | CRM Clients & Véhicules | ⭐⭐⭐⭐⭐ | Moyenne | Semaine 1 |
| 2 | Planning & Atelier | ⭐⭐⭐⭐⭐ | Haute | Semaine 2 |
| 3 | Diagnostic IA | ⭐⭐⭐⭐ | Très haute | Semaine 3 |
| 4 | Finance & Stock | ⭐⭐⭐⭐⭐ | Haute | Semaine 4-5 |
| 5 | RH, Sécurité, Marketing | ⭐⭐⭐ | Moyenne | Semaine 6-7 |

---

## 🗄️ NOUVELLES TABLES DB À CRÉER (Migrations)

```sql
-- Phase 1
ALTER TABLE Customers ADD Tags NVARCHAR(500), LoyaltyTier INT, PreferredContact NVARCHAR(50)
CREATE TABLE LoyaltyTransactions (Id, CustomerId, Points, Reason, Date)
ALTER TABLE Vehicles ADD Status INT, B2BClientId UNIQUEIDENTIFIER NULL

-- Phase 2
CREATE TABLE LeaveRequests (Id, EmployeeId, StartDate, EndDate, Type, Status, ManagerNote)
CREATE TABLE TaskTimers (Id, TaskId, TechnicianId, StartedAt, StoppedAt, DurationMinutes)
CREATE TABLE WorkshopBoxes (Id, Number, Status, CurrentVehicleId NULL)

-- Phase 4
CREATE TABLE SupplierOrders (Id, SupplierId, Status, TotalAmount, OrderDate, ExpectedDate)
CREATE TABLE SupplierOrderLines (Id, OrderId, PartId, Qty, UnitPrice)
CREATE TABLE WasteDisposals (Id, WasteType, QuantityKg, DisposalDate, ContractorName, BsdNumber)

-- Phase 5
CREATE TABLE Employees (Id, FirstName, LastName, Role, HireDate, ContractType, GarageId)
CREATE TABLE Skills (Id, Name, Category)
CREATE TABLE EmployeeSkills (EmployeeId, SkillId, Level, ValidUntil)
CREATE TABLE LegalDocuments (Id, Name, Category, FilePath, ExpiryDate, UploadedAt)
CREATE TABLE SurveyCampaigns (Id, RevisionId, SentAt, Token, CompletedAt, NpsScore)
CREATE TABLE InsuranceClaims (Id, VehicleId, Type, Status, InsuranceRef, ExpertId, CreatedAt)
CREATE TABLE ChatMessages (Id, Channel, SenderId, Content, SentAt, IsRead)
```

---

## 📦 NOUVEAUX PACKAGES NUGET À INSTALLER

```bash
dotnet add package QuestPDF                    # Génération PDF pro
dotnet add package EPPlus                      # Export Excel
dotnet add package Stripe.net                  # Paiements
dotnet add package Azure.Storage.Blobs         # Upload fichiers
dotnet add package Azure.AI.ComputerVision     # IA Photo
dotnet add package Blazored.FluentValidation   # Validation Blazor
dotnet add package Blazor-ApexCharts           # Graphiques interactifs
dotnet add package Microsoft.ML                # ML.NET prédiction
```

---

## ✅ CRITÈRES D'ACCEPTATION PAR MODULE

Pour chaque module, le ticket sera considéré terminé si :
- [ ] Le composant Blazor ne contient **aucune donnée codée en dur**
- [ ] Toutes les données viennent d'un appel `Api.GetAsync<T>()` ou `Api.PostAsync<T>()`
- [ ] Un état de chargement (`isLoading`) et un état d'erreur sont gérés proprement
- [ ] L'API backend retourne un `Result<T>` avec codes HTTP corrects (200, 400, 404, 500)
- [ ] La fonctionnalité est accessible avec le rôle approprié (JWT Claim)
- [ ] Les actions de mutation (Create/Update/Delete) ont une confirmation utilisateur
- [ ] Un test d'intégration minimal couvre le handler CQRS correspondant

---

*Plan sauvegardé dans `/construction/plan_fonctionnalisation.md` — MecaPro 2026*
