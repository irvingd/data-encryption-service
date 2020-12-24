#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["DataEncryptionServiceWebApi/DataEncryptionServiceWebApi.csproj", "DataEncryptionServiceWebApi/"]
COPY ["DataEncryptionService.Abstractions/DataEncryptionService.Abstractions.csproj", "DataEncryptionService.Abstractions/"]
COPY ["DataEncryptionService.Core/DataEncryptionService.Core.csproj", "DataEncryptionService.Core/"]
COPY ["DataEncryptionService.Integration.MongoDB/DataEncryptionService.Integration.MongoDB.csproj", "DataEncryptionService.Integration.MongoDB/"]
COPY ["DataEncryptionService.Integration.Vault/DataEncryptionService.Integration.Vault.csproj", "DataEncryptionService.Integration.Vault/"]
COPY ["../VaultSharp-master/src/VaultSharp/VaultSharp.csproj", "../VaultSharp-master/src/VaultSharp/"]
RUN dotnet restore "DataEncryptionServiceWebApi/DataEncryptionServiceWebApi.csproj"
COPY . .
WORKDIR "/src/DataEncryptionServiceWebApi"
RUN dotnet build "DataEncryptionServiceWebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataEncryptionServiceWebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataEncryptionServiceWebApi.dll"]