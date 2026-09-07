# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v1.2-end
```

## At `v1.2-end`

`Shop.Catalog.Api`, a .NET 10 minimal API holding its products in memory. No
database and no messaging — and now a **second Dockerfile, for development
only**. It mounts your source into the container and runs `dotnet watch`, so an
edit is picked up while the container keeps running:

```sh
docker build -f catalog-api.dev.Dockerfile -t catalog-api:dev .
docker run --rm -it -p 5101:5101 --name catalog-api \
  -v "$PWD/src:/src" \
  catalog-api:dev

# in another terminal
curl http://localhost:5101/
```

Edit `src/Shop.Catalog.Api/Program.cs`, then curl again. No rebuild, no restart.

Three things make that work:

- **`sdk:10.0`, one stage.** This image compiles every time you save, so it
  needs the SDK. It's 1.25 GB, and that's fine — it never leaves your machine.
- **`applicationUrl` is now `http://+:5101`.** Kestrel binds every interface in
  the container rather than just its loopback, which is what lets `-p` reach it.
  It binds every interface when you `dotnet run` on your own machine too — add a
  second launch profile if you'd rather it didn't.
- **`ENV ArtifactsPath=/artifacts`.** The container's `bin` and `obj` land there
  instead of in the mounted folder, so its root-owned build output never gets
  into your source tree.

`catalog-api.Dockerfile` from `v1-end` is untouched and still builds the 340 MB
runtime image. Two Dockerfiles, two jobs — one to ship, one to develop in.

That `docker run` is a lot to type for one container. Video 2 replaces it with
Compose.
