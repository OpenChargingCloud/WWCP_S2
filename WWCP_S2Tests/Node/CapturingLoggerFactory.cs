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

using System.Collections.Concurrent;
using System.Text;

using Microsoft.Extensions.Logging;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// A logger factory whose loggers record every log entry into a thread-safe list, so that a
    /// test can grep the log output of running nodes for a known secret (PLAN.md §3.6: "A test
    /// greps log output for a known token").
    ///
    /// <para>
    /// Everything a log sink could ever render is captured: the category, the log level, the
    /// event identification, the formatted message, the structured state as "key=value" pairs
    /// (a JSON or OpenTelemetry sink renders the state and never the message) and the text of
    /// an exception. The state of a logical scope is captured as well.
    /// </para>
    ///
    /// <para>
    /// <c>IsEnabled</c> is true for every level, so that no entry is filtered away before the
    /// test can see it, and the nodes log from many threads, so the capture is thread-safe.
    /// </para>
    /// </summary>
    internal sealed class CapturingLoggerFactory : ILoggerFactory
    {

        #region Data

        private readonly ConcurrentQueue<String>  lines   = new ();

        #endregion

        #region Properties

        /// <summary>
        /// Every captured log entry, oldest first.
        /// </summary>
        public IReadOnlyList<String>  Lines
            => [.. lines];

        /// <summary>
        /// Every captured log entry as one text: message, structured state and exception.
        /// </summary>
        public String                 AllText
            => String.Join(Environment.NewLine, lines);

        #endregion


        #region CreateLogger(CategoryName)

        /// <summary>
        /// Create a logger that records every entry of the given category.
        /// </summary>
        /// <param name="CategoryName">The category of the logger.</param>
        public ILogger CreateLogger(String CategoryName)
            => new CapturingLogger(this, CategoryName);

        #endregion

        #region AddProvider(Provider)

        /// <summary>
        /// Logging providers are ignored: this factory is the sink.
        /// </summary>
        /// <param name="Provider">A logging provider.</param>
        public void AddProvider(ILoggerProvider Provider)
        { }

        #endregion

        #region Dispose()

        /// <summary>
        /// Dispose this factory; the captured entries stay readable.
        /// </summary>
        public void Dispose()
        { }

        #endregion


        #region FirstLineContaining(Text)

        /// <summary>
        /// The first captured entry containing the given text, or null: the offending line of a
        /// failure message.
        /// </summary>
        /// <param name="Text">The text to search for.</param>
        public String? FirstLineContaining(String Text)
            => lines.FirstOrDefault(line => line.Contains(Text, StringComparison.Ordinal));

        #endregion

        #region Contains(Text)

        /// <summary>
        /// Whether any captured entry contains the given text.
        /// </summary>
        /// <param name="Text">The text to search for.</param>
        public Boolean Contains(String Text)
            => FirstLineContaining(Text) is not null;

        #endregion

        #region Clear()

        /// <summary>
        /// Forget everything captured so far.
        /// </summary>
        public void Clear()
            => lines.Clear();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"capturing logger factory, {lines.Count} entries";

        #endregion


        #region (private) Add(Line)

        private void Add(String Line)
            => lines.Enqueue(Line);

        #endregion

        #region (private class) CapturingLogger

        /// <summary>
        /// A logger appending every entry to the list of its factory.
        /// </summary>
        /// <param name="Factory">The factory collecting the entries.</param>
        /// <param name="CategoryName">The category of this logger.</param>
        private sealed class CapturingLogger(CapturingLoggerFactory  Factory,
                                             String                  CategoryName) : ILogger
        {

            #region BeginScope<TState>(State)

            /// <summary>
            /// Capture the state of a logical scope; ending the scope does nothing.
            /// </summary>
            /// <typeparam name="TState">The type of the state.</typeparam>
            /// <param name="State">The state of the scope.</param>
            public IDisposable? BeginScope<TState>(TState State)
                where TState : notnull
            {

                Factory.Add($"[scope] {CategoryName}: {Render(State)}");

                return NullScope.Instance;

            }

            #endregion

            #region IsEnabled(LogLevel)

            /// <summary>
            /// Every level is enabled: a test must see everything a sink would see.
            /// </summary>
            /// <param name="LogLevel">A log level.</param>
            public Boolean IsEnabled(LogLevel LogLevel)
                => true;

            #endregion

            #region Log<TState>(LogLevel, EventId, State, Exception, Formatter)

            /// <summary>
            /// Capture a log entry with everything a sink could render.
            /// </summary>
            /// <typeparam name="TState">The type of the state.</typeparam>
            /// <param name="LogLevel">The log level of the entry.</param>
            /// <param name="EventId">The identification of the event.</param>
            /// <param name="State">The state of the entry.</param>
            /// <param name="Exception">An optional exception.</param>
            /// <param name="Formatter">A function creating the message of the entry.</param>
            public void Log<TState>(LogLevel                          LogLevel,
                                    EventId                           EventId,
                                    TState                            State,
                                    Exception?                        Exception,
                                    Func<TState, Exception?, String>  Formatter)
            {

                var line   = new StringBuilder();

                line.Append('[').Append(LogLevel).Append("] ").
                     Append(CategoryName).Append(" (").Append(EventId).Append("): ").
                     Append(Formatter(State, Exception));

                // A structured sink renders the state, not the message: capture both.
                var state  = Render(State);

                if (state.Length > 0)
                    line.Append(" | state: ").Append(state);

                if (Exception is not null)
                    line.Append(" | exception: ").Append(Exception.ToString());

                Factory.Add(line.ToString());

            }

            #endregion

            #region (private static) Render<TState>(State)

            /// <summary>
            /// The structured state of an entry as "key=value" pairs, or its text.
            /// </summary>
            /// <typeparam name="TState">The type of the state.</typeparam>
            /// <param name="State">The state of an entry or a scope.</param>
            private static String Render<TState>(TState State)
            {

                if (State is IReadOnlyList<KeyValuePair<String, Object?>> values)
                    return String.Join(", ", values.Select(value => $"{value.Key}={value.Value}"));

                if (State is IEnumerable<KeyValuePair<String, Object?>> pairs)
                    return String.Join(", ", pairs.Select(pair => $"{pair.Key}={pair.Value}"));

                return State is null
                           ? ""
                           : State.ToString() ?? "";

            }

            #endregion

        }

        #endregion

        #region (private class) NullScope

        /// <summary>
        /// A logical scope that does nothing when it ends.
        /// </summary>
        private sealed class NullScope : IDisposable
        {

            public static readonly NullScope  Instance   = new ();

            private NullScope()
            { }

            public void Dispose()
            { }

        }

        #endregion

    }

}
