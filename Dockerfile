FROM microsoft/dotnet:2.1.500-sdk-alpine3.7 AS build
ARG MYGET_API_KEY

RUN apk add --no-cache \
  nodejs \
  yarn \
  libcurl

WORKDIR /app/src

COPY ./src/*.sln ./
COPY ./src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=alpine.3.7-x64

COPY ./src .

WORKDIR /app/docs

COPY ./docs/package.json ./docs/yarn.lock ./

WORKDIR /app/.git

COPY ./.git .

WORKDIR /app/build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /app

RUN MYGET_API_KEY=$MYGET_API_KEY \
  dotnet run --project build/build.csproj

FROM microsoft/dotnet:2.1.6-runtime-deps-alpine3.7 AS runtime

WORKDIR /app
COPY --from=build /app/publish ./

ENTRYPOINT ["/app/SqlStreamStore.HAL.DevServer"]
