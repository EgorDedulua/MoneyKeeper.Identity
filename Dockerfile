FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MoneyKeeper.Identity/MoneyKeeper.Identity.csproj", "MoneyKeeper.Identity/"]
COPY ["src/MoneyKeeper.Identity.Application/MoneyKeeper.Identity.Application.csproj", "MoneyKeeper.Identity.Application/"]
COPY ["src/MoneyKeeper.Identity.Core/MoneyKeeper.Identity.Core.csproj", "MoneyKeeper.Identity.Core/"]
COPY ["src/MoneyKeeper.Identity.Infrastructure/MoneyKeeper.Identity.Infrastructure.csproj", "MoneyKeeper.Identity.Infrastructure/"]
RUN dotnet restore "MoneyKeeper.Identity/MoneyKeeper.Identity.csproj"
COPY src/ /src/
WORKDIR /src/MoneyKeeper.Identity
RUN dotnet build "./MoneyKeeper.Identity.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MoneyKeeper.Identity.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyKeeper.Identity.dll"]