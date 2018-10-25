FROM microsoft/dotnet:2.1.403-sdk-alpine3.7 AS build
ARG TRAVIS_BUILD_NUMBER
ARG TRAVIS_PULL_REQUEST_SHA
ARG TRAVIS_COMMIT
ARG TRAVIS_PULL_REQUEST
ARG TRAVIS_BRANCH
ARG MYGET_API_KEY

RUN apk add nodejs yarn --no-cache

WORKDIR /src

COPY ./src/*.sln ./
COPY ./src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=alpine.3.7-x64

COPY ./src .

WORKDIR /docs

COPY ./docs/package.json ./docs/yarn.lock ./

WORKDIR /build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /

RUN TRAVIS_BUILD_NUMBER=$TRAVIS_BUILD_NUMBER \
  MYGET_API_KEY=$MYGET_API_KEY \
  TRAVIS_PULL_REQUEST_SHA=$TRAVIS_PULL_REQUEST_SHA \
  TRAVIS_COMMIT=$TRAVIS_COMMIT \
  TRAVIS_PULL_REQUEST=$TRAVIS_PULL_REQUEST \
  TRAVIS_BRANCH=$TRAVIS_BRANCH \
  dotnet run --project build/build.csproj

FROM microsoft/dotnet:2.1.5-runtime-deps-alpine3.7 AS runtime

WORKDIR /app
COPY --from=build /publish ./

ENTRYPOINT ["/app/SqlStreamStore.HAL.DevServer"]
