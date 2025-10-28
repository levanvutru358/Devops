# syntax=docker/dockerfile:1.7

################
# BUILD STAGE
################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution & project files (cache-friendly restore)
COPY Webshop.sln ./
COPY Webshop.Application/Webshop.Application.csproj ./Webshop.Application/
COPY Webshop.Common/Webshop.Common.csproj           ./Webshop.Common/
COPY Webshop.Domain/Webshop.Domain.csproj           ./Webshop.Domain/
COPY Webshop.Infrastructure/Webshop.Infrastructure.csproj ./Webshop.Infrastructure/
COPY Webshop.Api/Webshop.Api.csproj                 ./Webshop.Api/

# Restore dependencies
RUN dotnet restore Webshop.sln

# Copy the rest of the source code
COPY . .

# Publish (skip restore to reuse cache)
RUN dotnet publish Webshop.Api/Webshop.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

################
# RUNTIME STAGE
################
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copy published output from build stage
COPY --from=build /app/publish ./

# Remove unnecessary debug & doc files
RUN find /app -name "*.pdb" -delete && \
    find /app -name "*.xml" -delete || true

EXPOSE 8080

ENTRYPOINT ["dotnet", "Webshop.Api.dll"]
