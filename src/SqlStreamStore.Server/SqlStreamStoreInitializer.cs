using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Npgsql;
using Serilog;
using static SqlStreamStore.Server.Constants;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreInitializer
    {
        private static readonly ILogger s_Log = Log.ForContext<SqlStreamStoreInitializer>();

        private readonly SqlStreamStoreServerConfiguration _configuration;
        private readonly SqlStreamStoreFactory _streamStoreFactory;

        public SqlStreamStoreInitializer(SqlStreamStoreServerConfiguration configuration)
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
                    s_Log.Warning("Provider {provider} has no initialization.", _configuration.Provider);
                    return Task.CompletedTask;
            }
        }

        private async Task InitializeMySqlStreamStore(CancellationToken cancellationToken)
        {
            using (var streamStore = _streamStoreFactory.CreateMySqlStreamStore())
            {
                try
                {
                    await streamStore.CreateSchemaIfNotExists(cancellationToken);
                }
                catch (MySqlException)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript);
                    throw;
                }
            }
        }

        private async Task InitializeMsSqlStreamStore(CancellationToken cancellationToken)
        {
            using (var streamStore = _streamStoreFactory.CreateMsSqlStreamStore())
            {
                try
                {
                    await streamStore.CreateSchemaIfNotExists(cancellationToken);
                }
                catch (SqlException)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript);
                    throw;
                }
            }
        }

        private async Task InitializePostgresStreamStore(CancellationToken cancellationToken)
        {
            using (var streamStore = _streamStoreFactory.CreatePostgresStreamStore())
            {
                try
                {
                    await streamStore.CreateSchemaIfNotExists(cancellationToken);
                }
                catch (NpgsqlException)
                {
                    SchemaCreationFailed(streamStore.GetSchemaCreationScript);
                    throw;
                }
            }
        }

        private static void SchemaCreationFailed(Func<string> getSchemaCreationScript)
        {
            s_Log.Error(
                new StringBuilder()
                    .Append("Could not create schema.")
                    .AppendLine()
                    .Append(
                        "Does your connection string have enough permissions? If not, run the following sql script as a privileged user:")
                    .AppendLine()
                    .Append("{script}")
                    .ToString(),
                getSchemaCreationScript());
        }
    }
}