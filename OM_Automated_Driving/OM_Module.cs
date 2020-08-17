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
    /// Main class of library currently does the bulk of the work.  Will be refactored in the future to better reflect
    /// the single responsibility principle of object orientated design.
    /// </para>
    /// <para>
    /// This class must not be renamed from OM_Module.cls, otherwise interfacing capability with STISIM OM will
    /// be lost.  It must also be registered in the windows registry as a COM object.  use
    /// <c>regasm.exe path/to/this/dll</c> to perform this action.
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
        
        // Dimension all variables that are public to this class and the calling routine
        private string OM_BSAVData;
        private string OM_ErrorMessage;
        private long OM_LogFileHandle;
        private object OM_NewForm;
        private int OM_SaveControls;
        private string OM_TextMessage;
        private long OM_WillHandleCrash;

        // Create a type for the driver input information
        // TODO: Refactor into own file/class
        private struct DriverControlInputs
        {
            public float Steer;
            public float ThrottleIn;
            public float ThrottleOut;
            public float BreakOut;
            public float BreakIn;
            public int Gear;
            public long Buttons;
            public long DInput;
            public float SteerOut;
            public float Clutch;
        }
        
        private DriverControlInputs Driver = new DriverControlInputs();

        private bool tempBool;
        private OMDynamicVariables DynVars = new OMDynamicVariables();
        private OMStaticVariables StaticVars = new OMStaticVariables();
        private int PlaybackMode;
        private string tempSt;
        private long[] WorldIndex;
        
        // Create type for vehicle information
        // TODO: refactor into own file/class
        private struct vehicle
        {
            public float Acceleration;
            public long BrakeModel;
            public long index;
            public float InitialHeading;
            public double lat;
            public double lon;
            public SixDOFPosition SixDOF;
            public float speed;
            public float SpinDuration;
            public long SpinModel;
            public float SpinSpeed;
            public long VisFlag;
        }

        private vehicle[] v;
        
        // FROM TRFQuery.cls
        public struct SurrVehicle
        {
            public float Acceleration;
            public SixDOFPosition SixDOF;
            public float Speed;
            public float Xpos;
            public float Ypos;
            public double range;
            public int currentLane;
            public int direction;
            public float length;
            public bool updated;
            public float LatPos;
        }

        private SurrVehicle VAhead;
        private SurrVehicle VNextAhead;
        private SurrVehicle lahead;
        private SurrVehicle rahead;
        private SurrVehicle Lbehind;
        private SurrVehicle Rbehind;
        private TRFQuery traffic;

        private Controller cruise;
        private Controller follow;
        private Controller lat;
        private Bezier linearplanner;
        private long simulationtime;
        private int Freq;
        private double thw;
        private int THWCycle;
        private float vTarget;
        private float Vset;
        private float vCurr;
        private int mode;
        private long DriverInputPrev;
        private int ControlMode;
        private int LaneTarget;
        private int CurrentLane;
        private double calcThw;
        private double TTX;
        private bool running;
        private long cycleTHWBtn;
        private long ACCup;
        private long ACCDown;
        private long ActivateACC;
        private long ActivateHAD;
        private long CancelAutomation;
        private long LeftLaneChange;
        private long RightLaneChange;
        private long[] buttonArrary = new long[9];
        private string[] eventArray = new string[2];
        private int TOR;
        private double TORCD;
        private double TORLim;

        private double ThrottleSF;
        private double BrakeSF;
        private double SteeringSF;
        private long BrakeMin;
        private bool setup;

        private ClientClass socket;
        private int timecounter;
        private string lahead_;
        private string rahead_;
        private string rbehind_;
        private string lbehind_;
        private string ahead;
        private string msg;
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
            ref float Clutch, ref short Gear, ref int DInput)
        {
            try
            {
                
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
                StaticVars = SV;
                

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
    }
}