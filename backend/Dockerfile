FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props backend/Directory.Build.props
COPY backend/Directory.Packages.props backend/Directory.Packages.props
COPY backend/src/Planora.Domain/Planora.Domain.csproj backend/src/Planora.Domain/
COPY backend/src/Planora.Application/Planora.Application.csproj backend/src/Planora.Application/
COPY backend/src/Planora.Infrastructure/Planora.Infrastructure.csproj backend/src/Planora.Infrastructure/
COPY backend/src/Planora.Api/Planora.Api.csproj backend/src/Planora.Api/
RUN dotnet restore backend/src/Planora.Api/Planora.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/Planora.Api/Planora.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Planora.Api.dll"]
