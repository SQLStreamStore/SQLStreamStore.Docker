using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace SqlStreamStore.Server
{
    internal class SqlStreamStoreServerConfiguration
    {
        private static readonly string[] s_sensitiveKeys = typeof(ConfigurationData).GetProperties()
            .Where(property => property.GetCustomAttributes<SensitiveAttribute>().Any())
            .Select(property => property.Name)
            .ToArray();

        private static readonly string[] s_allKeys = typeof(ConfigurationData).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        private readonly ConfigurationData _configuration;
        private readonly IDictionary<string, (string source, string value)> _values;

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

            _values = new Dictionary<string, (string source, string value)>();

            void Log(string logName, IDictionary<string, string> data)
            {
                foreach (var (key, value) in data)
                {
                    _values[key] = (
                        logName,
                        s_sensitiveKeys.Contains(key)
                            ? new string('*', 8)
                            : value);
                }
            }

            _configuration = new ConfigurationData(
                new ConfigurationBuilder()
                    .Add(new Default(Log))
                    .Add(new CommandLine(args, Log))
                    .Add(new EnvironmentVariables(environment, Log))
                    .Build());
        }

        public override string ToString()
        {
            const string delimiter = " │ ";

            var column0Width = _values.Keys.Count > 0 ? _values.Keys.Max(x => x?.Length ?? 0) : 0;
            var column1Width = _values.Values.Count > 0 ? _values.Values.Max(_ => _.value?.Length ?? 0) : 0;
            var column2Width = _values.Values.Count > 0 ? _values.Values.Max(_ => _.source?.Length ?? 0) : 0;

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
                    s_allKeys.Select(key => new[] {delimiter, key, _values[key].value, _values[key].source}))
                .Aggregate(
                    new StringBuilder().AppendLine("SQL Stream Store Configuration:"),
                    (builder, values) => builder
                        .Append((values[1] ?? string.Empty).PadRight(column0Width, ' '))
                        .Append(values[0])
                        .Append((values[2] ?? string.Empty).PadRight(column1Width, ' '))
                        .Append(values[0])
                        .AppendLine(values[3]))
                .ToString();
        }

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
            [Sensitive] public string ConnectionString => _configuration.GetValue<string>(nameof(ConnectionString));
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
        }

        private class Default : IConfigurationSource
        {
            private readonly Action<string, IDictionary<string, string>> _log;

            public Default(Action<string, IDictionary<string, string>> log)
            {
                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                _log = log;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder) =>
                new DefaultConfigurationProvider(_log);
        }

        private class DefaultConfigurationProvider : ConfigurationProvider
        {
            private readonly Action<string, IDictionary<string, string>> _log;

            public DefaultConfigurationProvider(Action<string, IDictionary<string, string>> log)
            {
                if (log == null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                _log = log;
            }

            public override void Load()
            {
                Data = new Dictionary<string, string>
                {
                    [nameof(ConnectionString)] = default,
                    [nameof(Provider)] = "inmemory",
                    [nameof(LogLevel)] = nameof(LogEventLevel.Information),
                    [nameof(Schema)] = default,
                    [nameof(UseCanonicalUris)] = default,
                    [nameof(DisableDeletionTracking)] = default
                };

                _log(nameof(Default), Data);
            }
        }

        private class CommandLine : IConfigurationSource
        {
            private readonly IEnumerable<string> _args;
            private readonly Action<string, IDictionary<string, string>> _log;

            public CommandLine(
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

                Data = Data.Keys.ToDictionary(Computerize, x => Data[x]);

                _log(nameof(CommandLine), Data);
            }
        }

        private class EnvironmentVariables : IConfigurationSource
        {
            private readonly IDictionary _environment;
            private readonly Action<string, IDictionary<string, string>> _log;
            public string Prefix { get; set; } = "SQLSTREAMSTORE";

            public EnvironmentVariables(
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
                            key = Computerize(key.Remove(0, _prefix.Length)),
                            value = (string) entry.Value
                        })
                    .ToDictionary(x => x.key, x => x.value);

                _log(nameof(EnvironmentVariables), Data);
            }
        }

        private class SensitiveAttribute : Attribute
        {
        }
    }
}