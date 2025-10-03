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

        private LauncherViewModel GetViewModel()
        {
            return (LauncherViewModel)this.DataContext;
        }

    }
}