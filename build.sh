#!/usr/bin/env bash
DOCKERTAG=${TRAVIS_TAG:-latest}

docker build \
    --build-arg TRAVIS_BUILD_NUMBER=$TRAVIS_BUILD_NUMBER \
    --build-arg MYGET_API_KEY=$MYGET_API_KEY \
    --build-arg TRAVIS_PULL_REQUEST_SHA=$TRAVIS_PULL_REQUEST_SHA \
    --build-arg TRAVIS_COMMIT=$TRAVIS_COMMIT \
    --build-arg TRAVIS_PULL_REQUEST=$TRAVIS_PULL_REQUEST \
    --build-arg TRAVIS_BRANCH=$TRAVIS_BRANCH \
    --tag sql-stream-store-server:${DOCKERTAG} \
    . && \
docker images --filter=reference="sql-stream-store-server:${DOCKERTAG}"
