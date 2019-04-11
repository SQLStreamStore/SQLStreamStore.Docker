ARG CONTAINER_RUNTIME_VERSION=2.2.4
ARG CONTAINER_RUNTIME=alpine3.8

FROM node:10.12.0-alpine AS build-javascript
ARG CLIENT_PACKAGE=@sqlstreamstore/browser
ARG CLIENT_VERSION=0.9.2
ARG NPM_REGISTRY=https://www.myget.org/F/sqlstreamstore/npm/

ENV REACT_APP_CLIENT_VERSION=${CLIENT_VERSION}

WORKDIR /app

RUN echo "@sqlstreamstore:registry=${NPM_REGISTRY}" > .npmrc && \
    yarn init --yes && \
    yarn add ${CLIENT_PACKAGE}@${CLIENT_VERSION}

WORKDIR /app/node_modules/${CLIENT_PACKAGE}

RUN yarn && \
    yarn react-scripts-ts build && \
    echo ${CLIENT_VERSION} > /app/.clientversion

FROM mcr.microsoft.com/dotnet/core/sdk:2.2.203-stretch AS build-dotnet
ARG CLIENT_PACKAGE=@sqlstreamstore/browser
ARG RUNTIME=alpine-x64
ARG LIBRARY_VERSION=1.2.0

WORKDIR /app

COPY ./*.sln .git ./

RUN dotnet tool install -g minver-cli --version 1.0.0 && \
    /root/.dotnet/tools/minver > .version

WORKDIR /app/src

COPY ./src/*/*.csproj ./src/Directory.Build.props ./

RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

WORKDIR /app

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=${RUNTIME}

WORKDIR /app/src

COPY ./src .

COPY --from=build-javascript /app/node_modules/${CLIENT_PACKAGE}/build /app/src/SqlStreamStore.Server/Browser/build

WORKDIR /app/build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /app

RUN dotnet run --project build/build.csproj -- --runtime=${RUNTIME} --library-version=${LIBRARY_VERSION}

FROM mcr.microsoft.com/dotnet/core/runtime-deps:${CONTAINER_RUNTIME_VERSION}-${CONTAINER_RUNTIME} AS runtime

WORKDIR /app

COPY --from=build-dotnet /app/publish /app/.version ./

ENTRYPOINT ["/app/SqlStreamStore.Server"]
