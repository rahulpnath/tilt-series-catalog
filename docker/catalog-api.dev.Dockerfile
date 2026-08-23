# Dev image: the SDK is in here because we compile inside the container on save.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY src/ ./
WORKDIR /src/Shop.Catalog.Api

# Build output goes here rather than into obj/ and bin/ under the bind-mounted
# /src, where it would land in your working tree owned by root.
ENV ArtifactsPath=/artifacts

# Listen on every interface, not just loopback. The SDK image presets
# ASPNETCORE_HTTP_PORTS=8080, so clear it or .NET warns on every start.
ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_URLS=http://+:5101
EXPOSE 5101

# Nothing sets this in a container, and the default is Production.
ENV ASPNETCORE_ENVIRONMENT=Development

# The SDK image defaults this to true; inotify works on a Linux filesystem.
ENV DOTNET_USE_POLLING_FILE_WATCHER=false

ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--non-interactive"]
