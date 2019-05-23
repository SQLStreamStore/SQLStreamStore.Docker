using System;
using Serilog;
using static SqlStreamStore.Server.Constants;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreFactory
    {
        private static readonly ILogger s_Log = Log.ForContext<SqlStreamStoreFactory>();

        private readonly SqlStreamStoreServerConfiguration _configuration;

        public SqlStreamStoreFactory(SqlStreamStoreServerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
        }

        public IStreamStore Create()
        {
            var provider = _configuration.Provider;

            s_Log.Information("Creating stream store for provider {provider}.", provider);

            switch (provider)
            {
                case inmemory:
                    return CreateInMemoryStreamStore();
                case mssql:
                    return CreateMsSqlStreamStore();
                case mysql:
                    return CreateMySqlStreamStore();
                case postgres:
                    return CreatePostgresStreamStore();
                default:
                    throw new InvalidOperationException($"No provider factory for provider '{provider}' found.");
            }
        }

        public InMemoryStreamStore CreateInMemoryStreamStore()
        {
            if (_configuration.Schema != default)
            {
                LogNotSupported(mysql, nameof(_configuration.Schema));
            }

            if (_configuration.DisableDeletionTracking)
            {
                LogNotSupported(mysql, nameof(_configuration.DisableDeletionTracking));
            }

            return new InMemoryStreamStore();
        }

        public MsSqlStreamStoreV3 CreateMsSqlStreamStore()
        {
            var settings = new MsSqlStreamStoreV3Settings(_configuration.ConnectionString)
            {
                DisableDeletionTracking = _configuration.DisableDeletionTracking
            };

            if (_configuration.Schema != null)
            {
                settings.Schema = _configuration.Schema;
            }

            return new MsSqlStreamStoreV3(settings);
        }

        public MySqlStreamStore CreateMySqlStreamStore()
        {
            if (_configuration.Schema != default)
            {
                LogNotSupported(mysql, nameof(_configuration.Schema));
            }

            return new MySqlStreamStore(new MySqlStreamStoreSettings(_configuration.ConnectionString)
            {
                DisableDeletionTracking = _configuration.DisableDeletionTracking
            });
        }

        public PostgresStreamStore CreatePostgresStreamStore()
        {
            var settings = new PostgresStreamStoreSettings(_configuration.ConnectionString)
            {
                DisableDeletionTracking = _configuration.DisableDeletionTracking
            };

            if (_configuration.Schema != null)
            {
                settings.Schema = _configuration.Schema;
            }

            return new PostgresStreamStore(settings);
        }

        private static void LogNotSupported(string provider, string configurationKey) =>
            s_Log.Warning(
                "Configuration key '{configurationKey}' is not supported for provider {provider}. It will be ignored.",
                configurationKey,
                provider);
    }
}