using OPT.Framework.A170011_Flowsensoren_Fuegeanlage.Enumerations;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Alternative: Implementation of an interface. LCE project, therefore it is not implemented


namespace OPT.Framework.A170011_Flowsensoren_Fuegeanlage.Hardware.Module2100
{
    /// <summary>
    /// Module 2100 variables definition
    /// </summary>
    public class Module2100Sps
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public Module2100Sps()
        {
            //TODO test only
            // ReSharper disable once VirtualMemberCallInConstructor
            ManualMode = true;
        }

        /// <summary>
        /// SPS Automatic mode
        /// State of Panel switch
        /// </summary>
        public virtual bool AutomaticMode { get; set; }

        /// <summary>
        /// SPS Manual mode
        /// State of Panel switch
        /// </summary>
        public virtual bool ManualMode { get; set; }

        /// <summary>
        /// OPC Connection state
        /// </summary>
        public virtual EConnectionState ConnectionState { get; set; }

        /// <summary>
        /// Module 0200: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorModule0200Locked { get; set; }

        /// <summary>
        /// Module 0600: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorModule0600Locked { get; set; }

        /// <summary>
        /// Module 0800: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorModule0800Locked { get; set; }

        /// <summary>
        /// Module 1000: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorModule1000Locked { get; set; }

        /// <summary>
        /// Left side: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorLeftSide { get; set; }

        /// <summary>
        /// Right side: Safety door state: Locked => true
        /// </summary>
        public virtual bool SafetyDoorRightSide { get; set; }

        /// <summary>
        /// Emergency stop button pressed: Internal
        /// </summary>
        public virtual bool EmergencyStopInternal { get; set; }

        /// <summary>
        /// Emergency stop button pressed: Internal
        /// </summary>
        public virtual bool EmergencyCornerExternal { get; set; }

        /// <summary>
        /// Emergency stop button pressed: Housing
        /// </summary>
        public virtual bool EmergencyPanelHousing { get; set; }

        /// <summary>
        /// Emergency stop button pressed: Input module
        /// </summary>
        public virtual bool EmergencyStopInputModule { get; set; }

        /// <summary>
        /// Lock request for safety doors
        /// </summary>
        public virtual bool LockDoors { get; set; }

        /// <summary>
        /// Unlock request for safety doors
        /// </summary>
        public virtual bool UnLockDoors { get; set; }

        /// <summary>
        /// Upper light (4x LED)
        /// </summary>
        public virtual bool Light { get; set; }

        /// <summary>
        /// 0..100 % pwm value
        /// </summary>
        public virtual int LightPower { get; set; }

        /// <summary>
        /// Power switch
        /// </summary>
        public virtual bool MainPower { get; set; }

        /// <summary>
        /// Enum of user levels that can be set with the PitReader
        /// </summary>
        public virtual EUserLevel UserLevel { get; set; }

        /// <summary>
        /// All sps states
        /// </summary>
        public virtual SpsErrorStates SpsErrorStates { get; set; }

        /// <summary>
        /// Count of connected sps participants
        /// </summary>
        public virtual int SpsParticipantCount {get; set;}
    }
}
