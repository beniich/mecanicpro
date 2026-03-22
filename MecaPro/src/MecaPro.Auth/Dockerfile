# Use .NET 9 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy everything and restore
COPY . .
RUN dotnet restore MecaPro.sln

# Build and Publish
WORKDIR /source/src/MecaPro.Auth
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MecaPro.Auth.dll"]
