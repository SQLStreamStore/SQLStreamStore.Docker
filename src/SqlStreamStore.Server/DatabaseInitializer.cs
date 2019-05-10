using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Npgsql;
using Serilog;
using SqlStreamStore.Infrastructure;
using static SqlStreamStore.Server.Constants;

namespace SqlStreamStore.Server
{
    internal class DatabaseInitializer
    {
        private readonly SqlStreamStoreServerConfiguration _configuration;
        private readonly SqlStreamStoreFactory _streamStoreFactory;

        public DatabaseInitializer(SqlStreamStoreServerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
            _streamStoreFactory = new SqlStreamStoreFactory(configuration);
        }

        public Task Initialize(CancellationToken cancellationToken = default)
        {
            switch (_configuration.Provider)
            {
                case mssql:
                    return InitializeMsSqlStreamStore(cancellationToken);
                case mysql:
                    return InitializeMySqlStreamStore(cancellationToken);
                case postgres:
                    return InitializePostgresStreamStore(cancellationToken);
                default:
                    Log.Warning("Provider {provider} has no initialization.", _configuration.Provider);
                    return Task.CompletedTask;
            }
        }

        private async Task InitializeMySqlStreamStore(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder(_configuration.ConnectionString);

            using (var streamStore = _streamStoreFactory.CreateMySqlStreamStore())
            {
                try
                {
                    using (var connection = new MySqlConnection(
                        new MySqlConnectionStringBuilder(_configuration.ConnectionString)
                        {
                            Database = null
                        }.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                        using (var command = new MySqlCommand(
                            $"CREATE DATABASE IF NOT EXISTS `{connectionStringBuilder.Database}`",
                            connection))
                        {
                            await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                        }
                    }

                    await streamStore.CreateSchemaIfNotExists(cancellationToken);
                }
                catch (SqlException ex)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript, ex);
                    throw;
                }
            }
        }

        private async Task InitializeMsSqlStreamStore(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(_configuration.ConnectionString);

            using (var streamStore = _streamStoreFactory.CreateMsSqlStreamStore())
            {
                try
                {
                    using (var connection = new SqlConnection(
                        new SqlConnectionStringBuilder(_configuration.ConnectionString)
                        {
                            InitialCatalog = "master"
                        }.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                        using (var command = new SqlCommand(
                            $@"
IF  NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{connectionStringBuilder.InitialCatalog}')
BEGIN
    CREATE DATABASE [{connectionStringBuilder.InitialCatalog}]
END;
",
                            connection))
                        {
                            await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                        }
                    }

                    await streamStore.CreateSchemaIfNotExists(cancellationToken);
                }
                catch (SqlException ex)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript, ex);
                    throw;
                }
            }
        }

        private async Task InitializePostgresStreamStore(CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_configuration.ConnectionString);
            using (var streamStore = _streamStoreFactory.CreatePostgresStreamStore())
            {
                try
                {
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
                                $"CREATE DATABASE {connectionStringBuilder.Database}",
                                connection))
                            {
                                await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                            }
                        }

                        await streamStore.CreateSchemaIfNotExists(cancellationToken);
                    }
                }
                catch (NpgsqlException ex)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript, ex);
                    throw;
                }
            }
        }

        private static void SchemaCreationFailed(Func<string> getSchemaCreationScript, Exception ex)
            => Log.Error(
                new StringBuilder()
                    .Append("Could not create schema: {ex}")
                    .AppendLine()
                    .Append(
                        "Does your connection string have enough permissions? If not, run the following sql script as a privileged user:")
                    .AppendLine()
                    .Append("{script}")
                    .ToString(),
                ex,
                getSchemaCreationScript());
    }
}