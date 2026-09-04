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
    /// A transition between two operation modes of an actuator (OMBC, FRBC and DDBC),
    /// including the timers it (re)starts, the timers that block it, its costs and
    /// its duration.
    /// </summary>
    public sealed class Transition : IEquatable<Transition>
    {

        #region Properties

        /// <summary>
        /// The identification of the transition. Must be unique in the scope of the
        /// OMBC.SystemDescription, FRBC.ActuatorDescription or DDBC.ActuatorDescription
        /// in which it is used.
        /// </summary>
        [Mandatory]
        public Transition_Id             Id                       { get; }

        /// <summary>
        /// The identification of the operation mode (exact type differs per control type)
        /// that should be switched from.
        /// </summary>
        [Mandatory]
        public OperationMode_Id          From                     { get; }

        /// <summary>
        /// The identification of the operation mode (exact type differs per control type)
        /// that will be switched to.
        /// </summary>
        [Mandatory]
        public OperationMode_Id          To                       { get; }

        /// <summary>
        /// The identifications of the timers that will be (re)started when this transition is initiated.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Timer_Id>   StartTimers              { get; }

        /// <summary>
        /// The identifications of the timers that block this transition from initiating
        /// while at least one of these timers is not yet finished.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Timer_Id>   BlockingTimers           { get; }

        /// <summary>
        /// The optional absolute costs for going through this transition in the currency
        /// as described in the ResourceManagerDetails.
        /// </summary>
        [Optional]
        public Double?                   TransitionCosts          { get; }

        /// <summary>
        /// The optional time between the initiation of this transition and the time at
        /// which the device behaves according to the operation mode which is defined in
        /// the 'to' data element. When no value is provided it is assumed the transition
        /// duration is negligible.
        /// </summary>
        [Optional]
        public Duration?                 TransitionDuration       { get; }

        /// <summary>
        /// Indicates if this transition may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                   AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transition.
        /// </summary>
        /// <param name="Id">The identification of the transition.</param>
        /// <param name="From">The identification of the operation mode that should be switched from.</param>
        /// <param name="To">The identification of the operation mode that will be switched to.</param>
        /// <param name="StartTimers">The identifications of the timers that will be (re)started when this transition is initiated (0..1000).</param>
        /// <param name="BlockingTimers">The identifications of the timers that block this transition from initiating while at least one of them is not yet finished (0..1000).</param>
        /// <param name="AbnormalConditionOnly">Indicates if this transition may only be used during an abnormal condition.</param>
        /// <param name="TransitionCosts">The optional absolute costs for going through this transition.</param>
        /// <param name="TransitionDuration">The optional time between the initiation of this transition and the time at which the device behaves according to the target operation mode.</param>
        public Transition(Transition_Id            Id,
                          OperationMode_Id         From,
                          OperationMode_Id         To,
                          IReadOnlyList<Timer_Id>  StartTimers,
                          IReadOnlyList<Timer_Id>  BlockingTimers,
                          Boolean                  AbnormalConditionOnly,
                          Double?                  TransitionCosts      = null,
                          Duration?                TransitionDuration   = null)
        {

            ArgumentNullException.ThrowIfNull(StartTimers);
            ArgumentNullException.ThrowIfNull(BlockingTimers);

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("The identification of a transition must not be null or empty!",
                                            nameof(Id));

            if (From.IsNullOrEmpty)
                throw new ArgumentException("The 'from' operation mode identification of a transition must not be null or empty!",
                                            nameof(From));

            if (To.IsNullOrEmpty)
                throw new ArgumentException("The 'to' operation mode identification of a transition must not be null or empty!",
                                            nameof(To));

            if (StartTimers.Count > 1000)
                throw new ArgumentException($"A transition must not reference more than 1000 start timers, but {StartTimers.Count} were given!",
                                            nameof(StartTimers));

            if (BlockingTimers.Count > 1000)
                throw new ArgumentException($"A transition must not reference more than 1000 blocking timers, but {BlockingTimers.Count} were given!",
                                            nameof(BlockingTimers));

            if (TransitionCosts.HasValue && !Double.IsFinite(TransitionCosts.Value))
                throw new ArgumentException("The transition costs must be a finite number!",
                                            nameof(TransitionCosts));

            this.Id                     = Id;
            this.From                   = From;
            this.To                     = To;
            this.StartTimers            = [.. StartTimers];
            this.BlockingTimers         = [.. BlockingTimers];
            this.TransitionCosts        = TransitionCosts;
            this.TransitionDuration     = TransitionDuration;
            this.AbnormalConditionOnly  = AbnormalConditionOnly;

            unchecked
            {
                hashCode =  this.Id.                   GetHashCode()       * 23 ^
                            this.From.                 GetHashCode()       * 19 ^
                            this.To.                   GetHashCode()       * 17 ^
                            this.StartTimers.          CalcHashCode()      * 13 ^
                            this.BlockingTimers.       CalcHashCode()      * 11 ^
                           (this.TransitionCosts?.     GetHashCode() ?? 0) *  7 ^
                           (this.TransitionDuration?.  GetHashCode() ?? 0) *  3 ^
                            this.AbnormalConditionOnly.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // Transition.schema.json
        //   "title": "Transition",
        //   "properties": {
        //     "id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the Transition. Must be unique in the scope of the OMBC.SystemDescription,
        //                                                  FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used." },
        //     "from":                    { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the OperationMode (exact type differs per ControlType) that should be switched from." },
        //     "to":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the OperationMode (exact type differs per ControlType) that will be switched to." },
        //     "start_timers":            { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                  "items": { "$ref": "../schemas/ID.schema.json" },
        //                                  "description": "List of IDs of Timers that will be (re)started when this transition is initiated" },
        //     "blocking_timers":         { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                  "items": { "$ref": "../schemas/ID.schema.json" },
        //                                  "description": "List of IDs of Timers that block this Transition from initiating while at least one
        //                                                  of these Timers is not yet finished" },
        //     "transition_costs":        { "type": "number",
        //                                  "description": "Absolute costs for going through this Transition in the currency as described in
        //                                                  the ResourceManagerDetails." },
        //     "transition_duration":     { "$ref": "../schemas/Duration.schema.json",
        //                                  "description": "Indicates the time between the initiation of this Transition, and the time at which
        //                                                  the device behaves according to the Operation Mode which is defined in the 'to' data
        //                                                  element. When no value is provided it is assumed the transition duration is negligible." },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this Transition may only be used during an abnormal condition (see Clause )" }
        //   },
        //   "required": ["id", "from", "to", "start_timers", "blocking_timers", "abnormal_condition_only"],
        //   "additionalProperties": false
        //
        // Cross-object rules (CONVENTIONS.md §5): 'from'/'to' must reference declared operation modes and every
        // start/blocking timer must reference a declared timer; these are validated by the enclosing
        // FRBC/DDBC actuator description (or OMBC system description), not here.

        #endregion

        #region (static) TryParse(JSON, out Transition, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a transition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Transition">The parsed transition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out Transition?  Transition,
                                       [NotNullWhen(false)] out String?      ErrorResponse)

            => TryParse(JSON,
                        out Transition,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a transition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Transition">The parsed transition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out Transition?  Transition,
                                       [NotNullWhen(false)] out String?      ErrorResponse,
                                       S2ParserOptions?                      Options)

            => TryParse(JSON,
                        out Transition,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a transition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Transition">The parsed transition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomTransitionParser">A delegate to parse custom transitions.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out Transition?      Transition,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       S2ParserOptions?                          Options,
                                       CustomJObjectParserDelegate<Transition>?  CustomTransitionParser)
        {

            try
            {

                Transition = null;

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "transition identification",
                                             Transition_Id.TryParse,
                                             Options,
                                             out Transition_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region from                       [mandatory]

                if (!JSON.ParseMandatoryS2Id("from",
                                             "'from' operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id from,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region to                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("to",
                                             "'to' operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id to,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region start_timers               [mandatory]

                if (!JSON.ParseMandatoryS2Ids("start_timers",
                                              "start timer identifications",
                                              Timer_Id.TryParse,
                                              Options,
                                              0,
                                              1000,
                                              out IReadOnlyList<Timer_Id>? startTimers,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region blocking_timers            [mandatory]

                if (!JSON.ParseMandatoryS2Ids("blocking_timers",
                                              "blocking timer identifications",
                                              Timer_Id.TryParse,
                                              Options,
                                              0,
                                              1000,
                                              out IReadOnlyList<Timer_Id>? blockingTimers,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transition_costs           [optional]

                if (!JSON.ParseOptionalS2Number("transition_costs",
                                                "transition costs",
                                                out Double? transitionCosts,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transition_duration        [optional]

                if (!JSON.ParseOptionalS2Duration("transition_duration",
                                                  "transition duration",
                                                  out Duration? transitionDuration,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region abnormal_condition_only    [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("abnormal_condition_only",
                                                  "abnormal condition only",
                                                  out Boolean abnormalConditionOnly,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "from",
                                                    "to",
                                                    "start_timers",
                                                    "blocking_timers",
                                                    "transition_costs",
                                                    "transition_duration",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                Transition = new Transition(
                                 id,
                                 from,
                                 to,
                                 startTimers,
                                 blockingTimers,
                                 abnormalConditionOnly,
                                 transitionCosts,
                                 transitionDuration
                             );

                if (CustomTransitionParser is not null)
                    Transition = CustomTransitionParser(JSON,
                                                        Transition);

                return true;

            }
            catch (Exception e)
            {
                Transition     = null;
                ErrorResponse  = "The given JSON representation of a transition is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomTransitionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomTransitionSerializer">A delegate to serialize custom transitions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Transition>? CustomTransitionSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                       Id.  ToString()),
                                 new JProperty("from",                     From.ToString()),
                                 new JProperty("to",                       To.  ToString()),
                                 new JProperty("start_timers",             new JArray(StartTimers.   Select(timerId => timerId.ToString()))),
                                 new JProperty("blocking_timers",          new JArray(BlockingTimers.Select(timerId => timerId.ToString()))),

                           TransitionCosts.HasValue
                               ? new JProperty("transition_costs",         TransitionCosts.Value)
                               : null,

                           TransitionDuration.HasValue
                               ? new JProperty("transition_duration",      TransitionDuration.Value.ToJSON())
                               : null,

                                 new JProperty("abnormal_condition_only",  AbnormalConditionOnly)

                       );

            return CustomTransitionSerializer is not null
                       ? CustomTransitionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transition.
        /// </summary>
        public Transition Clone()

            => new (
                   Id.  Clone(),
                   From.Clone(),
                   To.  Clone(),
                   [.. StartTimers.   Select(timerId => timerId.Clone())],
                   [.. BlockingTimers.Select(timerId => timerId.Clone())],
                   AbnormalConditionOnly,
                   TransitionCosts,
                   TransitionDuration
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two transitions for equality.
        /// </summary>
        public static Boolean operator == (Transition? Transition1, Transition? Transition2)
        {

            if (ReferenceEquals(Transition1, Transition2))
                return true;

            if (Transition1 is null || Transition2 is null)
                return false;

            return Transition1.Equals(Transition2);

        }

        /// <summary>
        /// Compares two transitions for inequality.
        /// </summary>
        public static Boolean operator != (Transition? Transition1, Transition? Transition2)
            => !(Transition1 == Transition2);

        #endregion

        #region IEquatable<Transition> Members

        /// <summary>
        /// Compares two transitions for equality.
        /// </summary>
        /// <param name="Object">A transition to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Transition transition && Equals(transition);

        /// <summary>
        /// Compares two transitions for equality.
        /// </summary>
        /// <param name="Transition">A transition to compare with.</param>
        public Boolean Equals(Transition? Transition)

            => Transition is not null &&

               Id.                   Equals       (Transition.Id)                    &&
               From.                 Equals       (Transition.From)                  &&
               To.                   Equals       (Transition.To)                    &&
               StartTimers.          SequenceEqual(Transition.StartTimers)           &&
               BlockingTimers.       SequenceEqual(Transition.BlockingTimers)        &&
               AbnormalConditionOnly.Equals       (Transition.AbnormalConditionOnly) &&

               Nullable.Equals(TransitionCosts,    Transition.TransitionCosts) &&
               Nullable.Equals(TransitionDuration, Transition.TransitionDuration);

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

                   $"{Id}: {From} -> {To}",

                   TransitionDuration.HasValue
                       ? $" in {TransitionDuration.Value}"
                       : "",

                   TransitionCosts.HasValue
                       ? $" for {TransitionCosts.Value}"
                       : "",

                   AbnormalConditionOnly
                       ? " (abnormal conditions only)"
                       : ""

               );

        #endregion

    }

}
