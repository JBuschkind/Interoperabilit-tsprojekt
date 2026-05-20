#nullable enable

using System;
using OPT.Framework.API.HardwareControls;

namespace OPT.Framework.A200001_Ilca_SensorCalibration.Hardware
{
    public class GvlProxy
    {
        private readonly IPlcControl _plcControl;

        public GvlProxy(IHardwareControlPool hardwareControl)
        {
            _plcControl = hardwareControl.PlcControl;
        }

        public ushort GetGVL_PLC_SystemStatus()
        {
            var result = _plcControl.ReadValueFromPlcNode<ushort>("GVL_PLC.SystemStatus");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_PLC_AllPartitiantsPresent()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.AllPartitiantsPresent");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_PLC_CANOpenState()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.CANOpenState");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_PLC_IOLinkState()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_PLC.IOLinkState");
            return result.IsSuccess ? result.Value : default;
        }

        public System.DateTime GetGVL_PLC_AppTimestamp()
        {
            var result = _plcControl.ReadValueFromPlcNode<System.DateTime>("GVL_PLC.AppTimestamp");
            return result.IsSuccess ? result.Value : default;
        }

        public string GetGVL_PLC_AppVersion()
        {
            var result = _plcControl.ReadValueFromPlcNode<string>("GVL_PLC.AppVersion");
            return result.IsSuccess ? (result.Value ?? string.Empty) : string.Empty;
        }

        public double GetGVL_Power_SupplyVoltage24V_1()
        {
            var result = _plcControl.ReadValueFromPlcNode<double>("GVL_Power.SupplyVoltage24V_1");
            return result.IsSuccess ? result.Value : default;
        }

        public double GetGVL_Power_SupplyVoltage24V_2()
        {
            var result = _plcControl.ReadValueFromPlcNode<double>("GVL_Power.SupplyVoltage24V_2");
            return result.IsSuccess ? result.Value : default;
        }

        public double GetGVL_Power_SupplyVoltage24V_3()
        {
            var result = _plcControl.ReadValueFromPlcNode<double>("GVL_Power.SupplyVoltage24V_3");
            return result.IsSuccess ? result.Value : default;
        }

        public double GetGVL_Power_SupplyVoltage24V_4()
        {
            var result = _plcControl.ReadValueFromPlcNode<double>("GVL_Power.SupplyVoltage24V_4");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Power_SwitchSupplyPower_UUT()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Power.SwitchSupplyPower_UUT");
            return result.IsSuccess ? result.Value : default;
        }

        // GetGVL_Pressure_SystemPressureAtValveCluster: type 'stMeasure' is not a primitive scalar.
        //   Node: GVL_Pressure.SystemPressureAtValveCluster
        //   Implement manually (struct / array reads need framework-specific calls).

        // GetGVL_Pressure_SystemPressurePressureRegulator: type 'stMeasure' is not a primitive scalar.
        //   Node: GVL_Pressure.SystemPressurePressureRegulator
        //   Implement manually (struct / array reads need framework-specific calls).

        // GetGVL_Pressure_RelativePressure: type 'stMeasureWithZero[]' is not a primitive scalar.
        //   Node: GVL_Pressure.RelativePressure
        //   Implement manually (struct / array reads need framework-specific calls).

        // GetGVL_Pressure_PressureRegulator: type 'stControl[]' is not a primitive scalar.
        //   Node: GVL_Pressure.PressureRegulator
        //   Implement manually (struct / array reads need framework-specific calls).

        public double GetGVL_Resistance_FunctionalEarthUUT()
        {
            var result = _plcControl.ReadValueFromPlcNode<double>("GVL_Resistance.FunctionalEarthUUT");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Resistance_FunctionalEarthUUTOverrange()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Resistance.FunctionalEarthUUTOverrange");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Resistance_FunctionalEarthUUTUnderrange()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Resistance.FunctionalEarthUUTUnderrange");
            return result.IsSuccess ? result.Value : default;
        }

        // GetGVL_Serial_SerialControl: type 'stSerialInternal[]' is not a primitive scalar.
        //   Node: GVL_Serial.SerialControl
        //   Implement manually (struct / array reads need framework-specific calls).

        // GetGVL_Serial_SerialConnections: type 'stSerialConnection[]' is not a primitive scalar.
        //   Node: GVL_Serial.SerialConnections
        //   Implement manually (struct / array reads need framework-specific calls).

        public bool GetGVL_Status_Green()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.Green");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_Yellow()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.Yellow");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_Red()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.Red");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_Blue()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.Blue");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_White()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.White");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_IsDutAdapted()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.IsDutAdapted");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Status_SwitchDockSignal_UUT()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Status.SwitchDockSignal_UUT");
            return result.IsSuccess ? result.Value : default;
        }

        // GetGVL_Temperature_TemperatureSensor: type 'stTemperatureSource[]' is not a primitive scalar.
        //   Node: GVL_Temperature.TemperatureSensor
        //   Implement manually (struct / array reads need framework-specific calls).

        // GetGVL_Valves_P1: type 'bool[]' is not a primitive scalar.
        //   Node: GVL_Valves.P1
        //   Implement manually (struct / array reads need framework-specific calls).

        public bool GetGVL_Valves_V12()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Valves.V12");
            return result.IsSuccess ? result.Value : default;
        }

        public bool GetGVL_Valves_V12_cyclic()
        {
            var result = _plcControl.ReadValueFromPlcNode<bool>("GVL_Valves.V12_cyclic");
            return result.IsSuccess ? result.Value : default;
        }

        public short GetGVL_Valves_V12_onTime()
        {
            var result = _plcControl.ReadValueFromPlcNode<short>("GVL_Valves.V12_onTime");
            return result.IsSuccess ? result.Value : default;
        }

        public short GetGVL_Valves_V12_offTime()
        {
            var result = _plcControl.ReadValueFromPlcNode<short>("GVL_Valves.V12_offTime");
            return result.IsSuccess ? result.Value : default;
        }
    }
}
