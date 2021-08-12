﻿using System;
using Microsoft.Extensions.Logging;
using System.Text;

namespace VpnHood.Logging
{

    public abstract class TextLogger : ILogger, ILoggerProvider
    {
        private readonly LoggerExternalScopeProvider _scopeProvider = new();
        private readonly bool _includeScopes;

        public TextLogger(bool includeScopes)
        {
            _includeScopes = includeScopes;
        }

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public virtual void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        protected void GetScopeInformation(StringBuilder stringBuilder)
        {
            var scopeProvider = _scopeProvider;
            if (scopeProvider != null)
            {
                var initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, length) = state;
                    var first = length == builder.Length;
                    builder.Append(first ? "=> " : " => ").Append(scope);
                }, (stringBuilder, initialLength));
            }
        }

        protected string FormatLog<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logBuilder = new StringBuilder();

            if (_includeScopes)
            {
                logBuilder.AppendLine();
                logBuilder.Append($"{logLevel.ToString().Substring(0, 4)} ");
                GetScopeInformation(logBuilder);
                logBuilder.AppendLine();
            }

            var message = "|" + eventId.Name + " | " + formatter(state, exception);
            logBuilder.Append(message);
            return logBuilder.ToString();
        }

        public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
    }
}
