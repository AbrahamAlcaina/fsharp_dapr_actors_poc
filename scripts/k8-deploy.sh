#!/bin/bash
kubectl apply -f ./kubernetes/app/namespace.yaml
helm repo add bitnami https://charts.bitnami.com/bitnami
helm install redis bitnami/redis --namespace dapr-app
kubectl apply -f ./kubernetes/app/components -n dapr-app
kubectl apply -f ./kubernetes/app/sub.yaml -n dapr-app
kubectl apply -f ./kubernetes/app/pub.yaml -n dapr-app
kubectl apply -f ./kubernetes/app/logger.yaml -n dapr-app