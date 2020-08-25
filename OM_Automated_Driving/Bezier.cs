using System;

namespace OM_Automated_Driving
{
    public class Bezier
    {
        private float Duration;
        private float SpeedInitial;
        private float SpeedTarget;
        private long StartFrame;
        private float T;
        private float TimeInc;
        
        /// <summary>
        ///     Interpolating
        /// </summary>
        public bool Interpolating { get; set; }
    
        /// <summary>
        ///     Routine that saves arguments for the starting and final parameters used for trajectory computation
        /// </summary>
        /// <param name="Speed">Current driven vehicle speed</param>
        /// <param name="Target">Target speed</param>
        /// <param name="FrameCount">Current simulation frame counter</param>
        /// <param name="Acceleration">Comfortable acceleration factor</param>
        /// <param name="DeltaT">Simulation frame time</param>
        public void PlanTrajectory(float Speed, float Target, long FrameCount, float Acceleration, float DeltaT)
        {
            SpeedInitial = Speed;
            SpeedTarget = Target;
            StartFrame = FrameCount;
            T = 0;
            TimeInc = DeltaT;
            
            // Compute the time taken to equalise the speeds with the specified comfortable acceleration
            Duration = Math.Abs(Target - Speed) / Acceleration;
            Interpolating = true;
        }

        /// <summary>
        ///     Routine that uses the current time interval to find the corresponding target speed within
        ///     the time frame and return the value
        /// </summary>
        /// <param name="FrameCount">Current simulation frame counter</param>
        /// <returns>New Speed</returns>
        public float UpdateTrajectory(long FrameCount)
        {
            // Get the time value expressing how far into the maneuver the car is
            T = TimeInc * (FrameCount - StartFrame) / Duration;
            
            // Act on where we are in the maneuver
           
            // If the T is smaller than 0 (time before the start of the maneuver), use the starting speed, and set
            // the interpolating flag to false
            if (T < 0)
            {
                Interpolating = false;
                return SpeedInitial;
            }
            
            // Maneuver has completed, so set the final speed, and turn interpolation off
            else if (T >= 1)
            {
                Interpolating = false;
                return SpeedTarget;
            }

            // Update the instantaneous target speed computed from the 1st order (linear) Bezier trajectory
            else
            {
                return (1 - T) * SpeedInitial + T * SpeedTarget;
            }
        }
    }
}