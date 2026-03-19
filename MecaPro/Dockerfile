# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and project files first to cache restore
COPY MecaPro.sln .
COPY src/MecaPro.Domain/MecaPro.Domain.csproj src/MecaPro.Domain/
COPY src/MecaPro.Application/MecaPro.Application.csproj src/MecaPro.Application/
COPY src/MecaPro.Infrastructure/MecaPro.Infrastructure.csproj src/MecaPro.Infrastructure/
COPY src/MecaPro.API/MecaPro.API.csproj src/MecaPro.API/
COPY src/MecaPro.Blazor/MecaPro.Blazor.csproj src/MecaPro.Blazor/
COPY tests/MecaPro.Tests.Unit/MecaPro.Tests.Unit.csproj tests/MecaPro.Tests.Unit/
COPY tests/MecaPro.Tests.Integration/MecaPro.Tests.Integration.csproj tests/MecaPro.Tests.Integration/

RUN dotnet restore

# Copy all source code & build
COPY . .
WORKDIR /source/src/MecaPro.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Install dependencies for SkiaSharp (PDF generation on Linux)
RUN apt-get update && apt-get install -y libfontconfig1 && rm -rf /var/lib/apt/lists/*

EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MecaPro.API.dll"]
