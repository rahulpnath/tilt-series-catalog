# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v2-end
```

## At `v2-end`

The runbook is gone. **`compose.yaml`** at the repo root describes all four containers —
SQL Server, RabbitMQ, the API and the worker. The two app services build from the dev
Dockerfiles, so your source is bind-mounted and `dotnet watch` reloads on save. SQL
Server and RabbitMQ carry health checks, and `depends_on` holds the app containers back
until both report healthy.

The only thing installed on your machine is Docker. The .NET SDK runs inside the
containers.

### Running it

The SQL Server password is read from a `.env` file, which is not committed. Create it
once:

```sh
echo 'SA_PASSWORD=P@ssw0rd!Demo1' > .env
```

Then:

```sh
docker compose up
```

Add a product and watch the worker pick it up:

```sh
curl -X POST http://localhost:5101/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Pour Over Kettle","price":119.00}'
```

```
worker-1  | info: Shop.Catalog.Worker.ProductCreatedHandler[0]
worker-1  |       📦 product.created — #1 Pour Over Kettle at 119.00
```

`docker compose down` stops and removes the containers but keeps the database — it
lives in the `sql-data` named volume. `docker compose down -v` removes that too.

SQL Server is published on `localhost,1433` if you want to open it in SSMS or DataGrip,
and RabbitMQ's management UI is at http://localhost:15672 (`shop` / `shop`). The apps
don't use either — they reach both by service name, inside the Compose network.

`catalog-api.Dockerfile`, the runtime image from `v1-end`, is untouched and unused here.
