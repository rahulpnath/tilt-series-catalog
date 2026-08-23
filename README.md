# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v1-start
```

## At `v1-start`

`Shop.Catalog.Api`, a .NET 10 minimal API holding its products in memory. No
database, no messaging, no Docker — it runs because your machine has the SDK:

```sh
cd src
dotnet run --project Shop.Catalog.Api

curl http://localhost:5101/
curl http://localhost:5101/products
curl -X POST http://localhost:5101/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Pour Over Kettle","price":129.00}'
```

By `v1-end` the same API runs in a container that carries its own SDK, with your
`src` folder mounted into it and `dotnet watch` recompiling on every save.
