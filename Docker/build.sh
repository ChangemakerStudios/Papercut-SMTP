#!/bin/bash

if [ -d ./release/Papercut.Service ]; then
   rm -rf ./release/Papercut.Service
fi

dotnet publish src/Papercut.Service -c Release -o ../../release/Papercut.Service
cp ./Docker/Linux.Dockerfile ./release/Papercut.Service/Dockerfile

time=`date -u +%Y%m%d%H%M%S`
docker build ./release/Papercut.Service --tag "papercut:$time"

if [ -n "$DOCKER_USERNAME" ]; then
    docker tag papercut:$time $DOCKER_USERNAME/papercut:$time
    docker login -u "$DOCKER_USERNAME" -p "$DOCKER_PASSWORD"
    docker push $DOCKER_USERNAME/papercut:$time
fi