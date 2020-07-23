#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build-env
WORKDIR /app

ENV ASPNETCORE_URLS http://*:5002
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Gengar/Gengar.csproj", "Gengar/"]
RUN dotnet restore "Gengar/Gengar.csproj"
COPY . .
WORKDIR "/src/Gengar"
RUN dotnet build "Gengar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gengar.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gengar.dll"]