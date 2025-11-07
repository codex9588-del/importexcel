# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExcelImporter.csproj", "."]
RUN dotnet restore "./ExcelImporter.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ExcelImporter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExcelImporter.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExcelImporter.dll"]