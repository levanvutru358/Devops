# syntax=docker/dockerfile:1.7

###########
# BUILD
###########
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore (cache-friendly)
COPY EmoApi.csproj ./
RUN dotnet restore EmoApi.csproj

# Copy source & publish
COPY . .
RUN dotnet publish EmoApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

###########
# RUNTIME
###########
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY --from=build /app/publish ./

# Remove debug and xml docs to slim image
RUN find /app -name "*.pdb" -delete && \
    find /app -name "*.xml" -delete || true

EXPOSE 8080
ENTRYPOINT ["dotnet", "EmoApi.dll"]

