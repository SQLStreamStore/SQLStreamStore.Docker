#!/usr/bin/env bash
DOCKERTAG=${TRAVIS_TAG:-latest}

docker build \
    --build-arg TRAVIS_BUILD_NUMBER=$TRAVIS_BUILD_NUMBER \
    --build-arg MYGET_API_KEY=$MYGET_API_KEY \
    --tag sql-stream-store-server:${DOCKERTAG} \
    . 

docker images --filter=reference="sql-stream-store-server:${DOCKERTAG}"
