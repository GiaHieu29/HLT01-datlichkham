# Bước 1: build bằng .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ ./src/
RUN dotnet restore src/Hlt.Api/Hlt.Api.csproj
RUN dotnet publish src/Hlt.Api/Hlt.Api.csproj -c Release -o /app --no-restore

# Bước 2: chạy bằng runtime gọn nhẹ (lắng nghe cổng 8080)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Hlt.Api.dll"]