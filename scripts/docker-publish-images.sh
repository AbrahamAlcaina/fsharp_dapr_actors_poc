#!/bin/bash 

version=$1
if [[ -z "$version" ]]; then
  version=latest
fi


docker build . --target=sub-app --tag=sub-app
docker tag sub-app abrahamalcaina/sub-app
docker push abrahamalcaina/sub-app:$version

docker build . --target=pub-app --tag=pub-app
docker tag pub-app abrahamalcaina/pub-app
docker push abrahamalcaina/pub-app:$version

docker build . --target=logger-app --tag=logger-app
docker tag logger-app abrahamalcaina/logger-app
docker push abrahamalcaina/logger-app:$version