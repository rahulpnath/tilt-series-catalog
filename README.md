# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v1-end
```

## At `v1-end`

`Shop.Catalog.Api`, a .NET 10 minimal API holding its products in memory. No
database and no messaging — but it now has a Dockerfile, so it no longer needs
the .NET SDK on your machine:

```sh
docker build -f catalog-api.Dockerfile -t catalog-api .
docker run -d -p 5101:8080 catalog-api

curl http://localhost:5101/products
```

`catalog-api.Dockerfile` is a multi-stage build. `sdk:10.0` compiles and
publishes the app, `aspnet:10.0` runs it, and the SDK is left behind — **340 MB
instead of 1.25 GB**. `.dockerignore` keeps `bin` and `obj` out of the build
context.

The container listens on `8080`, which is what the ASP.NET base images default
to, so the port mapping is `5101:8080`.

If you do have the SDK, it still runs the way it did at `v1-start`:

```sh
dotnet run --project src/Shop.Catalog.Api
curl http://localhost:5101/products
```

There's no bind mount and no `dotnet watch` here — this image carries no SDK, so
a code change means building the image again. `v1.2-end` adds a second, dev-only
Dockerfile that fixes exactly that.
