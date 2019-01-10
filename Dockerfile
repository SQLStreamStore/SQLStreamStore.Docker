FROM node:10.12.0-alpine AS build-javascript
ARG CLIENT_VERSION=0.9.2
ARG NPM_REGISTRY=https://registry.npmjs.org
ARG CLIENT_PACKAGE=@sqlstreamstore/browser

WORKDIR /app

RUN echo "@sqlstreamstore:registry=${NPM_REGISTRY}" > .npmrc && \
    yarn init --yes && \
    yarn add ${CLIENT_PACKAGE}@${CLIENT_VERSION}

WORKDIR /app/node_modules/${CLIENT_PACKAGE}

RUN yarn && \
    yarn react-scripts-ts build

FROM microsoft/dotnet:2.2.102-sdk-stretch AS build-dotnet
ARG CLIENT_PACKAGE=@sqlstreamstore/browser

WORKDIR /app

COPY .git ./

RUN dotnet tool install -g minver-cli --version 1.0.0-beta.2 && \
  /root/.dotnet/tools/minver > .version

WORKDIR /app

COPY ./*.sln ./

WORKDIR /app/src

COPY ./src/*/*.csproj ./

RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

WORKDIR /app

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=alpine-x64

WORKDIR /app/src

COPY ./src .

COPY --from=build-javascript /app/node_modules/${CLIENT_PACKAGE}/build /app/src/SqlStreamStore.Server/Browser/build

WORKDIR /app/build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /app

RUN dotnet run --project build/build.csproj

FROM microsoft/dotnet:2.2.1-runtime-deps-alpine3.8 AS runtime

WORKDIR /app
COPY --from=build-dotnet /app/.version ./
COPY --from=build-dotnet /app/publish ./

ENTRYPOINT ["/app/SqlStreamStore.Server"]
