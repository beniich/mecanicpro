# Plan d'Exécution et Suivi

## Phase de Configuration
- [x] Corriger l'URL de l'API dans le frontend (Blazor `appsettings.json` + `Program.cs`)
- [x] Ajouter le middleware CORS dans l'API (`MecaPro.API/Program.cs`)
- [x] Corriger la configuration des clés JWT dans l'API (`appsettings.json`)

## Phase de Lancement
- [x] Installer `dotnet-ef` v9.0.0 (Global Tool)
- [x] Corriger les entités du Domaine pour compatibilité EF Core
- [x] Créer et appliquer les migrations de base de données (`MecaProDb`)
- [x] Nettoyer les ports réseau (5200, 5001)
- [x] Lancer l'infrastructure (Docker Compose)
- [x] Démarrer l'API et Blazor simultanément via `run-local.ps1`

## Prochaines étapes suggérées
- [ ] Implémenter le rafraîchissement des tokens (RefreshToken)
- [ ] Étendre la couverture des tests d'intégration pour le module Billing
- [ ] Configurer le déploiement CI/CD via GitHub Actions
