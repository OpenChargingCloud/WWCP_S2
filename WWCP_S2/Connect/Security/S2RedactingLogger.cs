/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP S2 <https://github.com/OpenChargingCloud/WWCP_S2>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Collections;
using System.Globalization;
using System.Text;

using Microsoft.Extensions.Logging;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A logger that passes every message through <see cref="S2LogRedaction"/> before it reaches
    /// the logger it decorates (PLAN.md §3.6). Both the formatted message and the values of the
    /// structured state are redacted, because a log sink may render either of them.
    ///
    /// <para>
    /// An <see cref="Exception"/> is forwarded unchanged: its message is read-only, and wrapping it
    /// would destroy the type a sink filters on and the stack trace it prints. This library therefore
    /// never puts request content into an exception it logs; an application that does must redact it
    /// before it throws.
    /// </para>
    /// </summary>
    /// <param name="InnerLogger">The logger to decorate.</param>
    public sealed class S2RedactingLogger(ILogger InnerLogger) : ILogger
    {

        #region Properties

        /// <summary>
        /// The decorated logger.
        /// </summary>
        public ILogger InnerLogger { get; } = InnerLogger ?? throw new ArgumentNullException(nameof(InnerLogger));

        #endregion


        #region BeginScope<TState>(State)

        /// <summary>
        /// Begin a logical operation scope, with the state redacted.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="State">The state of the scope.</param>
        public IDisposable? BeginScope<TState>(TState State)
            where TState : notnull
        {

            if (State is IReadOnlyList<KeyValuePair<String, Object?>> values)
                return InnerLogger.BeginScope(
                           new RedactedState(
                               values,
                               () => S2LogRedaction.Redact(State.ToString()) ?? ""
                           )
                       );

            return InnerLogger.BeginScope(State);

        }

        #endregion

        #region IsEnabled(LogLevel)

        /// <summary>
        /// Whether the given log level is enabled.
        /// </summary>
        /// <param name="LogLevel">A log level.</param>
        public Boolean IsEnabled(LogLevel LogLevel)
            => InnerLogger.IsEnabled(LogLevel);

        #endregion

        #region Log<TState>(LogLevel, EventId, State, Exception, Formatter)

        /// <summary>
        /// Write a redacted log entry.
        /// </summary>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <param name="LogLevel">The log level of the entry.</param>
        /// <param name="EventId">The identification of the event.</param>
        /// <param name="State">The state of the entry.</param>
        /// <param name="Exception">An optional exception.</param>
        /// <param name="Formatter">A function creating the message of the entry.</param>
        public void Log<TState>(LogLevel                         LogLevel,
                                EventId                          EventId,
                                TState                           State,
                                Exception?                       Exception,
                                Func<TState, Exception?, String> Formatter)
        {

            ArgumentNullException.ThrowIfNull(Formatter);

            // A structured sink (JSON, OpenTelemetry, ...) renders the state, a console sink
            // the formatted message: both have to be redacted.
            if (State is IReadOnlyList<KeyValuePair<String, Object?>> values)
            {

                // The message is rendered from the *redacted* values, never by redacting the
                // already formatted text: "token s3cr3t of node-1" is none of the three shapes
                // the textual redaction knows, so the secret would survive in the message while
                // the structured state of the very same entry was masked.
                InnerLogger.Log(
                    LogLevel,
                    EventId,
                    new RedactedState(
                        values,
                        () => S2LogRedaction.Redact(Formatter(State, Exception)) ?? ""
                    ),
                    Exception,
                    static (state, _) => state.ToString()
                );

            }

            // A plain text state is replaced by its redacted text, so that a sink rendering the
            // state instead of the message sees the redacted one as well.
            else if (State is String)
            {

                var message = S2LogRedaction.Redact(Formatter(State, Exception)) ?? "";

                InnerLogger.Log(
                    LogLevel,
                    EventId,
                    message,
                    Exception,
                    static (state, _) => state
                );

            }

            // Any other state object is forwarded as it is: replacing it would break every sink
            // that casts it back to its own type. Only its formatted message can be redacted,
            // which is why a state object that carries a secret must redact its own ToString().
            else
                InnerLogger.Log(
                    LogLevel,
                    EventId,
                    State,
                    Exception,
                    (state, exception) => S2LogRedaction.Redact(Formatter(state, exception)) ?? ""
                );

        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"redacting {InnerLogger}";

        #endregion


        #region (private class) RedactedState

        /// <summary>
        /// The structured state of a log entry, with every value redacted. The message template
        /// ("{OriginalFormat}") is kept as it is: it holds names, never values.
        /// </summary>
        private sealed class RedactedState : IReadOnlyList<KeyValuePair<String, Object?>>
        {

            /// <summary>
            /// The name under which the message template is kept in the state of a log entry.
            /// </summary>
            public const String OriginalFormat = "{OriginalFormat}";

            private readonly KeyValuePair<String, Object?>[]  values;
            private readonly String                           message;

            public RedactedState(IReadOnlyList<KeyValuePair<String, Object?>>  Values,
                                 Func<String>                                  FallbackMessage)
            {

                this.values  = new KeyValuePair<String, Object?>[Values.Count];

                String?  template   = null;
                var      arguments  = new List<Object?>(Values.Count);

                for (var i = 0; i < Values.Count; i++)
                {

                    var name   = Values[i].Key;
                    var value  = Values[i].Value;

                    if (name == OriginalFormat)
                    {
                        template   = value as String;
                        values[i]  = Values[i];
                        continue;
                    }

                    var redacted = S2LogRedaction.IsSecretName(name)
                                       ? S2LogRedaction.Mask
                                       : Redact(value);

                    values[i]    = new KeyValuePair<String, Object?>(name, redacted);

                    arguments.Add(redacted);

                }

                this.message = Render(template, arguments) ?? FallbackMessage();

            }

            /// <summary>
            /// Render the message template with the redacted values. Every placeholder name is
            /// replaced by its position, so that alignments and format specifiers survive
            /// ("{Count,5:N0}" becomes "{0,5:N0}"). Returns null when there is no template or it
            /// cannot be rendered; the caller then falls back to the redacted text of the
            /// original formatter.
            /// </summary>
            private static String? Render(String?        Template,
                                          List<Object?>  Arguments)
            {

                if (Template is null)
                    return null;

                try
                {

                    var format  = new StringBuilder(Template.Length + 8);
                    var index   = 0;

                    for (var i = 0; i < Template.Length; i++)
                    {

                        var character = Template[i];

                        if (character == '{')
                        {

                            // A doubled brace is an escape, not a placeholder.
                            if (i + 1 < Template.Length && Template[i + 1] == '{')
                            {
                                format.Append("{{");
                                i++;
                                continue;
                            }

                            var end = Template.IndexOf('}', i + 1);

                            if (end < 0)
                                return null;

                            // Everything from the first ',' or ':' on is an alignment or a format
                            // specifier and is kept; the name itself is replaced by the index.
                            var placeholder  = Template[(i + 1)..end];
                            var separator    = placeholder.IndexOfAny([ ',', ':' ]);

                            format.Append('{').
                                   Append(index++).
                                   Append(separator >= 0 ? placeholder[separator..] : "").
                                   Append('}');

                            i = end;
                            continue;

                        }

                        if (character == '}' && i + 1 < Template.Length && Template[i + 1] == '}')
                        {
                            format.Append("}}");
                            i++;
                            continue;
                        }

                        format.Append(character);

                    }

                    // A template whose placeholders do not match the values is not this
                    // decorator's to interpret.
                    if (index != Arguments.Count)
                        return null;

                    return String.Format(
                               CultureInfo.InvariantCulture,
                               format.ToString(),
                               [.. Arguments]
                           );

                }
                catch (FormatException)
                {
                    return null;
                }

            }

            private static Object? Redact(Object? Value)

                => Value switch {
                       null          => null,
                       String text   => S2LogRedaction.Redact(text),

                       // A value type is passed through: a number, a timestamp or an enumeration
                       // holds no secret, and the secrets of this library are readonly structs
                       // that redact their own ToString(). That is the invariant this shortcut
                       // rests on - and it is the same invariant the reference branch below needs,
                       // because no redaction can mask a bare token that appears without its name.
                       // A new secret-bearing type must therefore redact its ToString(), whether
                       // it is a struct or a class (CONVENTIONS.md §16).
                       ValueType     => Value,

                       _             => S2LogRedaction.Redact(Value.ToString())
                   };

            public KeyValuePair<String, Object?> this[Int32 Index]
                => values[Index];

            public Int32 Count
                => values.Length;

            public IEnumerator<KeyValuePair<String, Object?>> GetEnumerator()
                => ((IEnumerable<KeyValuePair<String, Object?>>) values).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => values.GetEnumerator();

            public override String ToString()
                => message;

        }

        #endregion

    }


    /// <summary>
    /// A logger factory that wraps every logger of the factory it decorates into an
    /// <see cref="S2RedactingLogger"/>, so that an application enables the redaction of
    /// S2 Connect secrets for its entire logging with a single call (PLAN.md §3.6).
    /// </summary>
    /// <param name="InnerFactory">The logger factory to decorate.</param>
    public sealed class S2RedactingLoggerFactory(ILoggerFactory InnerFactory) : ILoggerFactory
    {

        #region Properties

        /// <summary>
        /// The decorated logger factory.
        /// </summary>
        public ILoggerFactory InnerFactory { get; } = InnerFactory ?? throw new ArgumentNullException(nameof(InnerFactory));

        #endregion


        #region CreateLogger(CategoryName)

        /// <summary>
        /// Create a redacting logger of the given category.
        /// </summary>
        /// <param name="CategoryName">The category of the logger.</param>
        public ILogger CreateLogger(String CategoryName)
            => new S2RedactingLogger(InnerFactory.CreateLogger(CategoryName));

        #endregion

        #region AddProvider(Provider)

        /// <summary>
        /// Add a logging provider to the decorated factory.
        /// </summary>
        /// <param name="Provider">A logging provider.</param>
        public void AddProvider(ILoggerProvider Provider)
            => InnerFactory.AddProvider(Provider);

        #endregion

        #region Dispose()

        /// <summary>
        /// Dispose this factory. The decorated factory is not disposed: this factory does not
        /// own it, and its owner will keep logging with it.
        /// </summary>
        public void Dispose()
        { }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"redacting {InnerFactory}";

        #endregion

    }


    /// <summary>
    /// Extension methods for enabling the redaction of S2 Connect secrets.
    /// </summary>
    public static class S2RedactionExtensions
    {

        #region WithS2Redaction(this LoggerFactory)

        /// <summary>
        /// Wrap the given logger factory so that every message it logs passes through
        /// <see cref="S2LogRedaction"/>. Wrapping an already redacting factory returns it
        /// unchanged, so that the redaction is never applied twice.
        /// </summary>
        /// <param name="LoggerFactory">A logger factory, or null.</param>
        public static ILoggerFactory? WithS2Redaction(this ILoggerFactory? LoggerFactory)

            => LoggerFactory switch {
                   null                          => null,
                   S2RedactingLoggerFactory      => LoggerFactory,
                   _                             => new S2RedactingLoggerFactory(LoggerFactory)
               };

        #endregion

        #region WithS2Redaction(this Logger)

        /// <summary>
        /// Wrap the given logger so that every message it logs passes through
        /// <see cref="S2LogRedaction"/>.
        /// </summary>
        /// <param name="Logger">A logger, or null.</param>
        public static ILogger? WithS2Redaction(this ILogger? Logger)

            => Logger switch {
                   null                   => null,
                   S2RedactingLogger      => Logger,
                   _                      => new S2RedactingLogger(Logger)
               };

        #endregion

    }

}
