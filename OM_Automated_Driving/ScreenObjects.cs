using Interop.TJRWinTools3;

namespace OM_Automated_Driving
{
    
    // Create a type for holding information for the screen objects
    public struct ScreenObjects
    {
        public string Description;
        public int Handle;
        public long ModelID;
        public SixDOFPosition SixDOF;
        public long VisIndex;
    }
}