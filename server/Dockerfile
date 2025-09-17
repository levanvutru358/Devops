# syntax=docker/dockerfile:1.7

###########
# BUILD
###########
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore trước (cache friendly)
COPY EmoApi.csproj ./
RUN dotnet restore EmoApi.csproj

# Copy phần còn lại & publish
COPY . .
RUN dotnet publish EmoApi.csproj -c Release -o /app/publish --no-restore

###########
# RUNTIME
###########
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EmoApi.dll"]
