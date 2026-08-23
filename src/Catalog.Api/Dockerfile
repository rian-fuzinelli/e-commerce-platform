# Build multi-stage: a imagem final não carrega o SDK (~800 MB), só o runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar só os csproj primeiro faz o restore ficar em cache:
# alterar código-fonte não invalida a camada de restore das dependências.
COPY Directory.Build.props ./
COPY src/Catalog.Api/Catalog.Api.csproj src/Catalog.Api/
RUN dotnet restore src/Catalog.Api/Catalog.Api.csproj

COPY . .
RUN dotnet publish src/Catalog.Api/Catalog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Rodar como usuário não-root: se o container for comprometido,
# o atacante não tem privilégio administrativo dentro dele.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
