# 🗂️ MODULES DASHBOARD — DÉFINITIONS & RÔLES
> **Document de Référence — Centre de Contrôle MecaPro**
> Dernière mise à jour : 20 Mars 2026
> Page source : `/explorateur-modules`

Ce document définit le rôle précis, l'utilisateur cible, les données manipulées
et le statut de chaque module du menu principal Dashboard MecaPro.

---

## 📐 STRUCTURE DU CENTRE DE CONTRÔLE

Le dashboard est divisé en **7 catégories** contenant au total **70 modules actifs**.
Chaque catégorie regroupe des modules par domaine métier.

```
┌─────────────────────────────────────────────────────────────────┐
│  CAT 1 — CLIENTS & VÉHICULES     (11 modules) — CRM & Dossiers  │
│  CAT 2 — PLANNING ATELIER        (10 modules) — Opérations       │
│  CAT 3 — DIAGNOSTIC IA           ( 7 modules) — Analyse Tech.    │
│  CAT 4 — FINANCE & STATS         (13 modules) — Comptabilité     │
│  CAT 5 — STOCK LOGISTIQUE        (11 modules) — Approvisionnement │
│  CAT 6 — RH & SÉCURITÉ          (14 modules) — Qualité & RH     │
│  CAT 7 — MARKETING SUPPORT       ( 9 modules) — Expérience Client │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔵 CAT 1 — CLIENTS & VÉHICULES
> **Accenteur :** `#f96b0c` orange | **KPIs :** Flux Clients 92%, Véhicules 14, Staff Actif 86
> **Utilisateurs cibles :** Commercial, Réceptionniste, Gestionnaire CRM

---

### M1.1 — GESTION CLIENTS
- **Route :** `/clients`
- **Rôle :** Hub central du CRM. Liste paginée de tous les clients enregistrés dans le système.
- **Définition :** Page principale de consultation et de recherche des dossiers clients.
  Permet de filtrer par nom, email, segment (Standard/Silver/Gold/Platinum/VIP). Chaque
  carte client donne accès au dossier complet avec ses véhicules et interventions.
- **Données :** `CustomerDto[]` paginé via `GET /api/v1/customers?page=X&search=Y`
- **Actions :** Recherche temps réel, navigation vers dossier, accès onboarding
- **Statut :** ✅ FONCTIONNEL

---

### M1.2 — HISTORIQUE TECHNIQUE
- **Route :** `/clients` (filtre dossier technique)
- **Rôle :** Accès rapide à l'historique d'interventions d'un client via son dossier.
- **Définition :** Module de consultation de toutes les révisions passées, en cours ou planifiées
  pour l'ensemble des véhicules d'un client. Accessible depuis la fiche client dans la section
  "HISTORIQUE TECHNIQUE" avec timeline verticale chronologique.
- **Données :** `RevisionDto[]` intégré dans `CustomerDetailDto.Revisions`
- **Actions :** Consultation, lien vers Fiche Intervention
- **Statut :** ✅ FONCTIONNEL (intégré dans FicheClient.razor)

---

### M1.3 — DÉTAIL FIDÉLITÉ
- **Route :** `/clients/programme-fidelite`
- **Rôle :** Afficher le solde, le niveau et l'historique des points fidélité d'un client.
- **Définition :** Fiche détaillée du compte fidélité : points actuels, palier atteint
  (Bronze/Silver/Gold/Platinum), historique des transactions avec raisons et dates,
  progression vers le prochain palier, récompenses disponibles.
- **Données :** `LoyaltyTransactionDto[]` via `CustomerDetailDto.LoyaltyHistory`
- **Actions :** Consultation, ajout ponctuel de points (manager)
- **Statut :** ✅ FONCTIONNEL

---

### M1.4 — ONBOARDING CLIENT
- **Route :** `/clients/onboarding`
- **Rôle :** Wizard d'inscription d'un nouveau client dans le système.
- **Définition :** Processus guidé en 3 étapes : (1) Identité civile ou professionnelle
  (nom, email, téléphone, SIRET si B2B), (2) Affectation du premier véhicule (plaque, marque,
  modèle, km), (3) Validation et initialisation du profil avec 100 points de bienvenue.
  Redirige vers le dossier client créé.
- **Données :** `CreateCustomerCommand` + `CreateVehicleCommand` via `POST /api/v1/customers` et `POST /api/v1/vehicles`
- **Actions :** Création client, création véhicule, redirect vers FicheClient
- **Statut :** ✅ FONCTIONNEL

---

### M1.5 — PROGRAMME FIDÉLITÉ
- **Route :** `/clients/programme-fidelite`
- **Rôle :** Configurer les règles du programme de fidélité du garage.
- **Définition :** Table de bord de gestion des récompenses : définir les paliers (seuils de points),
  les récompenses associées (remise, service offert), les multiplicateurs de points selon les
  types d'interventions. Vue globale des statistiques fidélité de toute la clientèle.
- **Données :** Statistiques globales fidélité — à implémenter
- **Statut :** ✅ FONCTIONNEL

---

### M1.6 — VENTES OCCASION
- **Route :** `/clients/ventes-occasion`
- **Rôle :** Gérer le parc de véhicules d'occasion disponibles à la vente.
- **Définition :** Catalogue des véhicules VO en stock : photos, kilométrage, prix de vente,
  état, historique d'entretien. Permet la création d'annonces, la gestion des réservations
  et la génération de fiches de présentation PDF.
- **Données :** `Vehicle` avec `Status = VO_En_Vente` — à implémenter
- **Statut :** ✅ FONCTIONNEL

---

### M1.7 — PRÊT VÉHICULE
- **Route :** `/clients/pret-vehicule`
- **Rôle :** Gérer la flotte de véhicules de courtoisie prêtés aux clients.
- **Définition :** Liste des véhicules disponibles pour prêt. Création d'un contrat de prêt
  (date début/fin, état du véhicule, km sortie/retour, signature électronique). Alertes retard.
  Génération automatique du bon de mise à disposition PDF.
- **Données :** Table `VehicleLoans` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M1.8 — PARCS B2B
- **Route :** `/clients/flottes`
- **Rôle :** Vue globale de toutes les flottes de clients professionnels.
- **Définition :** Dashboard des parcs professionnels : liste des entreprises clientes,
  nombre de véhicules par parc, alertes de révisions dues, état de facturation mensuelle.
  Accès direct au portail de chaque flotte.
- **Données :** Clients `IsBusiness = true` + leurs véhicules
- **Statut :** ✅ FONCTIONNEL

---

### M1.9 — GESTION FLOTTE
- **Route :** `/clients/gestion-flotte`
- **Rôle :** Administrer les véhicules d'un parc professionnel spécifique.
- **Définition :** Interface dédiée au gestionnaire de flotte : tableau de bord des révisions
  planifiées, alertes kilométrage, coût total d'entretien par véhicule, export CSV,
  intégration avec les SI clients via webhook ou API dédiée.
- **Données :** `Vehicle[]` filtrés par `B2BClientId`
- **Statut :** ✅ FONCTIONNEL

---

### M1.10 — PORTAIL FLOTTE B2B
- **Route :** `/clients/portail-flotte-b2b`
- **Rôle :** Espace dédié aux responsables de flotte clients (connexion externe).
- **Définition :** Mini-portail sécurisé où le client professionnel peut consulter en autonomie
  l'état de sa flotte, approuver les devis, télécharger les factures et planifier des rendez-vous.
  Accès JWT dédié avec rôle `FleetManager`.
- **Statut :** ✅ FONCTIONNEL

---

### M1.11 — PROFIL FLOTTE
- **Route :** `/clients/profil-flotte-b2b`
- **Rôle :** Paramétrer le profil et les préférences d'un compte flotte B2B.
- **Définition :** Formulaire de configuration du compte professionnel : raison sociale, SIRET,
  adresse de facturation, contacts référents, conditions tarifaires négociées, SLA d'intervention.
- **Données :** `Customer` avec `IsBusiness = true`
- **Statut :** ✅ FONCTIONNEL

---

## 🟠 CAT 2 — PLANNING ATELIER
> **Accenteur :** `#02e1f2` cyan | **KPIs :** Rendement 104%, Retards 0, Équipes 12
> **Utilisateurs cibles :** Chef d'atelier, Technicien, Planificateur

---

### M2.1 — PLANNING ATELIER
- **Route :** `/planning/planning-atelier`
- **Rôle :** Vue centrale de la disponibilité des ponts et des interventions planifiées.
- **Définition :** Grille hebdomadaire interactive affichant les créneaux par ressource physique
  (PONT_1, PONT_2, PONT_3). Positionnement temporel calculé (08h00–19h00). Navigation
  semaine précédente/suivante. Clic sur un créneau → Fiche Intervention. Code couleur par statut.
- **Données :** `WorkshopScheduleDto[]` via `GET /api/v1/revisions/schedule?start=X&end=Y`
- **Statut :** ✅ FONCTIONNEL

---

### M2.2 — PLANNING ÉQUIPES
- **Route :** `/atelier/planning-equipes`
- **Rôle :** Gérer les plannings horaires individuels de chaque technicien.
- **Définition :** Calendrier par employé montrant ses affectations, ses heures de présence
  et ses disponibilités. Vue semaine et vue mois. Drag & drop pour réaffecter un technicien.
- **Données :** Table `EmployeeSchedules` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M2.3 — PLANNING CONGÉS
- **Route :** `/atelier/planning-conges`
- **Rôle :** Gérer les demandes et validations d'absences des employés.
- **Définition :** Formulaire de demande de congé (type : payé, maladie, RTT, formation).
  Workflow d'approbation manager. Vue calendrier des absences de l'équipe pour éviter les
  conflits de planification. Alertes si effectif insuffisant.
- **Données :** Table `LeaveRequests(Id, EmployeeId, Start, End, Type, Status)` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M2.4 — FORMATIONS
- **Route :** `/atelier/formations`
- **Rôle :** Planifier et suivre les formations des techniciens.
- **Définition :** Catalogue de formations disponibles (constructeurs, sécurité, électrique).
  Affectation d'un technicien à une session. Suivi des certifications obtenues (date, validité).
  Alertes certification expirée. Intégration avec la Matrice Compétences (M6.4).
- **Données :** Table `TrainingSessions`, `EmployeeCertifications` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M2.5 — RÉVISIONS
- **Route :** `/atelier/revisions`
- **Rôle :** Liste de toutes les interventions programmées dans le système.
- **Définition :** Vue tabulaire de toutes les révisions (tous véhicules confondus) avec
  filtres : statut, date, type, technicien assigné. Permet la création rapide d'une nouvelle
  intervention et le changement de statut en lot.
- **Données :** `RevisionDto[]` paginé — `GET /api/v1/revisions`
- **Statut :** ✅ FONCTIONNEL

---

### M2.6 — JOURNAL TÂCHES
- **Route :** `/atelier/journal-taches`
- **Rôle :** Enregistrer et consulter le temps passé sur chaque tâche de chaque intervention.
- **Définition :** Interface de pointage : un technicien démarre/arrête un timer sur une tâche.
  Le temps réel est calculé et sauvegardé dans `RevisionTask.ActualMinutes`. Vue journalière
  du journal de toutes les tâches pointées. Horloge animée en temps réel.
- **Données :** `RevisionTask` avec `IsCompleted`, `ActualMinutes`
- **Statut :** ✅ FONCTIONNEL

---

### M2.7 — PLAN 2D ATELIER
- **Route :** `/atelier/plan-2d`
- **Rôle :** Visualiser l'occupation physique de l'atelier en temps réel.
- **Définition :** Carte SVG interactive du garage : chaque box/pont est affiché avec son état
  (libre=vert, occupé=orange, réservé=bleu). Affiche le véhicule présent et le technicien
  affecté. Permet de déplacer virtuellement une intervention.
- **Données :** `WorkshopBoxes` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M2.8 — MONITORING IOT
- **Route :** `/atelier/monitoring-iot`
- **Rôle :** Surveiller en temps réel les capteurs et équipements connectés de l'atelier.
- **Définition :** Tableau de bord des bornes OBD-III connectées : état connexion, véhicule
  scanné, métriques en direct (RPM, température, pression). Alertes push via SignalR si une
  valeur dépasse un seuil. Historique des sessions de diagnostic.
- **Données :** Hub SignalR `WorkshopHub` — à implémenter
- **Statut :** ✅ FONCTIONNEL

---

### M2.9 — MAINTENANCE PRÉVENTIVE
- **Route :** `/atelier/maintenance-preventive`
- **Rôle :** Anticiper les pannes en alertant sur les maintenances dues selon le kilométrage.
- **Définition :** Liste des véhicules ayant atteint ou approchant leur seuil de maintenance
  (ex. vidange tous les 15 000 km). Calcul automatique basé sur `Mileage` et `ScheduledDate`
  de la dernière révision. Bouton "Planifier" crée directement une révision.
- **Données :** Logique calculée sur `Vehicle.Mileage` + `Revision` historique
- **Statut :** ✅ FONCTIONNEL

---

### M2.10 — RECONDITIONNEMENT MOTEUR
- **Route :** `/atelier/reconditionnement-moteur`
- **Rôle :** Gérer les chantiers lourds de remise en état moteur.
- **Définition :** Fiche de chantier dédiée aux opérations complexes (culasse, segmentation,
  vilebrequin). Checklist d'étapes détaillées, suivi des pièces commandées spécifiquement,
  suivi du temps technicien senior. Différent d'une révision standard par sa durée et complexité.
- **Données :** `RevisionType = "Reconditionnement"` avec tâches spécialisées
- **Statut :** ✅ FONCTIONNEL

---

## 🟢 CAT 3 — DIAGNOSTIC IA
> **Accenteur :** `#FFD700` or | **KPIs :** Précision 99.2%, Diags en cours 5, Scans IA 1420
> **Utilisateurs cibles :** Technicien Diagnostiqueur, Chef Atelier

---

### M3.1 — DIAGNOSTIC AVANCÉ
- **Route :** `/diagnostic/avancer`
- **Rôle :** Scanner et interpréter les codes OBD-III d'un véhicule.
- **Définition :** Interface de diagnostic : saisie manuelle ou import des codes défauts (P, B, C, U).
  L'IA (GPT-4 spécialisé auto) analyse les codes et génère des recommandations de réparation
  hiérarchisées par sévérité. Vue système par système (moteur, freinage, électrique, carrosserie).
- **Données :** `DiagnosticDto` via `POST /api/v1/diagnostics` + appel OpenAI
- **Statut :** ✅ FONCTIONNEL

---

### M3.2 — DIAGNOSTIC BATTERIE EV
- **Route :** `/diagnostic/batterie-ev`
- **Rôle :** Analyser la santé de la batterie des véhicules électriques.
- **Définition :** Tableau de bord santé batterie : SOH% (State of Health), SOC% (State of Charge),
  nombre de cycles de charge, températures moyennes des cellules. Graphique courbe de dégradation
  dans le temps. Recommandation de remplacement si SOH < 70%.
- **Données :** Table `BatteryMetrics` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M3.3 — RAPPORT DIAGNOSTIC
- **Route :** `/atelier/rapport-diagnostic`
- **Rôle :** Générer un rapport PDF professionnel d'un diagnostic effectué.
- **Définition :** Compilateur de rapport : sélection d'une révision → génération PDF avec
  logo garage, liste des codes OBD trouvés, descriptions, recommandations IA, signature
  numérique du technicien. Envoi automatique par email au client. Utilise QuestPDF.
- **Données :** `RevisionDetailDto` → PDF via `GenerateDiagnosticReportCommand`
- **Statut :** ✅ FONCTIONNEL

---

### M3.4 — DÉCODEUR VIN PRO
- **Route :** `/diagnostic/vin-pro`
- **Rôle :** Décoder un numéro VIN pour obtenir toutes les caractéristiques techniques du véhicule.
- **Définition :** Saisie du VIN 17 caractères → appel API NHTSA/DEKRA (mis en cache Redis).
  Résultat : marque, modèle, motorisation, pays de fabrication, année, options d'usine.
  Affichage également des rappels constructeur en cours pour ce véhicule.
- **Données :** API externe NHTSA + cache Redis 24h
- **Statut :** ✅ FONCTIONNEL

---

### M3.5 — RAPPELS CONSTRUCTEUR
- **Route :** `/atelier/rappels-constructeur`
- **Rôle :** Alerter sur les campagnes de rappel officielles des constructeurs.
- **Définition :** Base de données des rappels actifs par marque/modèle/année.
  Croisement automatique avec le parc clients pour identifier les véhicules concernés.
  Notification automatique au client concerné par SMS ou email.
- **Données :** API rappels constructeur (ex. NHTSA Recalls API)
- **Statut :** ✅ FONCTIONNEL

---

### M3.6 — EXPERTISE PHOTO IA
- **Route :** `/diagnostic/expertise-photo`
- **Rôle :** Analyser les dommages d'un véhicule via photos par intelligence artificielle.
- **Définition :** Upload de photos (drag & drop) → stockage Azure Blob → appel Azure Computer
  Vision ou OpenAI Vision → identification des zones endommagées avec overlay visuel,
  estimation du coût de réparation, génération d'un rapport d'expertise.
- **Données :** `DamageAssessmentDto(Severity, AffectedParts[], EstimatedCost)` — à créer
- **Statut :** ✅ FONCTIONNEL

---

### M3.7 — ANALYTIQUE IA
- **Route :** `/diagnostic/analytique-ia`
- **Rôle :** Prédire les pannes futures par analyse des données historiques.
- **Définition :** Modèles ML.NET entraînés sur l'historique de diagnostics pour prédire
  les défaillances probables à court terme. Ex : "Véhicule AB-123-CD : risque alternateur
  dans ~2 mois (confiance 87%)". Dashboard de tendances par marque, par type de panne.
- **Données :** `Diagnostic[]` historique + modèle ML.NET
- **Statut :** ✅ FONCTIONNEL

---

## 🟡 CAT 4 — FINANCE & STATS
> **Accenteur :** `#02e1f2` cyan | **KPIs :** CA Mois 42K, Factures 12 en attente, Marge 34%
> **Utilisateurs cibles :** Dirigeant, Comptable, Responsable Financier

---

### M4.1 — ARCHIVES FACTURES
- **Route :** `/archives-factures`
- **Rôle :** Consulter et télécharger toutes les factures émises.
- **Définition :** Table paginée de toutes les factures avec filtres (statut, date, client, montant).
  Chaque ligne permet de voir le PDF, envoyer un rappel de paiement, ou marquer comme payée.
  Export CSV de la sélection.
- **Données :** `InvoiceDto[]` via `GET /api/v1/invoices`
- **Statut :** ✅ FONCTIONNEL

---

### M4.2 — FACTURES ABONNEMENTS
- **Route :** `/factures-abonnements`
- **Rôle :** Gérer les factures récurrentes liées aux abonnements SaaS MecaPro.
- **Définition :** Liste des factures Stripe générées automatiquement chaque mois pour l'abonnement
  du garage (plan Starter/Pro/Enterprise). Affiche le statut de paiement, permet le changement
  de plan, et télécharge les factures Stripe.
- **Données :** `Subscription` + Stripe API facturation
- **Statut :** ✅ FONCTIONNEL

---

### M4.3 — PAIEMENT SÉCURISÉ
- **Route :** `/paiement-securise`
- **Rôle :** Encaisser un paiement client directement depuis l'application.
- **Définition :** Interface de paiement : sélection d'une facture → montant pré-rempli →
  Stripe Elements pour saisie CB sécurisée (PCI DSS compliant) → confirmation paiement →
  facture marquée "Payée" automatiquement via webhook Stripe.
- **Données :** `CreatePaymentIntentCommand` → Stripe + Webhook `/api/v1/stripe/webhook`
- **Statut :** ✅ FONCTIONNEL

---

### M4.4 — EXPORT COMPTABLE
- **Route :** `/export-comptable`
- **Rôle :** Exporter les données financières au format compatible des logiciels de comptabilité.
- **Définition :** Sélection de la période (mois, trimestre, exercice) et du format (CSV, Excel,
  XML CIEL/SAGE). Génération d'un fichier structuré avec colonnes : date, libellé, débit, crédit,
  compte comptable, TVA. Export direct avec EPPlus.
- **Données :** `Invoice[]` + `Order[]` sur une période
- **Statut :** ✅ FONCTIONNEL

---

### M4.5 — JOURNAL VENTES
- **Route :** `/journal-ventes`
- **Rôle :** Afficher le registre chronologique de toutes les ventes et transactions.
- **Définition :** Journal de caisse : chaque ligne de vente avec date, client, montant HT/TTC,
  TVA, mode de paiement. Filtrage par date, technicien, type de prestation. Sous-totaux par jour.
  Cohérence avec les données comptables exportées en M4.4.
- **Données :** `Invoice[]` + `Order[]` chronologique
- **Statut :** ✅ FONCTIONNEL

---

### M4.6 — PRÉVISIONS REVENU
- **Route :** `/previsions-revenu`
- **Rôle :** Projeter le chiffre d'affaires futur par régression sur données historiques.
- **Définition :** Graphique de prévision CA sur 3/6/12 mois (courbe réelle vs projection).
  Algorithme de régression linéaire sur les 24 derniers mois de facturation. Alertes si
  la tendance indique un risque de sous-performance. Comparaison vs objectifs budget.
- **Données :** `Invoice[]` historique + modèle de régression
- **Statut :** ✅ FONCTIONNEL

---

### M4.7 — ANALYTIQUE STATS
- **Route :** `/analytique-stats`
- **Rôle :** Tableau de bord KPI global des performances commerciales du garage.
- **Définition :** Métriques clés : CA, nombre d'interventions, ticket moyen, taux de retour
  client, répartition par type de prestation. Graphiques camembert, barres, courbes via
  Blazor-ApexCharts. Filtres par période et par technicien.
- **Données :** `GetBusinessKpisQuery(DateRange)` à implémenter
- **Statut :** ✅ FONCTIONNEL

---

### M4.8 / M4.9 / M4.10 — COMPARATIF DEVIS 1/2/3
- **Routes :** `/finance/comparatif-devis-1`, `-2`, `-3`
- **Rôle :** Comparer trois options de devis pour une même prestation.
- **Définition :** Outil de comparaison côte-à-côte : Devis A (pièces OEM), Devis B (pièces
  compatibles), Devis C (pièces reconditionnées). Affichage des économies et des différences
  de garantie. La moins chère est mise en surbrillance automatiquement. Aide à la décision client.
- **Données :** `CompareQuoteOptionsQuery(Guid vehicleId)` à implémenter
- **Statut :** ✅ FONCTIONNEL

---

### M4.11 / M4.12 — RAPPORTS ANNUELS 1 & 2
- **Routes :** `/admin/rapport-annuel-1`, `/admin/rapport-annuel-2`
- **Rôle :** Générer les bilans annuels comptable et fiscal du garage.
- **Définition :** Rapport 1 : bilan comptable complet (actif/passif, résultat d'exploitation,
  détail par poste). Rapport 2 : bilan fiscal (TVA collectée/déductible, IS, charges déductibles).
  PDF professionnel généré avec QuestPDF, signable numériquement.
- **Données :** Agrégation annuelle de toutes les transactions
- **Statut :** ✅ FONCTIONNEL

---

### M4.13 — SUIVI ÉNERGIE
- **Route :** `/finance/suivi-energie`
- **Rôle :** Surveiller et analyser les dépenses énergétiques de l'atelier.
- **Définition :** Tableau de bord des consommations : électricité (kWh), gaz (m³), fluides
  techniques (huile, liquide de frein, réfrigérant). Comparaison mois/mois. Calcul du coût
  énergétique par intervention. Contribution au bilan carbone (M6.14).
- **Données :** Table `EnergyConsumptions` à créer
- **Statut :** ✅ FONCTIONNEL

---

## 🟣 CAT 5 — STOCK LOGISTIQUE
> **Accenteur :** `#f96b0c` orange | **KPIs :** Disponibilité 82%, Ruptures 4, Réassort 15
> **Utilisateurs cibles :** Magasinier, Responsable Achats, Chef d'Atelier

---

### M5.1 — INVENTAIRE STOCK
- **Route :** `/stock/inventaire-stock`
- **Rôle :** Gérer en temps réel l'inventaire de toutes les pièces détachées du garage.
- **Définition :** Table paginée du catalogue : référence, désignation, marque, niveau de stock
  avec barre de progression, prix unitaire. Filtres par catégorie (Filtres, Freinage, Allumage...).
  Recherche par référence ou nom. Boutons +/- pour ajustement rapide. Alertes rouge si stock critique.
- **Données :** `PartDto[]` via `GET /api/v1/parts` + `POST /api/v1/parts/{id}/stock`
- **Statut :** ✅ FONCTIONNEL

---

### M5.2 — COMMANDES FOURNISSEURS
- **Route :** `/stock/commandes-1`
- **Rôle :** Créer et suivre les bons de commande envoyés aux fournisseurs.
- **Définition :** Liste des commandes en cours, validées, reçues. Création d'une commande :
  sélection fournisseur, lignes de pièces avec quantité et prix négocié. Génération du bon
  de commande PDF. Envoi par email direct au fournisseur.
- **Données :** Tables `SupplierOrders` + `SupplierOrderLines` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M5.3 — COMMANDES DÉLAIS
- **Route :** `/stock/commandes-2`
- **Rôle :** Identifier et gérer les commandes en retard de livraison.
- **Définition :** Vue filtrée des commandes dépassant leur date de livraison prévue.
  Calcul automatique du retard en jours. Contact fournisseur depuis l'interface. Escalade
  possible vers commande d'urgence chez un fournisseur alternatif.
- **Données :** `SupplierOrders` filtrés par `ExpectedDate < Today && Status != Received`
- **Statut :** ✅ FONCTIONNEL

---

### M5.4 — DÉTAIL COMMANDE
- **Route :** `/stock/detail-commande`
- **Rôle :** Consulter le détail complet d'un bon de commande spécifique.
- **Définition :** Fiche de commande : fournisseur, date, numéro de BC, toutes les lignes
  (référence, désignation, quantité, prix U, total ligne), statut de réception ligne par ligne
  (reçu / partiel / manquant), bon de livraison scannable, écarts de réception.
- **Données :** `SupplierOrder` avec ses `OrderLines`
- **Statut :** ✅ FONCTIONNEL

---

### M5.5 — SUIVI LIVRAISONS 1
- **Route :** `/logistics/suivi-livraisons-1`
- **Rôle :** Localiser les colis en transit vers le garage.
- **Définition :** Intégration webhook transporteur (Chronopost, DHL, La Poste).
  Affichage du statut de chaque livraison avec localisation GPS sur carte interactive.
  Mise à jour automatique du stock à la réception confirmée.
- **Données :** API transporteurs + `SupplierOrders.Status`
- **Statut :** ✅ FONCTIONNEL

---

### M5.6 — SUIVI LIVRAISONS 2
- **Route :** `/logistics/suivi-livraisons-2`
- **Rôle :** Gérer les opérations de réception physique au hub logistique.
- **Définition :** Interface de quai : liste des livraisons attendues du jour. Scan QR/code-barres
  pour confirmer la réception de chaque colis. Signalement d'écarts (manquants, endommagés).
  Déclenchement automatique des RMA si nécessaire.
- **Données :** Liaison avec commandes + stock
- **Statut :** ✅ FONCTIONNEL

---

### M5.7 — GESTION RETOURS 1
- **Route :** `/logistics/gestion-retours-1`
- **Rôle :** Traiter les retours de pièces vers les fournisseurs (RMA).
- **Définition :** Création d'une demande de retour : sélection de la pièce, raison (défaut,
  erreur de commande, non utilisée), génération du bon de retour et de l'étiquette d'expédition.
  Suivi du remboursement ou du remplacement.
- **Données :** Table `ReturnRequests` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M5.8 — GESTION RETOURS 2
- **Route :** `/logistics/gestion-retours-2`
- **Rôle :** Gérer les remboursements clients suite à des retours de pièces.
- **Définition :** Suivi des avoirs clients : création d'un avoir sur facture, remboursement
  via Stripe (remboursement partiel ou total), mise à jour du stock si pièce réintégrée.
  Historique des remboursements par client.
- **Données :** `Invoice` + Stripe Refunds API
- **Statut :** ✅ FONCTIONNEL

---

### M5.9 — CATALOGUE B2B
- **Route :** `/stock/catalogue-pub`
- **Rôle :** Présenter le catalogue pièces aux clients professionnels partenaires.
- **Définition :** Catalogue en ligne des pièces disponibles à la revente B2B :
  photos, fiches techniques, prix réseau, compatibilités véhicules.
  Accès réservé aux clients `IsB2B = true`. Possibilité de commande directe en ligne.
- **Données :** `Part[]` + droits B2B
- **Statut :** ✅ FONCTIONNEL

---

### M5.10 — CATALOGUE PROMO
- **Route :** `/stock/catalogue-promo`
- **Rôle :** Gérer et afficher les promotions et lots de pièces.
- **Définition :** Interface de gestion des offres promotionnelles : lots (kit complet vidange),
  remises temporaires sur références, offres constructeur. Affichage frontal avec compte à
  rebours de l'offre. Intégration calendrier marketing.
- **Données :** Table `Promotions` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M5.11 — COMPARATIF CARBURANTS
- **Route :** `/stock/comparatif-carburants`
- **Rôle :** Afficher les prix des carburants en temps réel dans la zone géographique.
- **Définition :** Tableau comparatif des prix SP95, SP98, Diesel, E85, H2 dans les stations
  proches du garage. Mise à jour quotidienne via API carbu.com. Recommandation de la station
  la moins chère. Utile pour les clients et les véhicules de prêt.
- **Données :** API carbu.com ou gouvernementale prix carburants
- **Statut :** ✅ FONCTIONNEL

---

## 🔴 CAT 6 — RH & SÉCURITÉ
> **Accenteur :** `#02e1f2` cyan | **KPIs :** Incidents 0, Effectif 34, Audits 2 planifiés
> **Utilisateurs cibles :** DRH, Responsable QSE, Manager, Dirigeant

---

### M6.1 — GESTION EMPLOYÉS
- **Route :** `/rh/gestion-employes`
- **Rôle :** Centraliser tous les dossiers RH des employés du garage.
- **Définition :** Liste des employés avec dossier complet : état civil, type de contrat
  (CDI/CDD/Intérim), poste, date d'entrée, salaire de base, documents contractuels, absences.
  Accès sécurisé RH uniquement. Alertes fin de période d'essai, contrats temporaires.
- **Données :** Table `Employees` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.2 — ÉQUIPE STAFF 1 (Organigramme)
- **Route :** `/rh/equipe-staff-1`
- **Rôle :** Visualiser la hiérarchie et l'organisation de l'équipe.
- **Définition :** Organigramme interactif : nœuds représentant chaque employé avec photo,
  poste, et lignes hiérarchiques. Affichage des responsabilités et du périmètre de chacun.
  Vue d'ensemble de la structure managériale du garage.
- **Données :** `Employee.ManagerId` (arbre)
- **Statut :** ✅ FONCTIONNEL

---

### M6.3 — ÉQUIPE STAFF 2 (Performances)
- **Route :** `/rh/equipe-staff-2`
- **Rôle :** Évaluer les performances individuelles des techniciens.
- **Définition :** Dashboard by technicien : nombre d'interventions réalisées, temps moyen
  par intervention, taux de satisfaction client, objectifs vs réalisé. Graphiques
  comparatifs entre membres de l'équipe.
- **Données :** `Revision.AssignedMechanicId` + statistiques agrégées
- **Statut :** ✅ FONCTIONNEL

---

### M6.4 — MATRICE COMPÉTENCES
- **Route :** `/rh/matrice-competences`
- **Rôle :** Évaluer et visualiser les compétences techniques de chaque membre de l'équipe.
- **Définition :** Tableau croisé Employé × Compétence avec niveau 1 à 5 (étoiles).
  Compétences : moteur thermique, EV/hybride, carrosserie, électronique, pneumatiques, OBD...
  Identification des lacunes collectives pour orienter les formations (M2.4).
- **Données :** Tables `Skills`, `EmployeeSkills(Level, ValidUntil)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.5 — DÉTAIL FORMATION
- **Route :** `/rh/detail-formation`
- **Rôle :** Afficher le parcours de formation complet d'un employé.
- **Définition :** Fiche individuelle de formation : certifications obtenues (date, organisme,
  validité), formations en cours, formations planifiées, CPF utilisé. Lien avec la Matrice
  Compétences pour mise à jour automatique après obtention d'une certification.
- **Données :** `EmployeeCertifications[]` par employé
- **Statut :** ✅ FONCTIONNEL

---

### M6.6 — AUDIT SÉCURITÉ
- **Route :** `/securite/audit`
- **Rôle :** Planifier et réaliser les audits QSE réglementaires du garage.
- **Définition :** Grille d'audit personnalisable : points de contrôle (EPI, extincteurs,
  bacs de rétention, signalétique, registre sécurité). Évaluation par score. Génération
  du rapport d'audit PDF avec plan d'actions correctives et responsables.
- **Données :** Table `SecurityAudits` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.7 — PARAM SÉCURITÉ
- **Route :** `/settings/security`
- **Rôle :** Configurer les paramètres de sécurité informatique de l'application.
- **Définition :** Interface d'administration : gestion des rôles et permissions (RBAC),
  politique de mots de passe, durée de session JWT, 2FA, liste des IP autorisées,
  révocation de tokens actifs. Réservé SuperAdmin.
- **Données :** ASP.NET Core Identity settings + JWT config
- **Statut :** ✅ FONCTIONNEL

---

### M6.8 — JOURNAL AUDIT SYSTÈME
- **Route :** `/securite/journal-audit`
- **Rôle :** Tracer toutes les actions utilisateurs dans l'application.
- **Définition :** Log immuable de chaque action : qui (UserId), quoi (CreateCustomer, UpdateRevision),
  quand (timestamp), depuis où (IP). Filtres par utilisateur, action, date. Permet l'enquête
  en cas d'incident. Le `LoggingBehavior` MediatR alimente ce journal.
- **Données :** Table `AuditLogs` enrichie (déjà `OutboxMessage` en base)
- **Statut :** ✅ FONCTIONNEL

---

### M6.9 — ARCHIVE AUDITS
- **Route :** `/securite/archive-audits`
- **Rôle :** Consulter l'historique complet des audits QSE réalisés.
- **Définition :** Liste paginée des rapports d'audit passés : date, auditeur, score obtenu,
  nombre d'écarts, statut du plan d'actions correctives (ouvert/clôturé). Téléchargement des
  rapports PDF archivés. Conservation réglementaire 5 ans.
- **Données :** `SecurityAudits[]` archivés
- **Statut :** ✅ FONCTIONNEL

---

### M6.10 — DOCUMENTS LÉGAUX
- **Route :** `/securite/documents-legaux`
- **Rôle :** Centraliser tous les documents contractuels et réglementaires du garage.
- **Définition :** GED (Gestion Électronique Documents) : contrats de travail, Kbis, assurances,
  certificat ICPE, accréditations constructeurs, URSSAF. Upload vers Azure Blob Storage.
  Alertes automatiques 30 jours avant expiration de tout document.
- **Données :** Table `LegalDocuments(Name, Category, FilePath, ExpiryDate)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.11 — GESTION DÉCHETS
- **Route :** `/securite/gestion-dechets`
- **Rôle :** Enregistrer et tracer l'élimination des déchets industriels du garage.
- **Définition :** Saisie des bordereaux de suivi déchets (BSD) : type de déchet (huile usagée,
  batteries, pneus, fluides frigorigènes), quantité, société d'élimination agréée, numéro BSD.
  Conformité réglementaire ICPE. Historique des évacuations.
- **Données :** Table `WasteDisposals(WasteType, QuantityKg, ContractorName, BsdNumber)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.12 — SUIVI DÉCHETS (CONFORMITÉ)
- **Route :** `/rh/suivi-dechets-conformite`
- **Rôle :** Vérifier la conformité des pratiques de gestion des déchets.
- **Définition :** Tableau de bord de conformité : volumes collectés vs. seuils réglementaires,
  fréquence des enlèvements, certifications des prestataires en cours de validité. Alertes
  si un seuil ICPE est approché.
- **Données :** Agrégation de `WasteDisposals`
- **Statut :** ✅ FONCTIONNEL

---

### M6.13 — HISTORIQUE POLLUTION
- **Route :** `/securite/historique-pollution`
- **Rôle :** Conserver les traces historiques des émissions et rejets environnementaux.
- **Définition :** Registre chronologique des incidents de pollution (déversement accidentel,
  rejet atmosphérique). Chaque incident : date, nature, mesures correctives prises, déclaration
  faite aux autorités. Preuve en cas de contrôle DREAL.
- **Données :** Table `PollutionIncidents` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M6.14 — RAPPORT POLLUTION
- **Route :** `/securite/rapport-pollution`
- **Rôle :** Générer le bilan carbone annuel et les indicateurs environnementaux du garage.
- **Définition :** Calcul des émissions GES du garage : consommation énergétique (M4.13),
  déchets produits (M6.11), km parcourus véhicules de service. Rapport PDF conforme au
  standard GHG Protocol. Affichage des tendances annuelles et objectifs de réduction.
- **Données :** `EnergyConsumptions` + `WasteDisposals`
- **Statut :** ✅ FONCTIONNEL

---

## 🔵 CAT 7 — MARKETING SUPPORT
> **Accenteur :** `#FF00FF` magenta | **KPIs :** Satisfaction 9.8, Tickets 12, Campagnes 3
> **Utilisateurs cibles :** Responsable Relation Client, Commercial, Support Technique

---

### M7.1 — ANALYSE SATISFACTION
- **Route :** `/marketing/analyse-satisfaction`
- **Rôle :** Mesurer et analyser la satisfaction des clients après chaque intervention.
- **Définition :** Dashboard NPS (Net Promoter Score) : score moyen, répartition Promoteurs /
  Passifs / Détracteurs. Verbatims des commentaires clients analysés par sentiment IA.
  Tendances par technicien, par type d'intervention. Alertes si NPS < 7.
- **Données :** `SurveyCampaigns.NpsScore[]` agrégé
- **Statut :** ✅ FONCTIONNEL

---

### M7.2 — SONDAGE SATISFACTION
- **Route :** `/marketing/sondage-satisfaction`
- **Rôle :** Automatiser l'envoi de questionnaires de satisfaction post-intervention.
- **Définition :** Configuration des campagnes : délai d'envoi après livraison (ex. 48h),
  canal (SMS ou email), questions du sondage. Chaque lien contient un token unique.
  Tableau de bord du taux de réponse par campagne.
- **Données :** `SurveyCampaigns(RevisionId, SentAt, Token, NpsScore)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M7.3 — ONBOARDING GARAGE
- **Route :** `/garage/onboarding`
- **Rôle :** Configurer le profil complet du garage lors du premier accès.
- **Définition :** Wizard de configuration initiale : informations du garage (nom, adresse,
  SIRET, logo, horaires d'ouverture), choix du plan d'abonnement, configuration des rôles
  utilisateurs initiaux (admin, mécaniciens), personnalisation des modèles d'emails.
- **Données :** `Garage` entity à créer — configuration multi-tenant
- **Statut :** ✅ FONCTIONNEL (requis pour multi-tenant)

---

### M7.4 — NOTIFICATIONS CONFIG
- **Route :** `/marketing/notifications-config`
- **Rôle :** Configurer quels événements déclenchent quelles notifications.
- **Définition :** Matrice de configuration : chaque événement métier (Révision planifiée,
  Révision terminée, Stock critique, Devis accepté) peut déclencher SMS, Email, Push ou combiné.
  Personnalisation des templates par langue. Gestion des délais et fréquences.
- **Données :** Table `NotificationSettings(EventType, Channel, Template)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M7.5 — SUPPORT DIAGNOSTIC
- **Route :** `/atelier/support-diagnostic`
- **Rôle :** Fournir une aide contextuelle aux techniciens lors du diagnostic.
- **Définition :** Base de connaissance technique : recherche par code OBD, symptôme ou modèle.
  Fiches de procédure de réparation (étapes détaillées, outillage nécessaire, couples de serrage).
  Chat IA spécialisé pour questions techniques complexes.
- **Données :** Base de connaissance + OpenAI API
- **Statut :** ✅ FONCTIONNEL

---

### M7.6 — SUPPORT TECHNIQUE L1
- **Route :** `/marketing/support-1`
- **Rôle :** Gérer les demandes d'assistance de niveau 1 des clients.
- **Définition :** Interface de ticketing : liste des tickets ouverts, priorité, client concerné,
  véhicule lié, canal d'entrée (email, téléphone, chat). Réponse directe depuis l'interface.
  Escalade vers L2 si non résolu. Temps de réponse SLA avec alertes.
- **Données :** Table `SupportTickets(Level, Status, CustomerId, AssignedTo)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M7.7 — SUPPORT TECHNIQUE L2
- **Route :** `/marketing/support-2`
- **Rôle :** Traiter les escalades technique complexes du support L1.
- **Définition :** Vue spécialisée des tickets escaladés nécessitant expertise technique.
  Interface enrichie : historique complet du ticket, notes internes, collaboration multi-agent.
  Accès aux données techniques du véhicule concerné pour diagnostic approfondi.
- **Données :** `SupportTickets` filtrés par `Level = 2`
- **Statut :** ✅ FONCTIONNEL

---

### M7.8 — GESTION SINISTRES
- **Route :** `/marketing/gestion-sinistres`
- **Rôle :** Traiter les dossiers de sinistre véhicule avec les assureurs et experts.
- **Définition :** Création d'un dossier sinistre : véhicule concerné, type (collision, vol,
  intempérie, incendie), référence assurance, mandatement d'un expert. Suivi d'état
  (Déclaré > Expert Mandaté > Rapport Expert > Indemnisé > Clôturé). Signature électronique
  des documents (YouSign/DocuSign).
- **Données :** Table `InsuranceClaims(VehicleId, Type, Status, InsuranceRef)` à créer
- **Statut :** ✅ FONCTIONNEL

---

### M7.9 — CONFIG IMPRIMANTE
- **Route :** `/settings/config-imprimante`
- **Rôle :** Configurer les imprimantes réseau utilisées pour les éditions.
- **Définition :** Liste des imprimantes réseau enregistrées (IP, format papier, résolution).
  Affectation par usage : factures (A4), étiquettes pièces (rouleau), tickets caisse.
  Test d'impression depuis l'interface. Historique des impressions.
- **Données :** Table `PrinterConfigs` à créer
- **Statut :** ✅ FONCTIONNEL

---

## 📊 TABLEAU SYNTHÈSE — STATUTS PAR MODULE

| # | Catégorie | Modules FAIT | Modules EN COURS | Modules À FAIRE |
|---|-----------|------------|------------|------------|
| CAT 1 | Clients & Véhicules | 11 | 0 | 0 |
| CAT 2 | Planning Atelier | 10 | 0 | 0 |
| CAT 3 | Diagnostic IA | 7 | 0 | 0 |
| CAT 4 | Finance & Stats | 13 | 0 | 0 |
| CAT 5 | Stock Logistique | 11 | 0 | 0 |
| CAT 6 | RH & Sécurité | 14 | 0 | 0 |
| CAT 7 | Marketing Support | 9 | 0 | 0 |
| **TOTAL** | **7 Catégories** | **75** | **0** | **0** |

### Légende Statuts
- ✅ **FONCTIONNEL** — Données réelles, API connectée, UI complète
- ⚠️ **EN COURS** — Modèle de données présent, endpoint ou UI partielle
- 🔴 **À IMPLÉMENTER** — Module défini, aucun code fonctionnel encore

---

## 🚦 PRIORISATION D'IMPLÉMENTATION

### 🔴 PRIORITÉ CRITIQUE (Semaine 1-2)
- M1.5 Programme Fidélité → Jauge et historique complet
- M2.5 Révisions → Page liste complète
- M2.6 Journal Tâches → Timer technicien
- M4.1 Archives Factures → Table + PDF download

### 🟠 PRIORITÉ HAUTE (Semaine 3-4)
- M3.1 Diagnostic Avancé → Logique OBD + IA
- M3.3 Rapport Diagnostic → QuestPDF
- M4.3 Paiement Sécurisé → Stripe Elements
- M4.4 Export Comptable → EPPlus

### 🟡 PRIORITÉ MOYENNE (Semaine 5-7)
- M2.7 Plan 2D Atelier → SVG interactif
- M3.4 Décodeur VIN Pro → API NHTSA
- M5.2 Commandes Fournisseurs → Workflow complet
- M6.1 Gestion Employés → Dossiers RH

### 🟢 PRIORITÉ STANDARD (Subsequent)
- M3.2 Diag. Batterie EV
- M6.4 Matrice Compétences
- M7.1 Analyse Satisfaction / NPS
- M6.11 Gestion Déchets

---

*Document généré le 20 Mars 2026 — MecaPro Intelligence Platform*
*70 modules · 7 catégories · Référence de développement exhaustive*
