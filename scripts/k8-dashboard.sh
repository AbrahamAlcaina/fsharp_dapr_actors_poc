#!/bin/bash
#To Create a Dashboard
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.3.1/aio/deploy/recommended.yaml

# create service account
echo "
apiVersion: v1
kind: ServiceAccount
metadata:
  name: admin-user
  namespace: kubernetes-dashboard
" > service-account.yaml

echo "
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: admin-user
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
- kind: ServiceAccount
  name: admin-user
  namespace: kubernetes-dashboard
" > role-binding.yaml

kubectl apply -f service-account.yaml --namespace=kubernetes-dashboard
kubectl apply -f role-binding.yaml --namespace=kubernetes-dashboard

rm service-account.yaml
rm role-binding.yaml

kubectl -n kubernetes-dashboard describe secret "$(kubectl -n kubernetes-dashboard get secret | grep admin-user | awk '{print $1}')"
echo "Now copy the token and paste it into Enter token field on login screen.!"
echo "http://localhost:8909/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/."

kubectl proxy --port=8909
