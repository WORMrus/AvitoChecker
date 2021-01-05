#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AvitoChecker/AvitoChecker.csproj", "AvitoChecker/"]
RUN dotnet restore "AvitoChecker/AvitoChecker.csproj"
COPY . .
WORKDIR "/src/AvitoChecker"
RUN dotnet build "AvitoChecker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AvitoChecker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AvitoChecker.dll"]