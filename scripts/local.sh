#!/bin/bash
dapr dashboard &
dapr run --app-port 5555 --app-id pub-app --dapr-http-port 3500 --components-path ./components/ --log-level info -- dotnet watch run -p ./projects/pub/Pub.fsproj &
dapr run --app-port 5556 --app-id sub-app --dapr-http-port 3501 --components-path ./components/ --log-level info -- dotnet watch run -p ./projects/sub/Sub.fsproj &
dapr run --app-port 5557 --app-id logger-app --dapr-http-port 3502 --components-path ./components/ --log-level info -- dotnet watch run -p ./projects/logger/logger.fsproj