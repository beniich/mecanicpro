#!/bin/bash
# Script d'installation automatique pour Vercel / Netlify
echo "Téléchargement du SDK .NET 9..."
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x ./dotnet-install.sh

# Installer .NET 9.0
./dotnet-install.sh --channel 9.0

# Configurer les variables d'environnement pour utiliser dotnet
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

echo "Version de .NET installée :"
dotnet --version

echo "Compilation de l'application Blazor en mode Release..."
# Compiler le frontend
dotnet publish MecaPro/src/MecaPro.Blazor/MecaPro.Blazor.csproj -c Release -o release

echo "Build terminé avec succès !"
