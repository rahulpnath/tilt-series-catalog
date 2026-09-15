# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v3-end
```

## At `v3-end`

SQL Server and RabbitMQ live in the workspace's **`infra/`** compose project, shared with
`Shop.Orders`, on a network named `shopnet`. `compose.yaml` here has the API and the worker,
each on this project's own network *and* on `shopnet` — declared `external`, because infra
owns it.

Two things changed in video 3. On `shopnet`, the API now also answers to **`catalog-api`**:

```yaml
    networks:
      default:
      shopnet:
        aliases:
          - catalog-api
```

The service is still `api` inside this project — so is Orders' — and on the shared network
that name is ambiguous. The alias is an extra name on that one network only, and it is what
Orders calls.

And `Program.cs` adds a wide-open **CORS policy**. The Storefront runs on
`http://localhost:5173` and this API on `http://localhost:5101` — different origins, so
without it the browser refuses every call. It is dev-only, and it exists only because the
apps sit on different ports of your machine; video 5 puts them behind one address and
removes the need for it.

### Running it

Bring infra up first, from the workspace root:

```sh
(cd infra && docker compose up -d --wait)
```

Then, here — the SQL password is read from a `.env` file, which is not committed:

```sh
echo 'SA_PASSWORD=P@ssw0rd!Demo1' > .env
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

`docker compose down` removes this project's containers and its own network; `shopnet` and
the database belong to infra and stay. SQL Server (`localhost,1433`) and the RabbitMQ UI
(http://localhost:15672, `shop` / `shop`) are published by infra now.

`catalog-api.Dockerfile`, the runtime image from `v1-end`, is untouched and unused here.
