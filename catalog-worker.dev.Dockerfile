# A DEV image, on purpose. It carries the whole .NET SDK, because we
# compile inside the container every time you save.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY src/ ./
WORKDIR /src/Shop.Catalog.Worker

# Build output stays in the container, out of the folder we bind-mount.
ENV ArtifactsPath=/artifacts

ENTRYPOINT ["dotnet", "watch", "run", "--non-interactive"]
