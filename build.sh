#!/usr/bin/env bash
set -e

DOCKERTAG=${TRAVIS_TAG:-latest}

docker build \
    --build-arg TRAVIS_OS_NAME=$TRAVIS_OS_NAME \
    --build-arg MYGET_API_KEY=$MYGET_API_KEY \
    --tag sql-stream-store-server:${DOCKERTAG} \
    .

docker images --filter=reference="sql-stream-store-server:${DOCKERTAG}"