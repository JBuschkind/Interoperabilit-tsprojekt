#nullable enable

using System;
using OPT.Framework.A200001_Ilca_SensorCalibration.Enumerations;
using OPT.Framework.API.HardwareControls;

namespace OPT.Framework.A200001_Ilca_SensorCalibration.Hardware
{
    /// <inheritdoc />
    public class PlcStatusControl : IPlcStatusControl
    {
        private readonly IPlcControl _plcControl;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public PlcStatusControl(IHardwareControlPool hardwareControl)
        {
            _plcControl = hardwareControl.PlcControl;
        }

        /// <inheritdoc />
        public EPlcState GetPlcSystemState()
        {
            var result = _plcControl.ReadValueFromPlcNode<ushort>("GVL_PLC.SystemStatus");
            return result.IsSuccess ? (EPlcState)result.Value : default;
        }

        /// <inheritdoc />
        public bool GetAllPlcNodesPresentState()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.AllPartitiantsPresent");
            return (result.IsSuccess && result.Value);
        }

        /// <inheritdoc />
        public bool GetCanOpenState()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.CANOpenState");
            return (result.IsSuccess && result.Value);
        }

        /// <inheritdoc />
        public TimeSpan GetCompileTime()
        {
            var result = _plcControl.ReadValueFromPlcNode<DateTime>("GVL_PLC.AppTimestamp");
            return result.IsSuccess ? DateTime.Now - result.Value : TimeSpan.Zero;
        }

        /// <inheritdoc />
        public string GetAppVersion()
        {
            var result = _plcControl.ReadValueFromPlcNode<string>("GVL_PLC.AppVersion");
            return result.Value;
        }
    }
}
