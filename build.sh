#!/usr/bin/env bash

set -e

CONTAINER_RUNTIME=${CONTAINER_RUNTIME:-alpine3.9}
LIBRARY_VERSION=${LIBRARY_VERSION:-1.2.0-beta.3.28}
CLIENT_VERSION=${CLIENT_VERSION:-0.9.3}

LOCAL_IMAGE="sql-stream-store-server"
LOCAL="${LOCAL_IMAGE}:latest"

REMOTE_IMAGE="sqlstreamstore/server"

docker build \
    --build-arg CONTAINER_RUNTIME_VERSION=${CONTAINER_RUNTIME_VERSION:-2.2.5} \
    --build-arg CONTAINER_RUNTIME=${CONTAINER_RUNTIME} \
    --build-arg RUNTIME=${RUNTIME:-alpine-x64} \
    --build-arg LIBRARY_VERSION=${LIBRARY_VERSION} \
    --build-arg CLIENT_VERSION=${CLIENT_VERSION} \
    --tag ${LOCAL} \
    .

SEMVER_REGEX="^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(\\-[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?(\\+[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?$"

[[ $LIBRARY_VERSION =~ $SEMVER_REGEX ]]

MAJOR_MINOR="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}-${CONTAINER_RUNTIME}"
MAJOR_MINOR_PATCH="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}-${CONTAINER_RUNTIME}"
MAJOR_MINOR_PATCH_PRE="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}${BASH_REMATCH[4]}-${CONTAINER_RUNTIME}"

if [[ -z ${BASH_REMATCH[4]} ]]; then
    echo "Detected a tag with no prerelease."
    docker tag $LOCAL $MAJOR_MINOR_PATCH
    docker tag $LOCAL $MAJOR_MINOR
else
    echo "Detected a prerelease."
    docker tag $LOCAL $MAJOR_MINOR_PATCH_PRE
fi

if [[ -n $DOCKER_USER ]]; then
    echo "${DOCKER_PASS}" | docker login --username "${DOCKER_USER}" --password-stdin
    docker push $REMOTE_IMAGE
fi

docker images --filter=reference="${REMOTE_IMAGE}"
