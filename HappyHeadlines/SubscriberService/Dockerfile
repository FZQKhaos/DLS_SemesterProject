FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SubscriberService/SubscriberService.csproj", "SubscriberService/"]
COPY ["Events/Events.csproj", "Events/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
COPY ["FeatureToggles/FeatureToggles.csproj", "FeatureToggles/"]
RUN dotnet restore "SubscriberService/SubscriberService.csproj"
COPY . .
WORKDIR "/src/SubscriberService"
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SubscriberService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubscriberService.dll"]
