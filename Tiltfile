# Catalog on its own, or included by the workspace Tiltfile. project_name is not
# optional: Tilt ignores compose's name: key and merges everything it loads.
docker_compose(['docker/compose.yaml'], project_name='shop')

dc_resource('catalog-api', labels=['catalog'], links=['http://localhost:5101/products'])
dc_resource('catalog-worker', labels=['catalog'])

# Renamed because resource names are global to Tilt: Orders has a `sqlserver`
# and a `rabbitmq` too, and the second one loaded would collide.
dc_resource('sqlserver', new_name='catalog-sqlserver', labels=['catalog'])
dc_resource('rabbitmq', new_name='catalog-rabbitmq', labels=['catalog'],
            links=['http://localhost:15672'])
