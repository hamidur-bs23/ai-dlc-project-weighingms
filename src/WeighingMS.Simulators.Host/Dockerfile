# Build context: repository root (needs global.json and Directory.*.props).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/WeighingMS.Simulators.Host/WeighingMS.Simulators.Host.csproj src/WeighingMS.Simulators.Host/
RUN dotnet restore src/WeighingMS.Simulators.Host/WeighingMS.Simulators.Host.csproj
COPY src/WeighingMS.Simulators.Host/ src/WeighingMS.Simulators.Host/
RUN dotnet publish src/WeighingMS.Simulators.Host/WeighingMS.Simulators.Host.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 5090
ENTRYPOINT ["dotnet", "WeighingMS.Simulators.Host.dll"]
