# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v2-start
```

## At `v2-start`

The catalog stopped being one process holding a list in memory.

- **`Shop.Catalog.Api`** now writes products to **SQL Server** with EF Core, and
  publishes a `ProductCreated` message with **Rebus** whenever one is added.
- **`Shop.Catalog.Worker`** is new — a worker service that subscribes to that
  message and logs it.
- **`Shop.Catalog.Contracts`** is new — it holds `ProductCreated`, because Rebus
  puts the type name on the wire and both sides need the same type.

`Shop.Catalog.Worker` also gets **`catalog-worker.dev.Dockerfile`** — the same dev
image `Shop.Catalog.Api` got in `v1.2-end`, with the paths changed and no port. There
is nothing new in it, so video 2 walks through it rather than writing it.

There is **no Compose configuration yet**. That is the point of the next video.

### Running it

Four things have to be up, in the right order, before the app does anything:

```sh
docker run -d --name sqlserver -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='P@ssw0rd!Demo1' \
  mcr.microsoft.com/mssql/server:2022-latest

docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=shop -e RABBITMQ_DEFAULT_PASS=shop \
  rabbitmq:3-management

dotnet run --project src/Shop.Catalog.Worker
dotnet run --project src/Shop.Catalog.Api
```

Then add a product and watch the worker pick it up:

```sh
curl -X POST http://localhost:5101/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Pour Over Kettle","price":119.00}'
```

```
info: Shop.Catalog.Worker.ProductCreatedHandler[0]
      product.created — #1 Pour Over Kettle at 119.00
```

That is four commands, in a required order, every morning — and two of them are
long enough that nobody types them from memory. Video 2 replaces all of it with
one file and `docker compose up`.

The two Dockerfiles from the earlier videos are untouched:
`catalog-api.Dockerfile` builds the 340 MB runtime image from `v1-end`, and
`catalog-api.dev.Dockerfile` is the hot-reloading dev image from `v1.2-end`.
Neither knows about the worker or the new dependencies yet.
