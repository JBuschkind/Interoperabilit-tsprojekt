using OPT.Framework.A200001_Ilca_SensorCalibration.Enumerations;
using System;
using OPT.Framework.API.HardwareControls;

namespace OPT.Framework.A200001_Ilca_SensorCalibration.Hardware
{
    /// <inheritdoc />
    public class  PlcStatusControl: IPlcStatusControl
    {
        private readonly IPlcControl _plcControl;

        /// <summary>
        /// ctor
        /// </summary>
        public PlcStatusControl(IHardwareControlPool hardwareControl)
        {
            _plcControl = hardwareControl.PlcControl;
        }

        /// <inheritdoc />
        public EPlcState GetPlcSystemState()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool GetAllPlcNodesPresentState()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.AllPartitiantsPresent");
            return (result.IsSuccess && result.Value);
        }

        /// <inheritdoc />
        public bool GetCanOpenState()
    ,  {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public TimeSpan GetCompileTime()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetAppVersion()
        {
            var result = _plcControl.ReadValueFromPlcNode<string>("GVL_PLC.AppVersion");
            return result.Value;
        }
    }
}