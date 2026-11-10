# Build context: repository root (needs global.json, Directory.*.props and the npm workspace files).
# Targets: migrator (EF migrations bundle, runs once) and api (the default: the Api serving the Angular build).
FROM node:26.13.0-bookworm-slim AS web
WORKDIR /repo
# The image ships npm 11; the workspace requires npm 12 (engine-strict, tech-stack).
RUN npm install --global npm@12.2.1
COPY package.json package-lock.json .npmrc ./
COPY web/package.json web/
COPY playwright/package.json playwright/
RUN npm ci --workspace web
COPY web/ web/
RUN npm run build --workspace web

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY .config/ .config/
COPY src/ src/
RUN dotnet tool restore
RUN dotnet publish src/WeighingMS.Api/WeighingMS.Api.csproj -c Release -o /app/api
RUN dotnet ef migrations bundle --project src/WeighingMS.Infrastructure --startup-project src/WeighingMS.Infrastructure --configuration Release --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /app/efbundle ./efbundle
ENTRYPOINT ["sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__Db\""]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/api .
COPY --from=web /repo/web/dist/web/browser ./wwwroot
EXPOSE 5080
ENTRYPOINT ["dotnet", "WeighingMS.Api.dll"]
