using System.Windows;
using Ventuz.Remoting4.MachineService;

namespace LittleVentuzLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Creates a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Double-click on machines adds it automatically to Cluster list
        /// </summary>
        private void MachineDoubleClick(object sender, RoutedEventArgs e)
        {
            if (availableMachinesListView.SelectedItems.Count > 0)
            {
                GetViewModel().AddMachineToCluster((VMS)availableMachinesListView.SelectedItem);
            }

        }

        private LauncherViewModel GetViewModel()
        {
            return (LauncherViewModel)this.DataContext;
        }

    }
}