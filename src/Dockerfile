FROM microsoft/dotnet:2.1.301-sdk-alpine3.7 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY SqlStreamStore.HAL/*.csproj ./SqlStreamStore.HAL/
COPY SqlStreamStore.HAL.DevServer/*.csproj ./SqlStreamStore.HAL.DevServer/

WORKDIR /src/SqlStreamStore.HAL.DevServer
RUN dotnet restore

# copy everything else and build app
WORKDIR /src
COPY SqlStreamStore.HAL/. ./SqlStreamStore.HAL/
COPY SqlStreamStore.HAL.DevServer/. ./SqlStreamStore.HAL.DevServer/
WORKDIR /src/SqlStreamStore.HAL.DevServer
RUN dotnet publish -c Release -o /build

FROM microsoft/dotnet:2.1.1-aspnetcore-runtime-alpine3.7 AS runtime

WORKDIR /app
COPY --from=build /build ./

ENTRYPOINT ["dotnet", "SqlStreamStore.HAL.DevServer.dll"]