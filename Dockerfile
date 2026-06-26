FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EduCollab.Api/EduCollab.Api.csproj", "EduCollab.Api/"]
COPY ["EduCollab.Application/EduCollab.Application.csproj", "EduCollab.Application/"]
COPY ["EduCollab.Contracts/EduCollab.Contracts.csproj", "EduCollab.Contracts/"]
RUN dotnet restore "EduCollab.Api/EduCollab.Api.csproj"

COPY . .
WORKDIR "/src/EduCollab.Api"
RUN dotnet publish "EduCollab.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p App_Data/Content/scenes App_Data/Content/assets App_Data/UserPreferences App_Data/WorkspaceThumbnails

COPY --from=build /app/publish .
COPY EduCollab.Api/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "EduCollab.Api.dll"]
