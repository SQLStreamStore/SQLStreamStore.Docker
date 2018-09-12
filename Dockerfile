FROM microsoft/dotnet:2.1.402-sdk-alpine3.7 AS build
ARG TRAVIS_BUILD_NUMBER
ARG MYGET_API_KEY
WORKDIR /src

COPY ./src/*.sln ./
COPY ./src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

COPY ./NuGet.Config ./

RUN dotnet restore --runtime=alpine.3.7-x64

COPY ./src .

WORKDIR /build

COPY ./build/build.csproj .

RUN dotnet restore

COPY ./build .

WORKDIR /

RUN TRAVIS_BUILD_NUMBER=$TRAVIS_BUILD_NUMBER MYGET_API_KEY=$MYGET_API_KEY dotnet run --project build/build.csproj

WORKDIR /src/SqlStreamStore.HAL.DevServer

RUN dotnet publish --configuration=Release --output=/publish --no-restore --runtime=alpine.3.7-x64

FROM microsoft/dotnet:2.1.4-runtime-deps-alpine3.7 AS runtime

WORKDIR /app
COPY --from=build /publish ./

ENTRYPOINT ["dotnet", "SqlStreamStore.HAL.DevServer.dll"]
