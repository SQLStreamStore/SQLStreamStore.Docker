FROM microsoft/dotnet:2.2.102-sdk-stretch AS build

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

WORKDIR /app/build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

COPY --from=sqlstreamstore/browser:0.9 /var/www /app/src/SqlStreamStore.Server/Browser/build

WORKDIR /app

RUN MYGET_API_KEY=$MYGET_API_KEY \
  dotnet run --project build/build.csproj

FROM microsoft/dotnet:2.2.1-runtime-deps-alpine3.8 AS runtime

WORKDIR /app
COPY --from=build /app/.version ./
COPY --from=build /app/publish ./

ENTRYPOINT ["/app/SqlStreamStore.Server"]
