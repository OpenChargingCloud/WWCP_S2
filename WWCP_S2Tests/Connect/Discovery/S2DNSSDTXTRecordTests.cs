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

using System.Text;

using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The DNS-SD TXT record of an S2 Connect endpoint (S2 Connect 1.0.0, "DNS-SD based discovery";
    /// RFC 6763 §6): constructor guards, the order of the character-strings, parsing with the
    /// RFC 6763 rules (case-insensitive keys, first occurrence wins, ignored strings), the URL
    /// rules, the round trip through a Hermod TXT resource record, the derived endpoint
    /// description and equality.
    /// </summary>
    [TestFixture]
    public sealed class S2DNSSDTXTRecordTests
    {

        #region Data

        private static readonly S2BaseURL       PairingUrl      = S2BaseURL.Parse("https://hp.local/pairing/");
        private static readonly S2BaseURL       LongPollingUrl  = S2BaseURL.Parse("https://hp.local/longpolling/");
        private static readonly URL             LogoUrl         = URL.Parse("https://hp.local/logo.png");
        private static readonly DNSServiceName  InstanceName    = DNSServiceName.Parse("hp._s2connect._tcp.local.");

        private static KeyValuePair<String, String?> Entry(String   Key,
                                                           String?  Value   = null)
            => new (Key, Value);

        private static S2DNSSDTXTRecord Parse(String[]          Strings,
                                              S2ParserOptions?  Options   = null)
        {

            var success = S2DNSSDTXTRecord.TryParse(Strings, out var record, out var errorResponse, Options);

            Assert.That(success, Is.True, errorResponse ?? "");
            Assert.That(record,  Is.Not.Null);

            return record!;

        }

        private static String? ParseError(String[]          Strings,
                                          S2ParserOptions?  Options   = null)
        {

            var success = S2DNSSDTXTRecord.TryParse(Strings, out var record, out var errorResponse, Options);

            Assert.That(success,       Is.False);
            Assert.That(record,        Is.Null);
            Assert.That(errorResponse, Is.Not.Null.And.Not.Empty);

            return errorResponse;

        }

        #endregion


        #region Constructor

        [Test]
        [S2C("Discovery.TXT.URLs")]
        public void Constructor_RequiresAtLeastOneURL()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2DNSSDTXTRecord(),                                                     Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(EndpointName: "Heat pump", LogoUrl: LogoUrl),          Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(default(S2BaseURL)),                                   Throws.ArgumentException, "an empty pairing URL");
                Assert.That(() => new S2DNSSDTXTRecord(LongPollingUrl: default(S2BaseURL)),                   Throws.ArgumentException, "an empty long-polling URL");
                Assert.That(new S2DNSSDTXTRecord(PairingUrl).HasPairingUrl,                                   Is.True);
                Assert.That(new S2DNSSDTXTRecord(LongPollingUrl: LongPollingUrl).HasLongPollingUrl,           Is.True);
            });
        }

        [Test]
        public void Constructor_RejectsKnownKeysAsAdditionalEntries()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("pairingUrl",     "https://other.local/pairing/") ]), Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("PAIRINGURL",     "https://other.local/pairing/") ]), Throws.ArgumentException, "known keys are case-insensitive");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("longpollingUrl", "https://other.local/longpolling/") ]), Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("txtver",         "1") ]),  Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("txtvers",        "1") ]),  Throws.ArgumentException, "the alias is a known key as well");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("e_name",         "x") ]),  Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("e_logoUrl",      "x") ]),  Throws.ArgumentException);
            });
        }

        [Test]
        public void Constructor_RejectsInvalidAdditionalKeys()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("ven=dor",  "acme") ]), Throws.ArgumentException, "'=' within the key");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("händler",  "acme") ]), Throws.ArgumentException, "a non-ASCII key");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("ven\tdor", "acme") ]), Throws.ArgumentException, "a control character within the key");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("",         "acme") ]), Throws.ArgumentException, "an empty key");
            });
        }

        [Test]
        public void Constructor_RejectsDuplicateAdditionalKeysIgnoringCase()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("vendor", "acme"), Entry("vendor", "acme")  ]), Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("vendor", "acme"), Entry("VENDOR", "other") ]), Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("flag"),           Entry("Flag")            ]), Throws.ArgumentException);
            });
        }

        [Test]
        [S2C("Discovery.TXT.MaxLength")]
        public void Constructor_RejectsEntriesLongerThan255Bytes()
        {

            Assert.That(S2DNSSDTXTRecord.MaxEntryLength, Is.EqualTo(255));

            // 'ä' is two bytes of UTF-8: 250 characters are 500 bytes, far beyond the 255 bytes of a DNS character-string.
            Assert.Multiple(() => {
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, EndpointName: new String('ä', 250)),                       Throws.ArgumentException);
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, EndpointName: new String('ä', 125)),                       Throws.ArgumentException, "\"e_name=\" plus 250 bytes are 257 bytes");
                Assert.That(() => new S2DNSSDTXTRecord(PairingUrl, AdditionalEntries: [ Entry("data", new String('x', 251)) ]), Throws.ArgumentException, "\"data=\" plus 251 bytes are 256 bytes");
            });

            // "e_name=" plus 124 'ä' are exactly 255 bytes and fit.
            var fits = new S2DNSSDTXTRecord(PairingUrl, EndpointName: new String('ä', 124));

            Assert.That(Encoding.UTF8.GetByteCount(fits.ToStrings()[1]), Is.EqualTo(255));

        }

        #endregion

        #region ToStrings() and properties

        [Test]
        [S2C("Discovery.TXT.txtver")]
        public void ToStrings_StartsWithTheVersionAndKeepsTheOrderOfTheSpecification()
        {

            var record = new S2DNSSDTXTRecord(
                             PairingUrl,
                             LongPollingUrl,
                             "Heat pump",
                             LogoUrl,
                             [ Entry("vendor", "acme"), Entry("flag") ]
                         );

            Assert.Multiple(() => {

                Assert.That(record.TXTVersion, Is.EqualTo("1"));
                Assert.That(record.TXTVersion, Is.EqualTo(S2ConnectDefaults.DNSSDTXTVersion));

                Assert.That(record.ToStrings(), Is.EqualTo(new[] {
                    "txtver=1",
                    "e_name=Heat pump",
                    "e_logoUrl=https://hp.local/logo.png",
                    "pairingUrl=https://hp.local/pairing/",
                    "longpollingUrl=https://hp.local/longpolling/",
                    "vendor=acme",
                    "flag"
                }));

                Assert.That(record.ToString(), Is.EqualTo("txtver=1, e_name=Heat pump, e_logoUrl=https://hp.local/logo.png, pairingUrl=https://hp.local/pairing/, longpollingUrl=https://hp.local/longpolling/, vendor=acme, flag"));

            });

        }

        [Test]
        public void ToStrings_OmitsAbsentEntries()
        {
            Assert.Multiple(() => {
                Assert.That(new S2DNSSDTXTRecord(PairingUrl).ToStrings(),                     Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/" }));
                Assert.That(new S2DNSSDTXTRecord(LongPollingUrl: LongPollingUrl).ToStrings(), Is.EqualTo(new[] { "txtver=1", "longpollingUrl=https://hp.local/longpolling/" }));
                Assert.That(new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl).ToStrings(),     Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/", "longpollingUrl=https://hp.local/longpolling/" }));
            });
        }

        [Test]
        public void HasPairingUrl_And_HasLongPollingUrl()
        {

            var pairingOnly      = new S2DNSSDTXTRecord(PairingUrl);
            var longPollingOnly  = new S2DNSSDTXTRecord(LongPollingUrl: LongPollingUrl);
            var both             = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl);

            Assert.Multiple(() => {

                Assert.That(pairingOnly.HasPairingUrl,          Is.True);
                Assert.That(pairingOnly.HasLongPollingUrl,      Is.False);
                Assert.That(pairingOnly.PairingUrl,             Is.EqualTo(PairingUrl));
                Assert.That(pairingOnly.LongPollingUrl,         Is.Null);

                Assert.That(longPollingOnly.HasPairingUrl,      Is.False);
                Assert.That(longPollingOnly.HasLongPollingUrl,  Is.True);
                Assert.That(longPollingOnly.PairingUrl,         Is.Null);
                Assert.That(longPollingOnly.LongPollingUrl,     Is.EqualTo(LongPollingUrl));

                Assert.That(both.HasPairingUrl,                 Is.True);
                Assert.That(both.HasLongPollingUrl,             Is.True);
                Assert.That(both.EndpointName,                  Is.Null);
                Assert.That(both.LogoUrl,                       Is.Null);
                Assert.That(both.AdditionalEntries,             Is.Empty);

            });

        }

        #endregion

        #region TryParse(Strings)

        [Test]
        [S2C("Discovery.TXT")]
        public void TryParse_ReadsEveryKey()
        {

            var record = Parse([
                             "txtver=1",
                             "e_name=Heat pump",
                             "e_logoUrl=https://hp.local/logo.png",
                             "pairingUrl=https://hp.local/pairing/",
                             "longpollingUrl=https://hp.local/longpolling/"
                         ]);

            Assert.Multiple(() => {
                Assert.That(record.TXTVersion,         Is.EqualTo("1"));
                Assert.That(record.EndpointName,       Is.EqualTo("Heat pump"));
                Assert.That(record.LogoUrl,            Is.EqualTo(LogoUrl));
                Assert.That(record.PairingUrl,         Is.EqualTo(PairingUrl));
                Assert.That(record.LongPollingUrl,     Is.EqualTo(LongPollingUrl));
                Assert.That(record.HasPairingUrl,      Is.True);
                Assert.That(record.HasLongPollingUrl,  Is.True);
                Assert.That(record.AdditionalEntries,  Is.Empty);
            });

        }

        [Test]
        [S2C("Discovery.TXT.txtver")]
        public void TryParse_AcceptsTheAliasTxtvers()
        {

            var record = Parse([ "txtvers=1", "pairingUrl=https://hp.local/pairing/" ]);

            Assert.Multiple(() => {
                Assert.That(record.TXTVersion,         Is.EqualTo("1"));
                Assert.That(record.PairingUrl,         Is.EqualTo(PairingUrl));
                Assert.That(record.AdditionalEntries,  Is.Empty, "the alias is not an additional entry");
                Assert.That(record.ToStrings()[0],     Is.EqualTo("txtver=1"), "serialised with the canonical key");
            });

        }

        [Test]
        [S2C("Discovery.TXT.txtver")]
        public void TryParse_TxtverWinsOverTxtvers()
        {
            Assert.Multiple(() => {
                Assert.That(Parse([ "txtvers=2", "txtver=1", "pairingUrl=https://hp.local/pairing/" ]).TXTVersion, Is.EqualTo("1"));
                Assert.That(Parse([ "txtver=1", "txtvers=2", "pairingUrl=https://hp.local/pairing/" ]).TXTVersion, Is.EqualTo("1"));
                Assert.That(ParseError([ "txtvers=1", "txtver=2", "pairingUrl=https://hp.local/pairing/" ]),        Does.Contain("'2'"));
            });
        }

        [Test]
        [S2C("Discovery.TXT.txtver")]
        public void TryParse_RequiresTheVersion()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError([ "pairingUrl=https://hp.local/pairing/" ]),                     Does.Contain("txtver"));
                Assert.That(ParseError([ "e_name=Heat pump", "pairingUrl=https://hp.local/pairing/" ]), Does.Contain("txtver"));
                Assert.That(ParseError([ "txtver", "pairingUrl=https://hp.local/pairing/" ]),           Does.Contain("txtver"), "a boolean attribute has no value");
            });
        }

        [Test]
        [S2C("Discovery.TXT.txtver")]
        public void TryParse_RejectsUnsupportedVersions()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError([ "txtver=2",  "pairingUrl=https://hp.local/pairing/" ]), Does.Contain("'2'"));
                Assert.That(ParseError([ "txtver=",   "pairingUrl=https://hp.local/pairing/" ]), Is.Not.Null);
                Assert.That(ParseError([ "txtver=10", "pairingUrl=https://hp.local/pairing/" ]), Does.Contain("'10'"));
                Assert.That(Parse([ "txtver= 1 ",     "pairingUrl=https://hp.local/pairing/" ]).TXTVersion, Is.EqualTo("1"), "surrounding white space is tolerated");
            });
        }

        [Test]
        public void TryParse_KeysAreCaseInsensitive()
        {

            var record = Parse([ "TXTVER=1", "E_NAME=Heat pump", "PAIRINGURL=https://hp.local/pairing/", "LongPollingURL=https://hp.local/longpolling/" ]);

            Assert.Multiple(() => {
                Assert.That(record.EndpointName,       Is.EqualTo("Heat pump"));
                Assert.That(record.PairingUrl,         Is.EqualTo(PairingUrl));
                Assert.That(record.LongPollingUrl,     Is.EqualTo(LongPollingUrl));
                Assert.That(record.AdditionalEntries,  Is.Empty);
                Assert.That(record.ToStrings(),        Is.EqualTo(new[] { "txtver=1", "e_name=Heat pump", "pairingUrl=https://hp.local/pairing/", "longpollingUrl=https://hp.local/longpolling/" }), "serialised with the canonical keys");
            });

        }

        [Test]
        public void TryParse_TheFirstOccurrenceOfAKeyWins()
        {

            var record = Parse([
                             "txtver=1",
                             "pairingUrl=https://first.local/pairing/",
                             "PairingUrl=https://second.local/pairing/",
                             "e_name=First",
                             "e_name=Second",
                             "vendor=acme",
                             "Vendor=other"
                         ]);

            Assert.Multiple(() => {
                Assert.That(record.PairingUrl?.Value,           Is.EqualTo("https://first.local/pairing/"));
                Assert.That(record.EndpointName,                Is.EqualTo("First"));
                Assert.That(record.AdditionalEntries,           Has.Count.EqualTo(1));
                Assert.That(record.AdditionalEntries["vendor"], Is.EqualTo("acme"));
            });

        }

        [Test]
        public void TryParse_IgnoresEmptyStringsAndStringsStartingWithAnEqualsSign()
        {

            var record = Parse([ "", "=ignored", "txtver=1", "", "pairingUrl=https://hp.local/pairing/", "=" ]);

            Assert.Multiple(() => {
                Assert.That(record.PairingUrl,         Is.EqualTo(PairingUrl));
                Assert.That(record.AdditionalEntries,  Is.Empty);
                Assert.That(record.ToStrings(),        Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/" }));
            });

        }

        [Test]
        [S2C("Discovery.TXT.URLs")]
        public void TryParse_RequiresAtLeastOneURL()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError([ "txtver=1" ]),                                      Does.Contain("pairingUrl").And.Contain("longpollingUrl"));
                Assert.That(ParseError([ "txtver=1", "e_name=Heat pump" ]),                  Does.Contain("pairingUrl"));
                Assert.That(ParseError([ "txtver=1", "pairingUrl=", "longpollingUrl=  " ]),  Does.Contain("pairingUrl"), "empty URLs count as absent");
            });
        }

        [Test]
        [S2C("Pairing.PairingURL")]
        public void TryParse_RejectsHTTPByDefault_ButAcceptsItWithAllowInsecureURLs()
        {

            var strings   = new[] { "txtver=1", "pairingUrl=http://hp.local/pairing/" };
            var insecure  = new S2ParserOptions { AllowInsecureURLs = true };

            Assert.Multiple(() => {
                Assert.That(ParseError(strings),                          Does.Contain("https"));
                Assert.That(ParseError(strings, S2ParserOptions.Default), Does.Contain("https"));
                Assert.That(ParseError(strings, S2ParserOptions.Strict),  Does.Contain("https"));
            });

            var record = Parse(strings, insecure);

            Assert.Multiple(() => {
                Assert.That(record.PairingUrl?.Value,   Is.EqualTo("http://hp.local/pairing/"));
                Assert.That(record.PairingUrl?.IsHTTPS, Is.False);
            });

            // The same rule applies to the long-polling URL.
            Assert.Multiple(() => {
                Assert.That(ParseError([ "txtver=1", "longpollingUrl=http://hp.local/longpolling/" ]),                        Does.Contain("https"));
                Assert.That(Parse([ "txtver=1", "longpollingUrl=http://hp.local/longpolling/" ], insecure).LongPollingUrl?.Value, Is.EqualTo("http://hp.local/longpolling/"));
            });

        }

        [Test]
        [S2C("Pairing.PairingURL")]
        public void TryParse_RejectsAPairingURLWithoutTrailingSlash()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError([ "txtver=1", "pairingUrl=https://hp.local/pairing" ]),         Does.Contain("pairingUrl"));
                Assert.That(ParseError([ "txtver=1", "pairingUrl=https://hp.local/pairing/v1/" ]),     Does.Contain("pairingUrl"), "the API version must not be included");
                Assert.That(ParseError([ "txtver=1", "longpollingUrl=https://hp.local/longpolling" ]), Does.Contain("longpollingUrl"));
            });
        }

        [Test]
        public void TryParse_RejectsAnInvalidLogoURL()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError([ "txtver=1", "e_logoUrl=not a url", "pairingUrl=https://hp.local/pairing/" ]),  Does.Contain("e_logoUrl"));
                Assert.That(Parse([ "txtver=1", "e_logoUrl=", "pairingUrl=https://hp.local/pairing/" ]).LogoUrl,          Is.Null, "an empty logo URL is treated as absent");
                Assert.That(Parse([ "txtver=1", "e_logoUrl= https://hp.local/logo.png ", "pairingUrl=https://hp.local/pairing/" ]).LogoUrl, Is.EqualTo(LogoUrl), "surrounding white space is trimmed");
            });
        }

        [Test]
        public void TryParse_AnEmptyEndpointNameBecomesNull()
        {

            var record = Parse([ "txtver=1", "e_name=", "pairingUrl=https://hp.local/pairing/" ]);

            Assert.Multiple(() => {
                Assert.That(record.EndpointName,                  Is.Null);
                Assert.That(record.ToStrings(),                   Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/" }));
                Assert.That(record.ToEndpointDescription().Name,  Is.Null);
            });

        }

        [Test]
        public void TryParse_UnknownKeysSurviveTheRoundTrip()
        {

            var record = Parse([ "txtver=1", "pairingUrl=https://hp.local/pairing/", "vendor=acme", "flag", "empty=", "path=a=b" ]);

            Assert.Multiple(() => {
                Assert.That(record.AdditionalEntries,           Has.Count.EqualTo(4));
                Assert.That(record.AdditionalEntries["vendor"], Is.EqualTo("acme"));
                Assert.That(record.AdditionalEntries["flag"],   Is.Null,           "a boolean attribute has no value");
                Assert.That(record.AdditionalEntries["empty"],  Is.EqualTo(""),    "an attribute with an empty value");
                Assert.That(record.AdditionalEntries["path"],   Is.EqualTo("a=b"), "only the first '=' separates key and value");
                Assert.That(record.AdditionalEntries["VENDOR"], Is.EqualTo("acme"), "keys are case-insensitive");
                Assert.That(record.AdditionalEntries.Keys,      Is.EquivalentTo(new[] { "vendor", "flag", "empty", "path" }));
            });

            var strings = record.ToStrings();

            Assert.That(strings, Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/", "vendor=acme", "flag", "empty=", "path=a=b" }));

            var reparsed = Parse([.. strings]);

            Assert.Multiple(() => {
                Assert.That(reparsed,                     Is.EqualTo(record));
                Assert.That(reparsed.AdditionalEntries,   Has.Count.EqualTo(4));
                Assert.That(reparsed.ToStrings(),         Is.EqualTo(strings));
            });

        }

        #endregion

        #region TryParse(TXT)

        [Test]
        public void TryParse_FromAHermodTXTResourceRecord()
        {

            var txt      = new TXT(InstanceName,
                                   DNSQueryClasses.IN,
                                   TimeSpan.FromMinutes(75),
                                   new[] { "txtver=1", "e_name=Heat pump", "pairingUrl=https://hp.local/pairing/", "vendor=acme" });

            var success  = S2DNSSDTXTRecord.TryParse(txt, out var parsed, out var errorResponse);

            Assert.That(success, Is.True, errorResponse ?? "");
            Assert.That(parsed,  Is.Not.Null);

            var record = parsed!;

            Assert.Multiple(() => {
                Assert.That(record.EndpointName,                Is.EqualTo("Heat pump"));
                Assert.That(record.PairingUrl,                  Is.EqualTo(PairingUrl));
                Assert.That(record.LongPollingUrl,              Is.Null);
                Assert.That(record.AdditionalEntries["vendor"], Is.EqualTo("acme"));
                Assert.That(record.ToStrings(),                 Is.EqualTo(txt.Strings));
            });

            // A TXT record without the version is rejected in the same way as the plain strings.
            var invalid = new TXT(InstanceName, DNSQueryClasses.IN, TimeSpan.FromMinutes(75), new[] { "pairingUrl=https://hp.local/pairing/" });

            Assert.That(S2DNSSDTXTRecord.TryParse(invalid, out var none, out var error), Is.False);
            Assert.Multiple(() => {
                Assert.That(none,  Is.Null);
                Assert.That(error, Does.Contain("txtver"));
            });

        }

        #endregion

        #region ToTXT(...) / ToEndpointDescription()

        [Test]
        public void ToTXT_CreatesTheResourceRecordOfTheServiceInstance()
        {

            var record  = new S2DNSSDTXTRecord(PairingUrl, EndpointName: "Heat pump", AdditionalEntries: [ Entry("vendor", "acme") ]);
            var txt     = record.ToTXT(InstanceName);
            var custom  = record.ToTXT(InstanceName, TimeSpan.FromSeconds(30));

            Assert.Multiple(() => {

                Assert.That(txt.DomainName.FullName,     Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(txt.DomainName,              Is.EqualTo(InstanceName));
                Assert.That(txt.Type,                    Is.EqualTo(DNSResourceRecordTypes.TXT));
                Assert.That(txt.Class,                   Is.EqualTo(DNSQueryClasses.IN));
                Assert.That(txt.TimeToLive,              Is.EqualTo(TimeSpan.FromMinutes(75)));
                Assert.That(txt.TimeToLive,              Is.EqualTo(MulticastDNS.SharedRecordTimeToLive));
                Assert.That(txt.Strings,                 Is.EqualTo(record.ToStrings()));
                Assert.That(txt.Strings,                 Is.EqualTo(new[] { "txtver=1", "e_name=Heat pump", "pairingUrl=https://hp.local/pairing/", "vendor=acme" }));
                Assert.That(txt.KeyValues["txtver"],     Is.EqualTo("1"));
                Assert.That(txt.KeyValues["pairingUrl"], Is.EqualTo("https://hp.local/pairing/"));
                Assert.That(txt.KeyValues["vendor"],     Is.EqualTo("acme"));

                Assert.That(custom.DomainName,           Is.EqualTo(InstanceName));
                Assert.That(custom.TimeToLive,           Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(custom.Strings,              Is.EqualTo(record.ToStrings()));

            });

        }

        [Test]
        public void ToEndpointDescription_DescribesALANEndpoint()
        {

            var full     = new S2DNSSDTXTRecord(PairingUrl, EndpointName: "Heat pump", LogoUrl: LogoUrl).ToEndpointDescription();
            var minimal  = new S2DNSSDTXTRecord(PairingUrl).ToEndpointDescription();

            Assert.Multiple(() => {

                Assert.That(full.Name,           Is.EqualTo("Heat pump"));
                Assert.That(full.LogoUrl,        Is.EqualTo(LogoUrl));
                Assert.That(full.Deployment,     Is.EqualTo(Deployment.LAN));
                Assert.That(full,                Is.EqualTo(new EndpointDescription("Heat pump", LogoUrl, Deployment.LAN)));

                Assert.That(minimal.Name,        Is.Null);
                Assert.That(minimal.LogoUrl,     Is.Null);
                Assert.That(minimal.Deployment,  Is.EqualTo(Deployment.LAN));

            });

        }

        #endregion

        #region Equality

        [Test]
        public void Equality_IsBasedOnTheCharacterStrings()
        {

            var a  = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl, "Heat pump", LogoUrl, [ Entry("vendor", "acme") ]);
            var b  = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl, "Heat pump", LogoUrl, [ Entry("vendor", "acme") ]);
            var c  = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl, "Boiler",    LogoUrl, [ Entry("vendor", "acme") ]);
            var d  = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl, "Heat pump", LogoUrl);
            var e  = new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl, "Heat pump", LogoUrl, [ Entry("vendor", "other") ]);
            var f  = new S2DNSSDTXTRecord(PairingUrl, null,           "Heat pump", LogoUrl, [ Entry("vendor", "acme") ]);

            Assert.Multiple(() => {

                Assert.That(a.Equals(b),            Is.True);
                Assert.That(a.Equals((Object) b),   Is.True);
                Assert.That(a,                      Is.EqualTo(b));
                Assert.That(a.GetHashCode(),        Is.EqualTo(b.GetHashCode()));

                Assert.That(a,                      Is.Not.EqualTo(c), "different name");
                Assert.That(a,                      Is.Not.EqualTo(d), "no additional entry");
                Assert.That(a,                      Is.Not.EqualTo(e), "different additional value");
                Assert.That(a,                      Is.Not.EqualTo(f), "no long-polling URL");

                Assert.That(a.Equals((Object?) null),           Is.False);
                Assert.That(a!.Equals((S2DNSSDTXTRecord?) null), Is.False);
                Assert.That(a!.Equals("txtver=1"),               Is.False);

            });

            // A parsed record equals the constructed one.
            Assert.That(Parse([.. a.ToStrings()]), Is.EqualTo(a));

        }

        [Test]
        public void Operators_HandleNull()
        {

            var a  = new S2DNSSDTXTRecord(PairingUrl, EndpointName: "Heat pump");
            var b  = new S2DNSSDTXTRecord(PairingUrl, EndpointName: "Heat pump");
            var c  = new S2DNSSDTXTRecord(PairingUrl, EndpointName: "Boiler");

            S2DNSSDTXTRecord? none = null;

            Assert.Multiple(() => {

                Assert.That(a == b,         Is.True);
                Assert.That(a != b,         Is.False);
                Assert.That(a == c,         Is.False);
                Assert.That(a != c,         Is.True);

                Assert.That(a == none,      Is.False);
                Assert.That(none == a,      Is.False);
                Assert.That(a != none,      Is.True);
                Assert.That(none != a,      Is.True);
                Assert.That(none == null,   Is.True);
                Assert.That(a.Equals(none), Is.False);

            });

        }

        #endregion

    }

}
