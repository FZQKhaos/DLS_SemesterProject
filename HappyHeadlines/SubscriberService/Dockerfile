FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SubscriberService/SubscriberService.csproj", "SubscriberService/"]
COPY ["Events/Events.csproj", "Events/"]
RUN dotnet restore "SubscriberService/SubscriberService.csproj"
COPY . .
RUN dotnet publish "SubscriberService/SubscriberService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubscriberService.dll"]
