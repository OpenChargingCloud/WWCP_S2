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

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The redacting logger of S2 Connect (PLAN.md §3.6): what a log sink really receives when
    /// an application wraps its logging with <c>WithS2Redaction()</c> - the formatted message,
    /// the structured state, the scopes, the exception - and the decorating logger factory
    /// that enables all of this with a single call.
    /// </summary>
    [TestFixture]
    public sealed class S2RedactingLoggerTests
    {

        #region (class) LogEntry

        /// <summary>
        /// One log entry as a sink behind the redacting logger sees it.
        /// </summary>
        /// <param name="LogLevel">The log level of the entry.</param>
        /// <param name="EventId">The identification of the event.</param>
        /// <param name="Message">The message the formatter produced.</param>
        /// <param name="Exception">The exception of the entry.</param>
        /// <param name="RawState">The state object as it arrived, whatever its type.</param>
        /// <param name="State">The state as a structured list, when it is one.</param>
        private sealed record LogEntry(LogLevel                                       LogLevel,
                                       EventId                                        EventId,
                                       String                                         Message,
                                       Exception?                                     Exception,
                                       Object?                                        RawState,
                                       IReadOnlyList<KeyValuePair<String, Object?>>?   State)
        {

            /// <summary>
            /// The value of the given entry of the structured state, or null.
            /// </summary>
            /// <param name="Name">The name of a state entry.</param>
            public Object? ValueOf(String Name)

                => State?.FirstOrDefault(entry => entry.Key == Name).Value;

            /// <summary>
            /// The value of the given entry of the structured state as a text, or null.
            /// </summary>
            /// <param name="Name">The name of a state entry.</param>
            public String? TextOf(String Name)

                => ValueOf(Name) as String;

            /// <summary>
            /// The whole structured state as a text, as a structured sink would render it.
            /// </summary>
            public String StateAsText()

                => State is null
                       ? ""
                       : String.Join(", ", State.Select(entry => $"{entry.Key}={entry.Value}"));

        }

        #endregion

        #region (class) CapturingLogger

        /// <summary>
        /// A log sink that records everything it is given.
        /// </summary>
        private sealed class CapturingLogger : ILogger
        {

            public List<LogEntry>  Entries         { get; }         = [];

            public List<LogLevel>  EnabledQueries  { get; }         = [];

            public Boolean         Enabled         { get; set; }    = true;

            public IDisposable?    ScopeToReturn   { get; set; }

            public Object?         ScopeState      { get; private set; }


            public IDisposable? BeginScope<TState>(TState State)
                where TState : notnull
            {

                ScopeState = State;

                return ScopeToReturn;

            }

            public Boolean IsEnabled(LogLevel LogLevel)
            {

                EnabledQueries.Add(LogLevel);

                return Enabled;

            }

            public void Log<TState>(LogLevel                          LogLevel,
                                    EventId                           EventId,
                                    TState                            State,
                                    Exception?                        Exception,
                                    Func<TState, Exception?, String>  Formatter)
            {

                Entries.Add(
                    new LogEntry(
                        LogLevel,
                        EventId,
                        Formatter(State, Exception),
                        Exception,
                        State,
                        State is IReadOnlyList<KeyValuePair<String, Object?>> values
                            ? values
                            : null
                    )
                );

            }

            public LogEntry SingleEntry
                => Entries.Single();

            public override String ToString()
                => "capturing logger";

        }

        #endregion

        #region (class) CapturingLoggerFactory

        /// <summary>
        /// A logger factory that records the categories it was asked for, the providers it was
        /// given and whether it was disposed.
        /// </summary>
        private sealed class CapturingLoggerFactory : ILoggerFactory
        {

            public List<String>                          Categories  { get; } = [];

            public Dictionary<String, CapturingLogger>   Loggers     { get; } = [];

            public List<ILoggerProvider>                 Providers   { get; } = [];

            public Boolean                               IsDisposed  { get; private set; }


            public ILogger CreateLogger(String CategoryName)
            {

                Categories.Add(CategoryName);

                if (!Loggers.TryGetValue(CategoryName, out var logger))
                {
                    logger = new CapturingLogger();
                    Loggers.Add(CategoryName, logger);
                }

                return logger;

            }

            public void AddProvider(ILoggerProvider Provider)
                => Providers.Add(Provider);

            public void Dispose()
                => IsDisposed = true;

            public override String ToString()
                => "capturing logger factory";

        }

        #endregion

        #region (class) CapturingLoggerProvider

        /// <summary>
        /// A logging provider, only used to watch it being handed on.
        /// </summary>
        private sealed class CapturingLoggerProvider : ILoggerProvider
        {

            public ILogger CreateLogger(String CategoryName)
                => new CapturingLogger();

            public void Dispose()
            { }

            public override String ToString()
                => "capturing logger provider";

        }

        #endregion

        #region (class) RecordingScope

        /// <summary>
        /// The scope an inner logger returns.
        /// </summary>
        private sealed class RecordingScope : IDisposable
        {

            public Boolean IsDisposed { get; private set; }

            public void Dispose()
                => IsDisposed = true;

            public override String ToString()
                => "recording scope";

        }

        #endregion


        #region Log_AStructuredEntry_MasksTheSecretValueOfTheState()

        /// <summary>
        /// A structured sink (JSON, OpenTelemetry, ...) renders the state of a log entry, so
        /// the value of every entry whose name is a secret name is masked - and everything
        /// else keeps its value.
        /// </summary>
        [Test]
        public void Log_AStructuredEntry_MasksTheSecretValueOfTheState()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            logger.LogInformation("token {Token} of {NodeId}", "s3cr3t-value", "node-1");

            var entry   = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.State,             Is.Not.Null,  "the entry must stay structured");

                Assert.That(entry.ValueOf("Token"),  Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(entry.ValueOf("NodeId"), Is.EqualTo("node-1"));

                Assert.That(entry.StateAsText(),     Does.Not.Contain("s3cr3t-value"));
                Assert.That(entry.StateAsText(),     Does.Contain("node-1"));

                Assert.That(entry.LogLevel,          Is.EqualTo(LogLevel.Information));

            });

        }

        #endregion

        #region Log_TheMessageTemplate_IsPassedThroughUnchanged()

        /// <summary>
        /// The "{OriginalFormat}" entry is the message template: it holds the names of the
        /// values, never a value, so it is passed through untouched - even when the template
        /// itself looks exactly like the "name=value" shape the redaction masks.
        /// </summary>
        [Test]
        public void Log_TheMessageTemplate_IsPassedThroughUnchanged()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            logger.LogInformation("accessToken={AccessToken} for {NodeId}", "s3cr3t-value", "node-1");

            var entry   = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.ValueOf("{OriginalFormat}"),  Is.EqualTo("accessToken={AccessToken} for {NodeId}"));

                Assert.That(entry.ValueOf("AccessToken"),       Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(entry.ValueOf("NodeId"),            Is.EqualTo("node-1"));

                // Here the message carries the "name=value" shape, so it is masked as well.
                Assert.That(entry.Message,                      Is.EqualTo("accessToken=[REDACTED] for node-1"));

            });

        }

        #endregion

        #region Log_AStructuredSecret_IsMaskedInTheMessageAsWell()

        /// <summary>
        /// The formatted message is rendered from the *redacted* values, not by running the
        /// already formatted text through the textual redaction: a template that names the
        /// secret in prose ("token {Token}") produces none of the three shapes that redaction
        /// knows, so the message of the entry would otherwise still carry the secret while the
        /// state of the very same entry was masked - and a console sink renders the message.
        /// </summary>
        [Test]
        public void Log_AStructuredSecret_IsMaskedInTheMessageAsWell()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            logger.LogInformation("token {Token} of {NodeId}", "s3cr3t-value", "node-1");

            var entry   = sink.SingleEntry;

            Assert.Multiple(() => {

                // The structured state is masked ...
                Assert.That(entry.ValueOf("Token"),  Is.EqualTo(S2LogRedaction.Mask));

                // ... and so is the formatted message of the same entry.
                Assert.That(entry.Message,           Is.EqualTo("token [REDACTED] of node-1"));
                Assert.That(entry.Message,           Does.Not.Contain("s3cr3t-value"));

            });

        }

        #endregion

        #region Log_AFormatSpecifierOfATemplate_Survives()

        /// <summary>
        /// Rendering from the redacted values must not cost the fidelity of the message: an
        /// alignment or a format specifier of a placeholder is kept, and a doubled brace stays
        /// an escaped brace.
        /// </summary>
        [Test]
        public void Log_AFormatSpecifierOfATemplate_Survives()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            logger.LogInformation("{{literal}} {Count:N0} of {Total,6} for {Token}", 1234, 42, "s3cr3t");

            var entry   = sink.SingleEntry;

            Assert.Multiple(() => {
                Assert.That(entry.Message, Does.StartWith("{literal} 1,234 of "));
                Assert.That(entry.Message, Does.EndWith("42 for [REDACTED]"));
                Assert.That(entry.Message, Does.Not.Contain("s3cr3t"));
            });

        }

        #endregion

        #region Log_AStateValueContainingASecretShape_IsRedacted()

        /// <summary>
        /// The value of a state entry does not have to be named like a secret to hold one: a
        /// logged request body is a plain string that carries the JSON shape, and it is
        /// redacted in the state as well as in the message.
        /// </summary>
        [Test]
        public void Log_AStateValueContainingASecretShape_IsRedacted()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            var body    = new JObject(
                              new JProperty("accessToken",  "s3cr3t-access-token"),
                              new JProperty("host",         "192.168.1.7")
                          ).ToString();

            logger.LogDebug("received {Body} from {NodeId}", body, "node-1");

            var entry   = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.TextOf("Body"),   Does.Not.Contain("s3cr3t-access-token"));
                Assert.That(entry.TextOf("Body"),   Does.Contain(S2LogRedaction.Mask));
                Assert.That(entry.TextOf("Body"),   Does.Contain("192.168.1.7"),  "what is no secret stays readable");

                Assert.That(entry.ValueOf("NodeId"), Is.EqualTo("node-1"));

                Assert.That(entry.Message,           Does.Not.Contain("s3cr3t-access-token"));
                Assert.That(entry.Message,           Does.Contain(S2LogRedaction.Mask));

            });

        }

        #endregion

        #region Log_ValueTypeStateEntries_ArePassedThrough()

        /// <summary>
        /// A number, a timestamp or an enumeration holds no secret and must reach a structured
        /// sink as the value it is, not as a text: a metrics pipeline behind the sink has to
        /// keep working. The log level and the event id are forwarded unchanged as well.
        /// </summary>
        [Test]
        public void Log_ValueTypeStateEntries_ArePassedThrough()
        {

            var sink       = new CapturingLogger();
            var logger     = new S2RedactingLogger(sink);

            var timestamp  = new DateTimeOffset(2026, 9, 5, 10, 11, 12, TimeSpan.FromHours(2));

            logger.LogWarning(
                new EventId(42, "PairingCompleted"),
                "{Count} attempts at {Timestamp} with {Level}",
                7,
                timestamp,
                LogLevel.Critical
            );

            var entry      = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.LogLevel,              Is.EqualTo(LogLevel.Warning));
                Assert.That(entry.EventId.Id,            Is.EqualTo(42));
                Assert.That(entry.EventId.Name,          Is.EqualTo("PairingCompleted"));

                Assert.That(entry.ValueOf("Count"),      Is.EqualTo(7));
                Assert.That(entry.ValueOf("Timestamp"),  Is.EqualTo(timestamp));
                Assert.That(entry.ValueOf("Level"),      Is.EqualTo(LogLevel.Critical));

            });

        }

        #endregion

        #region Log_TheException_IsForwardedUnchanged()

        /// <summary>
        /// The exception is handed to the sink as it is: it is the object a sink needs for its
        /// stack trace. Note that this also means that a secret inside an exception message is
        /// not redacted - the library must never put one there.
        /// </summary>
        [Test]
        public void Log_TheException_IsForwardedUnchanged()
        {

            var sink       = new CapturingLogger();
            var logger     = new S2RedactingLogger(sink);

            var exception  = new InvalidOperationException("the pairing attempt expired");

            logger.LogError(exception, "pairing failed for {NodeId}", "node-1");

            var entry      = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.Exception,   Is.SameAs(exception));
                Assert.That(entry.LogLevel,    Is.EqualTo(LogLevel.Error));
                Assert.That(entry.Message,     Is.EqualTo("pairing failed for node-1"));

            });

        }

        #endregion

        #region IsEnabled_DelegatesToTheInnerLogger()

        /// <summary>
        /// The decorator adds redaction, not a log level filter: whether a level is enabled is
        /// entirely the answer of the decorated logger.
        /// </summary>
        [Test]
        public void IsEnabled_DelegatesToTheInnerLogger()
        {

            var sink    = new CapturingLogger { Enabled = false };
            var logger  = new S2RedactingLogger(sink);

            var whileDisabled = logger.IsEnabled(LogLevel.Debug);

            sink.Enabled = true;

            var whileEnabled  = logger.IsEnabled(LogLevel.Debug);

            Assert.Multiple(() => {

                Assert.That(whileDisabled,        Is.False);
                Assert.That(whileEnabled,         Is.True);
                Assert.That(logger.IsEnabled(LogLevel.Trace),  Is.True);

                Assert.That(sink.EnabledQueries,  Is.EqualTo(new[] { LogLevel.Debug, LogLevel.Debug, LogLevel.Trace }));

            });

        }

        #endregion

        #region BeginScope_DelegatesAndReturnsTheInnerScope()

        /// <summary>
        /// A scope is opened on the decorated logger and its disposable is returned unchanged,
        /// so that "using (logger.BeginScope(...))" keeps working - with the state of the
        /// scope redacted, because a sink renders the scope of every entry within it.
        /// </summary>
        [Test]
        public void BeginScope_DelegatesAndReturnsTheInnerScope()
        {

            using var innerScope = new RecordingScope();

            var sink    = new CapturingLogger { ScopeToReturn = innerScope };
            var logger  = new S2RedactingLogger(sink);

            var scope   = logger.BeginScope("attempt {PairingAttemptId} of {NodeId}", "s3cr3t-attempt", "node-1");

            var state   = sink.ScopeState as IReadOnlyList<KeyValuePair<String, Object?>>;

            Assert.Multiple(() => {

                Assert.That(scope,               Is.SameAs(innerScope),  "the scope of the decorated logger is returned as it is");
                Assert.That(innerScope.IsDisposed,  Is.False,            "opening a scope must not close it");

                Assert.That(state,   Is.Not.Null,            "the scope state stays structured");
                Assert.That(state!.FirstOrDefault(entry => entry.Key == "PairingAttemptId").Value,  Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(state!.FirstOrDefault(entry => entry.Key == "NodeId").Value,            Is.EqualTo("node-1"));

                Assert.That(sink.ScopeState?.ToString(),  Does.Not.Contain("s3cr3t-attempt"));
                Assert.That(sink.ScopeState?.ToString(),  Does.Contain("node-1"));

            });

        }

        #endregion

        #region Log_ANonListState_RedactsTheFormattedMessage()

        /// <summary>
        /// Not every log entry is structured: a state that is no list of named values still
        /// gets its formatted message redacted, which is what a console sink renders.
        /// </summary>
        [Test]
        public void Log_ANonListState_RedactsTheFormattedMessage()
        {

            var sink        = new CapturingLogger();
            ILogger logger  = new S2RedactingLogger(sink);

            logger.Log<String>(
                LogLevel.Warning,
                new EventId(9),
                "Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9",
                null,
                static (state, _) => state
            );

            var entry       = sink.SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.Message,     Is.EqualTo("Authorization: Bearer [REDACTED]"));
                Assert.That(entry.State,       Is.Null,  "a plain text is no structured state");
                Assert.That(entry.LogLevel,    Is.EqualTo(LogLevel.Warning));
                Assert.That(entry.EventId.Id,  Is.EqualTo(9));

            });

        }

        #endregion

        #region Log_APlainTextState_IsRedactedInTheStateAsWell()

        /// <summary>
        /// A state that is plain text is replaced by its redacted text, so that a sink which
        /// renders the state instead of calling the formatter (many structured sinks do) sees
        /// the redacted one as well. A state object of any other type is forwarded unchanged -
        /// replacing it would break every sink that casts it back to its own type - which is
        /// why a state object holding a secret must redact its own ToString().
        /// </summary>
        [Test]
        public void Log_APlainTextState_IsRedactedInTheStateAsWell()
        {

            var sink        = new CapturingLogger();
            ILogger logger  = new S2RedactingLogger(sink);

            logger.Log<String>(
                LogLevel.Warning,
                new EventId(9),
                "Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9",
                null,
                static (state, _) => state
            );

            var entry       = sink.SingleEntry;

            Assert.Multiple(() => {

                // The message is redacted ...
                Assert.That(entry.Message,   Does.Not.Contain("0a1b2c3d4e5f60718293a4b5c6d7e8f9"));
                Assert.That(entry.Message,   Is.EqualTo("Authorization: Bearer [REDACTED]"));

                // ... and so is the state the sink receives.
                Assert.That(entry.RawState,  Is.EqualTo("Authorization: Bearer [REDACTED]"));

            });

        }

        #endregion

        #region ToString_MentionsTheDecoratedLogger()

        /// <summary>
        /// A redacting logger says whom it decorates, which is what one wants to see in a
        /// diagnostic dump of a composed logging pipeline.
        /// </summary>
        [Test]
        public void ToString_MentionsTheDecoratedLogger()
        {

            var sink    = new CapturingLogger();
            var logger  = new S2RedactingLogger(sink);

            Assert.Multiple(() => {
                Assert.That(logger.InnerLogger,  Is.SameAs(sink));
                Assert.That(logger.ToString(),   Is.EqualTo("redacting capturing logger"));
            });

        }

        #endregion


        #region Factory_CreateLogger_ReturnsARedactingLoggerOfTheInnerFactory()

        /// <summary>
        /// The factory asks the decorated factory for a logger of the very same category and
        /// wraps exactly that logger.
        /// </summary>
        [Test]
        public void Factory_CreateLogger_ReturnsARedactingLoggerOfTheInnerFactory()
        {

            using var innerFactory = new CapturingLoggerFactory();

            var factory  = new S2RedactingLoggerFactory(innerFactory);
            var logger   = factory.CreateLogger("S2.Connect.Pairing");

            Assert.Multiple(() => {

                Assert.That(logger,                                    Is.InstanceOf<S2RedactingLogger>());
                Assert.That(((S2RedactingLogger) logger).InnerLogger,  Is.SameAs(innerFactory.Loggers["S2.Connect.Pairing"]));

                Assert.That(innerFactory.Categories,                   Is.EqualTo(new[] { "S2.Connect.Pairing" }));

                Assert.That(factory.InnerFactory,                      Is.SameAs(innerFactory));
                Assert.That(factory.ToString(),                        Is.EqualTo("redacting capturing logger factory"));

            });

        }

        #endregion

        #region Factory_CreateLogger_TheLoggerRedacts()

        /// <summary>
        /// Wrapping the factory of a node is enough: every logger it hands out redacts, so no
        /// component of the node has to know about the redaction (PLAN.md §3.6, AS2Node).
        /// </summary>
        [Test]
        public void Factory_CreateLogger_TheLoggerRedacts()
        {

            using var innerFactory = new CapturingLoggerFactory();

            var factory  = new S2RedactingLoggerFactory(innerFactory);
            var logger   = factory.CreateLogger("S2.Connect.Pairing");

            logger.LogInformation("body {Body}", """{"accessToken": "s3cr3t-access-token"}""");

            var entry    = innerFactory.Loggers["S2.Connect.Pairing"].SingleEntry;

            Assert.Multiple(() => {

                Assert.That(entry.Message,         Does.Not.Contain("s3cr3t-access-token"));
                Assert.That(entry.Message,         Does.Contain(S2LogRedaction.Mask));

                Assert.That(entry.TextOf("Body"),  Does.Not.Contain("s3cr3t-access-token"));
                Assert.That(entry.TextOf("Body"),  Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));

            });

        }

        #endregion

        #region Factory_AddProvider_Delegates()

        /// <summary>
        /// Adding a logging provider is not the business of the decorator: it hands it on.
        /// </summary>
        [Test]
        public void Factory_AddProvider_Delegates()
        {

            using var innerFactory = new CapturingLoggerFactory();
            using var provider     = new CapturingLoggerProvider();

            var factory = new S2RedactingLoggerFactory(innerFactory);

            factory.AddProvider(provider);

            Assert.Multiple(() => {
                Assert.That(innerFactory.Providers,     Has.Count.EqualTo(1));
                Assert.That(innerFactory.Providers[0],  Is.SameAs(provider));
            });

        }

        #endregion

        #region Factory_Dispose_DoesNotDisposeTheInnerFactory()

        /// <summary>
        /// The decorator does not own the factory it decorates: its owner keeps logging with
        /// it after the decorator is gone.
        /// </summary>
        [Test]
        public void Factory_Dispose_DoesNotDisposeTheInnerFactory()
        {

            var innerFactory = new CapturingLoggerFactory();
            var factory      = new S2RedactingLoggerFactory(innerFactory);

            factory.Dispose();

            Assert.Multiple(() => {

                Assert.That(innerFactory.IsDisposed,  Is.False,  "the decorated factory must not be disposed");

                // ... and it still works.
                Assert.That(innerFactory.CreateLogger("after"),  Is.Not.Null);

            });

        }

        #endregion


        #region WithS2Redaction_OfANullFactory_IsNull()

        /// <summary>
        /// No logger factory, nothing to wrap: an application without logging stays without
        /// logging (a null factory is the normal case in the tests of this repository).
        /// </summary>
        [Test]
        public void WithS2Redaction_OfANullFactory_IsNull()
        {

            var wrapped = ((ILoggerFactory?) null).WithS2Redaction();

            Assert.That(wrapped, Is.Null);

        }

        #endregion

        #region WithS2Redaction_OfAFactory_Wraps()

        /// <summary>
        /// An ordinary factory is wrapped into a redacting one that keeps it as its inner one.
        /// </summary>
        [Test]
        public void WithS2Redaction_OfAFactory_Wraps()
        {

            using var innerFactory = new CapturingLoggerFactory();

            var wrapped = innerFactory.WithS2Redaction();

            Assert.Multiple(() => {
                Assert.That(wrapped,                                             Is.InstanceOf<S2RedactingLoggerFactory>());
                Assert.That(((S2RedactingLoggerFactory) wrapped!).InnerFactory,  Is.SameAs(innerFactory));
            });

        }

        #endregion

        #region WithS2Redaction_OfARedactingFactory_IsTheSameInstance()

        /// <summary>
        /// Wrapping twice would redact twice, which costs time and (see the redaction tests)
        /// even changes the text: an already redacting factory is returned as it is.
        /// </summary>
        [Test]
        public void WithS2Redaction_OfARedactingFactory_IsTheSameInstance()
        {

            using var innerFactory = new CapturingLoggerFactory();

            var once   = innerFactory.WithS2Redaction();
            var twice  = once.WithS2Redaction();

            Assert.Multiple(() => {
                Assert.That(once,   Is.InstanceOf<S2RedactingLoggerFactory>());
                Assert.That(twice,  Is.SameAs(once),  "the redaction must never be applied twice");
            });

        }

        #endregion

        #region WithS2Redaction_OfANullLogger_IsNull()

        /// <summary>
        /// No logger, nothing to wrap.
        /// </summary>
        [Test]
        public void WithS2Redaction_OfANullLogger_IsNull()
        {

            var wrapped = ((ILogger?) null).WithS2Redaction();

            Assert.That(wrapped, Is.Null);

        }

        #endregion

        #region WithS2Redaction_OfALogger_Wraps()

        /// <summary>
        /// A single logger can be wrapped as well, for a component that was given a logger
        /// instead of a factory.
        /// </summary>
        [Test]
        public void WithS2Redaction_OfALogger_Wraps()
        {

            var sink     = new CapturingLogger();

            var wrapped  = sink.WithS2Redaction();

            wrapped!.LogInformation("body {Body}", """{"accessToken": "s3cr3t-access-token"}""");

            Assert.Multiple(() => {

                Assert.That(wrapped,                                        Is.InstanceOf<S2RedactingLogger>());
                Assert.That(((S2RedactingLogger) wrapped!).InnerLogger,     Is.SameAs(sink));

                Assert.That(sink.SingleEntry.Message,                       Does.Not.Contain("s3cr3t-access-token"));

            });

        }

        #endregion

        #region WithS2Redaction_OfARedactingLogger_IsTheSameInstance()

        /// <summary>
        /// An already redacting logger is returned as it is.
        /// </summary>
        [Test]
        public void WithS2Redaction_OfARedactingLogger_IsTheSameInstance()
        {

            var sink   = new CapturingLogger();

            var once   = sink.WithS2Redaction();
            var twice  = once.WithS2Redaction();

            Assert.Multiple(() => {
                Assert.That(once,   Is.InstanceOf<S2RedactingLogger>());
                Assert.That(twice,  Is.SameAs(once),  "the redaction must never be applied twice");
            });

        }

        #endregion

    }

}
