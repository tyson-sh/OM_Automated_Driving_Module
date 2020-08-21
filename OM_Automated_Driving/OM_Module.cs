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
            public float Proportional;
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
            public int Handle;
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
                // Disable the autonomous system
                LaneControlActive = false;
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
                // Dimension all variables local to this routine
                long lng;
                int ModelIndex; // Change from long to int (no implicit conversion for ref params)
                int NumVerts; // Change from long to int (no implicit conversion for ref params)
                ColorAttributes PolyColor;
                float[] UT = new float[4];
                float[] VT = new float[4];
                float[] XPoly = new float[4];
                float[] YPoly = new float[4];
                float[] ZPoly = new float[4];
                
                
                // Get the handles to the simulator's 3D roadway world and 2D screen world
                ID_World = WorldIndex[WORLD_ROADWAY];
                ID_Screen = WorldIndex[WORLD_SCREEN];

                // Assign references to the main graphics and terrain objects so they can be used in other modules
                graphics = GraphicsIn;
                terrain = TerrainIn;

                // Make the static variables available to all other methods
                StaticVars =  (OMStaticVariables)CloneStructure(SV);
                
                // Setup our autonomous vehicle classes
                SetupPIDController(ref Cruise, ref Params.CruisePID);
                SetupPIDController(ref Follow, ref Params.FollowPID);
                SetupPIDController(ref LateralPos, ref Params.LanePID);
                
                // Set some initial values
                ControlMode = AUTONOMOUS_MANUAL;
                LaneControlActive = false;
                VehTargetSpeed = 60;
                SignalActive = TURNSIG_NONE;
                
                // Set the initial headway settings
                ThwCycle = 2;
                Thw = Params.Headway[ThwCycle];
                
                // Add a couple of display images to the dashboard overlay form
                NumVerts = 4;
                XPoly[0] = 0;
                XPoly[1] = 0;
                XPoly[2] = 0;
                XPoly[3] = 0;
                YPoly[0] = 0;
                ZPoly[0] = 0;
                YPoly[1] = YPoly[0]; // WTF
                ZPoly[1] = Params.Image_Size * StaticVars.SimWindow.Height;
                YPoly[2] = Params.Image_Size * StaticVars.SimWindow.Width;
                ZPoly[2] = ZPoly[1];
                YPoly[3] = YPoly[2];
                ZPoly[3] = ZPoly[0];
                
                // Setup our texture coordinates
                UT[0] = 0;
                VT[0] = 0;
                UT[1] = UT[0];
                VT[1] = 1;
                UT[2] = 1;
                VT[2] = VT[1];
                UT[3] = UT[2];
                VT[3] = VT[0];
                
                // Define the polygon color
                PolyColor.Red = 1;
                PolyColor.Green = 1;
                PolyColor.Blue = 1;
                PolyColor.Alpha = 1;
                
                // Create the ACC image
                if (tools.FileExist(Params.Image_ACC))
                {
                    // Start the model definition
                    ACCDisplay.Description = "OM_ACC_Display";
                    ModelIndex = GraphicsIn.StartModelDefinition(ref ACCDisplay.Description, 
                        ref ID_Screen, Convert.ToInt32(NumVerts));
                    
                    // Pass the information to the graphics renderer
                    lng = graphics.SetMaterial(Params.Image_ACC, PolyColor, TEXTURE_CLAMP, null, null, null);
                    graphics.AddGLPrimitive( NumVerts, XPoly, YPoly, ZPoly, UT, VT, ID_Screen, ModelIndex);
                    lng = graphics.EndModelDefinition(ID_Screen, ModelIndex);
                    
                    // Set the background position on the screen
                    ACCDisplay.SixDOF.Y = StaticVars.SimWindow.OffsetX + Params.Image_Left * StaticVars.SimWindow.Width;
                    ACCDisplay.SixDOF.Z = StaticVars.SimWindow.OffsetY + Params.Image_Top * StaticVars.SimWindow.Height;
                    ACCDisplay.Handle = graphics.LoadGraphicObject(ACCDisplay.SixDOF, ID_Screen, null,
                        ACCDisplay.Description, STAGE_ORTHAGONAL);
                    graphics.SetObjectPosition(ACCDisplay.Handle, ACCDisplay.SixDOF);
                    graphics.SetObjectVisibility(ACCDisplay.Handle, GRAPHICS_IMAGE_OFF);
                }
                
                // Create the HAD image
                if (tools.FileExist(Params.Image_HAD))
                {

                    // Start the model definition
                    LaneKeepingDisplay.Description = "OM_HAD_Display";
                    ModelIndex = graphics.StartModelDefinition(LaneKeepingDisplay.Description, ID_Screen, NumVerts);

                    // Pass the information to the graphics renderer
                    lng = graphics.SetMaterial(Params.Image_HAD, PolyColor, TEXTURE_CLAMP, null, null, null);
                    graphics.AddGLPrimitive(NumVerts, XPoly, YPoly, ZPoly, UT, VT, ID_Screen, ModelIndex);
                    lng = graphics.EndModelDefinition(ID_Screen, ModelIndex);

                    // Set the background Position on the screen
                    ACCDisplay.SixDOF.Y = StaticVars.SimWindow.OffsetX + Params.Image_Left * StaticVars.SimWindow.Width;
                    ACCDisplay.SixDOF.Z = StaticVars.SimWindow.OffsetY -
                                          (Params.Image_Top - Params.Image_Size) * StaticVars.SimWindow.Height;
                    ACCDisplay.Handle = graphics.LoadGraphicObject(ACCDisplay.SixDOF, ID_Screen, null,
                        ACCDisplay.Description, STAGE_ORTHAGONAL);
                    graphics.SetObjectPosition(ACCDisplay.Handle, ACCDisplay.SixDOF);
                    graphics.SetObjectVisibility(ACCDisplay.Handle, GRAPHICS_IMAGE_OFF);
                }

                return true;
            }
            catch (Exception e)
            {
                OM_ErrorMessage = "Initialize Module threw an exception " + Environment.NewLine + 
                                  "Message: " + e.Message + Environment.NewLine + 
                                  "Source: " + e.Source +
                                  "Data " + e.Data + Environment.NewLine + "Stack trace: " + e.StackTrace;
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
                
                // Dimension all variables local to this routine
                int ErrorType;
                int FileNum;
                string FileParam = "";
                int I;
                int J;
                string ParamName;
                string ParamVal;
                
                // Assign a reference to the local sound object so that it can be used in other modules
                sound = SoundIn;
                
                // Make sure we have a set of controls and that the system is not set for autopilot.
                // If it is, throw error and abort simulation run
                if ((Config.IControlFlag < CONTROLLER_GAME) || (Config.IControlFlag > CONTROLLER_STI_ADS_II))
                {
                    ErrorType = 1;
                    throw new InvalidOperationException("An error occured");
                }
                
                // TODO: Potential bug... Test this later
                if (Convert.ToBoolean(Config.IAutoPilot))
                {
                    throw new InvalidOperationException("System is in autopilot mode");
                }
                
                // Setup any labels that will be used to display data in the STISIM Drive runtime window display

                if (SV.DisplaySystem.Equals("CenterDisplay"))
                {
                    SV.DisplayStrings[1] = "Autonomous Mode";
                    SV.DisplayStrings[2] = "ACC Mode";
                }
                
                // Set some default values in case data is not provided in the INI file
                Array.Resize(ref Params.Headway, 3); // Potentially mistranslated
                Array.Resize(ref Params.Buttons, Convert.ToInt32(BUTTON_RIGHTLANECHANGE + 1)); // Potentially mistranslated

                Params.CruisePID.Derivative = 100;
                Params.CruisePID.Integral = 2000;
                Params.CruisePID.Proportional = 5000;

                Params.FollowPID.Derivative = 5;
                Params.FollowPID.Integral = 20;
                Params.FollowPID.Proportional = 3000;
                
                // Sticking to the source... But I think that this Section should be LanePID
                Params.FollowPID.Derivative = 400;
                Params.FollowPID.Integral = 150;
                Params.FollowPID.Proportional = 700;

                Params.Headway[0] = 1;
                Params.Headway[1] = 1.5f;
                Params.Headway[2] = 2;
                Params.Range = 328;

                tools.WriteToTJRFile(ref OM_LogFileHandle, "Just a test");
                
                // If there is an initialization file specified, perform the initializing
                //
                // NOTE: During the initial rewrite of this if statement I have deviated pretty far from the
                // initial source.  This is because the way a file is read has changed fairly dramatically since VB6 
                // was widely used
                if (File.Exists(ParamFile))
                {
                    // Initialize stream reader (this statement also closes the reader)
                    using (StreamReader streamReader = new StreamReader(ParamFile))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            FileParam = line.Trim();

                            // Check to make sure line is not blank
                            if (FileParam.Length <= 0 || FileParam.Equals("")) continue;
                            
                            // 1:1 conversion, not sure what tools.Extract does
                            FileParam = tools.Extract(ref FileParam, "%", 1);
                            
                            // Last check to ensure line is not null
                            if (FileParam == null) continue;
                            ParamName = tools.Extract(ref FileParam, "=", 1).Trim();
                            ParamVal = tools.Extract(ref FileParam, "=", 2).Trim();

                            if (!ParamName.Substring(0).Equals("%")) 
                            {
                                switch (ParamName.ToUpper())
                                    {
                                        case "CYCLE HEADWAY TIME" : 
                                            Params.Buttons[BUTTON_CYCLEHEADWAYTIME] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "DECREASE VEHICLE SPEED" :
                                            Params.Buttons[BUTTON_DECREASESPEED] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "INCREASE VEHICLE SPEED" :
                                            Params.Buttons[BUTTON_INCREASESPEED] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "ACTIVATE ACC" :
                                            Params.Buttons[BUTTON_ACTIVATEACC] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "ACTIVATE HAD" :
                                            Params.Buttons[BUTTON_ACTIVATEHAD] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "CANCEL AUTONOMOUS MODE" :
                                            Params.Buttons[BUTTON_CANCEL] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "COMMAND LEFT LANE CHANGE" :
                                            Params.Buttons[BUTTON_LEFTLANECHANGE] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "COMMAND RIGHT LANE CHANGE" :
                                            Params.Buttons[BUTTON_RIGHTLANECHANGE] = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "EXCEED SPEED LIMIT" :
                                            Params.LimitSpeed = Convert.ToInt64(ParamVal);
                                            break;
                                        
                                        case "BRAKE GAIN" :
                                            Params.BrakeGain = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "THROTTLE GAIN" :
                                            Params.ThrottleGain = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "STEERING GAIN" :
                                            Params.SteeringGain = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "STEERING INPUT LIMIT" :
                                            Params.SteeringLimit = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "HEADWAY TIME SMALL" :
                                            Params.Headway[0] = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "HEADWAY TIME MODERATE" :
                                            Params.Headway[1] = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "HEADWAY TIME LARGE" :
                                            Params.Headway[2] = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "RANGE" :
                                            Params.Range = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "CRUISE PROPORTIONAL" :
                                            Params.CruisePID.Proportional = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "CRUISE INTEGRAL" :
                                            Params.CruisePID.Integral = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "CRUISE DERIVATIVE" :
                                            Params.CruisePID.Derivative = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "FOLLOW PROPORTIONAL" :
                                            Params.FollowPID.Proportional = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "FOLLOW INTEGRAL" :
                                            Params.FollowPID.Integral = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "FOLLOW DERIVATIVE" :
                                            Params.FollowPID.Derivative = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "LANE CHANGE PROPORTIONAL" :
                                            Params.LanePID.Proportional = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "LANE CHANGE INTEGRAL" :
                                            Params.LanePID.Integral = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "LANE CHANGE DERIVATIVE" :
                                            Params.LanePID.Derivative = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "LANE CHANGE DELAY" :
                                            Params.LaneChangeDelay = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "ACCMODE IMAGE" :
                                            Params.Image_ACC = ParamVal.Trim();
                                            break;
                                        
                                        case "HADMODE IMAGE" :
                                            Params.Image_HAD = ParamVal.Trim();
                                            break;
                                        
                                        case "IMAGE SIZE" :
                                            Params.Image_Size = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "IMAGE TOP" :
                                            Params.Image_Top = Convert.ToSingle(ParamVal);
                                            break;
                                        
                                        case "Image LEFT" :
                                            Params.Image_Left = Convert.ToSingle(ParamVal);
                                            break;
                                    }
                            }
                        }
                        
                        // Since buttons will be performing certain functions, manipulate our button settings
                        
                        // Get the turn signal values
                        LeftTurnButton = Config.ExtButtonMasksLng[TURN_BUTTON_LEFT];
                        RightTurnButton = Config.ExtButtonMasksLng[TURN_BUTTON_RIGHT];
                        SignalToggle = Config.Dashboard.TurnSig.Toggle;
                        
                        // Disable any buttons that have also been assigned an autonomous function
                        // TODO: Source starts loop at 1, usually starts at 0
                        for (int i = 1; i < Config.ExtButtonMasksLng.Length; i++)
                        {
                            for (int j = 0; j < Params.Buttons.Length; j++)
                            {
                                if (Config.ExtButtonMasksLng[i] == Params.Buttons[j])
                                {
                                    Config.ExtButtonMasksLng[i] = 0;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ErrorType = 3;
                    throw new FileNotFoundException("Cannot locate param file");
                }
                
                // Adjust config settings and pre-compute some variables using config file info
                
                // Get the amount the pedals will travel
                BrakeTravel = Math.Abs(Config.IBrakeMax - Config.IBrakeMin);
                ThrottleTravel = Math.Abs(Config.IThrottleMax - Config.IThrottleMin);
                
                // Get the side of the road the driver is supposed to drive on
                DriveOnLeft = Convert.ToInt64(Config.RoadSide);
                
                // Compute the gains for the control axis
                if (Convert.ToBoolean(Params.BrakeGain))
                {
                    BrakeSF = - Params.BrakeGain;
                }
                else
                {
                    BrakeSF = -10 * Math.Abs(Config.UDotBrkMax);
                }

                if (Convert.ToBoolean(Params.ThrottleGain))
                {
                    ThrottleSF = Params.ThrottleGain;
                }
                else
                {
                    ThrottleSF = 50 * Math.Abs(Config.UDotAcMax);
                }

                if (Convert.ToBoolean(Params.SteeringGain))
                {
                    SteeringSF = Params.SteeringGain;
                }
                else
                {
                    SteeringSF = Config.FKSW / 0.014f;
                }

                // Save a local version of the configuration file
                Gains = (GAINSParams) CloneStructure(Config);
                
                // Set it so that the simulator records the inputs from this module
                OM_SaveControls = 1;

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

        private void SetupPIDController(ref Controller controller, ref PIDValues Values)
        {
            controller.KDerivative = Values.Derivative;
            controller.KIntegral = Values.Integral;
            controller.KProportional = Values.Proportional;
        }
    }
}