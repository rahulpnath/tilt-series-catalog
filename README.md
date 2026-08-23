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

## At `v1-end`

Same API, now in a **dev image**: the .NET SDK, your `src` folder bind-mounted
into it, and `dotnet watch` recompiling on every save. Your machine no longer
needs a .NET SDK.

```sh
docker build -t shop-catalog-api:dev -f docker/catalog-api.dev.Dockerfile .

docker run --rm -it -p 5101:5101 --name shop-catalog-api \
  -v "$PWD/src:/src" \
  shop-catalog-api:dev

curl http://localhost:5101/
```

Edit `src/Shop.Catalog.Api/Program.cs`, change the string on `/`, save, and
curl again. New response, no rebuild — the container is compiling what your
editor writes.

### The two flags that matter

`-p 5101:5101` publishes a port: it opens a door from your host to the
container. Without it the API is running and completely unreachable.

`-v "$PWD/src:/src"` is a bind mount: your folder, seen from inside the
container. This is what makes `dotnet watch` useful — it watches your editor's
output, not a copy frozen at build time.

### Two builds, one folder

That bind mount creates a problem, and the Dockerfile has already solved it.
Your host builds into `src/Shop.Catalog.Api/obj` and `bin`. So would the
container — into those same two folders, because a bind mount means there is
only one copy of them — and the container runs as **root**.

So the image sends its build somewhere your tree can't see:

```dockerfile
ENV ArtifactsPath=/artifacts
```

`/artifacts` exists only inside the container. MSBuild reads environment
variables as properties, and `ArtifactsPath` switches on the artifacts output
layout by itself — no `Directory.Build.props`, no change to any `.csproj`.

Nearly every tutorial solves this the other way, by *masking* the two folders
with anonymous volumes:

```sh
-v /src/Shop.Catalog.Api/obj -v /src/Shop.Catalog.Api/bin   # don't
```

That works on the machine it was written on and breaks on a fresh clone. Docker
creates a mount point only when the path doesn't exist — and in a fresh clone
`obj/` doesn't, so Docker creates it on your host: empty, and owned by **root**.
Your next host build then dies before it compiles anything:

```
NuGet.targets(196,5): error : Access to the path '.../obj/<guid>.tmp' is denied.
NuGet.targets(196,5): error :   Permission denied
Build FAILED.
```

It's the *directory* that stops you, not the files in it. Root-owned files
inside a folder you own are replaced without complaint — deleting a file is a
permission on its **parent**. A root-owned folder is the one you can't write
into.

If you've already hit it: on a fresh clone those folders are empty, so
`rm -rf src/*/obj src/*/bin` clears them. Once a container has filled them,
`rm` can't — borrow the container's root instead:

```sh
docker run --rm -v "$PWD/src:/src" --entrypoint sh shop-catalog-api:dev \
  -c 'rm -rf /src/*/obj /src/*/bin'
```

### If `watch` never fires — check where the repo lives

On WSL2 this is almost always the repo sitting on the Windows drive:

```sh
findmnt -no FSTYPE,SOURCE,TARGET .
# 9p  C:\  /mnt/c        ← the problem
```

Across that 9p mount, a file written by a **Windows** process (which is what
your editor is, if you're editing from Windows) generates no inotify event on
the Linux side. `dotnet watch` never learns anything changed — no error, no
message, it just sits at `Waiting for changes` forever.

Confusingly, editing that same file from *inside* WSL2 works fine, so testing
with `sed` will tell you everything is healthy. It isn't.

The fix is to move the repo into the WSL2 filesystem (somewhere under `~`), not
to set `DOTNET_USE_POLLING_FILE_WATCHER=true`. Polling papers over it, burns
CPU, and adds latency to every save.

---

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

That is the starting point Video 1 opens on, and the problem it opens with:
it runs because *your* machine happens to have the right SDK.
