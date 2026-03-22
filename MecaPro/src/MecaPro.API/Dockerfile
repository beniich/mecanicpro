FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore MecaPro.sln

WORKDIR /source/src/MecaPro.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y libfontconfig1 && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENTRYPOINT ["dotnet", "MecaPro.API.dll"]
