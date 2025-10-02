using Ventuz.Remoting4;
using Ventuz.Remoting4.MachineService;

namespace LittleVentuzLauncher
{
    public class VmsEventArgs : EventArgs
    {

        private VMS _vms;

        public VmsEventArgs(VMS vms ) { 
            _vms = vms;
        }

        public VMS VMS { get { return _vms; } }

    }
}
