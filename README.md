# Shop.Catalog

The catalog context of the demo shop, used by the
**[Local Dev from First Principles](https://github.com/rahulpnath/tilt-series)**
series (Docker, Tilt & Caddy).

Don't clone this repo on its own — clone the workspace repo and let it assemble
the right set of repos at the right tag:

```sh
git clone https://github.com/rahulpnath/tilt-series
cd tilt-series
./bootstrap.sh v4-end
```

## At `v4-end`

A `Tiltfile`, and nothing else. `compose.yaml` is untouched.

```sh
tilt up          # from this folder — Catalog on its own
```

Four resources in the Tilt UI instead of four interleaved log streams, each with
its own logs and its own restart button. `dotnet watch` is still what reloads
the code; Tilt does not build these images and does not sync files.

Two arguments in that file are load-bearing:

- **`project_name='shop'`** — the same name `compose.yaml` already declares.
  Tilt does **not** read the `name:` key. Leave the argument out and, as soon as
  the workspace Tiltfile loads a second repo, Tilt merges every compose file
  into one project named after the first repo's folder — and resolves that
  repo's `context: ..` and `../src` for all of them. No warning, no error.
- **`new_name=` on `sqlserver` and `rabbitmq`** — Tilt resource names are global
  across every included repo, and `Shop.Orders` has services with those exact
  names. Without the rename, the second repo loaded stops with
  `Error in docker_compose: dc_resource named "rabbitmq" already exists`.

`v4-start` did not change this repo — its tag points at the same commit as
`v3-end`.

---

## Earlier: `v3-end`

`catalog-api` now joins **`shopnet`**, an external network created by hand and
shared with `Shop.Orders`, so `orders-api` can reach it by service name.

```yaml
networks:
  - default     # still needed! naming any network switches off the automatic one
  - shopnet
```

Miss that `default` line and this container keeps its shared network but loses
its own `sqlserver` and `rabbitmq`. With both listed it sits on two networks at
once and resolves names on each:

```
shop_default  172.27.0.5     ← its own project: sqlserver, rabbitmq
shopnet       172.21.0.2     ← the shared one: orders-api can find it here
```

`shopnet` is declared `external: true`, which means Compose will not create it.
If it doesn't exist, `up` refuses to start:

```
network shopnet declared as external, but could not be found
```

Create it once, outside any project: `docker network create shopnet`.

---

## Earlier: `v3-start`

Two sibling repos had arrived — `Shop.Orders` and `Shop.Storefront` — each with
its own compose file and its own project. Nothing in this repo was wired to
them, and nothing here can see them.

One thing did change in here: `Program.cs` now adds a wide-open **CORS policy**.
The Storefront runs on `http://localhost:5173` and this API on
`http://localhost:5101` — different origins, so without it the browser refuses
every call. It ships pre-built because Video 3 is about names and networks, not
browser security, and Video 5 removes the need for it entirely.

Everything below still describes how this repo runs on its own.

## Earlier: `v2-end`

The runbook is gone. One file, one command:

```sh
docker compose -f docker/compose.yaml up
```

Compose builds both images, creates a network, starts SQL Server and RabbitMQ,
**waits for SQL to actually be ready**, then starts the API and worker and
streams all four log streams into one terminal.

- <http://localhost:5101/products> — the API
- <http://localhost:15672> — RabbitMQ management (shop / shop)

### One file, and what it's called

`compose.yaml` is the current name. `docker-compose.yml` is the older one and
still works — you will see it in most tutorials and most repos. Older files also
carry a top-level `version:` key; it is obsolete, and Compose warns if you leave
it in.

There is deliberately **no** `docker-compose.override.yml` or `.dev.yml` split
here. That split separates shared shape from environment-specific bits, and
nothing needs it yet — a second compose file shows up in Video 3, when a second
*repo* does.

### The password lives in one place

Two things need the SA password: the `sqlserver` service, and the connection
string the API is handed. Written out twice, they drift. So they aren't:

```yaml
# compose.yaml
environment:
  MSSQL_SA_PASSWORD: "${SA_PASSWORD}"
```

```sh
# .env, sitting next to compose.yaml — Compose reads it automatically
SA_PASSWORD=P@ssw0rd!Demo1
```

`${...}` substitution is a Compose feature, not a shell one, and Video 6 reuses
exactly this mechanism for `${BRANCH}` to give every worktree its own container
names.

This is a throwaway password for a container that lives twenty minutes. Real
secret management is a different subject.

Note the connection string is passed as `ConnectionStrings__Db`. A double
underscore becomes a colon when .NET reads configuration, so that sets
`ConnectionStrings:Db` and overrides `appsettings.json` without a code change.

### The project name is doing most of the work

One line, `name: shop`, decides all of this:

```
shop-catalog-api-1      ← <project>-<service>-<n>
shop-catalog-worker-1
shop-sqlserver-1
shop-rabbitmq-1
shop_default            ← the network they all share
```

It is also the label Compose groups by, which is how `docker compose ps`, `logs`
and `down` know what they're operating on. Leave it out and the project name
defaults to **the directory the file sits in** — `docker` here — and it changes
the day you rename a folder. Video 6 is built on being able to choose this name.

There is no `container_name:` anywhere yet. Taking control of those is a Video 6
problem.

### Service names are DNS

The API's connection string says `Server=sqlserver,1433` and it resolves — not
because anything set a hostname, but because **`sqlserver` is the service name**,
and Compose registers every service name on the network it created. Same for
`amqp://shop:shop@rabbitmq`.

Which is also why Video 1 could never have done this: Docker's *default* bridge
network has no DNS between containers at all. Compose quietly created a
user-defined network, and that is where the names come from. Video 3 is about
what happens when the thing you're calling is in a different project.

### Not everything needs a door

Only two ports are published: the API on 5101, and RabbitMQ's management UI on
15672. SQL Server has no `ports:` entry at all — `docker compose ps` shows it as
`1433/tcp`, reachable on the network, closed to your machine. Nothing on your
host needs to speak to it.

### `depends_on` — the short form orders, the long form waits

```yaml
depends_on:
  sqlserver:
    condition: service_healthy
```

`depends_on: [sqlserver]` — the short form — only controls *start order*. A
container that has started and a database that will accept a connection are
different events, often 20 seconds apart, and the short form only knows about
the first.

The long form waits for a `healthcheck` to pass. Catalog's:

```yaml
healthcheck:
  test: ["CMD", "/opt/mssql-tools18/bin/sqlcmd",
         "-S", "localhost", "-U", "sa", "-P", "${SA_PASSWORD}", "-C",
         "-Q", "SELECT 1"]
```

Two details that will cost you an evening otherwise: `sqlcmd` is **not on the
PATH** in this image, and its directory moved — `/opt/mssql-tools/bin` in older
images, `/opt/mssql-tools18/bin` here. Version 18 also encrypts by default, so
`-C` is required to trust the container's self-signed certificate.

You can watch it work:

```
 Container shop-sqlserver-1     Waiting
 Container shop-sqlserver-1     Healthy
 Container shop-catalog-api-1   Starting
```

The API's own retry loop is still there as a backstop, and now it never fires —
measured across a cold start with the healthcheck in place: zero retries.

### Persistence

`sql-data` is a named volume, so the database outlives the containers:

```sh
docker compose -f docker/compose.yaml down     # products still there on the way back up
docker compose -f docker/compose.yaml down -v  # -v takes the volume with it
```

`down -v` is the destructive one. It is also how you get a clean database when
you want one.

### Every V1 flag is still here, just declared

Nothing was replaced — the `docker run` flags from Video 1 became lines in the
file:

| Video 1 | `compose.yaml` |
|---|---|
| `-p 5101:5101` | `ports: ["5101:5101"]` |
| `-v "$PWD/src:/src"` | `volumes: - ../src:/src` |
| `-f docker/catalog-api.dev.Dockerfile .` | `build: {context: .., dockerfile: ...}` |

Paths in `build.context` and the bind mount are relative to **the compose file**,
so `..` is the repo root. That holds while you run Compose yourself; Video 4
hands the same file to Tilt, which runs from somewhere else, and that is the line
which has to change.

Nothing in there masks `obj/` and `bin/`, and nothing needs to: both dev images
set `ArtifactsPath=/artifacts`, so each container compiles to a path that exists
only inside it. The alternative is six more `volumes:` lines — two folders per
project, three projects — that work for whoever wrote them and break on a fresh
clone. The `v1-end` README has the long version of why.

Verified after a full `compose up`: zero root-owned entries anywhere in the tree,
and a host `dotnet build` still succeeds while the containers are running.

### A note on `docker compose watch`

You may be reaching for it. It does a different job: `watch` syncs files into the
container or rebuilds the image, while `dotnet watch` runs *inside* the container
and recompiles in place. A bind mount is used here because from Video 4 Tilt
drives these same compose files and does not use `develop.watch`, and because
rebuilding a .NET image on every keystroke is far slower than hot reload.

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
