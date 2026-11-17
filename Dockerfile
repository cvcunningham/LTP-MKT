# Builds the C# API (mockup_1/api) for Railway. The API also serves the website (mockup_1/client)
# from wwwroot/, so one service hosts everything.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY mockup_1/api/api.csproj ./
RUN dotnet restore
COPY mockup_1/api/ ./
RUN dotnet publish api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
# The CSV files are copied into MySQL the first time the API starts with empty tables.
COPY mockup_1/api/Data/ ./Data/
# The website files.
COPY mockup_1/client/ ./wwwroot/

# Railway tells the app which port to use through PORT.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet api.dll"]
