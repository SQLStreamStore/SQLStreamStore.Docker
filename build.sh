#!/usr/bin/env bash

docker build --build-arg TRAVIS_BUILD_NUMBER=$TRAVIS_BUILD_NUMBER --build-arg MYGET_API_KEY=$MYGET_API_KEY .
