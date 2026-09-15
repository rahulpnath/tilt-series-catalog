docker_compose('compose.yaml', project_name='shop-catalog')

dc_resource('api', new_name='catalog-api', labels=['catalog'])
dc_resource('worker', new_name='catalog-worker', labels=['catalog'])
