using System.Collections.Generic;
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

        public bool Initialize(OMStaticVariables SV, long[] worldIndex, TJR3DGraphics graphicsIn, STI_3D_Terrain terrainIn)
        {
            int i;
            int numVerts;
            
            // Get the handles to the simulator's 3D roadway world and 2D screen world
            // POTENTIALLY WRONG! C# does not have a mechanism for accessing word based index.  Going with index
            // specified on page 19 for time being.
            ID_World = worldIndex[0];
            ID_Screen = worldIndex[1];
            
            // Assign references to the main graphics and terrain objects so they can be used in other modules
            _graphics = graphicsIn;
            _terrain = terrainIn;
            
            // Make the static variables available to all other methods
            _staticVars = SV;
            
            return true;
        }

        public bool PostRun(string comments, string driverName, string runNumber, string driverID)
        {
            // Not a direct matching implementation, originally both these parameters set to VB 'nothing'
            // could cause compile time error as null != nothing
            _sound = null;
            _tools = null;
            
            return true;
        }

        public bool SavePlaybackData(float[] playBackData, string playbackString)
        {
            return true;
        }

        public bool Shutdown(int runCompleted)
        {
            // Not a direct matching implementation, originally both these parameters set to VB 'nothing'
            // could cause compile time error as null != nothing

            _graphics = null;
            _terrain = null;

            return true;
        }

        public bool StartUp(GAINSParams config, object backForm, OMStaticVariables SV,
            bool useNew, float[] playbackData, string playBackString, string paramFile, TJRSoundEffects soundIn)
        {
            int fileNum;
            
            // Assign a reference to the local sound object so that it can be used in other modules
            _sound = soundIn;
            
            // Setup any labels that will be used to display data in the STISIM Drive runtime window display
            SV.DisplayStrings[1] = "";
            SV.DisplayStrings[2] = "";
            SV.DisplayStrings[3] = "";
            SV.DisplayStrings[4] = "";
            SV.DisplayStrings[5] = "";
            
            // If there is an initialization file specified then do the initializing.
            // This is another statement that is slightly different to the orginal
            if (paramFile == null || paramFile.Equals(""))
            {
                // TODO Handle the initial parameter file
            }
            
            // Save the initial configuration file
            _gains = config;

            useNew = false;
            return true;
        }

        public bool Update(OMDynamicVariables DV, DYNAMICSParams vehicle, int numEvents,
            float[] eDist, float[] eDes, float[] eIndex)
        {
            _dynVars = DV;
            return true;
        }
    }
}