#!/bin/bash
while test $# -gt 0; do
  case $1 in
    -d|--delete-current)
      DELETE=true
      shift # past argument
      ;;    
    -a|--app)
      APP=true
      shift # past argument
      ;;
    -h|--help)      
      echo ""
      echo ""
      echo "Avaliable options:"
      echo "  -d --delete "
      echo "  Delete previous installation"
      echo "  -a --app"
      echo "  Install app"
      shift
      ;;
    *)
      break
      ;;
  esac
done

if [[ $DELETE ]]
then 
  echo "0.- delete previous installation"
  echo "0.1.- delete app"
  kubectl delete namespace dapr-app
  echo "0.2.- delete monitoring"
  helm uninstall elasticsearch --namespace dapr-monitoring
  helm uninstall kibana --namespace dapr-monitoring
  kubectl delete namespace dapr-monitoring
  echo "0.2.- delete dapr"
  helm uninstall dapr --namespace dapr-system
  kubectl delete namespace dapr-system  
fi

# Darp
echo "1.- Install Dapr"
kubectl create namespace dapr-system
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update
helm install dapr dapr/dapr --namespace dapr-system --set global.logAsJson=true
echo "1.1.- Wait 1 minutes to be ready"

# Monitoring 
echo "2.- Create monitoring namespace"
kubectl create namespace dapr-monitoring

# Kibana
echo "3.- Install Elastic Search"
helm repo add elastic https://helm.elastic.co
helm repo update
helm install elasticsearch elastic/elasticsearch -n dapr-monitoring --set replicas=1
echo "4.- Install Kibana"
helm install kibana elastic/kibana -n dapr-monitoring

# Fluentd
echo "5.- Install Fluentd"
kubectl apply -f ./fluentd/fluentd-config-map.yaml
kubectl apply -f ./fluentd/fluentd-dapr-with-rbac.yaml

# zipkin
echo "6.- Install Zipkin"
kubectl create deployment zipkin --image openzipkin/zipkin -n dapr-monitoring 
kubectl expose deployment zipkin --type ClusterIP --port 9411 -n dapr-monitoring 

if [[ $APP ]]
then 
  echo "7.- Install App"
  
  echo "7.1.- Restart sidecar injection after upgrade dapr"
  kubectl rollout restart deployment dapr-sidecar-injector  -n dapr-system
  sleep 1m
  kubectl logs -l app=dapr-sidecar-injector -n dapr-system

  cd ../../
  ./scripts/k8-deploy.sh
fi