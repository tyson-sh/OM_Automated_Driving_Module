namespace OM_Automated_Driving
{
    public class Controller
    {
        // New value of the derivative gain
        public float KProportional;
        
        // New value of the integral gain
        public float KIntegral;
        
        // New value of the proportional gain
        public float KDerivative;

        private float ErrorOld;
        private float ErrorSum;

        /// <summary>
        ///     PID control algorithm for controlling signals
        /// </summary>
        /// <param name="SetPoint">Value we are trying to reach</param>
        /// <param name="ProcessVar">Current Value</param>
        /// <param name="TimeStep">Elapsed time since last update</param>
        /// <returns>Computed PID controller value</returns>
        public float Control(float SetPoint, float ProcessVar, float TimeStep)
        {
            // Dimension all variables local to this routine
            float Derivative;
            float Integral;
            float SigError;
            
            // Calculate the error between the target and the driver
            SigError = SetPoint - ProcessVar;
            
            // Compute the integral and derivative terms
            ErrorSum = ErrorSum + SigError;
            Integral = ErrorSum * TimeStep;
            Derivative = (SigError - ErrorOld) / TimeStep;
            ErrorOld = SigError;
            
            // Return the new control value
            return KProportional * SigError + KIntegral * Integral + KDerivative * Derivative;
        }
        
        /// <summary>
        ///     Method that resets the previous error value
        /// </summary>
        public void ResetDV()
        {
            ErrorOld = 0;
        }
    }
}