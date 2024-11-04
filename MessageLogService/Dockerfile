# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /MessageLogService
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /MessageLogService

# Copy project files and restore dependencies
COPY MessageLogService.csproj .
RUN dotnet restore

# Copy the rest of the files and build the project
COPY . .
RUN dotnet build "MessageLogService.csproj" -c Release -o /MessageLogService/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MessageLogService.csproj" -c Release -o /MessageLogService/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /MessageLogService/publish .
ENTRYPOINT ["dotnet", "MessageLogService.dll"]