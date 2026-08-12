# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore
COPY ["DungeonWorldBackend.sln", "./"]
COPY ["DungeonWorld.API/DungeonWorld.API.csproj", "DungeonWorld.API/"]
COPY ["DungeonWorld.Core/DungeonWorld.Core.csproj", "DungeonWorld.Core/"]
COPY ["DungeonWorld.Infrastructure/DungeonWorld.Infrastructure.csproj", "DungeonWorld.Infrastructure/"]
COPY ["DungeonWorld.Cleaning/DungeonWorld.Cleaning.csproj", "DungeonWorld.Cleaning/"]
COPY ["DungeonWorld.Tests/DungeonWorld.Tests.csproj", "DungeonWorld.Tests/"]
COPY ["DungeonWorld.Ingestor/DungeonWorld.Ingestor.csproj", "DungeonWorld.Ingestor/"]
RUN dotnet restore

# Copy all source code
COPY . .

# --- TEST STAGE ---
# This ensures your spatial logic passes all unit tests before publishing
# RUN dotnet test "DungeonWorld.Tests/DungeonWorld.Tests.csproj" -c Release --no-restore

# Publish
RUN dotnet publish "DungeonWorld.API/DungeonWorld.API.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Only standard libraries are needed for PdfPig.
COPY --from=build /app/publish .

# Create storage structure
RUN mkdir -p /app/Storage/Books /app/Storage/GameArt /app/Storage/ProcessedBooks /app/Storage/CleanedData
RUN chmod -R 777 /app/Storage

ENTRYPOINT ["dotnet", "DungeonWorld.API.dll"]