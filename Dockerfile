# Use official .NET 8 runtime as a base image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./weather-server ./weather-server
WORKDIR /src/weather-server
RUN dotnet publish weather-server.csproj -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ./weather-server/sample.txt ./sample.txt
ENTRYPOINT ["dotnet", "weather-server.dll"]
