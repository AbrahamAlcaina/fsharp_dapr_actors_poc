#!/bin/bash
dapr dashboard &
dapr run --app-port 5555 --app-id pub-app --dapr-http-port 3501 --components-path ./components/ --log-level info -- dotnet watch run -p ./projects/pub/Pub.fsproj &
dapr run --app-port 5000 --app-id sub-app --dapr-http-port 3500 --components-path ./components/ --log-level info -- dotnet watch run -p ./projects/sub/Sub.fsproj