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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The exponential back-off of the reconnection strategy (S2 Connect 1.0.0, "Reconnection
    /// strategy"): delay_n = random(0, min(max_delay, base_delay × 2^n)) with base_delay = 2 s
    /// and max_delay = 600 s, the attempt counter and the constructor guards.
    /// </summary>
    [TestFixture]
    public sealed class ReconnectStrategyTests
    {

        #region Defaults_EqualTheNormativeValues()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void Defaults_EqualTheNormativeValues()
        {

            var strategy = new ReconnectStrategy();

            Assert.Multiple(() => {
                Assert.That(strategy.BaseDelay,  Is.EqualTo(S2ConnectDefaults.ReconnectBaseDelay));
                Assert.That(strategy.BaseDelay,  Is.EqualTo(TimeSpan.FromSeconds(2)));
                Assert.That(strategy.MaxDelay,   Is.EqualTo(S2ConnectDefaults.ReconnectMaxDelay));
                Assert.That(strategy.MaxDelay,   Is.EqualTo(TimeSpan.FromSeconds(600)));
                Assert.That(strategy.Attempt,    Is.EqualTo(0), "n starts at 0");
            });

        }

        #endregion

        #region UpperBound_DoublesTheBaseDelay_CappedAtTheMaximalDelay()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void UpperBound_DoublesTheBaseDelay_CappedAtTheMaximalDelay()
        {

            var strategy = new ReconnectStrategy();

            Assert.Multiple(() => {
                Assert.That(strategy.UpperBound(0),               Is.EqualTo(TimeSpan.FromSeconds(2)));
                Assert.That(strategy.UpperBound(1),               Is.EqualTo(TimeSpan.FromSeconds(4)));
                Assert.That(strategy.UpperBound(2),               Is.EqualTo(TimeSpan.FromSeconds(8)));
                Assert.That(strategy.UpperBound(7),               Is.EqualTo(TimeSpan.FromSeconds(256)));
                Assert.That(strategy.UpperBound(8),               Is.EqualTo(TimeSpan.FromSeconds(512)));
                Assert.That(strategy.UpperBound(9),               Is.EqualTo(TimeSpan.FromSeconds(600)), "2 s × 2^9 = 1024 s is capped at max_delay");
                Assert.That(strategy.UpperBound(10),              Is.EqualTo(TimeSpan.FromSeconds(600)));
                Assert.That(strategy.UpperBound(39),              Is.EqualTo(TimeSpan.FromSeconds(600)));
                Assert.That(strategy.UpperBound(40),              Is.EqualTo(TimeSpan.FromSeconds(600)));
                Assert.That(strategy.UpperBound(Int32.MaxValue),  Is.EqualTo(TimeSpan.FromSeconds(600)), "huge attempt numbers do not overflow");
            });

        }

        #endregion

        #region UpperBound_RejectsANegativeAttempt()

        [Test]
        public void UpperBound_RejectsANegativeAttempt()
        {

            var strategy = new ReconnectStrategy();

            Assert.That(() => strategy.UpperBound(-1), Throws.TypeOf<ArgumentOutOfRangeException>());

        }

        #endregion

        #region DelayFor_StaysWithinZeroAndTheUpperBound()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void DelayFor_StaysWithinZeroAndTheUpperBound()
        {

            var strategy = new ReconnectStrategy();

            for (var attempt = 0; attempt <= 12; attempt++)
            {

                var upperBound = strategy.UpperBound(attempt);

                for (var repetition = 0; repetition < 50; repetition++)
                    Assert.That(strategy.DelayFor(attempt), Is.InRange(TimeSpan.Zero, upperBound), $"random(0, {upperBound}) of attempt {attempt}");

            }

        }

        #endregion

        #region DelayFor_RejectsANegativeAttempt()

        [Test]
        public void DelayFor_RejectsANegativeAttempt()
        {

            var strategy = new ReconnectStrategy();

            Assert.That(() => strategy.DelayFor(-1), Throws.TypeOf<ArgumentOutOfRangeException>());

        }

        #endregion

        #region DelayFor_IsRandom()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void DelayFor_IsRandom()
        {

            var strategy  = new ReconnectStrategy();

            // Attempt 5: random(0, 64 s) with millisecond resolution; 100 equal samples are practically impossible.
            var samples   = Enumerable.Range(0, 100).Select(_ => strategy.DelayFor(5)).ToList();

            Assert.Multiple(() => {
                Assert.That(samples.Distinct().Count(),  Is.GreaterThan(1), "the delays are not all equal");
                Assert.That(samples,                     Has.All.InRange(TimeSpan.Zero, TimeSpan.FromSeconds(64)));
            });

        }

        #endregion

        #region NextDelay_AdvancesTheAttempt_AndReset_StartsOver()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void NextDelay_AdvancesTheAttempt_AndReset_StartsOver()
        {

            var strategy = new ReconnectStrategy();

            for (var expectedAttempt = 0; expectedAttempt < 5; expectedAttempt++)
            {

                Assert.That(strategy.Attempt, Is.EqualTo(expectedAttempt));

                var upperBound  = strategy.UpperBound(expectedAttempt);
                var delay       = strategy.NextDelay();

                Assert.That(delay, Is.InRange(TimeSpan.Zero, upperBound), $"the delay of attempt {expectedAttempt} uses the bound of that attempt");

            }

            Assert.That(strategy.Attempt, Is.EqualTo(5), "five attempts were counted");

            strategy.Reset();

            Assert.That(strategy.Attempt,     Is.EqualTo(0), "a successful connection starts over at n = 0");
            Assert.That(strategy.NextDelay(), Is.InRange(TimeSpan.Zero, strategy.UpperBound(0)));
            Assert.That(strategy.Attempt,     Is.EqualTo(1));

        }

        #endregion

        #region NextDelay_IsThreadSafe()

        [Test]
        public void NextDelay_IsThreadSafe()
        {

            var strategy = new ReconnectStrategy();

            Parallel.For(0, 1000, _ => strategy.NextDelay());

            Assert.That(strategy.Attempt, Is.EqualTo(1000));

        }

        #endregion

        #region CustomBaseAndMaxDelay_AreUsed()

        [Test]
        [S2C("Reconnection.BackOff")]
        public void CustomBaseAndMaxDelay_AreUsed()
        {

            var custom          = new ReconnectStrategy(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            var sameBaseAndMax  = new ReconnectStrategy(TimeSpan.FromSeconds(1),        TimeSpan.FromSeconds(1));
            var onlyBase        = new ReconnectStrategy(TimeSpan.FromSeconds(1));
            var onlyMax         = new ReconnectStrategy(MaxDelay: TimeSpan.FromSeconds(10));

            Assert.Multiple(() => {

                Assert.That(custom.BaseDelay,              Is.EqualTo(TimeSpan.FromMilliseconds(100)));
                Assert.That(custom.MaxDelay,               Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(custom.UpperBound(0),          Is.EqualTo(TimeSpan.FromMilliseconds(100)));
                Assert.That(custom.UpperBound(1),          Is.EqualTo(TimeSpan.FromMilliseconds(200)));
                Assert.That(custom.UpperBound(2),          Is.EqualTo(TimeSpan.FromMilliseconds(400)));
                Assert.That(custom.UpperBound(3),          Is.EqualTo(TimeSpan.FromMilliseconds(800)));
                Assert.That(custom.UpperBound(4),          Is.EqualTo(TimeSpan.FromSeconds(1)), "1.6 s is capped at 1 s");
                Assert.That(custom.UpperBound(10),         Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(custom.DelayFor(20),           Is.InRange(TimeSpan.Zero, TimeSpan.FromSeconds(1)));

                Assert.That(sameBaseAndMax.UpperBound(0),  Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(sameBaseAndMax.UpperBound(5),  Is.EqualTo(TimeSpan.FromSeconds(1)));

                Assert.That(onlyBase.MaxDelay,             Is.EqualTo(S2ConnectDefaults.ReconnectMaxDelay));
                Assert.That(onlyBase.UpperBound(1),        Is.EqualTo(TimeSpan.FromSeconds(2)));

                Assert.That(onlyMax.BaseDelay,             Is.EqualTo(S2ConnectDefaults.ReconnectBaseDelay));
                Assert.That(onlyMax.UpperBound(2),         Is.EqualTo(TimeSpan.FromSeconds(8)));
                Assert.That(onlyMax.UpperBound(3),         Is.EqualTo(TimeSpan.FromSeconds(10)), "16 s is capped at 10 s");

            });

        }

        #endregion

        #region Constructor_RejectsANonPositiveBaseDelay_AndAMaximumBelowTheBase()

        [Test]
        public void Constructor_RejectsANonPositiveBaseDelay_AndAMaximumBelowTheBase()
        {

            Assert.Multiple(() => {
                Assert.That(() => new ReconnectStrategy(TimeSpan.Zero),                                     Throws.TypeOf<ArgumentOutOfRangeException>(), "zero base delay");
                Assert.That(() => new ReconnectStrategy(TimeSpan.FromSeconds(-1)),                          Throws.TypeOf<ArgumentOutOfRangeException>(), "negative base delay");
                Assert.That(() => new ReconnectStrategy(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)),  Throws.TypeOf<ArgumentOutOfRangeException>(), "maximal delay below the base delay");
                Assert.That(() => new ReconnectStrategy(MaxDelay: TimeSpan.FromSeconds(1)),                 Throws.TypeOf<ArgumentOutOfRangeException>(), "maximal delay below the default base delay of 2 s");
                Assert.That(() => new ReconnectStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)),  Throws.Nothing, "maximal delay equal to the base delay");
            });

        }

        #endregion

        #region ToString_DescribesTheStrategy()

        [Test]
        public void ToString_DescribesTheStrategy()
        {

            var strategy = new ReconnectStrategy();

            Assert.That(strategy.ToString(), Is.EqualTo("reconnect strategy: base 2 s, max 600 s, next attempt 0"));

            strategy.NextDelay();

            Assert.That(strategy.ToString(), Does.EndWith("next attempt 1"));

        }

        #endregion

    }

}
