# run-local.ps1 — Script de lancement MecaPro pour environnement Windows

Write-Host "🚀 Préparation du lancement de MecaPro..." -ForegroundColor Cyan

# run-local.ps1 — Script de lancement MecaPro pour environnement Windows

$DockerPath = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
$DockerComposePath = "C:\Program Files\Docker\Docker\resources\bin\docker-compose.exe"

# 1. Vérification des prérequis
if (!(Test-Path $DockerPath)) {
    # Fallback to PATH search
    if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "❌ Docker n'est pas installé ou n'est pas dans votre PATH. Veuillez installer Docker Desktop."
        exit
    }
    $DockerPath = "docker"
    $DockerComposePath = "docker-compose"
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "❌ .NET SDK n'est pas installé."
    exit
}

# 2. Lancement de l'infrastructure
Write-Host "1️⃣  Lancement de l'infrastructure (SQL Server, Redis, Seq)..." -ForegroundColor Yellow
& $DockerComposePath up -d sqlserver redis seq

# 3. Attente du démarrage de SQL Server
Write-Host "⌛ Attente du démarrage de SQL Server (30s)..."
Start-Sleep -Seconds 30

# 4. Application des migrations
Write-Host "2️⃣  Mise à jour de la base de données..." -ForegroundColor Yellow
dotnet ef database update --project src/MecaPro.Infrastructure --startup-project src/MecaPro.API

# 5. Lancement de l'API
Write-Host "3️⃣  Lancement de l'API (HTTPS: https://localhost:5001)..." -ForegroundColor Green
Start-Process dotnet "run --project src/MecaPro.API"

# 6. Lancement du Frontend Blazor
Write-Host "4️⃣  Lancement de l'application Blazor (http://localhost:5200)..." -ForegroundColor Green
dotnet run --project src/MecaPro.Blazor
