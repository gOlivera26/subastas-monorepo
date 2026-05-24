FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BackendTemplate.API/BackendTemplate.API.csproj", "BackendTemplate.API/"]
COPY ["BackendTemplate.Application/BackendTemplate.Application.csproj", "BackendTemplate.Application/"]
COPY ["BackendTemplate.Domain/BackendTemplate.Domain.csproj", "BackendTemplate.Domain/"]

RUN dotnet restore "BackendTemplate.API/BackendTemplate.API.csproj"

COPY . .

WORKDIR "/src/BackendTemplate.API"
RUN dotnet build "BackendTemplate.API.csproj" -c Release -o /app/build
RUN dotnet publish "BackendTemplate.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BackendTemplate.API.dll"]
