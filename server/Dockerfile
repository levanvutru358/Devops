# syntax=docker/dockerfile:1.7

############
# BUILD
############
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copy toàn bộ source vào build context
COPY . .

# 2) Restore: ưu tiên .sln; nếu không có thì restore theo csproj API
RUN if [ -f Webshop.sln ]; then \
      dotnet restore Webshop.sln ; \
    else \
      csproj=$(find . -maxdepth 2 -name "Webshop.Api.csproj" | head -n1); \
      if [ -z "$csproj" ]; then \
        echo "Không tìm thấy Webshop.Api.csproj trong context" && exit 1; \
      fi; \
      dotnet restore "$csproj" ; \
    fi

# 3) Publish: tìm đúng csproj API rồi publish
RUN csproj=$(find . -maxdepth 2 -name "Webshop.Api.csproj" | head -n1) && \
    dotnet publish "$csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

############
# RUNTIME
############
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /app/publish ./
RUN find /app -name "*.pdb" -delete && find /app -name "*.xml" -delete || true
EXPOSE 8080
ENTRYPOINT ["dotnet","Webshop.Api.dll"]
