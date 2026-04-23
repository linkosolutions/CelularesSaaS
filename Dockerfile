FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/CelularesSaaS.Domain/CelularesSaaS.Domain.csproj", "src/CelularesSaaS.Domain/"]
COPY ["src/CelularesSaaS.Application/CelularesSaaS.Application.csproj", "src/CelularesSaaS.Application/"]
COPY ["src/CelularesSaaS.Infrastructure/CelularesSaaS.Infrastructure.csproj", "src/CelularesSaaS.Infrastructure/"]
COPY ["src/CelularesSaaS.Api/CelularesSaaS.Api.csproj", "src/CelularesSaaS.Api/"]

COPY nuget.config .
RUN dotnet restore "src/CelularesSaaS.Api/CelularesSaaS.Api.csproj"

COPY . .
RUN dotnet build "src/CelularesSaaS.Api/CelularesSaaS.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/CelularesSaaS.Api/CelularesSaaS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:$PORT

ENTRYPOINT ["dotnet", "CelularesSaaS.Api.dll"]