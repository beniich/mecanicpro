# Rapport d'Audit & Corrections de Configuration

## Problèmes identifiés et résolus

| Composant | Problème | Solution Appliquée |
| :--- | :--- | :--- |
| **CORS** | Requêtes API bloquées depuis Blazor | Ajout d'une politique CORS dans `Program.cs` de l'API. |
| **API URL** | Blazor pointait vers lui-même | Configuration de `ApiBaseUrl: https://localhost:5001` dans `appsettings.json`. |
| **Authentification** | Clés JWT invalides (crash au démarrage) | Génération et injection de clés RSA Publiques/Privées valides. |
| **Base de Données** | Erreurs de mapping EF Core (Address, Email, etc.) | Ajout de constructeurs sans paramètres et configuration `OwnsOne`. |
| **Environnement** | Host asymétrique | Passage de `ASPNETCORE_ENVIRONMENT` à `Development` partout. |

## Vérification Finale
- **API :** Répond positivement sur le port 5001.
- **Base de données :** Migrations appliquées, schéma OK.
- **Blazor :** Chargement initial réussi sur le port 5200.
