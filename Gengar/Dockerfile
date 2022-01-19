#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS build
WORKDIR /app


ENV TZ=America/New_York
ENV TIME_ZONE=America/New_York
ENV ASPNETCORE_URLS http://*:5002
EXPOSE 5002

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0.101-bullseye-slim
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "Gengar.dll"]