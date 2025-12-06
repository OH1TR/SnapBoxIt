# Build stage for Node.js (Vite)
FROM node:20-alpine AS node-build
WORKDIR /app/ClientApp

# Copy package files
COPY ClientApp/package*.json ./

# Install dependencies
RUN npm ci

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
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "SnapBoxPWA.dll"]
