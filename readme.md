## Documentation

### Generate images

docker build . --target=pub-app --tag=pub-app
docker build . --target=sub-app --tag=sub-app

### Run locally

dapr run --app-id pub-app --dapr-http-port 3500 -- dotnet watch run -p projects/pub/pub.fsproj

dapr run --app-id sub-app --dapr-http-port 3501 -- dotnet watch run -p projects/sub/sub.fsproj

dapr dashboard

### Darp

http://localhost:3500/v1.0/actors/PlaceActor/b1/method/Status

### Kibana

https://docs.dapr.io/operations/monitoring/logging/fluentd/

kubectl port-forward svc/kibana-kibana 5601 -n dapr-monitoring

# URL

PUB http://localhost:5555/
SUB http://localhost:5000/

Redis http://localhost:63790/
Kibana http://localhost:5601/
Zipkin http://localhost:9411/
Dapr Dashboard http://localhost:8080/
