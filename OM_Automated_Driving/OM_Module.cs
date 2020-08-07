using System;
using System.IO;
using Interop.TJRWinTools3;
using System.Runtime.InteropServices;

namespace OM_Automated_Driving
{
    [Guid("a8be81f3-a471-4085-8bad-13b9ca5da2f6")]
    [ComVisible(true)]
    public interface IOM_Module
    {
        [DispId(1)]
        string BSAVData { get; }
        
        [DispId(2)]
        object DashboardForm { set; }

        [DispId(3)]
        string ErrorMessage { get; }
        
        [DispId(4)]
        SimEvents EventsIn { set; }

        [DispId(5)]
        SimEvents EventsOut { get; }
    
        [DispId(6)]
        int LogFileHandle { set; }
        
        [DispId(7)]
        object NewForm { set;  }
        
        [DispId(8)]
        short SaveControls { get; }
        
        [DispId(9)]
        object StartForm { set; }
        
        [DispId(10)]
        string TextMessage { get; }
        
        [DispId(11)]
        int WillHandleCrash { get; }

        [DispId(12)]
        bool AddNew(OMParameters OMVars);

        [DispId(13)]
        bool ControlInputs(DYNAMICSParams Dyn, ref float Steering, ref float Throttle, ref float Brake, 
            ref float Clutch, ref short Gear, ref int DInput);

        [DispId(14)]
        bool Dynamics(ref DYNAMICSParams Dyn);

        [DispId(15)]
        bool HandleCrash(ref short Override, short CrashEvent, short EventIndex);

        [DispId(16)]
        bool Initialize(ref OMStaticVariables SV, int[] WorldIndex, TJR3DGraphics GraphicsIn, STI_3D_Terrain TerrainIn);

        [DispId(17)]
        bool PostRun(string Comments, string DriverName, string RunNumber, string DriverID);

        [DispId(18)]
        bool SavePlaybackData(ref float[] PlaybackData, ref string PlaybackString);

        [DispId(19)]
        bool Shutdown(int RunCompleted);

        [DispId(20)]
        bool StartUp(ref GAINSParams Config, object BackForm, ref OMStaticVariables SV, ref bool UseNew,
             float[] PlaybackData, string PlaybackString, string ParamFile, TJRSoundEffects SoundIn);

        [DispId(21)]
        bool Update(ref OMDynamicVariables DV, DYNAMICSParams Vehicle, short NumEvents, ref float[] EDist,
            short[] EDes, short[] EIndex);

    }
    
    [Guid("e802851c-de8c-4b30-afec-eecd6c87bbec"),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface OMCOM_Events
    {
        // Left empty intentionally
    }
    
    [Guid("b18abdab-01d8-4b10-b3f9-24d558a22ee9"), 
     ClassInterface(ClassInterfaceType.None), 
     ComSourceInterfaces(typeof(OMCOM_Events))
    ]
    [ComVisible(true)]
    [ProgId("OM_Automated_Driving.OM_Module")]
    [Serializable]
    public class OM_Module : IOM_Module
    {
        
        // Setup references to the STISIM Drive COM objects
        TJR3DGraphics graphics = new TJR3DGraphics();
        TJRSoundEffects sound = new TJRSoundEffects();
        STI_3D_Terrain terrain = new STI_3D_Terrain();
        TJRWinToolsCls tools = new TJRWinToolsCls();
        
        // Define constants for the worlds used by the graphics object
        static int WORLD_ROADWAY = Convert.ToInt32(SimConstants.WORLD_ROADWAY);
        static int WORLD_SCREEN = Convert.ToInt32(SimConstants.WORLD_ORTHOGRAPHIC);
        
        // Instantiate all variables that are public to this class and the calling routine
        public SimEvents Events;
        private string OM_BSAVData;
        private object OM_DashboardForm;
        private string OM_ErrorMessage;
        private int OM_LogFileHandle;
        private object OM_NewForm;
        private short OM_SaveControls;
        private object OM_StartForm;
        private string OM_TextMessage;
        private int OM_WillHandleCrash;
        
        // Create a type for the driver input information
        // TODO: Refactor into own file/class
        private struct DriverControlInputs
        {
            // Brake control count from the controller card
            public float Brake;
            
            // Current state of the driver's input buttons
            public int Buttons;
            
            // Clutch control count from the controller card
            public float Clutch;
            
            // Current transmission gear 
            public short Gear;
            
            // Steering angle count from the controller card
            public float Steer;
            
            // Throttle control count from the controller card
            public float Throttle;
        }
        private DriverControlInputs Driver = new DriverControlInputs();
        
        // Define all variables that will be global to this class
        
        // User defined type containing STISIM Drive variables that change as the run progresses
        private OMDynamicVariables DynVars = new OMDynamicVariables {};
        
        // Type for holding the configuration parameters
        private GAINSParams Gains = new GAINSParams {};
        
        // Graphics ID for the screen world
        private int ID_Screen;
        
        // Graphics ID for the world view
        private int ID_World;
        
        // User defined type containing STISIM drive variables that are fixed by the simulator
        private OMStaticVariables StaticVars = new OMStaticVariables {};

        // Zero parameter constructor included to satisfy COM registration requirements
        public OM_Module()
        {
        }
    
        // Public properties for interfacing with STISIM internal variables
        public string BSAVData
        {
            get { return OM_BSAVData; }
        }

        public object DashboardForm
        {
            set { OM_DashboardForm = value; }
        }

        public string ErrorMessage
        {
            get { return OM_ErrorMessage; }
        }
        
        // NOTE: not deep clone, potential source of error
        public SimEvents EventsIn
        {
            set { Events = value; }
        }

        public SimEvents EventsOut
        {
            get { return Events; }
        }

        public int LogFileHandle
        {
            set { OM_LogFileHandle = value; }
        }

        public object NewForm
        {
            set { OM_NewForm = value;  }
        }

        public short SaveControls
        {
            get { return OM_SaveControls; }
        }

        public object StartForm
        {
            set { OM_StartForm = value; }
        }

        public string TextMessage
        {
            get { return OM_TextMessage; }
        }

        public int WillHandleCrash
        {
            get { return OM_WillHandleCrash; }
        }
        
        // Public functions for defining OM behaviour

        public bool AddNew(OMParameters OMVars)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "AddNew" + e.Message;
                return false;
            }
            
        }

        public bool ControlInputs(DYNAMICSParams Dyn, ref float Steering, ref float Throttle, ref float Brake,
            ref float Clutch, ref short Gear, ref int DInput)
        {
            try
            {
                Driver.Brake = Brake;
                Driver.Buttons = DInput;
                Driver.Clutch = Clutch;
                Driver.Gear = Gear;
                Driver.Steer = Steering;
                Driver.Throttle = Throttle;

                Gear = 0;
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "ControlInputs" + e.Message;
                return false;
            }
            
        }

        public bool Dynamics(ref DYNAMICSParams Dyn)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Dynamic " + e.Message;
                return false;
            }
            
        }

        public bool HandleCrash(ref short Override, short CrashEvent, short EventIndex)
        {
            try
            {
                Override = 0;
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "handle crash " + e.Message;
                return false;
            }
            
        }

        public bool Initialize(ref OMStaticVariables SV, int[] WorldIndex, TJR3DGraphics GraphicsIn, 
            STI_3D_Terrain TerrainIn)
        {
            try
            {
                // Get the handles to the simulator's 3D roadway world and 2D screen world
                ID_World = WorldIndex[WORLD_ROADWAY];
                ID_Screen = WorldIndex[WORLD_SCREEN];

                // Assign references to the main graphics and terrain objects so they can be used in other modules
                graphics = GraphicsIn;
                terrain = TerrainIn;

                // Make the static variables available to all other methods
                StaticVars = SV;
                

                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Initialize" + e.Message;
                return false;
            }
            
        }

        public bool PostRun(string Comments, string DriverName, string runNumber, string DriverID)
        {

            try
            {
                // Release some of the objects that were created
                sound = null;
                tools = null;

                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "PostRun " + e.Message;
                return false;
            }

        }

        public bool SavePlaybackData(ref float[] PlaybackData, ref string PlaybackString)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "SavePlaybackData" + e.Message;
                return false;
            }
        }

        public bool Shutdown(int RunCompleted)
        {
            try
            {
                // Release some of the objects that were created
                graphics = null;
                terrain = null;

                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "shutdown " + e.Message + e.Source + e.StackTrace;
                return false;
            }
        }

        public bool StartUp(ref GAINSParams Config, object BackForm, ref OMStaticVariables SV, ref bool UseNew,
            float[] PlaybackData, string PlaybackString, string ParamFile, TJRSoundEffects SoundIn)
        {
            try
            {
                // Assign a reference to the local sound object so that it can be used in other modules
                sound = SoundIn;

                StreamReader ParamsIn;

                // If there is an initialization file specified then do the initializing
                if (File.Exists(ParamFile) && ParamFile.Length > 0)
                {
                    ParamsIn = new StreamReader(ParamFile);
                    ParamsIn.Close();
                }

                // Save a local version of the configuration file
                Gains = Config;

                UseNew = false;
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Startup " + e.Message;
                return false;
            }
            
        }

        public bool Update(ref OMDynamicVariables DV, DYNAMICSParams Vehicle, short NumEvents, ref float[] EDist, 
            short[] EDes, short[] EIndex)
        {
            try
            {
                DynVars = (OMDynamicVariables) CloneStructure(DV);
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Update " + e.Message;
                return false;
            }
        }

        // Private methods
        private SimEvents CloneEvents(SimEvents StrIn)
        {
            return StrIn;
        }
        
        private object CloneStructure(object StrIn)
        {
            return StrIn;
        }

        private string ProcessError(string ModuleName)
        {
            bool Bool;
            string st;

            st = "(custom) Simulation run aborted! an error has occurred in Open Module " + ModuleName;
            st = "";
            return st;
        }
    }
}