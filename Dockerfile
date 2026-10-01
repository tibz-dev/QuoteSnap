FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY QuoteSnap.Domain/QuoteSnap.Domain.csproj QuoteSnap.Domain/
COPY QuoteSnap.Application/QuoteSnap.Application.csproj QuoteSnap.Application/
COPY QuoteSnap.Infrastructure/QuoteSnap.Infrastructure.csproj QuoteSnap.Infrastructure/
COPY QuoteSnap.Api/QuoteSnap.Api.csproj QuoteSnap.Api/

RUN dotnet restore QuoteSnap.Api/QuoteSnap.Api.csproj

COPY . .

RUN dotnet publish QuoteSnap.Api/QuoteSnap.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "QuoteSnap.Api.dll"]
