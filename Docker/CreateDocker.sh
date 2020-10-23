#!/bin/bash
cd ..
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true

docker build -f ./Docker/Dockerfile --tag mopsbot:latest .
docker tag mopsbot:latest marilyth/mopsbot:latest
docker push marilyth/mopsbot:latest