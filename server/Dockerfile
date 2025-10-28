# syntax=docker/dockerfile:1.7

######## BUILD ########
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy toàn bộ (đã có .dockerignore để lọc)
COPY . .

# Restore theo solution thật
RUN dotnet restore EmoApp.sln

# Tìm project trong thư mục server và publish
RUN proj=$(find server -maxdepth 2 -type f -name "*.csproj" | head -n1) && \
    test -n "$proj" || (echo "Không tìm thấy *.csproj trong thư mục server" && exit 1) && \
    echo "Publishing: $proj" && \
    dotnet publish "$proj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

######## RUNTIME ########
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /app/publish ./
# chọn đúng DLL theo runtimeconfig.json (tránh run nhầm thư viện)
CMD app=$(ls *.runtimeconfig.json | head -n1); dll="${app%.runtimeconfig.json}.dll"; \
    echo "Starting $dll"; exec dotnet "$dll"
EXPOSE 8080
