#!/usr/bin/env bash
set -e

DOCKERTAG=${TRAVIS_TAG:-latest}
BUILD_NUMBER=${TRAVIS_BUILD_NUMBER:-0}
COMMIT=${TRAVIS_PULL_REQUEST_SHA:-${TRAVIS_COMMIT:-unknown}}
MINVERBUILDMETADATA="build.${BUILD_NUMBER}.${COMMIT}"

docker build \
    --build-arg MINVERBUILDMETADATA=$MINVERBUILDMETADATA \
    --build-arg MYGET_API_KEY=$MYGET_API_KEY \
    --tag sql-stream-store-server:${DOCKERTAG} \
    .

docker images --filter=reference="sql-stream-store-server:${DOCKERTAG}"