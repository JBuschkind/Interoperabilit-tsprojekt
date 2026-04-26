using Opc.Ua;
using OPT.Framework.A170011_Flowsensoren_Fuegeanlage.Enumerations;
using OPT.Framework.A170011_Flowsensoren_Fuegeanlage.Hardware.ModuleBase;
using System;
// ReSharper disable StringLiteralTypo

namespace OPT.Framework.A170011_Flowsensoren_Fuegeanlage.Hardware.Module2100
{
    /// <inheritdoc />
    public class Module2100SpsProxy : Module2100Sps
    {
        private readonly IOpcValueReader _opcValueReader;
        private readonly IOpcValueWriter _opcValueWriter;
        private readonly Module2100Sps _model;
        private DateTime _lastRead;
        private readonly TimeSpan _updateInterval;

        private readonly NodeId _lockDoorsNodeId = NodeIdFactory.Create("Schnittstelle SPS - PC","PC -> SPS","ST1-6verr", 3);
        private readonly NodeId _unLockDoorsNodeId = NodeIdFactory.Create("Schnittstelle SPS - PC", "PC -> SPS", "ST1-6entr",3);

        //=A1+01-70K12:+A1 Power
        //=A1+01-70K13:+A1 Beleuchtung

        private readonly NodeId _mainPowerNodeId =
            NodeIdFactory.Create( "=A1+01-70K12:+A1", 3);

        private readonly NodeId _lightNodeId =
            NodeIdFactory.Create( "=A1+01-70K13:+A1", 3);

        private readonly NodeId _manualModeSwitchNodeId =
            NodeIdFactory.Create("=A2+02-42S5:12", 3);

        private readonly NodeId _lightPower1NodeId =
            NodeIdFactory.Create("M2100 Dimmer Beleuchtung 1", 3);

        private readonly NodeId _lightPower2NodeId =
            NodeIdFactory.Create("M2100 Dimmer Beleuchtung 2", 3);

        private readonly NodeId _safetyDoorModule0200LockedNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 2 =A3+03-30S1 verr",3);
        private readonly NodeId _safetyDoorModule0600LockedNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 3 =A2+02-30S2 verr", 3);
        private readonly NodeId _safetyDoorModule0800LockedNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 6 =A1+01-30S2 verr", 3);
        private readonly NodeId _safetyDoorModule1000LockedNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 1 =A3+03-30S2 verr", 3);
        private readonly NodeId _safetyDoorLeftSideNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 5 =A1+01-30S3 verr", 3);
        private readonly NodeId _safetyDoorRightSideNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "ST 4 =A2+02-30S1 verr", 3);

        private readonly NodeId _emergencyStopInternalNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 1 =A1+01-30S4 quitt", 3);
        private readonly NodeId _emergencyCornerExternalNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 2 =A1+01-30S5 quitt", 3);
        private readonly NodeId _emergencyPanelHousingNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 3 =A2+02-30S2 quitt", 3);
        private readonly NodeId _emergencyStopInputModuleNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 4 =A3+03-30S4 quitt", 3);

        private readonly NodeId _spsStateNodeId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "SPS an", 3);
        private readonly NodeId _contactorStateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Einschaltschütze an", 3);
        private readonly NodeId _nh1StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "N-H 1 =A1+01-30S4 quitt", 3);
        private readonly NodeId _busFestoContrM400StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Contr M400", 3);
        private readonly NodeId _busFestoContrM1000StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Contr M1000", 3);
        private readonly NodeId _busLinmotContrM7000StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Linmot Contr M700", 3);

        private readonly NodeId _busFestoValve1StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Insel1 =A3+03", 3);
        private readonly NodeId _busFestoValve2StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Festo Insel2 =A1+01", 3);
        private readonly NodeId _busSwitchA1011StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A1_01_1", 3);
        private readonly NodeId _busSwitchA1012StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A1_01_2", 3);
        private readonly NodeId _busSiemensIoa202StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A2_02", 3);
        private readonly NodeId _busSwitchA2021StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A2_02_1", 3);
        private readonly NodeId _busSwitchA2022StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A2_02_2", 3);
        private readonly NodeId _busSiemensIoa303StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A3_03", 3);
        private readonly NodeId _busSwitchA3031StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_1", 3);
        private readonly NodeId _busSwitchA3032StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_2", 3);
        private readonly NodeId _busSwitchA3033StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A3_03_3", 3);
        private readonly NodeId _busSiemensIoa505StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A5_05", 3);
        private readonly NodeId _busSwitchA5051StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A5_05_1", 3);
        private readonly NodeId _busSiemensIoa606StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Siemens IO A6_06", 3);
        private readonly NodeId _busSwitchA8081StateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Switch A8_08_1", 3);
        private readonly NodeId _busAmadaStateId =
            NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "Bus nio Amada", 3);

        private readonly NodeId _userLevelNodeId = NodeIdFactory.Create("Schnittstelle SPS - PC", "SPS -> PC", "UserLevel", 3);



        /// <summary>
        /// C´tor
        /// </summary>
        /// <param name="opcValueReader"></param>
        /// <param name="opcValueWriter"></param>
        public Module2100SpsProxy(IOpcValueReader opcValueReader, IOpcValueWriter opcValueWriter)
        {
            _opcValueReader = opcValueReader;
            _opcValueWriter = opcValueWriter;
            _opcValueReader = opcValueReader;
            _opcValueWriter = opcValueWriter;
            _model = new Module2100Sps();
            _lastRead = DateTime.MinValue;
            _updateInterval = TimeSpan.FromMilliseconds(500);
        }

        /// <inheritdoc />
        public override bool AutomaticMode
        {
            get
            {
                ReadValues();
                return _model.AutomaticMode;
            }
        }

        /// <inheritdoc />
        public override bool ManualMode
        {
            get
            {
                ReadValues();
                return _model.ManualMode;
            }
        }

        /// <inheritdoc />
        public override EConnectionState ConnectionState => _opcValueReader.ConnectionState;

        /// <inheritdoc />
        public override bool LockDoors
        {
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                _opcValueWriter.Write(_unLockDoorsNodeId, false);
                //edge is required
                _opcValueWriter.Write(_lockDoorsNodeId, false);
                _opcValueWriter.Write(_lockDoorsNodeId, true);
            }
        }

        /// <inheritdoc />
        public override bool UnLockDoors
        {
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                _opcValueWriter.Write(_lockDoorsNodeId, false);
                _opcValueWriter.Write(_unLockDoorsNodeId, true);
            }

        }

        /// <inheritdoc />
          public override bool Light
        {
            set => _opcValueWriter.Write(_lightNodeId, value);
        }

        /// <inheritdoc />
        public override int LightPower
        {
            set
            {
                ushort pwmValue = Convert.ToUInt16(value * 270);
                _opcValueWriter.Write(_lightPower1NodeId, pwmValue);
                _opcValueWriter.Write(_lightPower2NodeId, pwmValue);
            }
        }

        /// <inheritdoc />
        public override bool MainPower
        {
            set => _opcValueWriter.Write(_mainPowerNodeId, value);
        }

        /// <inheritdoc />
        public override bool SafetyDoorModule0200Locked
        {
            get
            {
                _model.SafetyDoorModule0200Locked = _opcValueReader.ReadValue<bool>(_safetyDoorModule0200LockedNodeId);
                return _model.SafetyDoorModule0200Locked;
            }
        }

        /// <inheritdoc />
        public override bool SafetyDoorModule0600Locked
        {
            get
            {
                _model.SafetyDoorModule0600Locked = _opcValueReader.ReadValue<bool>(_safetyDoorModule0600LockedNodeId);
                return _model.SafetyDoorModule0600Locked;
            }
        }

        /// <inheritdoc />
        public override bool SafetyDoorModule0800Locked
        {
            get
            {
                _model.SafetyDoorModule0800Locked = _opcValueReader.ReadValue<bool>(_safetyDoorModule0800LockedNodeId);
                return _model.SafetyDoorModule0800Locked;
            }
        }
        /// <inheritdoc />
        public override bool SafetyDoorModule1000Locked
        {
            get
            {
                _model.SafetyDoorModule1000Locked = _opcValueReader.ReadValue<bool>(_safetyDoorModule1000LockedNodeId);
                return _model.SafetyDoorModule1000Locked;
            }
        }
        /// <inheritdoc />
        public override bool SafetyDoorLeftSide
        {
            get
            {
                _model.SafetyDoorLeftSide = _opcValueReader.ReadValue<bool>(_safetyDoorLeftSideNodeId);
                return _model.SafetyDoorLeftSide;
            }
        }
        /// <inheritdoc />
        public override bool SafetyDoorRightSide
        {
            get
            {
                _model.SafetyDoorRightSide = _opcValueReader.ReadValue<bool>(_safetyDoorRightSideNodeId);
                return _model.SafetyDoorRightSide;
            }
        }

        /// <inheritdoc />
        public override bool EmergencyStopInternal
        {
            get
            {
                _model.EmergencyStopInternal = _opcValueReader.ReadValue<bool>(_emergencyStopInternalNodeId);
                return _model.EmergencyStopInternal;
            }
        }

        /// <inheritdoc />
        public override bool EmergencyCornerExternal
        {
            get
            {
                _model.EmergencyCornerExternal = _opcValueReader.ReadValue<bool>(_emergencyCornerExternalNodeId);
                return _model.EmergencyCornerExternal;
            }
        }

        /// <inheritdoc />
        public override bool EmergencyPanelHousing
        {
            get
            {
                _model.EmergencyPanelHousing = _opcValueReader.ReadValue<bool>(_emergencyPanelHousingNodeId);
                return _model.EmergencyPanelHousing;
            }
        }

        /// <inheritdoc />
        public override bool EmergencyStopInputModule
        {
            get
            {
                _model.EmergencyStopInputModule = _opcValueReader.ReadValue<bool>(_emergencyStopInputModuleNodeId);
                return _model.EmergencyStopInputModule;
            }
        }

        /// <inheritdoc/>
        public override EUserLevel UserLevel
        {
            get
            {
                _model.UserLevel = (EUserLevel) _opcValueReader.ReadValue<int>(_userLevelNodeId);
                return _model.UserLevel;
            }
        }

        /// <inheritdoc />
        public override SpsErrorStates SpsErrorStates
        {
            get
            {
                var model = new SpsErrorStates
                {
                    SpsState = _opcValueReader.ReadValue<bool>(_spsStateNodeId),
                    ContactorState = _opcValueReader.ReadValue<bool>(_contactorStateId),
                    Nh1State = _opcValueReader.ReadValue<bool>(_nh1StateId),
                    BusFestoM400State = _opcValueReader.ReadValue<bool>(_busFestoContrM400StateId),
                    BusFestoM700State = _opcValueReader.ReadValue<bool>(_busFestoContrM1000StateId),
                    BusLinmotM700State = _opcValueReader.ReadValue<bool>(_busLinmotContrM7000StateId),
                    BusFestoValve1State = _opcValueReader.ReadValue<bool>(_busFestoValve1StateId),
                    BusFestoValve2State = _opcValueReader.ReadValue<bool>(_busFestoValve2StateId),
                    BusSwitchA1011State = _opcValueReader.ReadValue<bool>(_busSwitchA1011StateId),
                    BusSwitchA1012State = _opcValueReader.ReadValue<bool>(_busSwitchA1012StateId),
                    BusSwitchA2021State = _opcValueReader.ReadValue<bool>(_busSwitchA2021StateId),
                    BusSwitchA2022State = _opcValueReader.ReadValue<bool>(_busSwitchA2022StateId),
                    BusSwitchA3031State = _opcValueReader.ReadValue<bool>(_busSwitchA3031StateId),
                    BusSwitchA3032State = _opcValueReader.ReadValue<bool>(_busSwitchA3032StateId),
                    BusSwitchA3033State = _opcValueReader.ReadValue<bool>(_busSwitchA3033StateId),
                    BusSwitchA5051State = _opcValueReader.ReadValue<bool>(_busSwitchA5051StateId),
                    BusSwitchA8081State = _opcValueReader.ReadValue<bool>(_busSwitchA8081StateId),
                    BusSiemensIoa202State = _opcValueReader.ReadValue<bool>(_busSiemensIoa202StateId),
                    BusSiemensIoa303State = _opcValueReader.ReadValue<bool>(_busSiemensIoa303StateId),
                    BusSiemensIoa505State = _opcValueReader.ReadValue<bool>(_busSiemensIoa505StateId),
                    BusSiemensIoa606State = _opcValueReader.ReadValue<bool>(_busSiemensIoa606StateId),
                    BusAmadaState = _opcValueReader.ReadValue<bool>(_busAmadaStateId)
                };

                return model;
            }

        }

        /// <inheritdoc />
        public override int SpsParticipantCount
        {
            get
            {
                int count = 0;

                if (TestIfValueIsReadable<bool>(_safetyDoorModule0200LockedNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_safetyDoorModule0600LockedNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_safetyDoorModule0800LockedNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_safetyDoorModule1000LockedNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_safetyDoorLeftSideNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_safetyDoorRightSideNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_emergencyStopInternalNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_emergencyCornerExternalNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_emergencyPanelHousingNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<bool>(_emergencyStopInputModuleNodeId))
                {
                    count++;
                }

                if (TestIfValueIsReadable<int>(_userLevelNodeId))
                {
                    count++;
                }

                return count;
            }
        }

        private bool TestIfValueIsReadable<T>(NodeId nodeId)
        {
            try
            {
                _opcValueReader.ReadValue<T>(nodeId);
            }
            catch (Exception)
            {

                return false;
            }

            return true;
        }

        private void ReadValues()
        {
            if (!IsUpdateRequired())
            {
                return;
            }

            _model.ManualMode = _opcValueReader.ReadValue<bool>(_manualModeSwitchNodeId);
            _model.AutomaticMode = !_model.ManualMode;


            _lastRead = DateTime.Now;
        }

        private bool IsUpdateRequired()
        {
            return DateTime.Now - _lastRead > _updateInterval;
        }
    }
}
