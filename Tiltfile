docker_compose('compose.yaml', project_name='shop-catalog')

dc_resource('catalog-api', labels=['catalog'], links=['https://catalog.localhost/products'])
dc_resource('catalog-worker', labels=['catalog'])
