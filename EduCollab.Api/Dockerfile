# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EduCollab.Api/EduCollab.Api.csproj", "EduCollab.Api/"]
COPY ["EduCollab.Application/EduCollab.Application.csproj", "EduCollab.Application/"]
COPY ["EduCollab.Contracts/EduCollab.Contracts.csproj", "EduCollab.Contracts/"]
COPY ["EduCollab.Infrastructure/EduCollab.Infrastructure.csproj", "EduCollab.Infrastructure/"]
RUN dotnet restore "./EduCollab.Api/EduCollab.Api.csproj"
COPY . .
WORKDIR "/src/EduCollab.Api"
RUN dotnet build "./EduCollab.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EduCollab.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apt-get update \
    && apt-get install -y --no-install-recommends gosu \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p App_Data/Content/scenes App_Data/Content/assets App_Data/UserPreferences App_Data/WorkspaceThumbnails

COPY --from=publish /app/publish .
COPY EduCollab.Api/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "EduCollab.Api.dll"]