# Dev image for the background worker. No port — nothing connects to a worker.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY src/ ./
WORKDIR /src/Shop.Catalog.Worker

# Build output goes here rather than into obj/ and bin/ under the bind-mounted
# /src, where it would land in your working tree owned by root.
ENV ArtifactsPath=/artifacts

# A worker is a Generic Host, so the variable is DOTNET_, not ASPNETCORE_.
ENV DOTNET_ENVIRONMENT=Development

ENV DOTNET_USE_POLLING_FILE_WATCHER=false

ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--non-interactive"]
