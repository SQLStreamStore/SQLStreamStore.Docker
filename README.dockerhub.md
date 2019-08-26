# SQL Stream Store Docker Container

This container includes both the SQLStreamStore.Server and the user interface. The server runs on port 80 inside the container.

As of this time, no `latest` tag is used. The following convention is used instead: `${version}-{runtime}` e.g., `1.2.0-alpine3.9`.

## Getting Started

```bash
docker pull 
```

### Environment Variables

Name|Description|Valid Values|Default
---|---|---|---
SQLSTREAMSTORE_PROVIDER|The provider.|`inmemory`, `mssql`, `postgres`, `mysql`|`inmemory`
SQLSTREAMSTORE_CONNECTION_STRING|The connection string. Not valid when provider=`inmemory`|
SQLSTREAMSTORE_SCHEMA|The database schema. Not valid when provider=`inmemory, mysql`|
SQLSTREAMSTORE_LOG_LEVEL|The log level.|`FATAL`, `ERROR`, `WARNING`, `INFORMATION`, `VERBOSE`|`INFORMATION`
SQLSTREAMSTORE_DISABLE_DELETION_TRACKING|Records deleted streams and messages in a `$deleted` stream.|`true`,`false`|`false`
SQLSTREAMSTORE_USE_CANONICAL_URIS|Maximize cache hits by sorting query string parameters. Do not use in environments where the query string is sorted by the host e.g. AWS API Gateway.|`true`, `false`|`false`

### Commands

Some helper commands are provided to help you initialize the database.

Name|Description
---|---
`init-database`|Creates a database (e.g., `CREATE DATABASE`) if you don't have one.
`init`|Initializes the SQL Stream Store Schema. This includes any tables, indices, and stored procedures SQL Stream Store requires.

### Examples

#### Quickstart
This runs in memory, so nothing is persisted.
```bash
docker run --rm  -p 5000:80 sqlstreamstore/server:1.2.0-alpine3.9
```

#### Postgres w/ Verbose Logging

```bash
# Start a postgres container
docker run -p 5432:5432 --rm --detach --name sss-postgres postgres:9.6

# Get its ip address to supply to the connection string 
docker inspect --format '{{ .NetworkSettings.IPAddress }}' sss-postgres # 172.17.0.3

# Create the database 
docker run --rm \
    -e "SQLSTREAMSTORE_PROVIDER=postgres" \
    -e "SQLSTREAMSTORE_CONNECTION_STRING=host=172.17.0.3;User Id=postgres;database=mydatabase" \
    -e "SQLSTREAMSTORE_LOG_LEVEL=verbose" -e "SQLSTREAMSTORE_SCHEMA=public" \
    -p 5000:80 \
    sqlstreamstore/server:1.2.0-alpine3.9 init-database

# Create all tables, indices, and functions
docker run --rm \
    -e "SQLSTREAMSTORE_PROVIDER=postgres" \
    -e "SQLSTREAMSTORE_CONNECTION_STRING=host=172.17.0.3;User Id=postgres;database=mydatabase" \
    -e "SQLSTREAMSTORE_LOG_LEVEL=verbose" -e "SQLSTREAMSTORE_SCHEMA=public" \
    -p 5000:80 \
    sqlstreamstore/server:1.2.0-alpine3.9 init

# Run the server
docker run --rm \
    -e "SQLSTREAMSTORE_PROVIDER=postgres" \
    -e "SQLSTREAMSTORE_CONNECTION_STRING=host=172.17.0.3;User Id=postgres;database=mydatabase" \
    -e "SQLSTREAMSTORE_LOG_LEVEL=verbose" -e "SQLSTREAMSTORE_SCHEMA=public" \
    -p 5000:80 \
    sqlstreamstore/server:1.2.0-alpine3.9
```