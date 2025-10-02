using System.Windows;

namespace LittleVentuzLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private Launcher? _launcher;
        private LauncherViewModel? _viewModel;

        public void App_Startup(object sender, StartupEventArgs e)
        {

            // Create objects
            _launcher = new Launcher();
            _viewModel = new LauncherViewModel(_launcher);

            // Start VMS discovery
            _launcher.StartVmsDiscovery();

            // Open the MainWindow
            MainWindow window = new MainWindow();
            window.DataContext = _viewModel;
            window.Show();

        }

    }

}
