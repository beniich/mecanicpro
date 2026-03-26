# MecaPro OS V5.0 — Architecture & Fonctionnement

L'écosystème **MecaPro OS V5.0** est conçu comme une plateforme hybride ultra-performante alliant une interface "Cyber-Atelier" moderne, un backend microservices robuste et une intelligence artificielle visuelle avancée.

---

## 🏗️ 1. Architecture Globale (Macro)

L'application repose sur un modèle à trois couches principales :

### **A. Frontend (L'Interface OS)**
*   **Technologie :** Next.js 14+ (App Router), TypeScript, Tailwind CSS.
*   **Rôle :** Offrir une expérience "OS" fluide. Contrairement à un site web classique, il fonctionne avec un `OsShell` (coquille système) qui gère la navigation, les notifications et le "look & feel" sans recharger l'application entière.
*   **Localisation :** `./frontend-next/`

### **B. Services Backend (Le Cerveau métier)**
*   **Technologie :** C#.NET 9.0 (ASP.NET Core).
*   **Structure :** Microservices (API Gateway + modules spécialisés : Auth, Inventory, Notifications, etc.).
*   **Base de données :** SQL Server (Persistance) et Redis (Cache/Temps réel).
*   **Localisation :** `./MecaPro/`

### **C. AI Sub-Agent (L'Expert Technique)**
*   **Technologie :** Node.js, Express, Anthropic Claude 3.5 Sonnet (Vision).
*   **Rôle :** Traitement des diagnostics photo, analyse prédictive et assistance technique via chat. Il communique de façon synchrone avec le backend C#.
*   **Localisation :** `./sub-agent/`

---

## 🦴 2. Squelette du Frontend (V5.0)

La structure du code Next.js est optimisée pour la vitesse et l'homogénéité :

*   **`styles/globals.css` :** Le système de design centralisé. Toutes les couleurs (Orange MecaPro, Cyan Cyber), les polices (Rajdhani) et les animations (Laser Sweep) sont définies ici via des variables CSS.
*   **`components/layout/OsShell.tsx` :** Le cadre de l'application. Il contient la barre supérieure, la barre latérale gauche (Quick Tiles) et la barre d'état système en bas.
*   **`app/all-pages.tsx` :** Un registre centralisé de composants. Cela permet de développer de nouveaux modules à une vitesse record tout en garantissant qu'ils utilisent tous les mêmes éléments UI (Cartes, Tableaux, Badges).
*   **`app/[module]/page.tsx` :** Chaque route Next.js n'est qu'un point d'entrée léger qui appelle la logique définie dans `all-pages.tsx`.

---

## 🔄 3. Comment l'application fonctionne ?

### **Flux d'Exemple : Le Diagnostic IA**
1.  **Action Utilisateur :** Le mécanicien ouvre le module `/diagnostics` et téléverse une photo d'un moteur ou d'un dommage.
2.  **Frontend (Next.js) :** Envoie la photo au backend C# via une route API standard (`POST /api/v1/ai-agent/vision`).
3.  **Backend (C#) :** Reçoit la requête, vérifie l'autorisation de l'utilisateur, puis agit comme un "proxy" intelligent vers l'IA Sub-Agent.
4.  **AI Sub-Agent (Node.js) :** Analyse l'image avec **Claude 3.5 Sonnet**. Il identifie la pièce, détecte les anomalies (fuites, usure) et renvoie un rapport JSON structuré.
5.  **Affichage :** Le backend renvoie le rapport au frontend qui l'affiche instantanément avec une jauge de confiance IA.

### **Flux d'Exemple : Gestion de Stock**
1.  **Action Utilisateur :** Le magasinier reçoit des pièces et les scanne.
2.  **API Microservices :** Le service `Inventory` met à jour la base SQL Server.
3.  **Real-time Update :** Une notification est générée via RabbitMQ ou SignalR pour mettre à jour les jauges dans le `Hub` de tous les écrans de l'atelier.

---

## 🚀 4. Pourquoi cette structure ?

*   **Modularité :** On peut ajouter un 21ème module simplement en ajoutant une fonction dans `all-pages.tsx`.
*   **Vitesse :** L'interface est ultra-légère grâce au rendu côté serveur (SSR) et aux composants optimisés.
*   **Intelligence :** L'intégration directe d'un agent IA permet d'automatiser des tâches qui demandaient auparavant une expertise humaine manuelle.
