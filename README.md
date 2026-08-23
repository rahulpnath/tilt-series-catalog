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

Catalog is **two** processes now, and between them they need **two** pieces of
infrastructure:

| | |
|---|---|
| `Shop.Catalog.Api` | minimal API on :5101 — saves products to SQL Server, publishes `ProductCreated` |
| `Shop.Catalog.Worker` | background service — handles `ProductCreated` and logs it |
| `Shop.Catalog.Contracts` | the message type, so both sides resolve the same one |

Messaging is [Rebus](https://rebus.fm) rather than raw `RabbitMQ.Client`:
`bus.Publish(...)` on one side, an `IHandleMessages<ProductCreated>` on the
other, and no exchange or queue plumbing in your code.

**Nothing about Docker has changed since `v1-end`** — and that is the problem.
Video 1's `docker run` command still starts the API, but the API now needs a
database and a broker that don't exist, so it never serves a request.

Starting Catalog by hand is now four containers that have to find each other:

```sh
# Containers only resolve each other by name on a user-defined network —
# Docker's default bridge has no DNS at all.
docker network create shop-net

docker run -d --name sqlserver --network shop-net \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='P@ssw0rd!Demo1' \
  mcr.microsoft.com/mssql/server:2022-latest

docker run -d --name rabbitmq --network shop-net \
  -e RABBITMQ_DEFAULT_USER=shop -e RABBITMQ_DEFAULT_PASS=shop \
  -p 15672:15672 rabbitmq:3-management

docker build -t shop-catalog-api:dev    -f docker/catalog-api.dev.Dockerfile    .
docker build -t shop-catalog-worker:dev -f docker/catalog-worker.dev.Dockerfile .

docker run -d --name catalog-api --network shop-net -p 5101:5101 \
  -v "$PWD/src:/src" \
  shop-catalog-api:dev

docker run -d --name catalog-worker --network shop-net \
  -v "$PWD/src:/src" \
  shop-catalog-worker:dev
```

Then:

```sh
curl -X POST http://localhost:5101/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Pour Over Kettle","price":129.00}'

curl http://localhost:5101/products      # it came back from SQL Server
docker logs catalog-worker               # 📦 product.created — #1 Pour Over Kettle
```

A network, two builds and four `docker run`s, in an order that matters, written
down nowhere but here. That is what Video 2 replaces with one file.

Note that only the ports *you* need are published: the API on 5101 and the
RabbitMQ UI on 15672. SQL Server gets no `-p` at all — nothing on your machine
talks to it, only the API does, and it does that across the network.

### Two things to watch while it starts

Both are the same problem — *a dependency isn't ready yet* — and the two
libraries handle it very differently.

**SQL Server takes ~20 seconds** before it accepts a connection. `DbBootstrap`
retries for it, 30 times, two seconds apart. On a normal start you may see zero
retries: building the API inside the container takes long enough that SQL is
ready by the time it looks. Start the API with no SQL Server at all and the loop
earns its keep:

```
warn: SQL not ready yet (attempt 1/30): An exception has been raised that is
      likely due to a transient failure...
```

Eight of those in the first minute, and the container stays up. Start SQL and it
picks up with no restart. Delete the loop and the first failed connection takes
the process down.

The API answers **nothing** while it waits, not even `/health` — the bootstrap
runs before `app.Run()`, so there is no server listening yet.

**Rebus is more patient, but not infinitely so.** With RabbitMQ down it logs:

```
warn: Could not establish connection: "None of the specified endpoints were reachable"
```

every few seconds while it retries. Measured here: **11 attempts over 58
seconds**, then it gives up, throws, and the process exits. RabbitMQ starts
accepting connections about 15 seconds after its container does, so an ordinary
start never approaches that budget — but a broker that is genuinely missing kills
the app rather than being waited out forever.

Video 2 fixes the ordering properly, with a healthcheck.

### "Up" does not mean "running"

If the app inside the container throws on startup, `dotnet watch` prints this
and then does nothing:

```
dotnet watch ❌ [Shop.Catalog.Api (net10.0)] Exited with error code 134
dotnet watch ⏳ Waiting for a file to change before restarting ...
```

The watcher is alive, so the **container stays `Up`** — `docker ps` will tell
you everything is fine. The application is dead, and stays dead until you touch
a file. Any time something is unreachable but its container looks healthy, read
the logs before you believe `docker ps`.

### If `watch` never fires — check where the repo lives

On WSL2 this is almost always the repo sitting on the Windows drive:

```sh
findmnt -no FSTYPE,SOURCE,TARGET .
# 9p  C:\  /mnt/c        ← the problem
```

Across that 9p mount, a file written by a **Windows** process — which is what
your editor is, if you're editing from Windows — generates no inotify event on
the Linux side. `dotnet watch` never learns anything changed: no error, no
message, it just sits at `Waiting for changes` forever.

Confusingly, editing that same file from *inside* WSL2 works fine, so testing
with `sed` will tell you everything is healthy. It isn't.

The fix is to move the repo into the WSL2 filesystem, somewhere under `~` — not
to set `DOTNET_USE_POLLING_FILE_WATCHER=true`. Polling papers over it, burns
CPU, and adds latency to every save.
