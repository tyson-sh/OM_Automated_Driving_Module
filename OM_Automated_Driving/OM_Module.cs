/*
************************************************************************************************************************
Copyright <2017> <Alexander Eriksson, Joost De Winter, Neville A Stanton, Transportation Research Group,University of 
Southampton, Uk>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
rights to use, copy, modify, merge, publish, distribute, sublicense,and/or sell copies of the Software, and to permit 
persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Acknowledgement: The authors conducted this work within the Marie Curie Initial Training Network (ITN) HF Auto - Human 
Factors of Automated Driving (PITN-GA-2013-605817).

Note: the OM_Module shell class is accredited to Theodore J. Rosenthal and Jeff P. Chrstos (2013) and is provided with 
the STISIM Open Module feature.
************************************************************************************************************************
*/

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
    
    ///
    /// <summary>
    /// <para>
    ///     Main class of library currently does the bulk of the work.  Will be refactored in the future to better
    ///     reflect the single responsibility principle of object orientated design.
    /// </para>
    /// <para>
    ///     This class must not be renamed from OM_Module.cls, otherwise interfacing capability with STISIM OM will
    ///     be lost.  It must also be registered in the windows registry as a COM object.  use
    ///     <c>regasm.exe path/to/this/dll</c> to perform this action.
    /// </para>
    /// </summary>
    /// 
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
        
        // Setup constant properties
        private const long ACCMODE_OFF = 0;
        private const long ACCMODE_CRUISE = 1;
        private const long ACCMODE_FOLLOWING = 2;
        private const long ACCMODE_ADAPTING = 3;
        private const long AUTONOMOUS_MANUAL = 0;
        private const long AUTONOMOUS_ACC = 1;
        private const long AUTONOMOUS_FULL = 2;
        private const float DELTASPEED = 3.28f * 3.5f;
        private const float FEETPERSECTOMPH = 3600f / 5280f;
        private const long OPTION_OFF = -1;
        private const float NO_THREAT = -999f;
        private const float SPEED_CONST_1 = 1.09728f;
        private const float SPEED_CONST_2 = 5f;
        private const float SPEED_INCREMENT = 1.61f;
        private const float SPEED_MAX = 130f;
        private const long TURNSIG_NONE = 0;
        private const long TURNSIG_LEFT = 1;
        private const long TURNSIG_RIGHT = 2;
        private const float TURNSIG_OFF_THRESHOLD = 1f;
        
        // TODO Remove
        private int DisplayTemp;
        
        // Define constants for the vehicles around the driven vehicle
        private const long VEH_FRONT = 0;
        private const long VEH_LEFTFRONT = 1;
        private const long VEH_RIGHTFRONT = 2;
        private const long VEH_LEFTREAR = 3;
        private const long VEH_RIGHTREAR = 4;
        
        // Define constants for the vehicles around the driven vehicle
        private const long BUTTON_CYCLEHEADWAYTIME = 0;
        private const long BUTTON_DECREASESPEED = 1;
        private const long BUTTON_INCREASESPEED = 2;
        private const long BUTTON_ACTIVATEACC = 3;
        private const long BUTTON_ACTIVATEHAD = 4;
        private const long BUTTON_CANCEL = 5;
        private const long BUTTON_LEFTLANECHANGE = 6;
        private const long BUTTON_RIGHTLANECHANGE = 7;

        // Define constants for the worlds used by the graphics object
        static int WORLD_ROADWAY = Convert.ToInt32(SimConstants.WORLD_ROADWAY);
        static int WORLD_SCREEN = Convert.ToInt32(SimConstants.WORLD_ORTHOGRAPHIC);
        
        // Assign STISIM Drive simulator constants to local constant names
        private const int CONTROLLER_GAME = (int) ControllerConstants.CONTROLLER_GAME;
        private const int CONTROLLER_STI_ADS_II = (int) ControllerConstants.CONTROLLER_STI_ADS_II;
        private const int DIRECTION_DRIVER = (int) SimConstants.DIRECTION_DRIVER;
        private const int EFFECT_BRAKING = (int) SimConstants.EFFECT_BRAKING;
        private const int EFFECT_Cornering = (int) SimConstants.EFFECT_CORNERING;
        private const int EVENTDEFVEHICLE = (int) SimConstants.EVENTDEFVEHICLE;
        private const int GRAPHICS_IMAGE_OFF = (int) GraphicsConstants.GRAPHICS_IMAGE_OFF;
        private const int GRAPHICS_IMAGE_ON = (int) GraphicsConstants.GRAPHICS_IMAGE_ON;
        private const int STAGE_ORTHAGONAL = (int) GraphicsConstants.STAGE_ORTHAGONAL;
        private const int TEXTURE_CLAMP = (int) GraphicsConstants.TEXTURE_CLAMP;
        private const int TURN_BUTTON_LEFT = (int) SimConstants.BUTTON_LEFT;
        private const int TURN_BUTTON_RIGHT = (int) SimConstants.BUTTON_RIGHT;

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
            // Original braking input from the driver
            public float BrakeIn;
            
            // Braking input that will be used
            public float BrakeOut;

            // Current state of the driver's input buttons
            public long Buttons;
            
            // Save the current values of the buttons
            public long ButtonsPrev;
            
            // Clutch control count from the controller card
            public float Clutch;
            
            // Current transmission gear 
            public short Gear;
            
            // Original steering angle input from the driver
            public float SteerIn;
            
            // Steering input that will be used
            public float SteerOut;

            // Original throttle control input from the driver
            public float ThrottleIn;
            
            // Gas pedal input that will be used
            public float ThrottleOut;
        }
        private DriverControlInputs Driver = new DriverControlInputs();
        
        // Create a type and variable
        // TODO: Refactor into own file/class
        private struct PIDValues
        {
            public float Derivative;
            public float Integral;
            public float proportional;
        }
        
        // TODO: Refactor into own file/class
        private struct InputParams
        {
            public float BrakeGain;
            public long[] Buttons;
            public PIDValues CruisePID;
            public PIDValues FollowPID;
            public float[] Headway;
            public string Image_ACC;
            public string Image_HAD;
            public float Image_Left;
            public float Image_Size;
            public float Image_Top;
            public float LaneChangeDelay;
            public PIDValues LanePID;
            public long LimitSpeed;
            public float Range;
            public float SteeringGain;
            public float SteeringLimit;
            public float ThrottleGain;
        }
        
        InputParams Params = new InputParams();
        
        // Create a type for holding other vehicle information
        // TODO: Refactor into own file/class
        private struct DetectedVehicle
        {
            public float Distance;
            public long VehicleIndex;
        }
        
        // Create a type for holding information for the screen objects
        // TODO: Refactor into own file/class
        private struct ScreenObjects
        {
            public string Description;
            public long Handle;
            public long ModelID;
            public SixDOFPosition SixDOF;
            public long VisIndex;
        }
        
        ScreenObjects ACCDisplay = new ScreenObjects();
        ScreenObjects LaneKeepingDisplay = new ScreenObjects();
        
        // Define variables that will be global to this class and hold OM information
        private long BrakeTravel;
        private long ButtonPressed;
        private long DriveOnLeft;
        private long LaneTarget;
        private long ThrottleTravel;

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
        
        // Define some autonomous objects
        private Controller Cruise = new Controller();
        private Controller Follow = new Controller();
        private Controller LateralPos = new Controller();
        private Bezier Trajectory = new Bezier();
        
        // Define variables that will be global to this class and hold OM information
        private long ACCMode; // Change from int to long (no implicit conversion in c#)
        private long ControlMode; // Change from int to long (no implicit conversion in c#)
        private bool LaneControlActive;
        private long LeftTurnButton;
        private long RightTurnButton;
        private long SignalActive;
        private short SignalToggle;
        private long SimFrameCount = 0;
        private float Thw;
        private int ThwCycle;
        private float VehCurSpeed;
        private float VehTargetSpeed;
        
        // Scaling for controller values
        private float BrakeSF;
        private float SteeringSF;
        private float ThrottleSF;
        
        /// <summary>
        ///     Zero parameter constructor included to satisfy COM registration requirements
        /// </summary>
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
            set { Events = CloneEvents(value); }
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
        
        
        /// <summary>
        ///     Function for adding a new interactive Open Module event.
        /// </summary>
        /// <param name="OMVars">
        ///     User defined type containing the parameters for the given Open Module being acted on.
        /// </param>
        /// <returns>
        ///    True if everything initialized is fine, otherwise the exception will be handled and the method will
        ///    return false
        /// </returns>
        /// 
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
        
        ///
        /// <summary>
        ///     Function for handling any user defined control inputs.
        /// </summary>
        /// <param name="Dyn">User defined type containing simulation dynamics variables</param>
        /// <param name="Steering">Steering wheel angle input digital count</param>
        /// <param name="Throttle">Throttle pedal input digital count</param>
        /// <param name="Brake">Braking pedal input digital count</param>
        /// <param name="Clutch">Clutch pedal input digital count</param>
        /// <param name="Gear">Current transmission geal</param>
        /// <param name="DInput">Current button values></param>
        /// <returns>
        ///    True if everything initialized fine.  If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
        public bool ControlInputs(DYNAMICSParams Dyn, ref float Steering, ref float Throttle, ref float Brake,
            ref float Clutch, ref short Gear, ref int Buttons)
        {
            try
            {
                DisplayTemp = Buttons;
                // Instantiate all variables local to this routine
                long TurnSigState = 0; // If buggy move to class level static field
                
                // Save the current button state
                Driver.ButtonsPrev = Driver.Buttons;
                
                // Make the driver inputs available to the other methods
                Driver.BrakeIn = Brake;
                Driver.ThrottleIn = Throttle;
                Driver.SteerIn = Steering;
                Driver.Gear = Gear;
                Driver.Buttons = Buttons;
                
                // If the driver applies the gas or brake pedal, turn the system off
                if ((Brake > 0.025 * BrakeTravel) || (Throttle > 0.025 * ThrottleTravel))
                {
                    ACCMode = ACCMODE_OFF;
                    ControlMode = AUTONOMOUS_MANUAL;
                    TurnDisplayOff();
                }
                
                // Depending on which driving mode we are in, set the driver inputs that will be passed back
                // TODO: Seriously rethink the structure of this nested switch statement

                switch (ControlMode)
                {
                    // Full manual mode
                    case AUTONOMOUS_MANUAL:
                        Steering = Driver.SteerIn;
                        Throttle = Driver.ThrottleIn;
                        Brake = Driver.BrakeIn;
                        SignalActive = TURNSIG_NONE;
                        break;
                    
                    // Lateral automation mode
                    case AUTONOMOUS_ACC:
                        
                        switch (ACCMode)
                        {
                            case ACCMODE_OFF:
                                Throttle = Driver.ThrottleIn;
                                Brake = Driver.BrakeIn;
                                break;
                            
                            case ACCMODE_CRUISE:
                                Throttle = Driver.ThrottleOut;
                                Brake = Driver.BrakeIn;
                                break;
                            
                            case ACCMODE_FOLLOWING:
                                Throttle = Driver.ThrottleOut;
                                Brake = Driver.BrakeOut;
                                break;

                        }
                        break;
                    
                    // Longitudinal automation mode
                    case AUTONOMOUS_FULL:
                        Steering = Driver.SteerOut;
                        Throttle = Driver.ThrottleOut;
                        Brake = Driver.BrakeOut;
                        break;
                }
                
                // Limit our inputs
                if (ControlMode != AUTONOMOUS_MANUAL)
                {
                    if (Throttle < 0)
                    {
                        Throttle = 0;
                    }

                    if (Throttle > ThrottleTravel)
                    {
                        Throttle = ThrottleTravel;
                    }

                    if (Brake < 0)
                    {
                        Brake = 0;
                    }

                    if (Brake > BrakeTravel)
                    {
                        Brake = BrakeTravel;
                    }
                }
                
                // Handle the turn signals if an auto lane change has been commanded
                // NOTE: What happens if zero?
                if (SignalToggle != 0) // Potentially buggy
                {
                    if ((TurnSigState - SignalActive) > 0)
                    {
                        // Beware Overflow
                        Buttons = Convert.ToInt32(ProcessSignal(SignalActive, Buttons));
                    } 
                    else
                    {
                        // Beware Overflow
                        Buttons = Convert.ToInt32(ProcessSignal(TurnSigState, Buttons));
                    }
                }
                else
                {
                    Buttons = Convert.ToInt32(ProcessSignal(SignalActive, Buttons));
                }

                TurnSigState = SignalActive;
                
                Gear = 0;
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "ControlInputs" + e.Message;
                return false;
            }
            
        }

        ///
        /// <summary>
        ///     Function for handling all Open Module dynamic updates.
        /// </summary>
        /// <param name="Dyn">User defined type containing the driver's vehicle dynamic variables</param>
        /// <returns>
        ///    True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///    method will return false. 
        /// </returns>
        /// 
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
        
        ///
        /// <summary>
        ///     Function for handling all Open Module action in the event of a driver crash during the simulation run.
        /// </summary>
        /// <param name="Override">
        ///     Parameter defining how STISIM Drive will handle the crash when this method returns
        ///     control to it
        /// </param>
        /// <param name="CrashEvent">Event designator for the event that caused the crash</param>
        /// <param name="EventIndex">Index specifying which instance of the crash event caused the crash</param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
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
        
        ///
        /// <summary>
        ///     Function for handling all Open Module initialization.
        /// </summary>
        /// <param name="SV">User defined type containing simulation static variables</param>
        /// <param name="WorldIndex">Handle for the various graphics context that hold the roadway environments</param>
        /// <param name="GraphicsIn">
        ///     Reference to the graphics object that the main simulator uses to draw the 3D world
        /// </param>
        /// <param name="TerrainIn">Reference to the terrain object that is used by the main simulation loop</param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
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
                StaticVars =  (OMStaticVariables)CloneStructure(SV);
                
                SV.DisplayStrings[1] = "Test_Display 1 = ";
                SV.DisplayStrings[2] = "Test_Display 2 = ";
                SV.DisplayStrings[3] = "Hot_Damn = ";
                SV.DisplayStrings[4] = "Frame_Count = ";
                SV.DisplayStrings[5] = "Button State: ";

                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Initialize" + e.Message;
                return false;
            }
            
        }
        
        ///
        /// <summary>
        ///     Function for handling anything before the software exits.
        /// </summary>
        /// <param name="Comments">Comments entered in the subject information form</param>
        /// <param name="DriverName">Name of the driver from the subject information form</param>
        /// <param name="runNumber">Run number entered in the subject information form</param>
        /// <param name="DriverID">ID entered from the subject information form</param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
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
        
        ///
        /// <summary>
        ///     Function for specifying any OM data that will be stored as part of a playback file.
        /// </summary>
        /// <param name="PlaybackData">Array containing the data that will be saved</param>
        /// <param name="PlaybackString">String containing string data that will be saved</param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
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
        
        /// <summary>
        ///     Function for handling Open Module processes immediately after a simulation run has ended.
        /// </summary>
        /// <param name="RunCompleted">
        ///     Flag specifying if the run completed successfully or not
        ///     0 - Aborted before start of run
        ///     1 - Run completed successfully
        ///     2 - Aborted during the run
        /// </param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
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
        
        ///
        /// <summary>
        ///     Function for handling Open Module processes immediately after the software is started.
        /// </summary>
        /// <param name="Config">Configuration file parameters</param>
        /// <param name="BackForm">Current STISIM Drive background form</param>
        /// <param name="SV">User defined type containing simulation static variables</param>
        /// <param name="UseNew">Flag specifying if a new background form will be used (True) or not (False)</param>
        /// <param name="PlaybackData">
        ///     Array containing any data that is being transferred from the playback file back into this module
        /// </param>
        /// <param name="PlaybackString">
        ///     String containing any string data that is being transferred from the playback file back into this module
        /// </param>
        /// <param name="ParamFile">
        ///     Name of a file that contains any parameters that will be required by the Open Module code
        /// </param>
        /// <param name="SoundIn">Simulation sound object</param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
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
                Gains = (GAINSParams) CloneStructure(Config);

                UseNew = false;
                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Startup " + e.Message;
                return false;
            }
        }
        
        ///
        /// <summary>
        ///     Function for handling all Open Module action during the actual simulation loop.
        /// </summary>
        /// <param name="DV">
        ///     User defined type containing the simulation parameters that are changing at each time step
        /// </param>
        /// <param name="Vehicle">User defined type containing the drivers vehicle dynamic variables</param>
        /// <param name="NumEvents">Number of events that are in the current display list</param>
        /// <param name="EDist">Distance from the driver to the event</param>
        /// <param name="EDes">Event designator for each active event</param>
        /// <param name="EIndex">
        ///     Event index for each event in the display list. This value is the index into the Events UDT so that
        ///     you can get the parameters for each individual event in the display list
        /// </param>
        /// <returns>
        ///     True if everything initialized fine. If exception is thrown it will be handled in the catch, and the
        ///     method will return false.
        /// </returns>
        /// 
        public bool Update(ref OMDynamicVariables DV, DYNAMICSParams Vehicle, short NumEvents, ref float[] EDist, 
            short[] EDes, short[] EIndex)
        {
            try
            {
                DynVars = (OMDynamicVariables) CloneStructure(DV);
                DV.DisplayStrings[1] = "test1";
                DV.DisplayStrings[2] = "test2";
                DV.DisplayStrings[3] = "It works!! I simply can't believe it!";
                DV.DisplayStrings[4] = Convert.ToString(SimFrameCount++);
                DV.DisplayStrings[5] = Convert.ToString(DisplayTemp);
                
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

        private void TurnDisplayOff()
        {
            graphics.SetObjectVisibility((int)ACCDisplay.Handle, GRAPHICS_IMAGE_OFF);
            graphics.SetObjectVisibility((int)LaneKeepingDisplay.Handle, GRAPHICS_IMAGE_OFF);
            SignalActive = TURNSIG_NONE;
        }
        
        private long ProcessSignal(long state, long Buttons)
        {
            // New to bitwise operations, this could be false
            // Also while comments indicate that left = 2 && right = 1, In the variable definitions
            // it is the inverse. Potentially a bug
            return state switch
            {
                TURNSIG_LEFT => ~((~Buttons) | (~LeftTurnButton)),
                TURNSIG_RIGHT => ~((~Buttons) | (~RightTurnButton)),
                _ => Buttons
            };
        }
    }
}