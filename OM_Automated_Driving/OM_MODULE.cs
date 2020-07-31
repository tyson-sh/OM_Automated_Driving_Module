using Interop.TJRWinTools3;

namespace OM_Automated_Driving
{
    public class OM_MODULE
    {
        // Create an instance of the windows tools class
        private TJRWinToolsCls _tools = new TJRWinToolsCls();
        
        // Create and instance of the graphics object
        private TJR3DGraphics _graphics = new TJR3DGraphics();
        
        // Create an instance of the terrain object
        private STI_3D_Terrain _terrain = new STI_3D_Terrain();
        
        // TODO check to see if DirectSound8 object is required or known about
        
        // Create an instance of the sound object
        private TJRSoundEffects _sound = new TJRSoundEffects();
        
        // Create a struct for the driver input information
        struct DriverControlInputs
        {
            public float brake;
            public long buttons;
            public float clutch;
            public int gear;
            public float steer;
            public float throttle;
        }
        private DriverControlInputs _driver = new DriverControlInputs();
        
        // Define all variables that will be global to this class
        private OMDynamicVariables _dynVars = new OMDynamicVariables();
        private SimEvents _events = new SimEvents();
        private long ID_Screen;
        private long ID_World;
        private GAINSParams _gains = new GAINSParams();
        private OMStaticVariables _staticVars = new OMStaticVariables();
        
        /*
         * This section contains the public properties that the main STISIM Drive 
         * modules can access during the course of a simulation run  
         */

        public string BSAVData;
        public object DashboardForm;
        public string ErrorMessage;
        public long LogFileHandle;
        public object NewForm;
        public int SaveControls;
        public object StartForm;
        public string TextMessage;
        public long WillHandleCrash;
        
        // Placeholder addNew method
        public bool AddNew(OMParameters omVars)
        {
            return true;
        }

        public bool ControlInputs(DYNAMICSParams dynamicsParams, float steering, float throttle,
            float brake, float clutch, int gear, long dInput)
        {
            _driver.brake = brake;
            _driver.buttons = dInput;
            _driver.clutch = clutch;
            _driver.gear = gear;
            _driver.steer = steering;
            _driver.throttle = throttle;

            gear = 0;
            return true;
        }

        public bool Dynamics(DYNAMICSParams dyn)
        {
            return true;
        }

        public bool HandleCrash(int overRide, int crashEvent, int eventIndex)
        {
            overRide = 0;
            return true;
        }
    }
}