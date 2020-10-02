namespace OM_Automated_Driving
{
    // Create a type for the driver input information
    public struct DriverControlInputs
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
}