### Ths file is used to build the docker image for the MessageLogService project ###
#
## cmd to build the image and push it to the registry:
# docker buildx build --pull -t messagelogservice
#
## To build the image for linux/amd64 for kubernetes cluster
# docker build --platform linux/amd64 -t eshopregistry.azurecr.io/messagelogservice:latest .
#
## To push the image to the registry
# docker tag messagelogservice eshopregistry.azurecr.io/messagelogservice:latest
# az acr login --name eshopregistry
# docker push eshopregistry.azurecr.io/messagelogservice:latest

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



