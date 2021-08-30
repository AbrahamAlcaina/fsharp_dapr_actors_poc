# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS base
WORKDIR /app

RUN dotnet new tool-manifest
RUN dotnet tool install Paket
RUN dotnet tool restore

COPY ./.paket ./.paket
COPY paket* ./
COPY ./dapr.sln  ./
COPY ./projects/ ./projects
RUN dotnet paket install  
RUN dotnet paket restore  

# PUB 
FROM base AS pub-builder
WORKDIR /app/projects/pub
RUN dotnet tool restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS pub-app
WORKDIR /app
COPY --from=pub-builder /app/projects/pub/out .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Pub.App.dll"]

# SUB
FROM base AS sub-builder
WORKDIR /app/projects/sub
RUN dotnet tool restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS sub-app
WORKDIR /app
COPY --from=sub-builder /app/projects/sub/out .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Sub.App.dll"]