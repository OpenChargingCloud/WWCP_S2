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

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The certificate pin store of S2 Connect (PLAN.md §3.5, D13): what a node pinned per domain
    /// name after a pairing, its normalisation of domain names, the rotation of a pin, unpinning
    /// after an unpairing and its JSON persistence.
    /// </summary>
    [TestFixture]
    public sealed class CertificatePinStoreTests
    {

        #region Helpers

        /// <summary>
        /// A deterministic 32 byte (SHA-256 sized) fingerprint.
        /// </summary>
        private static CertificateFingerprint Fingerprint(Byte Seed)
        {

            var bytes = new Byte[32];
            Array.Fill(bytes, Seed);

            return CertificateFingerprint.FromBytes(bytes);

        }

        #endregion


        #region EmptyStore_HasNothingPinned()

        /// <summary>
        /// A fresh store knows nothing: nothing is pinned, nothing matches and nothing is found.
        /// </summary>
        [Test]
        public void EmptyStore_HasNothingPinned()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x11);

            var found        = store.TryGetPins("myhost.local", out var fingerprints);

            Assert.Multiple(() => {

                Assert.That(store.Count,                                  Is.EqualTo(0));
                Assert.That(store.DomainNames,                            Is.Empty);
                Assert.That(store.IsPinned("myhost.local"),               Is.False);
                Assert.That(store.Matches ("myhost.local", fingerprint),  Is.False);
                Assert.That(found,                                        Is.False);
                Assert.That(fingerprints,                                 Is.Null);

            });

        }

        #endregion

        #region Pin_TheSameFingerprintTwice_IsNewOnlyOnce()

        /// <summary>
        /// Pinning returns whether the fingerprint was new for this domain name; pinning the same
        /// one twice changes nothing.
        /// </summary>
        [Test]
        public void Pin_TheSameFingerprintTwice_IsNewOnlyOnce()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x22);

            var first        = store.Pin("myhost.local", fingerprint);
            var second       = store.Pin("myhost.local", fingerprint);

            Assert.Multiple(() => {

                Assert.That(first,                                        Is.True,   "the first pin is new");
                Assert.That(second,                                       Is.False,  "the same fingerprint is not pinned twice");
                Assert.That(store.Count,                                  Is.EqualTo(1));
                Assert.That(store.IsPinned("myhost.local"),               Is.True);
                Assert.That(store.Matches ("myhost.local", fingerprint),  Is.True);

            });

        }

        #endregion

        #region Pin_TwoFingerprintsForOneHost_IsARotation()

        /// <summary>
        /// More than one fingerprint per domain name is what makes a planned certificate rotation
        /// possible: pin the new one, roll the server, drop the old one.
        /// </summary>
        [Test]
        public void Pin_TwoFingerprintsForOneHost_IsARotation()
        {

            var store  = new CertificatePinStore();
            var oldFP  = Fingerprint(0x33);
            var newFP  = Fingerprint(0x44);

            var first  = store.Pin("myhost.local", oldFP);
            var second = store.Pin("myhost.local", newFP);

            var found  = store.TryGetPins("myhost.local", out var fingerprints);

            Assert.Multiple(() => {

                Assert.That(first,                                  Is.True);
                Assert.That(second,                                 Is.True,  "a second, different fingerprint is added");
                Assert.That(store.Count,                            Is.EqualTo(1), "still a single domain name");
                Assert.That(store.Matches("myhost.local", oldFP),   Is.True);
                Assert.That(store.Matches("myhost.local", newFP),   Is.True);
                Assert.That(found,                                  Is.True);
                Assert.That(fingerprints!.Count,                    Is.EqualTo(2));

            });

        }

        #endregion

        #region DomainNames_AreNormalised()

        /// <summary>
        /// Domain names are normalised the way the pairing challenge normalises them
        /// (<c>ChallengeResponse.NormaliseDomainName</c>): trimmed, without a trailing dot and
        /// lower case, so "MyHost.Local." and "myhost.local" are the same host.
        /// </summary>
        [Test]
        public void DomainNames_AreNormalised()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x55);

            var pinned       = store.Pin("MyHost.Local.", fingerprint);
            var pinnedAgain  = store.Pin(" myhost.local ", fingerprint);

            Assert.Multiple(() => {

                Assert.That(pinned,                                        Is.True);
                Assert.That(pinnedAgain,                                   Is.False,  "the normalised name is the same host");

                Assert.That(store.Count,                                   Is.EqualTo(1));
                Assert.That(store.DomainNames,                             Does.Contain("myhost.local"));

                Assert.That(store.IsPinned("myhost.local"),                Is.True);
                Assert.That(store.IsPinned("MYHOST.LOCAL"),                Is.True);
                Assert.That(store.IsPinned("myhost.local."),               Is.True);

                Assert.That(store.Matches ("myhost.local",  fingerprint),  Is.True);
                Assert.That(store.Matches ("MyHost.Local.", fingerprint),  Is.True);

            });

        }

        #endregion

        #region Matches_AnotherHost_IsFalse()

        /// <summary>
        /// A fingerprint is only ever pinned for one host: the very same certificate presented by
        /// another host matches nothing.
        /// </summary>
        [Test]
        public void Matches_AnotherHost_IsFalse()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x66);

            store.Pin("myhost.local", fingerprint);

            Assert.Multiple(() => {

                Assert.That(store.Matches ("myhost.local", fingerprint),  Is.True);
                Assert.That(store.Matches ("other.local",  fingerprint),  Is.False);
                Assert.That(store.IsPinned("other.local"),                Is.False);

            });

        }

        #endregion

        #region Matches_AnUnknownFingerprint_IsFalse()

        /// <summary>
        /// A pinned host that presents an unknown certificate matches nothing.
        /// </summary>
        [Test]
        public void Matches_AnUnknownFingerprint_IsFalse()
        {

            var store = new CertificatePinStore();

            store.Pin("myhost.local", Fingerprint(0x77));

            Assert.That(store.Matches("myhost.local", Fingerprint(0x78)),  Is.False);

        }

        #endregion


        #region PinAll_WithTheSHA256Key_Pins()

        /// <summary>
        /// A <c>certificateFingerprint</c> map as received during a pairing: the mandatory
        /// "SHA256" entry is pinned.
        /// </summary>
        [Test]
        public void PinAll_WithTheSHA256Key_Pins()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x88);

            var map          = new Dictionary<String, CertificateFingerprint> {
                                   { S2ConnectDefaults.CertificateFingerprintSHA256Key, fingerprint }
                               };

            var pinned       = store.PinAll("myhost.local", map);

            Assert.Multiple(() => {

                Assert.That(pinned,                                       Is.EqualTo(1));
                Assert.That(store.Matches("myhost.local", fingerprint),   Is.True);

            });

        }

        #endregion

        #region PinAll_WithALowerCaseKey_Pins()

        /// <summary>
        /// The algorithm key of the map is matched case-insensitively.
        /// </summary>
        [Test]
        public void PinAll_WithALowerCaseKey_Pins()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x99);

            var map          = new Dictionary<String, CertificateFingerprint> {
                                   { "sha256", fingerprint }
                               };

            var pinned       = store.PinAll("myhost.local", map);

            Assert.Multiple(() => {

                Assert.That(pinned,                                       Is.EqualTo(1));
                Assert.That(store.Matches("myhost.local", fingerprint),   Is.True);

            });

        }

        #endregion

        #region PinAll_WithAnUnknownAlgorithm_PinsNothing()

        /// <summary>
        /// Only SHA-256 is defined by S2 Connect: an entry of another algorithm is ignored, and
        /// nothing at all is pinned for that host.
        /// </summary>
        [Test]
        public void PinAll_WithAnUnknownAlgorithm_PinsNothing()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0xAA);

            var map          = new Dictionary<String, CertificateFingerprint> {
                                   { "SHA1", fingerprint }
                               };

            var pinned       = store.PinAll("myhost.local", map);

            Assert.Multiple(() => {

                Assert.That(pinned,                                       Is.EqualTo(0));
                Assert.That(store.Count,                                  Is.EqualTo(0));
                Assert.That(store.IsPinned("myhost.local"),               Is.False);
                Assert.That(store.Matches ("myhost.local", fingerprint),  Is.False);

            });

        }

        #endregion


        #region Unpin_ASingleFingerprint_KeepsTheOthers()

        /// <summary>
        /// The end of a rotation: dropping the old fingerprint keeps the new one.
        /// </summary>
        [Test]
        public void Unpin_ASingleFingerprint_KeepsTheOthers()
        {

            var store  = new CertificatePinStore();
            var oldFP  = Fingerprint(0xB1);
            var newFP  = Fingerprint(0xB2);

            store.Pin("myhost.local", oldFP);
            store.Pin("myhost.local", newFP);

            var removed = store.Unpin("myhost.local", oldFP);

            Assert.Multiple(() => {

                Assert.That(removed,                                Is.True);
                Assert.That(store.Matches("myhost.local", oldFP),   Is.False);
                Assert.That(store.Matches("myhost.local", newFP),   Is.True);
                Assert.That(store.IsPinned("myhost.local"),         Is.True);
                Assert.That(store.Count,                            Is.EqualTo(1));

            });

        }

        #endregion

        #region Unpin_WithoutAFingerprint_RemovesTheHost()

        /// <summary>
        /// Unpairing: every fingerprint of the host goes away.
        /// </summary>
        [Test]
        public void Unpin_WithoutAFingerprint_RemovesTheHost()
        {

            var store  = new CertificatePinStore();
            var oldFP  = Fingerprint(0xC1);
            var newFP  = Fingerprint(0xC2);

            store.Pin("myhost.local", oldFP);
            store.Pin("myhost.local", newFP);
            store.Pin("other.local",  Fingerprint(0xC3));

            var removed = store.Unpin("myhost.local");

            Assert.Multiple(() => {

                Assert.That(removed,                                Is.True);
                Assert.That(store.IsPinned("myhost.local"),         Is.False);
                Assert.That(store.Matches("myhost.local", oldFP),   Is.False);
                Assert.That(store.Matches("myhost.local", newFP),   Is.False);
                Assert.That(store.IsPinned("other.local"),          Is.True,  "another host is untouched");
                Assert.That(store.Count,                            Is.EqualTo(1));

            });

        }

        #endregion

        #region Unpin_AnUnknownHost_IsFalse()

        /// <summary>
        /// Unpinning a host that was never pinned removes nothing.
        /// </summary>
        [Test]
        public void Unpin_AnUnknownHost_IsFalse()
        {

            var store = new CertificatePinStore();

            store.Pin("myhost.local", Fingerprint(0xD1));

            Assert.Multiple(() => {

                Assert.That(store.Unpin("unknown.local"),                      Is.False);
                Assert.That(store.Unpin("unknown.local", Fingerprint(0xD1)),   Is.False);
                Assert.That(store.Unpin("myhost.local",  Fingerprint(0xD2)),   Is.False,  "an unknown fingerprint of a known host");
                Assert.That(store.Count,                                       Is.EqualTo(1));

            });

        }

        #endregion

        #region Clear_EmptiesTheStore()

        /// <summary>
        /// Clear removes every pin of every host.
        /// </summary>
        [Test]
        public void Clear_EmptiesTheStore()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0xE1);

            store.Pin("myhost.local", fingerprint);
            store.Pin("other.local",  Fingerprint(0xE2));

            store.Clear();

            Assert.Multiple(() => {

                Assert.That(store.Count,                                  Is.EqualTo(0));
                Assert.That(store.DomainNames,                            Is.Empty);
                Assert.That(store.IsPinned("myhost.local"),               Is.False);
                Assert.That(store.Matches ("myhost.local", fingerprint),  Is.False);

            });

        }

        #endregion

        #region DomainNames_ListThePinnedHosts_AndCountCountsHosts()

        /// <summary>
        /// The store is indexed by domain name: Count counts hosts, not fingerprints.
        /// </summary>
        [Test]
        public void DomainNames_ListThePinnedHosts_AndCountCountsHosts()
        {

            var store = new CertificatePinStore();

            store.Pin("myhost.local", Fingerprint(0xF1));
            store.Pin("myhost.local", Fingerprint(0xF2));
            store.Pin("myhost.local", Fingerprint(0xF3));
            store.Pin("other.local",  Fingerprint(0xF4));

            Assert.Multiple(() => {

                Assert.That(store.Count,        Is.EqualTo(2), "two hosts, four fingerprints");
                Assert.That(store.DomainNames,  Is.EquivalentTo(new[] { "myhost.local", "other.local" }));

            });

        }

        #endregion


        #region ToJSON_TryParse_RoundTrip()

        /// <summary>
        /// The store survives a persistence round trip: every host keeps every fingerprint.
        /// </summary>
        [Test]
        public void ToJSON_TryParse_RoundTrip()
        {

            var store  = new CertificatePinStore();
            var fpA1   = Fingerprint(0x01);
            var fpA2   = Fingerprint(0x02);
            var fpB1   = Fingerprint(0x03);

            store.Pin("myhost.local", fpA1);
            store.Pin("myhost.local", fpA2);
            store.Pin("other.local",  fpB1);

            var json    = store.ToJSON();
            var parsed  = CertificatePinStore.TryParse(json, out var parsedStore, out var errorResponse);

            Assert.Multiple(() => {

                Assert.That(parsed,                                        Is.True, errorResponse ?? "");
                Assert.That(errorResponse,                                 Is.Null);
                Assert.That(parsedStore,                                   Is.Not.Null);

                Assert.That(parsedStore!.Count,                            Is.EqualTo(2));
                Assert.That(parsedStore.Matches("myhost.local", fpA1),     Is.True);
                Assert.That(parsedStore.Matches("myhost.local", fpA2),     Is.True);
                Assert.That(parsedStore.Matches("other.local",  fpB1),     Is.True);

                Assert.That(parsedStore.Matches("other.local",  fpA1),     Is.False);
                Assert.That(parsedStore.Matches("myhost.local", fpB1),     Is.False);

                Assert.That(parsedStore.ToJSON().ToString(),               Is.EqualTo(json.ToString()));

            });

        }

        #endregion

        #region TryParse_ANonArrayValue_Fails()

        /// <summary>
        /// The pins of a domain name must be a JSON array.
        /// </summary>
        [Test]
        public void TryParse_ANonArrayValue_Fails()
        {

            var json    = new JObject(
                              new JProperty("myhost.local", "01:02:03")
                          );

            var parsed  = CertificatePinStore.TryParse(json, out var parsedStore, out var errorResponse);

            Assert.Multiple(() => {

                Assert.That(parsed,          Is.False);
                Assert.That(parsedStore,     Is.Null);
                Assert.That(errorResponse,   Is.Not.Null);
                Assert.That(errorResponse,   Does.Contain("myhost.local"));

            });

        }

        #endregion

        #region TryParse_AnInvalidFingerprint_Fails()

        /// <summary>
        /// An entry that is not a hexadecimal fingerprint is rejected, not silently skipped.
        /// </summary>
        [Test]
        public void TryParse_AnInvalidFingerprint_Fails()
        {

            var json    = new JObject(
                              new JProperty("myhost.local", new JArray("no-hex-at-all"))
                          );

            var parsed  = CertificatePinStore.TryParse(json, out var parsedStore, out var errorResponse);

            Assert.Multiple(() => {

                Assert.That(parsed,          Is.False);
                Assert.That(parsedStore,     Is.Null);
                Assert.That(errorResponse,   Is.Not.Null);
                Assert.That(errorResponse,   Does.Contain("myhost.local"));

            });

        }

        #endregion


        #region Pin_WithoutADomainName_Throws()

        /// <summary>
        /// A pin without a domain name is a programming error, not an empty pin.
        /// </summary>
        [Test]
        public void Pin_WithoutADomainName_Throws()
        {

            var store        = new CertificatePinStore();
            var fingerprint  = Fingerprint(0x5A);

            Assert.Multiple(() => {

                Assert.That(() => { store.Pin(null!, fingerprint); },  Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { store.Pin("",    fingerprint); },  Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { store.Pin("   ", fingerprint); },  Throws.InstanceOf<ArgumentException>());

            });

        }

        #endregion

        #region ToString_MentionsTheCounts()

        /// <summary>
        /// The text representation names both counts: domain names and fingerprints.
        /// </summary>
        [Test]
        public void ToString_MentionsTheCounts()
        {

            var store = new CertificatePinStore();

            store.Pin("myhost.local", Fingerprint(0x6A));
            store.Pin("myhost.local", Fingerprint(0x6B));
            store.Pin("other.local",  Fingerprint(0x6C));

            var text = store.ToString();

            Assert.Multiple(() => {

                Assert.That(text,  Does.Contain("2"),            "two domain names");
                Assert.That(text,  Does.Contain("3"),            "three fingerprints");
                Assert.That(text,  Does.Contain("domain name"));
                Assert.That(text,  Does.Contain("fingerprint"));

            });

        }

        #endregion

    }

}
