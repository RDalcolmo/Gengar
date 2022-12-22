FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Gengar/Gengar.csproj", "Gengar/"]
RUN dotnet restore "Gengar/Gengar.csproj"
COPY . .
WORKDIR "/src/Gengar"
RUN dotnet build "Gengar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gengar.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gengar.dll"]