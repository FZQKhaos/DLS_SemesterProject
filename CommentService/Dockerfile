# Build stage: uses the full SDK image so it can compile the project.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy just the csproj first so Docker can cache the (slow) restore step
# and skip it on rebuilds where only .cs files changed.
COPY CommentService/CommentService.csproj CommentService/
RUN dotnet restore CommentService/CommentService.csproj

COPY CommentService/ CommentService/
RUN dotnet publish CommentService/CommentService.csproj -c Release -o /app --no-restore

# Runtime stage: only the ASP.NET runtime, not the whole SDK - much smaller image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CommentService.dll"]
