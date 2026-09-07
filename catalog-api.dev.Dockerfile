# A DEV image, on purpose. It carries the whole .NET SDK, because we
# compile inside the container every time you save.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY src/ ./
WORKDIR /src/Shop.Catalog.Api

# The SDK image sets this to 8080. Clear it, or .NET warns about
# overriding the port our launch profile asks for, on every start.
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 5101

# Build output stays in the container, out of the folder we bind-mount.
ENV ArtifactsPath=/artifacts

ENTRYPOINT ["dotnet", "watch", "run", "--non-interactive"]
