!#!/bin/bash
echo "pub    http://localhost:5555"
echo "sub    http://localhost:5000"
echo "redis  http://localhost:63790"
echo "kibana http://localhost:5601"
echo "zipkin http://localhost:9412"
echo "dapr   http://localhost:8080"

# App
kubectl port-forward svc/pubapp 5555:80 -n dapr-app &
kubectl port-forward svc/subapp 5000:80 -n dapr-app &
kubectl port-forward svc/redis-master 63790:6379 -n dapr-app &
# Monitoring
kubectl port-forward svc/kibana-kibana 5601 -n dapr-monitoring &
kubectl port-forward svc/zipkin 9412:9411 -n dapr-monitoring &
kubectl port-forward svc/dapr-dashboard 8080:8080 -n dapr-system 

