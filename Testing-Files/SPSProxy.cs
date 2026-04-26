using Opc.Ua;
using System;
using lululu;
using lalala;
using lelele;

// ReSharper disable StringLiteralTypo

namespace OPT.Framework.MyProject.Hardware
{
    /// <inheritdoc />
    public class SpsProxy : Sps
    {
        private readonly IOpcValueReader _opcValueReader;
        private readonly IOpcValueWriter _opcValueWriter;
        private readonly Sps _model;
        private DateTime _lastRead;
        private readonly TimeSpan _updateInterval;

        private readonly NodeId _sPSAnNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "SPS an", 5);

        private readonly NodeId _einschaltschuetzeAnNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Einschaltschütze an", 5);

        private readonly NodeId _nH1A10130S4QuittNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 1 =A1+01-30S4 quitt", 5);

        private readonly NodeId _nH2A10130S5QuittNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 2 =A1+01-30S5 quitt", 5);

        private readonly NodeId _nH3A20230S2QuittNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 3 =A2+02-30S2 quitt", 5);

        private readonly NodeId _nH4A30330S4QuittNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 4 =A3+03-30S4 quitt", 5);

        private readonly NodeId _nH14QuittNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 1-4 quitt", 5);

        private readonly NodeId _sT1A30330S2VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 1 =A3+03-30S2 verr", 5);

        private readonly NodeId _sT2A30330S1VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 2 =A3+03-30S1 verr", 5);

        private readonly NodeId _sT3A20230S2VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 3 =A2+02-30S2 verr", 5);

        private readonly NodeId _sT4A20230S1VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 4 =A2+02-30S1 verr", 5);

        private readonly NodeId _sT5A10130S3VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 5 =A1+01-30S3 verr", 5);

        private readonly NodeId _sT6A10130S2VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 6 =A1+01-30S2 verr", 5);

        private readonly NodeId _sT16VerrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 1-6 verr", 5);

        private readonly NodeId _sT7A30331S5GeschlNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 7 =A3+03-31S5 geschl", 5);

        private readonly NodeId _busNioFestoContrM400NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Contr M400", 5);

        private readonly NodeId _busNioFestoContrM1000NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Contr M1000", 5);

        private readonly NodeId _busNioLinmotContrM700NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Linmot Contr M700", 5);

        private readonly NodeId _busNioFestoInsel1A303NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Insel1 =A3+03", 5);

        private readonly NodeId _busNioFestoInsel2A101NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Insel2 =A1+01", 5);

        private readonly NodeId _busNioSwitchA1011NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A1_01_1", 5);

        private readonly NodeId _busNioSwitchA1012NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A1_01_2", 5);

        private readonly NodeId _busNioSiemensIOA202NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A2_02", 5);

        private readonly NodeId _busNioSwitchA2021NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A2_02_1", 5);

        private readonly NodeId _busNioSwitchA2022NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A2_02_2", 5);

        private readonly NodeId _busNioSiemensIOA303NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A3_03", 5);

        private readonly NodeId _busNioSwitchA3031NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_1", 5);

        private readonly NodeId _busNioSwitchA3032NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_2", 5);

        private readonly NodeId _busNioSwitchA3033NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_3", 5);

        private readonly NodeId _busNioSiemensIOA505NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A5_05", 5);

        private readonly NodeId _busNioSwitchA5051NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A5_05_1", 5);

        private readonly NodeId _busNioSiemensIOA606NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A6_06", 5);

        private readonly NodeId _busNioSwitchA8081NodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A8_08_1", 5);

        private readonly NodeId _busNioAmadaNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Amada", 5);

        private readonly NodeId _userLevel1ActiveNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel1Active", 5);

        private readonly NodeId _userLevel2ActiveNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel2Active", 5);

        private readonly NodeId _userLevel3ActiveNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel3Active", 5);

        private readonly NodeId _userLevel4ActiveNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel4Active", 5);

        private readonly NodeId _userLevelNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel", 5);

        private readonly NodeId _sT16verrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "PC -> SPS", "ST1-6verr", 5);

        private readonly NodeId _sT16entrNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "PC -> SPS", "ST1-6entr", 5);

        /// <summary>
        /// Ctor
        /// </summary>
        public SpsProxy(IOpcValueReader opcValueReader, IOpcValueWriter opcValueWriter)
        {
            _opcValueReader = opcValueReader;
            _opcValueWriter = opcValueWriter;
            _model = new Sps();
            _lastRead = DateTime.MinValue;
            _updateInterval = TimeSpan.FromMilliseconds(666);
        }

        /// <inheritdoc />
        public override bool SPSAn
        {
            get
            {
                ReadValues();
                return _model.SPSAn;
            }
        }

        /// <inheritdoc />
        public override bool EinschaltschuetzeAn
        {
            get
            {
                ReadValues();
                return _model.EinschaltschuetzeAn;
            }
        }

        /// <inheritdoc />
        public override bool NH1A10130S4Quitt
        {
            get
            {
                ReadValues();
                return _model.NH1A10130S4Quitt;
            }
        }

        /// <inheritdoc />
        public override bool NH2A10130S5Quitt
        {
            get
            {
                ReadValues();
                return _model.NH2A10130S5Quitt;
            }
        }

        /// <inheritdoc />
        public override bool NH3A20230S2Quitt
        {
            get
            {
                ReadValues();
                return _model.NH3A20230S2Quitt;
            }
        }

        /// <inheritdoc />
        public override bool NH4A30330S4Quitt
        {
            get
            {
                ReadValues();
                return _model.NH4A30330S4Quitt;
            }
        }

        /// <inheritdoc />
        public override bool NH14Quitt
        {
            get
            {
                ReadValues();
                return _model.NH14Quitt;
            }
        }

        /// <inheritdoc />
        public override bool ST1A30330S2Verr
        {
            get
            {
                ReadValues();
                return _model.ST1A30330S2Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST2A30330S1Verr
        {
            get
            {
                ReadValues();
                return _model.ST2A30330S1Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST3A20230S2Verr
        {
            get
            {
                ReadValues();
                return _model.ST3A20230S2Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST4A20230S1Verr
        {
            get
            {
                ReadValues();
                return _model.ST4A20230S1Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST5A10130S3Verr
        {
            get
            {
                ReadValues();
                return _model.ST5A10130S3Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST6A10130S2Verr
        {
            get
            {
                ReadValues();
                return _model.ST6A10130S2Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST16Verr
        {
            get
            {
                ReadValues();
                return _model.ST16Verr;
            }
        }

        /// <inheritdoc />
        public override bool ST7A30331S5Geschl
        {
            get
            {
                ReadValues();
                return _model.ST7A30331S5Geschl;
            }
        }

        /// <inheritdoc />
        public override bool BusNioFestoContrM400
        {
            get
            {
                ReadValues();
                return _model.BusNioFestoContrM400;
            }
        }

        /// <inheritdoc />
        public override bool BusNioFestoContrM1000
        {
            get
            {
                ReadValues();
                return _model.BusNioFestoContrM1000;
            }
        }

        /// <inheritdoc />
        public override bool BusNioLinmotContrM700
        {
            get
            {
                ReadValues();
                return _model.BusNioLinmotContrM700;
            }
        }

        /// <inheritdoc />
        public override bool BusNioFestoInsel1A303
        {
            get
            {
                ReadValues();
                return _model.BusNioFestoInsel1A303;
            }
        }

        /// <inheritdoc />
        public override bool BusNioFestoInsel2A101
        {
            get
            {
                ReadValues();
                return _model.BusNioFestoInsel2A101;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA1011
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA1011;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA1012
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA1012;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSiemensIOA202
        {
            get
            {
                ReadValues();
                return _model.BusNioSiemensIOA202;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA2021
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA2021;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA2022
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA2022;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSiemensIOA303
        {
            get
            {
                ReadValues();
                return _model.BusNioSiemensIOA303;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA3031
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA3031;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA3032
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA3032;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA3033
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA3033;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSiemensIOA505
        {
            get
            {
                ReadValues();
                return _model.BusNioSiemensIOA505;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA5051
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA5051;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSiemensIOA606
        {
            get
            {
                ReadValues();
                return _model.BusNioSiemensIOA606;
            }
        }

        /// <inheritdoc />
        public override bool BusNioSwitchA8081
        {
            get
            {
                ReadValues();
                return _model.BusNioSwitchA8081;
            }
        }

        /// <inheritdoc />
        public override bool BusNioAmada
        {
            get
            {
                ReadValues();
                return _model.BusNioAmada;
            }
        }

        /// <inheritdoc />
        public override bool UserLevel1Active
        {
            get
            {
                ReadValues();
                return _model.UserLevel1Active;
            }
        }

        /// <inheritdoc />
        public override bool UserLevel2Active
        {
            get
            {
                ReadValues();
                return _model.UserLevel2Active;
            }
        }

        /// <inheritdoc />
        public override bool UserLevel3Active
        {
            get
            {
                ReadValues();
                return _model.UserLevel3Active;
            }
        }

        /// <inheritdoc />
        public override bool UserLevel4Active
        {
            get
            {
                ReadValues();
                return _model.UserLevel4Active;
            }
        }

        /// <inheritdoc />
        public override short UserLevel
        {
            get
            {
                ReadValues();
                return _model.UserLevel;
            }
        }

        /// <inheritdoc />
        public override bool ST16verr
        {
            set
            {
                _opcValueWriter.Write(_sT16verrNodeId, value);
            }
        }

        /// <inheritdoc />
        public override bool ST16entr
        {
            set
            {
                _opcValueWriter.Write(_sT16entrNodeId, value);
            }
        }

        private void ReadValues()
        {
            if (!IsUpdateRequired()) return;

            _model.SPSAn = _opcValueReader.ReadValue<bool>(_sPSAnNodeId);
            _model.EinschaltschuetzeAn = _opcValueReader.ReadValue<bool>(_einschaltschuetzeAnNodeId);
            _model.NH1A10130S4Quitt = _opcValueReader.ReadValue<bool>(_nH1A10130S4QuittNodeId);
            _model.NH2A10130S5Quitt = _opcValueReader.ReadValue<bool>(_nH2A10130S5QuittNodeId);
            _model.NH3A20230S2Quitt = _opcValueReader.ReadValue<bool>(_nH3A20230S2QuittNodeId);
            _model.NH4A30330S4Quitt = _opcValueReader.ReadValue<bool>(_nH4A30330S4QuittNodeId);
            _model.NH14Quitt = _opcValueReader.ReadValue<bool>(_nH14QuittNodeId);
            _model.ST1A30330S2Verr = _opcValueReader.ReadValue<bool>(_sT1A30330S2VerrNodeId);
            _model.ST2A30330S1Verr = _opcValueReader.ReadValue<bool>(_sT2A30330S1VerrNodeId);
            _model.ST3A20230S2Verr = _opcValueReader.ReadValue<bool>(_sT3A20230S2VerrNodeId);
            _model.ST4A20230S1Verr = _opcValueReader.ReadValue<bool>(_sT4A20230S1VerrNodeId);
            _model.ST5A10130S3Verr = _opcValueReader.ReadValue<bool>(_sT5A10130S3VerrNodeId);
            _model.ST6A10130S2Verr = _opcValueReader.ReadValue<bool>(_sT6A10130S2VerrNodeId);
            _model.ST16Verr = _opcValueReader.ReadValue<bool>(_sT16VerrNodeId);
            _model.ST7A30331S5Geschl = _opcValueReader.ReadValue<bool>(_sT7A30331S5GeschlNodeId);
            _model.BusNioFestoContrM400 = _opcValueReader.ReadValue<bool>(_busNioFestoContrM400NodeId);
            _model.BusNioFestoContrM1000 = _opcValueReader.ReadValue<bool>(_busNioFestoContrM1000NodeId);
            _model.BusNioLinmotContrM700 = _opcValueReader.ReadValue<bool>(_busNioLinmotContrM700NodeId);
            _model.BusNioFestoInsel1A303 = _opcValueReader.ReadValue<bool>(_busNioFestoInsel1A303NodeId);
            _model.BusNioFestoInsel2A101 = _opcValueReader.ReadValue<bool>(_busNioFestoInsel2A101NodeId);
            _model.BusNioSwitchA1011 = _opcValueReader.ReadValue<bool>(_busNioSwitchA1011NodeId);
            _model.BusNioSwitchA1012 = _opcValueReader.ReadValue<bool>(_busNioSwitchA1012NodeId);
            _model.BusNioSiemensIOA202 = _opcValueReader.ReadValue<bool>(_busNioSiemensIOA202NodeId);
            _model.BusNioSwitchA2021 = _opcValueReader.ReadValue<bool>(_busNioSwitchA2021NodeId);
            _model.BusNioSwitchA2022 = _opcValueReader.ReadValue<bool>(_busNioSwitchA2022NodeId);
            _model.BusNioSiemensIOA303 = _opcValueReader.ReadValue<bool>(_busNioSiemensIOA303NodeId);
            _model.BusNioSwitchA3031 = _opcValueReader.ReadValue<bool>(_busNioSwitchA3031NodeId);
            _model.BusNioSwitchA3032 = _opcValueReader.ReadValue<bool>(_busNioSwitchA3032NodeId);
            _model.BusNioSwitchA3033 = _opcValueReader.ReadValue<bool>(_busNioSwitchA3033NodeId);
            _model.BusNioSiemensIOA505 = _opcValueReader.ReadValue<bool>(_busNioSiemensIOA505NodeId);
            _model.BusNioSwitchA5051 = _opcValueReader.ReadValue<bool>(_busNioSwitchA5051NodeId);
            _model.BusNioSiemensIOA606 = _opcValueReader.ReadValue<bool>(_busNioSiemensIOA606NodeId);
            _model.BusNioSwitchA8081 = _opcValueReader.ReadValue<bool>(_busNioSwitchA8081NodeId);
            _model.BusNioAmada = _opcValueReader.ReadValue<bool>(_busNioAmadaNodeId);
            _model.UserLevel1Active = _opcValueReader.ReadValue<bool>(_userLevel1ActiveNodeId);
            _model.UserLevel2Active = _opcValueReader.ReadValue<bool>(_userLevel2ActiveNodeId);
            _model.UserLevel3Active = _opcValueReader.ReadValue<bool>(_userLevel3ActiveNodeId);
            _model.UserLevel4Active = _opcValueReader.ReadValue<bool>(_userLevel4ActiveNodeId);
            _model.UserLevel = _opcValueReader.ReadValue<short>(_userLevelNodeId);

            _lastRead = DateTime.Now;
        }

        private bool IsUpdateRequired()
        {
            return DateTime.Now - _lastRead > _updateInterval;
        }
    }
}
