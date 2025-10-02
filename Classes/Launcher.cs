using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ventuz.Remoting4;
using Ventuz.Remoting4.MachineService;
using Ventuz.Remoting4.MachineService.VMS2;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LittleVentuzLauncher
{
    /// <summary>
    /// Provides a mechanism to repeatedly launch Ventuz instances with specific VPRs.
    /// </summary>
    public class Launcher
    {

        #region Members

        private Cluster _cluster;

        private VMSDiscovery _vmsDiscovery;
        private List<VMS> _discoveredMachines = new List<VMS>();

        private List<VMSClient> _activeVmsClients = new List<VMSClient>();

        private List<VMS> _selectedMachines = new List<VMS>();
        private List<string> _vprList = new List<string>();
        private int _vprIndex = 0;

        private ManualResetEventSlim _vprLoadedEvent = new ManualResetEventSlim();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a <see cref="VMS"/> has been discovered.
        /// </summary>
        public event EventHandler<VmsEventArgs>? VmsDiscovered;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the <see cref="Launcher"/> class.
        /// </summary>
        public Launcher()
        {
            int[] additionalListeningPorts = new int[] { 21406, 20406, 19406, 19402 };

            // Start discovering
            _vmsDiscovery = new VMSDiscovery(10, 22406, additionalListeningPorts);
            _vmsDiscovery.PropertyChanged += HandleVmsDiscoveryPropertyChanged;

            _cluster = null;

        }

        #endregion

        #region Properties

        #endregion

        #region Methods

        #region VMS Discovery

        /// <summary>
        /// Starts the VMS discovery process.
        /// </summary>
        public void StartVmsDiscovery()
        {

            _vmsDiscovery.Discover();

        }

        /// <summary>
        /// Handlles the <see cref="VMSDiscovery.PropertyChanged"/> event.
        /// </summary>
        private void HandleVmsDiscoveryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

            if (_vmsDiscovery != null)
            {
                foreach (VMS vms in _vmsDiscovery.Items)
                {
                    if (!_discoveredMachines.Contains(vms))
                    {
                        // Add VMS to discovered machines list
                        _discoveredMachines.Add(vms);

                        // Notify of newly discovered VMS
                        VmsDiscovered?.Invoke(this, new VmsEventArgs(vms));
                    }
                }
            }
        }

        #endregion

        #region Launching Loop

        public async void StartLaunching(List<VMS> vmsList, List<string> vprs)
        {

            // Store 
            _vprList = vprs;
            _selectedMachines = vmsList;

            bool keepLaunching = true;
            int vprIndex = 0;
            string vprFullPath = string.Empty;

            while (keepLaunching)
            {

                StartCluster(vmsList);

                // Check for VPR in list
                if (vprIndex <= vprs.Count)
                {

                    // Get VPR path
                    vprFullPath = vprs.ElementAt(vprIndex);

                    _vprLoadedEvent.Reset();

                    await StartVmsClientsAsync(vprFullPath);

                    _vprLoadedEvent.Wait();

                }

                StopCluster();

                // Increment VPR index
                vprIndex++;
                if (vprIndex >= vprs.Count) { vprIndex = 0; }

            }

        }

        private void StartCluster(List<VMS> vmsList)
        {

            // Cluster and VMS Clients
            _cluster = new Cluster();
            _activeVmsClients.Clear();

            foreach (VMS vms in vmsList)
            {

                // Create new VMSClient
                VMSClient client = new VMSClient(vms);
                _activeVmsClients.Add(client);

                IPAddress ip = vms.IPAddress;
                if (IsLocalIpAddress(ip))
                {
                    // for local machine use localhost to avoid network issues
                    ip = IPAddress.Loopback;
                }
                _cluster.AddMachine(new IPEndPoint(ip, Cluster.DEFAULT_PORT));
            }

            // Add Cluster event handlers
            _cluster.ClusterStateChanged += HandleClusterStateChanged;
            _cluster.SceneStatusChanged += HandleSceneStatusChanged;

            // Start cluster
            _cluster.Start();

        }

        private void StopCluster()
        {
            if (_cluster != null)
            {
                // Shut down Cluster
                _cluster.Shutdown();

                // SECOND: stop Runtimes and disconnect from VMS clients
                foreach (VMSClient client in _activeVmsClients)
                {
                    try
                    {
                        client.Kill();
                    }
                    catch (Exception ex)
                    {
                    }
                    client.Disconnect();
                }

                _cluster = null;

            }
        }

        public void StopLaunching()
        {
            StopCluster();
        }

        private async Task StartVmsClientsAsync(string vpr)
        {

            Debug.WriteLine($"Starting {vpr}");

            if (System.IO.File.Exists(vpr))
            {

                // try to start clients with specified Project
                foreach (VMSClient client in _activeVmsClients)
                {

                    VMSProjectDetails project = GetVmsProjectDetails(client, vpr);

                    if (project == null)
                    {
                        // Scan projects and try again
                        client.Scan();
                        while (client.ScanState())
                        {
                            await Task.Delay(2000);
                        }
                        project = GetVmsProjectDetails(client, vpr);

                    }

                    if (project != null)
                    {
                        client.Start(project.ID.ToString());
                    }

                    // Wait 30 seconds for scene to load
                    await Task.Delay(30000);

                    // OK state:
                    _vprLoadedEvent.Set();

                }

            }
            else
            {
                // Resume launching loop
                _vprLoadedEvent.Set();
            }

        }

        private VMSProjectDetails GetVmsProjectDetails(VMSClient client, string projectFile)
        {

            Stopwatch sw = Stopwatch.StartNew();
            IList<VMSProjectDetails> projects = client.Proj(null); // Nothing is used here because VPRs do not work

            foreach (VMSProjectDetails project in projects)
            {
                if (project.FullPath.Equals(projectFile, StringComparison.OrdinalIgnoreCase))
                {
                    sw.Stop();
                    Debug.WriteLine($"Project Details for {projectFile} found in {sw.ElapsedMilliseconds}ms");
                    return project;
                }
            }

            return null;

        }

        private void HandleClusterStateChanged(object? sender, EventArgs e)
        {

            if (_cluster == null)
                return;

            if (_cluster.ClusterState == ClusterState.Ok)
            {
            }

        }

        /// <summary>
        /// Handles the <see cref="Cluster.SceneStatusChanged"/> event.
        /// </summary>
        private void HandleSceneStatusChanged(object? sender, SceneStatusChangedEventArgs e)
        {

        }

        private void CheckForMainSceneActivation(IID iid, SceneStatus status)
        {

            Debug.WriteLine($"Scene {status.Identity} status is now {GetSceneStatusString(status)}");

            if (status.Identity == "*defaultlayout*") { return; } // Ignore default layout

            switch (GetSceneStatusString(status))
            {
                case "Active":
                case "Validated":

                    // OK state:
                    _vprLoadedEvent.Set();

                    break;

                default:
                    // Other states are ignored
                    return;
            }

        }

        #endregion

        #region Utilities

        public bool IsLocalIpAddress(IPAddress ipAddress)
        {

            // Check if the IP address is a loopback address
            if (IPAddress.IsLoopback(ipAddress))
                return true;

            // Get all network interfaces
            bool hasNICs = false;
            bool hasOperationalNIC = false;
            bool hasUnicastIPs = false;

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                hasNICs = true;

                // Consider only operational interfaces of the desired type (e.g., Ethernet, Wi-Fi)
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    hasOperationalNIC = true;

                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                    {
                        hasUnicastIPs = true;

                        // Check for IPv4 addresses
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (ip.Address.Equals(ipAddress))
                            {
                                return true; // Found a match on a local interface
                            }
                        }
                    }
                }
            }

            if (!hasNICs)
            {
                return false;
            }

            if (!hasOperationalNIC)
            {
                return false;
            }

            if (!hasUnicastIPs)
            {
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"{ipAddress.ToString()} is not in the 'AddressFamily.InterNetwork'");
            return false; // Not a local machine's IP address
        }


        /// <summary>
        /// Returns the SceneStatus.Status value and an optional loading/validation progress.
        /// </summary>
        public static uint GetSceneStatusValues(SceneStatus status, out uint progress)
        {
            progress = status.Status & 0xFF;
            return status.Status & 0xFFFFFF00;
        }

        /// <summary>
        /// Returns the string representation of a SceneStatus.
        /// </summary>
        public static string GetSceneStatusString(SceneStatus status)
        {
            uint p;
            switch (GetSceneStatusValues(status, out p))
            {
                case 0x0:               // No Status or Loading
                    if (p == 0)
                        return "No Status";
                    else
                        return "Loading";

                case 0x00000100:        // Loaded or Validating
                    if (p == 0)
                        return "Loaded";
                    else
                        return "Validating";

                case 0x00000300:        // Validated
                    if (status.PortIndex == null)
                        return "Validated";
                    else
                        return "Active";

                case 0x80010000:        // Scene Disposed
                    return "Disposed";

                case 0x80020000:        // Load Error
                    return "Load Error";

                default:
                    Console.WriteLine("SHOULD NOT COME HERE!!!");
                    break;
            }

            return "Undefined";
        }

        #endregion

        #endregion

    }
}
