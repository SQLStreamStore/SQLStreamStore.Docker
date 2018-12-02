#!/usr/bin/env bash

set -e

LOCAL_IMAGE="sql-stream-store-server"
LOCAL="${LOCAL_IMAGE}:latest"

REMOTE_IMAGE="sql-stream-store/server"

docker build \
    --build-arg MYGET_API_KEY=$MYGET_API_KEY \
    --tag ${LOCAL} \
    .

VERSION=$(docker run --entrypoint=cat ${LOCAL} /app/.version)

SEMVER_REGEX="^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(\\-[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?(\\+[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?$"

[[ $VERSION =~ $SEMVER_REGEX ]]

LATEST="${REMOTE_IMAGE}:latest"
MAJOR="${REMOTE_IMAGE}:${BASH_REMATCH[1]}"
MAJOR_MINOR="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}"
MAJOR_MINOR_PATCH="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}"
MAJOR_MINOR_PATCH_PRE="${REMOTE_IMAGE}:${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}${BASH_REMATCH[4]}"

if [[ -n $TRAVIS_TAG && -z ${BASH_REMATCH[4]} ]]; then
    echo "Detected a tag with no prerelease."
    docker tag $LOCAL $LATEST
    docker tag $LOCAL $MAJOR_MINOR_PATCH
    docker tag $LOCAL $MAJOR_MINOR
    if [[ ${BASH_REMATCH[1]} != "0" ]]; then
        docker tag $LOCAL $MAJOR
    else
        echo "Detected unstable version."
    fi
else
    echo "Detected a prerelease."
    docker tag $LOCAL $MAJOR_MINOR_PATCH_PRE
fi

docker images --filter=reference="${REMOTE_IMAGE}"
