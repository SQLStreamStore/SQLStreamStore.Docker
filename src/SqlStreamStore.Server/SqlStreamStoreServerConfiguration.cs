using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Serilog.Events;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreServerConfiguration
    {
        private readonly ConfigurationData _configuration;

        public IDictionary Environment { get; }
        public string[] Args { get; }

        public bool UseCanonicalUris => _configuration.UseCanonicalUris;
        public LogEventLevel LogLevel => _configuration.LogLevel;
        public string ConnectionString => _configuration.ConnectionString;
        public string Schema => _configuration.Schema;
        public string Provider => _configuration.Provider.ToLowerInvariant();

        public bool DisableDeletionTracking => _configuration.DisableDeletionTracking;

        public SqlStreamStoreServerConfiguration(
            IDictionary environment,
            string[] args)
        {
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            Environment = environment;
            Args = args;

            _configuration = new ConfigurationData(
                new ConfigurationBuilder()
                    .Add(new DefaultSource())
                    .Add(new CommandLineSource(args))
                    .Add(new EnvironmentVariablesSource(environment))
                    .Build());
        }

        public override string ToString() => _configuration.ToString();

        private static string Computerize(string value) =>
            string.Join(
                string.Empty,
                (value?.Replace("-", "_").ToLowerInvariant()
                 ?? string.Empty).Split('_')
                .Select(x => new string(x.Select((c, i) => i == 0 ? char.ToUpper(c) : c).ToArray())));

        private class ConfigurationData
        {
            private readonly IConfigurationRoot _configuration;

            public bool UseCanonicalUris => _configuration.GetValue<bool>(nameof(UseCanonicalUris));
            public LogEventLevel LogLevel => _configuration.GetValue(nameof(LogLevel), LogEventLevel.Information);
            public string ConnectionString => _configuration.GetValue<string>(nameof(ConnectionString));
            public string Schema => _configuration.GetValue<string>(nameof(Schema));
            public string Provider => _configuration.GetValue<string>(nameof(Provider));
            public bool DisableDeletionTracking => _configuration.GetValue<bool>(nameof(DisableDeletionTracking));

            public ConfigurationData(IConfigurationRoot configuration)
            {
                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                _configuration = configuration;
            }

            public override string ToString()
            {
                var values = ProtectConfiguration(CollectConfiguration());

                const string delimiter = " │ ";

                var column0Width = values.Keys.Count > 0 ? values.Keys.Max(x => x?.Length ?? 0) : 0;
                var column1Width = values.Values.Count > 0 ? values.Values.Max(_ => _.value?.Length ?? 0) : 0;
                var column2Width = values.Values.Count > 0 ? values.Values.Max(_ => _.source?.Length ?? 0) : 0;

                return new[]
                    {
                        new[]
                        {
                            delimiter,
                            "Argument",
                            "Value",
                            "Source"
                        },
                        new[]
                        {
                            "─┼─",
                            new string('─', column0Width),
                            new string('─', column1Width),
                            new string('─', column2Width)
                        }
                    }
                    .Concat(
                        values.Keys.Select(key => new[] {delimiter, key, values[key].value, values[key].source}))
                    .Aggregate(
                        new StringBuilder().AppendLine("SQL Stream Store Configuration:"),
                        (builder, v) => builder
                            .Append((v[1] ?? string.Empty).PadRight(column0Width, ' '))
                            .Append(v[0])
                            .Append((v[2] ?? string.Empty).PadRight(column1Width, ' '))
                            .Append(v[0])
                            .AppendLine(v[3]))
                    .ToString();
            }

            private static string ProtectConnectionString(string connectionString)
            {
                var builder = new DbConnectionStringBuilder(false)
                {
                    ConnectionString = connectionString
                };

                var sensitiveKeys = new[]
                {
                    "Password",
                    "Pwd",
                };


                foreach (var key in sensitiveKeys)
                {
                    if (builder.ContainsKey(key))
                    {
                        builder[key] = "******";
                    }
                }

                return builder.ConnectionString;
            }

            private IDictionary<string, (string source, string value)> ProtectConfiguration(
                IDictionary<string, (string source, string value)> values)
                => values.ToDictionary(
                    x => x.Key,
                    x => x.Key == nameof(ConnectionString)
                        ? (x.Value.source, ProtectConnectionString(x.Value.value))
                        : x.Value);

            private IDictionary<string, (string source, string value)> CollectConfiguration()
            {
                var values = new Dictionary<string, (string source, string value)>();

                foreach (var provider in _configuration.Providers)
                {
                    var source = provider.GetType().Name;
                    foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), default))
                    {
                        if (provider.TryGet(key, out var value))
                        {
                            values[key] = (source, value);
                        }
                    }
                }

                return values;
            }
        }

        private class DefaultSource : IConfigurationSource
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder) =>
                new Default();
        }

        private class Default : ConfigurationProvider
        {
            public override void Load()
            {
                Data = new Dictionary<string, string>
                {
                    [nameof(ConnectionString)] = default,
                    [nameof(Provider)] = Constants.inmemory,
                    [nameof(LogLevel)] = nameof(LogEventLevel.Information),
                    [nameof(Schema)] = default,
                    [nameof(UseCanonicalUris)] = default,
                    [nameof(DisableDeletionTracking)] = default
                };
            }
        }

        private class CommandLineSource : IConfigurationSource
        {
            private readonly IEnumerable<string> _args;

            public CommandLineSource(IEnumerable<string> args)
            {
                if (args == null)
                {
                    throw new ArgumentNullException(nameof(args));
                }

                _args = args;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new CommandLine(_args);
        }

        private class CommandLine : CommandLineConfigurationProvider
        {
            public CommandLine(
                IEnumerable<string> args,
                IDictionary<string, string> switchMappings = null)
                : base(args, switchMappings)
            {
            }

            public override void Load()
            {
                base.Load();

                Data = Data.Keys.ToDictionary(Computerize, x => Data[x]);
            }
        }

        private class EnvironmentVariablesSource : IConfigurationSource
        {
            private readonly IDictionary _environment;
            public string Prefix { get; set; } = "SQLSTREAMSTORE";

            public EnvironmentVariablesSource(
                IDictionary environment)
            {
                if (environment == null)
                {
                    throw new ArgumentNullException(nameof(environment));
                }

                _environment = environment;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new EnvironmentVariables(Prefix, _environment);
        }

        private class EnvironmentVariables : ConfigurationProvider
        {
            private readonly IDictionary _environment;
            private readonly string _prefix;

            public EnvironmentVariables(
                string prefix,
                IDictionary environment)
            {
                if (environment == null)
                {
                    throw new ArgumentNullException(nameof(environment));
                }

                _prefix = $"{prefix}_";
                _environment = environment;
            }

            public override void Load()
            {
                Data = (from entry in _environment.OfType<DictionaryEntry>()
                        let key = (string) entry.Key
                        where key.StartsWith(_prefix)
                        select new
                        {
                            key = Computerize(key.Remove(0, _prefix.Length)),
                            value = (string) entry.Value
                        })
                    .ToDictionary(x => x.key, x => x.value);
            }
        }
    }
}