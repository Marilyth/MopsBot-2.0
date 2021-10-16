#!/bin/bash
BASEDIR=$(dirname "$0")
cd ${BASEDIR}/..
docker build --tag mopsbot:latest -f ./Docker/Dockerfile .
#docker tag mopsbot:latest marilyth/mopsbot:latest
#docker push marilyth/mopsbot:latest