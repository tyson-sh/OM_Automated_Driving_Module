namespace OM_Automated_Driving
{
    public struct InputParameters
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
}