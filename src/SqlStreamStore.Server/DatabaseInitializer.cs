using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Npgsql;
using Serilog;
using SqlStreamStore.Infrastructure;

namespace SqlStreamStore.Server
{
    internal class DatabaseInitializer
    {
        private readonly SqlStreamStoreServerConfiguration _configuration;

        public DatabaseInitializer(SqlStreamStoreServerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
        }

        public Task Initialize(CancellationToken cancellationToken = default)
        {
            switch (_configuration.Provider)
            {
                case Constants.mssql:
                    return InitializeMsSql(cancellationToken);
                case Constants.mysql:
                    return InitializeMySql(cancellationToken);
                case Constants.postgres:
                    return InitializePostgres(cancellationToken);
                default:
                    Log.Warning("Provider {provider} has no database initializer.", _configuration.Provider);
                    return Task.CompletedTask;
            }
        }

        private async Task InitializeMySql(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder(_configuration.ConnectionString);

            var cmdText = $"CREATE DATABASE IF NOT EXISTS `{connectionStringBuilder.Database}`";

            Log.Information(
                "Creating database '{database}' at server '{server}' with the statement: {cmdText}",
                connectionStringBuilder.Database,
                connectionStringBuilder.Server,
                cmdText);

            using (var connection = new MySqlConnection(
                new MySqlConnectionStringBuilder(_configuration.ConnectionString)
                {
                    Database = null
                }.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                using (var command = new MySqlCommand(
                    cmdText,
                    connection))
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                }
            }
        }

        private async Task InitializeMsSql(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(_configuration.ConnectionString);

            var cmdText = $@"
IF  NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{connectionStringBuilder.InitialCatalog}')
BEGIN
    CREATE DATABASE [{connectionStringBuilder.InitialCatalog}]
END;
";
            Log.Information(
                "Creating database '{database}' at server '{server}' with the statement: {cmdText}",
                connectionStringBuilder.InitialCatalog,
                connectionStringBuilder.DataSource,
                cmdText);

            using (var connection = new SqlConnection(
                new SqlConnectionStringBuilder(_configuration.ConnectionString)
                {
                    InitialCatalog = "master"
                }.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                using (var command = new SqlCommand(
                    cmdText,
                    connection))
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                }
            }
        }

        private async Task InitializePostgres(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_configuration.ConnectionString);

            var cmdText = $"CREATE DATABASE {connectionStringBuilder.Database}";

            Log.Information(
                "Creating database '{database}' at server '{server}' with the statement: {cmdText}",
                connectionStringBuilder.Database,
                connectionStringBuilder.Host,
                cmdText);

            using (var connection = new NpgsqlConnection(
                new NpgsqlConnectionStringBuilder(_configuration.ConnectionString)
                {
                    Database = null
                }.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                async Task<bool> DatabaseExists()
                {
                    using (var command = new NpgsqlCommand(
                        $"SELECT 1 FROM pg_database WHERE datname = '{connectionStringBuilder.Database}'",
                        connection))
                    {
                        return await command.ExecuteScalarAsync(cancellationToken).NotOnCapturedContext()
                               != null;
                    }
                }

                if (!await DatabaseExists())
                {
                    using (var command = new NpgsqlCommand(
                        cmdText,
                        connection))
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                    }
                }
            }
        }
    }
}