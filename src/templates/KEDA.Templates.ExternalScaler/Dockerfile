FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["templates/KEDA.Templates.ExternalScaler/KEDA.Templates.ExternalScaler.csproj", "templates/KEDA.Templates.ExternalScaler/"]
RUN dotnet restore "templates/KEDA.Templates.ExternalScaler/KEDA.Templates.ExternalScaler.csproj"
COPY . .
WORKDIR "/src/templates/KEDA.Templates.ExternalScaler"
RUN dotnet build "KEDA.Templates.ExternalScaler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KEDA.Templates.ExternalScaler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KEDA.Templates.ExternalScaler.dll"]
