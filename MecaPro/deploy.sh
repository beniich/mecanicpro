#!/bin/bash
# deploy.sh — Script de déploiement pour Serveur Dédié (Production)

# Couleurs pour l'output
GREEN='\03---5[0;32m'
YELLOW='\03---5[1;33m'
NC='\03---5[0m' # No Color

echo -e "${YELLOW}🚀 Démarrage du déploiement de MecaPro...${NC}"

# Variables
APP_DIR="/var/www/mecapro"
GIT_BRANCH="main"

echo -e "1️⃣  Mise à jour du code source depuis Git..."
cd $APP_DIR || exit
git pull origin $GIT_BRANCH

echo -e "2️⃣  Rechargement des conteneurs via Docker Compose..."
# Stop l'ancien, recrée les images et redémarre
docker-compose --env-file .env.production up -d --build

echo -e "3️⃣  Mise à jour de la base de données (Entity Framework Migrations)..."
# Exécute la migration dans le conteneur API en cours d'exécution
docker exec mecapro_api dotnet ef database update --project src/MecaPro.Infrastructure --startup-project src/MecaPro.API

echo -e "4️⃣  Nettoyage des anciennes images Docker (Prune)..."
docker image prune -f

echo -e "${GREEN}✅ Déploiement terminé avec succès !${NC}"
echo -e "L'API est disponible sur : http://localhost:5000 (derrière Nginx)"
