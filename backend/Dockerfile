# Construction : docker build -t lostfound-api -f Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/LostFoundDocs.Api/LostFoundDocs.Api.csproj src/LostFoundDocs.Api/
RUN dotnet restore src/LostFoundDocs.Api/LostFoundDocs.Api.csproj
COPY src/ src/
RUN dotnet publish src/LostFoundDocs.Api/LostFoundDocs.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5080 \
    Storage__DatabasePath=/app/data/lostfound.db \
    Storage__PhotosPath=/app/data/photos
VOLUME /app/data
EXPOSE 5080
ENTRYPOINT ["dotnet", "LostFoundDocs.Api.dll"]
