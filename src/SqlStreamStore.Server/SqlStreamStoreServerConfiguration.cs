using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.VisualBasic;
using Serilog.Events;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreServerConfiguration
    {
        private readonly IConfigurationRoot _configuration;
        private Dictionary<string, (string source, string value)> _values;

        public bool UseCanonicalUris => _configuration.GetValue<bool>("use-canonical-uris");
        public LogEventLevel LogLevel => _configuration.GetValue("log-level", LogEventLevel.Information);
        public string ConnectionString => _configuration.GetValue<string>("connection-string");
        public string Schema => _configuration.GetValue<string>("schema");
        public string Provider => _configuration.GetValue<string>("provider");

        public SqlStreamStoreServerConfiguration(
            IDictionary environment,
            string[] args)
        {
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            _values = new Dictionary<string, (string source, string value)>();

            void Log(string logName, IDictionary<string, string> data)
            {
                foreach (var (key, value) in data)
                {
                    _values[key] = (logName, value);
                    Console.WriteLine($"{key} came from {logName}");
                }
            }

            _configuration = new ConfigurationBuilder()
                .Add(new CommandLineConfigurationSource(args, Log))
                .Add(new EnvironmentVariablesConfigurationSource(environment, Log))
                .Build();
        }

        public override string ToString()
        {
            const string delimiter = " | ";

            var column0Width = _values.Keys.Count > 0 ? _values.Keys.Max(x => x.Length) : 0;
            var column1Width = _values.Values.Count > 0 ?_values.Values.Max(_ => _.value.Length) : 0;
            var column2Witdth = _values.Values.Count > 0 ?_values.Values.Max(_ => _.source.Length) : 0;

            StringBuilder Append(StringBuilder builder, string column0, string column1, string column2) =>
                builder
                    .Append(column0.PadRight(column0Width, ' '))
                    .Append(delimiter)
                    .Append(column1.PadRight(column1Width, ' '))
                    .Append(delimiter)
                    .AppendLine(column2);

            return _values.Keys.OrderBy(x => x).Aggregate(
                Append(new StringBuilder().AppendLine("SQL Stream Store Configuration:"), "Argument", "Value", "Source")
                    .AppendLine(new string('-', column0Width + column1Width + column2Witdth)),
                (builder, key) => Append(
                    builder,
                    key,
                    _values[key].value,
                    _values[key].source)).ToString();
        }

        private class CommandLineConfigurationSource : IConfigurationSource
        {
            private readonly IEnumerable<string> _args;
            private readonly Action<string, IDictionary<string, string>> _log;

            public CommandLineConfigurationSource(
                IEnumerable<string> args,
                Action<string, IDictionary<string, string>> log)
            {
                if (args == null)
                {
                    throw new ArgumentNullException(nameof(args));
                }

                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                _args = args;
                _log = log;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new CommandLineConfigurationProvider(_args, _log);
        }

        private class CommandLineConfigurationProvider
            : Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationProvider
        {
            private readonly Action<string, IDictionary<string, string>> _log;

            public CommandLineConfigurationProvider(IEnumerable<string> args,
                Action<string, IDictionary<string, string>> log,
                IDictionary<string, string> switchMappings = null) : base(args, switchMappings)
            {
                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                _log = log;
            }

            public override void Load()
            {
                base.Load();

                _log(nameof(CommandLineConfigurationSource), Data);
            }
        }

        private class EnvironmentVariablesConfigurationSource : IConfigurationSource
        {
            private readonly IDictionary _environment;
            private readonly Action<string, IDictionary<string, string>> _log;
            public string Prefix { get; set; } = "SQLSTREAMSTORE";

            public EnvironmentVariablesConfigurationSource(
                IDictionary environment,
                Action<string, IDictionary<string, string>> log)
            {
                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                if (environment == null)
                {
                    throw new ArgumentNullException(nameof(environment));
                }

                _environment = environment;
                _log = log;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new EnvironmentVariablesConfigurationProvider(Prefix, _environment, _log);
        }

        private class EnvironmentVariablesConfigurationProvider : ConfigurationProvider
        {
            private readonly IDictionary _environment;
            private readonly Action<string, IDictionary<string, string>> _log;
            private readonly string _prefix;

            public EnvironmentVariablesConfigurationProvider(
                string prefix,
                IDictionary environment,
                Action<string, IDictionary<string, string>> log)
            {
                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                if (environment == null)
                {
                    throw new ArgumentNullException(nameof(environment));
                }

                _prefix = $"{prefix}_";
                _environment = environment;
                _log = log;
            }

            public override void Load()
            {
                Data = (from entry in _environment.OfType<DictionaryEntry>()
                        let key = (string) entry.Key
                        where key.StartsWith(_prefix)
                        select new
                        {
                            key = key.Remove(0, _prefix.Length).Replace('_', '-').ToLowerInvariant(),
                            value = (string) entry.Value
                        })
                    .ToDictionary(x => x.key, x => x.value);

                _log(nameof(EnvironmentVariablesConfigurationSource), Data);
            }
        }
    }
}