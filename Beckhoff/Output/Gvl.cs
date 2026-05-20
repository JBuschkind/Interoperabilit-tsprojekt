#nullable enable

using System;

namespace OPT.Framework.A200001_Ilca_SensorCalibration.Hardware
{
    // Stub placeholders for derived types referenced but not defined in this XML.
    // Fill in members manually or replace with project library imports.
    public class ComBuffer
    {
    }

    public class ComError_t
    {
    }

    public class ST_LibVersion
    {
    }

    public class stControl
    {
        /// <summary>
        /// set value
        /// </summary>
        public double TargetValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class stMeasure
    {
        /// <summary>
        /// read value
        /// </summary>
        public double ActualValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class stRingBuffer
    {
        public byte[] Buffer { get; set; } = new byte[4096];
        public ushort RdIdx { get; set; }
        public ushort WrIdx { get; set; }
        public ushort BufferSize { get; set; } = 4096;
    }

    public class stSerialConfiguration
    {
        public uint BaudRate { get; set; } = 57600;
        public short Parity { get; set; } = 0;
        public short DataBits { get; set; } = 8;
        public short StopBits { get; set; } = 1;
        public bool DTR { get; set; } = false;
        public bool RTS { get; set; } = false;
    }

    public class stSerialConnection
    {
        public stRingBuffer TxBuffer { get; set; } = new stRingBuffer();
        public stRingBuffer RxBuffer { get; set; } = new stRingBuffer();
        public bool Reset { get; set; } = true;
        public bool ResetAck { get; set; }
        public stSerialConfiguration Configuration { get; set; } = new stSerialConfiguration();
        public ComError_t TxError { get; set; } = new ComError_t();
        public ComError_t RxError { get; set; } = new ComError_t();
    }

    public class stSerialInternal
    {
        public ComBuffer RxBuffer { get; set; } = new ComBuffer();
        public ComBuffer TxBuffer { get; set; } = new ComBuffer();
        public bool CtrlError { get; set; }
        public ComError_t CtrlErrorID { get; set; } = new ComError_t();
    }

    public class stPressureSource
    {
        /// <summary>
        /// PLC: read value
        /// </summary>
        public double ActualValue { get; set; }
        /// <summary>
        /// PLC: Unit of ActualValues (Framework >= 4.0.0.0 allowed: mbar, bar)
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>
        /// PLC
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class stTemperatureSource
    {
        /// <summary>
        /// PLC: read value
        /// </summary>
        public double ActualValue { get; set; }
        /// <summary>
        /// PLC: Unit of ActualValues
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>
        /// PLC
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class stMeasureWithZero : stMeasure
    {
        public bool Zero { get; set; }
    }

    public class GVL_PLC
    {
        /// <summary>
        /// 0x0001 = Link error
        /// 0x0002 = I/O locked after link error (I/O reset required)
        /// 0x0004 = Link error (redundancy adapter)
        /// 0x0008 = Missing one frame (redundancy mode)
        /// 0x0010 = Out of send resources (I/O reset required)
        /// 0x0020 = Watchdog triggered
        /// 0x0040 = Ethernet driver (miniport) not found
        /// 0x0080 = I/O reset active
        /// 0x0100 = At least one device in 'INIT' state
        /// 0x0200 = At least one device in 'PRE-OP' state
        /// 0x0400 = At least one device in 'SAFE-OP' state
        /// 0x0800 = At least one device indicates an error state
        /// 0x1000 = DC not in sync
        /// </summary>
        public ushort SystemStatus { get; set; }
        public bool AllPartitiantsPresent { get; set; }
        /// <summary>
        /// CANOpenState. True if ok
        /// </summary>
        public bool CANOpenState { get; set; }
        /// <summary>
        /// IOLinkState. True if ok
        /// </summary>
        public bool IOLinkState { get; set; }
        /// <summary>
        /// Compiler timestamp
        /// </summary>
        public System.DateTime AppTimestamp { get; set; }
        /// <summary>
        /// Current plc version
        /// </summary>
        public string AppVersion { get; set; } = string.Empty;
        /// <summary>
        /// set Number of Expected Ethercat Partitians
        /// </summary>
        public const ushort iExpectedNumberOfPartitiants = 14;
    }

    public class GVL_Power
    {
        public double SupplyVoltage24V_1 { get; set; }
        public double SupplyVoltage24V_2 { get; set; }
        public double SupplyVoltage24V_3 { get; set; }
        public double SupplyVoltage24V_4 { get; set; }
        public bool SwitchSupplyPower_UUT { get; set; }
    }

    public class GVL_Pressure
    {
        public stMeasure SystemPressureAtValveCluster { get; set; } = new stMeasure();
        public stMeasure SystemPressurePressureRegulator { get; set; } = new stMeasure();
        public stMeasureWithZero[] RelativePressure { get; set; } = new stMeasureWithZero[1];
        public stControl[] PressureRegulator { get; set; } = new stControl[1];
        public const short NumberOfWika = 1;
        public const short NumberOfRegulator = 1;
    }

    public class GVL_Resistance
    {
        public double FunctionalEarthUUT { get; set; }
        public bool FunctionalEarthUUTOverrange { get; set; }
        public bool FunctionalEarthUUTUnderrange { get; set; }
    }

    public class GVL_Serial
    {
        public stSerialInternal[] SerialControl { get; set; } = new stSerialInternal[1];
        public stSerialConnection[] SerialConnections { get; set; } = new stSerialConnection[1];
        public const short iNumberOfSerial = 1;
        /// <summary>
        /// define side of EL6002 Clamp (0=first, 1=second), this information is needed for configuration of the serial
        /// </summary>
        public static readonly short[] aSerialConnectionSide = new short[1];
    }

    public class GVL_Status
    {
        public bool Green { get; set; }
        public bool Yellow { get; set; }
        public bool Red { get; set; }
        public bool Blue { get; set; }
        public bool White { get; set; }
        public bool IsDutAdapted { get; set; }
        public bool SwitchDockSignal_UUT { get; set; }
    }

    public class GVL_Temperature
    {
        public stTemperatureSource[] TemperatureSensor { get; set; } = new stTemperatureSource[1];
        /// <summary>
        /// Maximal Count for Sensors in the System
        /// </summary>
        public const short maxTemperatureSensorCount = 1;
    }

    public class GVL_Valves
    {
        /// <summary>
        /// Define the valve states as an array of BOOL
        /// </summary>
        public bool[] P1 { get; set; } = new bool[16];
        public bool V12 { get; set; }
        /// <summary>
        /// Cycle V12 states with times below, starting with low state
        /// </summary>
        public bool V12_cyclic { get; set; } = false;
        /// <summary>
        /// Time V12 is switched on in ms
        /// </summary>
        public short V12_onTime { get; set; } = 5000;
        /// <summary>
        /// Time V12 is switched off in ms
        /// </summary>
        public short V12_offTime { get; set; } = 5000;
        /// <summary>
        /// Minimum and maximum valve index
        /// </summary>
        public const short minValveCount = 1;
        public const short maxValveCount = 16;
    }

    public class Global_Version
    {
        public static readonly ST_LibVersion stLibVersion_A220053_Vapor_5000_Function_Test = new ST_LibVersion();
    }

}
