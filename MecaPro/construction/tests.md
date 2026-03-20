# Structure et Stratégie de Test

Le projet utilise **xUnit** pour les tests automatisés, avec une focalisation sur les tests unitaires et d'intégration.

## 🧪 Catégories de Tests

### 1. Tests Unitaires (`tests/MecaPro.Tests.Unit`)
- **Objectif :** Vérifier la logique métier isolée (Domaine et Application).
- **Outils :** `xUnit`, `Moq`, `FluentAssertions`.
- **Fichiers clés :** `UnitTests.cs`.

### 2. Tests d'Intégration (Prévus)
- **Objectif :** Vérifier la persistance (DB) et la communication entre services.
- **Outils :** `WebApplicationFactory`, `Testcontainers` (SQL Server).

### 3. Tests de l'API (Manual / Smoke)
- **Objectif :** Vérifier que les endpoints répondent correctement.
- **Validation actuelle :** Utilisation de `curl.exe` pour tester la connectivité.

## 🚀 Exécution des tests
Pour lancer tous les tests de la solution :
```bash
dotnet test
```

## État actuel
Les tests unitaires ont été mis à jour après la migration vers .NET 9 pour résoudre les problèmes de références de namespaces.
