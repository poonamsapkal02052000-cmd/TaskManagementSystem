# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskManager.Api/TaskManager.Api.csproj TaskManager.Api/
RUN dotnet restore TaskManager.Api/TaskManager.Api.csproj

COPY TaskManager.Api/ TaskManager.Api/
RUN dotnet publish TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
