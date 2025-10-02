using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Data;
using System.Windows;
using Ventuz.Remoting4.MachineService;

namespace LittleVentuzLauncher
{
    /// <summary>
    /// Provides a view model for <see cref="Launcher"/> objects.
    /// </summary>
    public class LauncherViewModel : ObservableObject
    {

        #region Members

        private Launcher _launcher;
        private ObservableCollection<VMS> _discoveredMachines;
        private object _discoveredMachinesLock = new object();

        private ObservableCollection<VMS> _clusterMachines;

        private string _vprList = string.Empty;
        private bool _isLaunching = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the <see cref="LauncherViewModel"/> class.
        /// </summary>
        public LauncherViewModel(Launcher launcher)
        {

            // Store refernce to launcher
            _launcher = launcher;

            // Create collections
            _discoveredMachines = new ObservableCollection<VMS>();
            _clusterMachines = new ObservableCollection<VMS>();

            BindingOperations.EnableCollectionSynchronization(_discoveredMachines, _discoveredMachinesLock);

            // Add event handlers
            _launcher.VmsDiscovered += HandleVmsDiscovered;

            // Create commands
            AddMachinesToClusterCommand = new RelayCommand<object>(AddMachinesToCluster);
            StartLaunchingCommand = new RelayCommand(StartLaunching,  CanStartLaunching);
            StopLaunchingCommand = new RelayCommand(StopLaunching, CanStopLaunching);

        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a collection of discovered machines.
        /// </summary>
        public ObservableCollection<VMS> DiscoveredMachines { get { return _discoveredMachines; } }

        /// <summary>
        /// Gets a collection of machines in the cluster.
        /// </summary>
        public ObservableCollection<VMS> ClusterMachines { get { return _clusterMachines; } }

        public RelayCommand<object>? AddMachinesToClusterCommand { get; set; }

        public RelayCommand<object>? RemoveMachinesFromClusterCommand { get; set; }

        public RelayCommand? StartLaunchingCommand { get; set; }
        public RelayCommand? StopLaunchingCommand { get; set; }

        /// <summary>
        /// Gets whether VPR launching is in progress.
        /// </summary>
        public bool IsLaunching
        {
            get { return _isLaunching; }
            private set { 
                SetProperty(ref _isLaunching, value);
                StartLaunchingCommand?.NotifyCanExecuteChanged();
                StopLaunchingCommand?.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the VPR list.
        /// </summary>
        public string VprList
        {
            get { return _vprList; }
            set
            {
                SetProperty(ref _vprList, value);
                StartLaunchingCommand?.NotifyCanExecuteChanged();
            }
        }

        #endregion

        #region Methods

        #region Cluster Management

        /// <summary>
        /// Adds the selected machine(s) to the cluster.
        /// </summary>
        /// <param name="parameter"></param>
        public void AddMachinesToCluster(object? parameter)
        {

            System.Collections.IList items = (System.Collections.IList)parameter;
            var machines = items.Cast<VMS>();

            foreach (VMS vms in machines)
            {
                AddMachineToCluster(vms);
            }

        }

        /// <summary>
        /// Adds a machine to the cluster if it does not already exist.
        /// </summary>
        public void AddMachineToCluster(VMS vms)
        {

            // check if VMS already in _clusterVMachines
            bool alreadyExists = false;

            foreach (VMS clusterVms in _clusterMachines)
            {
                if (clusterVms.IPAddress.Equals(vms.IPAddress))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                // Add to cluster machines
                _clusterMachines.Add(vms);
                StartLaunchingCommand?.NotifyCanExecuteChanged();
            }

        }

        #endregion

        #region VPR Launching

        private void StartLaunching()
        {

            // Start launching VPRs on cluster machines
            IsLaunching = true;

            // Create lists
            List<string> vprList = VprList.Split(Environment.NewLine).ToList();
            List<VMS> vmsList = ClusterMachines.ToList();

            _launcher.StartLaunching(vmsList, vprList);

        }

        private bool CanStartLaunching()
        {
            if (_vprList.Length == 0) return false;
            if (_clusterMachines.Count == 0) return false;
            if (IsLaunching) return false;
            return true;
        }

        private void StopLaunching()
        {
            // Stop launching VPRs on cluster machines
        }

        private bool CanStopLaunching()
        {
            return IsLaunching;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles discovered <see cref="VMS"/>.
        /// </summary>
        private void HandleVmsDiscovered(object? sender, VmsEventArgs e)
        {

            _discoveredMachines.Add(e.VMS);
        }

        #endregion

        #endregion

    }
}
