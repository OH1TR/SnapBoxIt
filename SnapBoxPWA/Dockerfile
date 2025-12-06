# Build stage for Node.js (Vite)
FROM node:20-alpine AS node-build
WORKDIR /app/ClientApp

# Copy package files
COPY ClientApp/package*.json ./

# Install dependencies
RUN npm install

# Copy client app source
COPY ClientApp/ ./

# Build the Vite app
RUN npm run build

# Build stage for .NET
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY SnapBoxPWA.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Copy the built Vite app from node-build stage
COPY --from=node-build /app/ClientApp/dist ./wwwroot

# Build the .NET application
RUN doten
