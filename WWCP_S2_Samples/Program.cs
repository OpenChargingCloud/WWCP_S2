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

using System.Net.Sockets;
using System.Net;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2;
using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Node;

#endregion

namespace cloud.charging.open.protocols.S2.Samples
{

    /// <summary>
    /// The WWCP S2 sample applications: an EV charger RM (FRBC), a PV RM (PEBC), a minimal CEM and
    /// a DNS-SD pairing tool. Run "demo" for an in-process end-to-end run (discovery, pairing,
    /// session, an FRBC instruction, unpairing); "browse" to look for endpoints on the local
    /// network via Multicast DNS.
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">The command: "demo" (default), "browse", or "version".</param>
        public static async Task<Int32> Main(String[] args)
        {

            var command = args.Length > 0 ? args[0].ToLowerInvariant() : "demo";

            switch (command)
            {

                case "version":
                    PrintVersions();
                    return 0;

                case "browse":
                    await PairingTool.RunOverMulticastDNSAsync(TimeSpan.FromSeconds(args.Length > 1 && Int32.TryParse(args[1], out var s) ? s : 10));
                    return 0;

                case "demo":
                    await RunDemoAsync();
                    return 0;

                default:
                    Console.WriteLine("Usage: S2 samples [demo|browse [seconds]|version]");
                    return 1;

            }

        }


        #region RunDemoAsync()

        /// <summary>
        /// An in-process end-to-end demo: a CEM and an EV charger RM on one in-memory service
        /// discovery pair over ".local" URLs and run a Fill Rate Based Control exchange.
        /// </summary>
        // README quick-start: begin demo
        public static async Task RunDemoAsync()
        {

            PrintVersions();
            Console.WriteLine();

            // Shared fingerprints stand in for the TLS material until Phase 11 (the demo uses plain HTTP).
            var serverFingerprint  = CertificateFingerprint.Parse("A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90:A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90");
            var caFingerprint      = CertificateFingerprint.Parse("CA:FE:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD");

            var cemHTTPPort        = FreePort();
            var cemWSPort          = FreePort();
            var rmHTTPPort         = FreePort();

            await using var discovery = new InMemoryServiceDiscovery();
            await discovery.StartAsync();

            // The in-memory DNS resolves the ".local" host names to loopback.
            discovery.AddHost(DomainName.Parse("cem.local"), IPv4Address.Localhost);
            discovery.AddHost(DomainName.Parse("rm.local"),  IPv4Address.Localhost);

            await using var cem = MinimalCEM.BuildNode(
                                      S2BaseURL.Parse($"http://cem.local:{cemHTTPPort}/pairing/",    AllowHTTP: true),
                                      S2BaseURL.Parse($"http://cem.local:{cemHTTPPort}/connection/", AllowHTTP: true),
                                      URL.Parse($"ws://cem.local:{cemWSPort}/"),
                                      cemHTTPPort,
                                      cemWSPort,
                                      discovery,
                                      serverFingerprint,
                                      caFingerprint
                                  );

            await using var rm  = EVChargerRM.BuildNode(
                                      S2BaseURL.Parse($"http://rm.local:{rmHTTPPort}/pairing/", AllowHTTP: true),
                                      rmHTTPPort,
                                      discovery,
                                      serverFingerprint,
                                      serverFingerprint
                                  );

            await cem.StartAsync();
            await rm. StartAsync();

            Console.WriteLine("CEM and EV charger started. Putting the CEM into pairing mode ...");

            // Put the CEM into pairing mode; the RM discovers it and pairs.
            var token       = cem.Node.IssueDynamicPairingToken().PairingToken;

            await using var browser = await discovery.BrowseAsync(EnergyManagementRole.CEM);
            var discovered  = await browser.WaitForEndpointAsync(Timeout: TimeSpan.FromSeconds(5));

            if (discovered is null)
            {
                Console.WriteLine("The CEM was not discovered.");
                return;
            }

            Console.WriteLine($"Discovered the CEM at {discovered.PairingUrl?.Value}. Pairing ...");

            var pairing = await rm.PairAsync(discovered.PairingUrl!.Value, token, Deployment.LAN);
            Console.WriteLine(pairing.IsSuccess ? "Paired." : $"Pairing failed: {pairing}");

            // Wait for the session and the FRBC exchange.
            await WaitUntil(() => cem.Sessions.Count == 1 && rm.Sessions.Count == 1, TimeSpan.FromSeconds(10));
            Console.WriteLine("Session established.");

            var cemSession = cem.Sessions[0];
            await WaitUntil(() => cemSession.ActiveControlType == ControlType.FillRateBasedControl, TimeSpan.FromSeconds(5));
            Console.WriteLine("Fill Rate Based Control activated. Sending a charging instruction ...");

            await cemSession.SendAndAwaitReceptionStatusAsync(
                      new FRBC_Instruction(
                          Instruction_Id.Parse("instr1"),
                          Actuator_Id.Parse("actuator1"),
                          OperationMode_Id.Parse("om2"),
                          1.0,
                          DateTimeOffset.UtcNow,
                          AbnormalCondition: false
                      )
                  );

            await Task.Delay(200);

            Console.WriteLine("Unpairing ...");
            await rm.UnpairAsync(cem.NodeId);

            Console.WriteLine("Demo finished.");

        }
        // README quick-start: end demo

        #endregion

        #region PrintVersions()

        private static void PrintVersions()
        {
            Console.WriteLine($"WWCP S2 samples, library {Version.LibraryVersion}, " +
                              $"S2 JSON {String.Join(", ", Version.S2JSONVersions)}, " +
                              $"S2 Connect API {String.Join(", ", Version.S2ConnectAPIVersions)}");
        }

        #endregion

        #region FreePort() / WaitUntil(...)

        private static IPPort FreePort()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return IPPort.Parse((UInt16) port);
        }

        private static async Task WaitUntil(Func<Boolean> Probe, TimeSpan Timeout)
        {
            var deadline = DateTimeOffset.UtcNow + Timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Probe())
                    return;
                await Task.Delay(20);
            }
        }

        #endregion

    }

}
