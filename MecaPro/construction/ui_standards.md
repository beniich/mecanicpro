# Premium UI Design Standards

Ce document définit le système de design "Premium" implémenté dans MecaPro (Mars 2025). 

## Palette de Couleurs (Inspirée par Cyber/Industriel Premium)

Les couleurs sont configurées via le CDN Tailwind et surchargées dans `index.html`.

- **Fond (Background):** `#0c0e12` (Noir profond, mat)
- **Surfaces (Cards):** `#111318` (Gris anthracite)
- **Accent Primaire:** `#f5a623` (Orange industriel / Or) - Utilisé pour MECAPRO Logo et actions critiques.
- **Accent Secondaire:** `#524534` (Bronze/Brun métallique) - Utilisé pour les bordures et séparateurs.
- **Texte:** `#e2e2e8` (Gris très clair / Blanc cassé)

## Typographie (Google Fonts)

1. **Headline (Titres):** `Bebas Neue` - Utilisé pour les titres de section et le logo (Majuscules, espacement large).
2. **Body (Corps):** `DM Sans` - Utilisé pour la lecture générale, interface utilisateur.
3. **Technical (Données):** `JetBrains Mono` - Utilisé pour les VIN, dates, montants et compteurs.

## Composants de Mise en Page

### 1. Bento Grid (Tableau de Bord / Hero)
Utilisez des proportions asymétriques pour les sections KPI.
Exemple dans `Revisions.razor`: `grid-cols-1 md:grid-cols-3` avec `md:col-span-2` pour le contenu principal.

### 2. Header Unifié
Le header est fixe (`sticky top-0`) avec une bordure bronze ultra-fine (`border-[#524534]/15`).

### 3. Log Technique (Tables)
Les tables doivent être épurées, sans lignes verticales, avec des en-têtes en `JetBrains Mono` (font-bold, tracking-[0.2em]).

## Micro-Animations
Toutes les pages utilisent `animate-in fade-in duration-700` pour une transition fluide lors de la navigation.

## Navigation Mobile
Une barre de navigation basse est implémentée pour l'usage type "en atelier" sur smartphone/tablette, avec des icônes Material Symbols contrastées.
