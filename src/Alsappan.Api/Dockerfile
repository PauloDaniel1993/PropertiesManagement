# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Alsappan.slnx ./
COPY src/Alsappan.Domain/Alsappan.Domain.csproj src/Alsappan.Domain/
COPY src/Alsappan.Application/Alsappan.Application.csproj src/Alsappan.Application/
COPY src/Alsappan.Infrastructure/Alsappan.Infrastructure.csproj src/Alsappan.Infrastructure/
COPY src/Alsappan.Api/Alsappan.Api.csproj src/Alsappan.Api/

RUN dotnet restore src/Alsappan.Api/Alsappan.Api.csproj

COPY src src
RUN dotnet publish src/Alsappan.Api/Alsappan.Api.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish \
  /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
  DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Alsappan.Api.dll"]
