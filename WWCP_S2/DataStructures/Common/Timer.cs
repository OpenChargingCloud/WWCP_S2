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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// A timer that can block or be (re)started by transitions between operation modes
    /// (OMBC, FRBC and DDBC).
    /// </summary>
    public sealed class Timer : IEquatable<Timer>
    {

        #region Properties

        /// <summary>
        /// The identification of the timer. Must be unique in the scope of the OMBC.SystemDescription,
        /// FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used.
        /// </summary>
        [Mandatory]
        public Timer_Id  Id                 { get; }

        /// <summary>
        /// The optional human readable name/description of the timer. This element is only
        /// intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?   DiagnosticLabel    { get; }

        /// <summary>
        /// The time it takes for the timer to finish after it has been started.
        /// </summary>
        [Mandatory]
        public Duration  Duration           { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new timer.
        /// </summary>
        /// <param name="Id">The identification of the timer.</param>
        /// <param name="Duration">The time it takes for the timer to finish after it has been started.</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the timer (diagnostic purposes only).</param>
        public Timer(Timer_Id  Id,
                     Duration  Duration,
                     String?   DiagnosticLabel   = null)
        {

            this.Id               = Id;
            this.Duration         = Duration;
            this.DiagnosticLabel  = DiagnosticLabel;

            unchecked
            {
                hashCode = this.Id.              GetHashCode()       * 5 ^
                           this.Duration.        GetHashCode()       * 3 ^
                          (this.DiagnosticLabel?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // Timer.schema.json
        //   "title": "Timer",
        //   "properties": {
        //     "id":               { "$ref": "../schemas/ID.schema.json",
        //                           "description": "ID of the Timer. Must be unique in the scope of the OMBC.SystemDescription,
        //                                           FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used." },
        //     "diagnostic_label": { "type": "string",
        //                           "description": "Human readable name/description of the Timer. This element is only intended
        //                                           for diagnostic purposes and not for HMI applications." },
        //     "duration":         { "$ref": "../schemas/Duration.schema.json",
        //                           "description": "The time it takes for the Timer to finish after it has been started" }
        //   },
        //   "required": ["id", "duration"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out Timer, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a timer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Timer">The parsed timer.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out Timer?   Timer,
                                       [NotNullWhen(false)] out String?  ErrorResponse)

            => TryParse(JSON,
                        out Timer,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a timer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Timer">The parsed timer.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out Timer?   Timer,
                                       [NotNullWhen(false)] out String?  ErrorResponse,
                                       S2ParserOptions?                  Options)

            => TryParse(JSON,
                        out Timer,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a timer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Timer">The parsed timer.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomTimerParser">A delegate to parse custom timers.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       [NotNullWhen(true)]  out Timer?      Timer,
                                       [NotNullWhen(false)] out String?     ErrorResponse,
                                       S2ParserOptions?                     Options,
                                       CustomJObjectParserDelegate<Timer>?  CustomTimerParser)
        {

            try
            {

                Timer = null;

                #region id                  [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "timer identification",
                                             Timer_Id.TryParse,
                                             Options,
                                             out Timer_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label    [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region duration            [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "diagnostic_label",
                                                    "duration"))
                {
                    return false;
                }

                #endregion


                Timer = new Timer(
                            id,
                            duration,
                            diagnosticLabel
                        );

                if (CustomTimerParser is not null)
                    Timer = CustomTimerParser(JSON,
                                              Timer);

                return true;

            }
            catch (Exception e)
            {
                Timer          = null;
                ErrorResponse  = "The given JSON representation of a timer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomTimerSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomTimerSerializer">A delegate to serialize custom timers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Timer>? CustomTimerSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",  DiagnosticLabel)
                               : null,

                                 new JProperty("duration",          Duration.ToJSON())

                       );

            return CustomTimerSerializer is not null
                       ? CustomTimerSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this timer.
        /// </summary>
        public Timer Clone()

            => new (
                   Id.Clone(),
                   Duration,
                   DiagnosticLabel?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two timers for equality.
        /// </summary>
        public static Boolean operator == (Timer? Timer1, Timer? Timer2)
        {

            if (ReferenceEquals(Timer1, Timer2))
                return true;

            if (Timer1 is null || Timer2 is null)
                return false;

            return Timer1.Equals(Timer2);

        }

        /// <summary>
        /// Compares two timers for inequality.
        /// </summary>
        public static Boolean operator != (Timer? Timer1, Timer? Timer2)
            => !(Timer1 == Timer2);

        #endregion

        #region IEquatable<Timer> Members

        /// <summary>
        /// Compares two timers for equality.
        /// </summary>
        /// <param name="Object">A timer to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Timer timer && Equals(timer);

        /// <summary>
        /// Compares two timers for equality.
        /// </summary>
        /// <param name="Timer">A timer to compare with.</param>
        public Boolean Equals(Timer? Timer)

            => Timer is not null &&

               Id.      Equals(Timer.Id) &&
               Duration.Equals(Timer.Duration) &&

               String.Equals(DiagnosticLabel, Timer.DiagnosticLabel, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   $"{Id}: {Duration}",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : ""

               );

        #endregion

    }

}
