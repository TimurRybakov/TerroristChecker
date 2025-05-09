#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TerroristChecker.Api/TerroristChecker.Api.csproj", "TerroristChecker.Api/"]
COPY ["TerroristChecker.Application/TerroristChecker.Application.csproj", "TerroristChecker.Application/"]
COPY ["TerroristChecker.Domain/TerroristChecker.Domain.csproj", "TerroristChecker.Domain/"]
COPY ["TerroristChecker.InfrastructureServices/TerroristChecker.InfrastructureServices.csproj", "TerroristChecker.InfrastructureServices/"]
COPY ["TerroristChecker.Persistence/TerroristChecker.Persistence.csproj", "TerroristChecker.Persistence/"]
RUN dotnet restore "./TerroristChecker.Api/TerroristChecker.Api.csproj"
COPY . .
WORKDIR "/src/TerroristChecker.Api"
RUN dotnet build "./TerroristChecker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TerroristChecker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TerroristChecker.Api.dll"]
#dotnet  ./bin/Debug/net8.0/TerroristChecker.Api.dll