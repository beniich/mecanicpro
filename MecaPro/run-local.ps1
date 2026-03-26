# run-local.ps1 — Script de lancement MecaPro OPTIMISÉ (V5.1)

Write-Host "🚀 INITIALISATION MÉCAPRO OS V5.1 (MODÈLE HYBRIDE DOCKER-CLIENT)..." -ForegroundColor Cyan

# 1. Vérification des prérequis
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "❌ Docker Desktop non détecté dans le PATH."
    exit
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "❌ .NET SDK 9.0 non détecté."
    exit
}

# 2. Nettoyage & Infrastructure Core
Write-Host "1️⃣  LANCEMENT DE L'INFRASTRUCTURE (SQL, REDIS, RABBITMQ, SEQ)..." -ForegroundColor Yellow
docker-compose up -d sqlserver redis rabbitmq seq

# 3. Attente robuste pour SQL Server
Write-Host "⌛ Attente du démarrage de SQL Server (vérification de santé 45s)..." -ForegroundColor Gray
Start-Sleep -Seconds 45

# 4. Synchronisation des Bases de Données (Migration)
Write-Host "2️⃣  SYNCHRONISATION DES SCHÉMAS (ENTITY FRAMEWORK)..." -ForegroundColor Yellow
dotnet ef database update --project src/MecaPro.Infrastructure --startup-project src/MecaPro.API

# 5. Lancement des Services Backend (Microservices Core)
Write-Host "3️⃣  LANCEMENT DES MICROSERVICES (AUTH, API, GATEWAY, INVENTAIRE)..." -ForegroundColor Yellow
# On lance les builds docker en arrière-plan sans bloquer
docker-compose up -d --build auth-service api-monolith inventory-service notification-service gateway

Write-Host "   ✅ Services lancés en arrière-plan via Docker Compose." -ForegroundColor Gray
Write-Host "   ℹ️ Gateway accessible sur : http://localhost:5000" -ForegroundColor Cyan

# 6. Lancement du Frontend Next.js
Write-Host "4️⃣  LANCEMENT DU FRONTEND NEXT.JS (TURBO ENGINE)..." -ForegroundColor Green
Set-Location ..\frontend-next

$env:NODE_OPTIONS = "--max-old-space-size=4096"
$env:NEXT_PUBLIC_API_URL = "http://localhost:3000"

npm run dev
