#!/usr/bin/env sh

set -e

tags="$(yq -r '.jobs["continuous-integration"].strategy.matrix.env[] | .["library-version"] + "-" + .["container-runtime"] + "|" + .["os-version"]' ${GITHUB_WORKSPACE}/.github/workflows/ci.yml)"

tee -a ${GITHUB_WORKSPACE}/README.dockerhub.md <<EOF

# Docker Images
Tags | OS Version
---|---
${tags}
EOF
