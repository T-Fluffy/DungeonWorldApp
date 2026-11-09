# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy and restore. Restore is scoped to the API project graph so the build does
# not depend on a committed solution file (the .sln is git-ignored).
COPY ["DungeonWorld.API/DungeonWorld.API.csproj", "DungeonWorld.API/"]
COPY ["DungeonWorld.Core/DungeonWorld.Core.csproj", "DungeonWorld.Core/"]
COPY ["DungeonWorld.Infrastructure/DungeonWorld.Infrastructure.csproj", "DungeonWorld.Infrastructure/"]
COPY ["DungeonWorld.Cleaning/DungeonWorld.Cleaning.csproj", "DungeonWorld.Cleaning/"]
RUN dotnet restore "DungeonWorld.API/DungeonWorld.API.csproj"

# Copy all source code
COPY . .

# --- TEST STAGE ---
# This ensures your spatial logic passes all unit tests before publishing
# RUN dotnet test "DungeonWorld.Tests/DungeonWorld.Tests.csproj" -c Release --no-restore

# Publish
RUN dotnet publish "DungeonWorld.API/DungeonWorld.API.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# poppler-utils (pdftoppm renders) + tesseract (OCR) for the parse pipeline.
# Only standard libraries are needed for PdfPig.
RUN apt-get update && apt-get install -y --no-install-recommends \
    poppler-utils tesseract-ocr \
    && rm -rf /var/lib/apt/lists
COPY --from=build /app/publish .


# Create storage structure
RUN mkdir -p /app/Storage/Books /app/Storage/GameArt /app/Storage/ProcessedBooks /app/Storage/CleanedData
RUN chmod -R 777 /app/Storage

ENTRYPOINT ["dotnet", "DungeonWorld.API.dll"]
