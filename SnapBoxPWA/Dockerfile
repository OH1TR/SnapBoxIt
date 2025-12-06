# Build stage for Node.js (Vite)
FROM node:20-alpine AS node-build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy all source files
COPY . ./

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
COPY --from=node-build /app/wwwroot ./wwwroot

# Build and publish the .NET application
# Set BuildingInsideDocker=true to skip npm tasks since frontend is already built
RUN dotnet build -c Release --no-restore /p:BuildingInsideDocker=true
RUN dotnet publish -c Release -o /app/publish --no-build /p:BuildingInsideDocker=true

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
