FROM microsoft/dotnet:2.1.403-sdk-alpine3.7 AS build
ARG MYGET_API_KEY
ARG MINVER_BUILD_METADATA

RUN apk add --no-cache \
  nodejs \
  yarn \
  libcurl

WORKDIR /src

COPY ./src/*.sln ./
COPY ./src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=alpine.3.7-x64

COPY ./src .

WORKDIR /docs

COPY ./docs/package.json ./docs/yarn.lock ./

WORKDIR /.git

COPY ./.git .

WORKDIR /build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /

RUN MINVER_BUILD_METADATA=$MINVER_BUILD_METADATA \
  MYGET_API_KEY=$MYGET_API_KEY \
  dotnet run --project build/build.csproj

FROM microsoft/dotnet:2.1.5-runtime-deps-alpine3.7 AS runtime

WORKDIR /app
COPY --from=build /publish ./

ENTRYPOINT ["/app/SqlStreamStore.HAL.DevServer"]
