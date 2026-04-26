// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace OPT.Framework.MyProject.Hardware
{
    /// <summary>
    /// Variables definition
    /// </summary>
    public class Sps
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public Sps()
        {
        }

        /// <summary>
        /// SPS ist eingeschaltet und hochgefahren
        /// </summary>
        public virtual bool SPSAn { get; set; }

        /// <summary>
        /// SPS ist hochgefahren und Einschaltschütze sind an
        /// </summary>
        public virtual bool EinschaltschuetzeAn { get; set; }

        /// <summary>
        /// Not-Halt  Anlage intern =A1+01-30S4 quittiert
        /// </summary>
        public virtual bool NH1A10130S4Quitt { get; set; }

        /// <summary>
        /// Not-Halt  Ecke außerhalb =A1+01-30S5 quittiert
        /// </summary>
        public virtual bool NH2A10130S5Quitt { get; set; }

        /// <summary>
        /// Not-Halt Panelgehäuse =A2+02-30S2 quittiert
        /// </summary>
        public virtual bool NH3A20230S2Quitt { get; set; }

        /// <summary>
        /// Not-Halt Eingabemodul =A3+03-30S4 quittiert
        /// </summary>
        public virtual bool NH4A30330S4Quitt { get; set; }

        /// <summary>
        /// Not-Halt 1-4 quittiert
        /// </summary>
        public virtual bool NH14Quitt { get; set; }

        /// <summary>
        /// Schutztür  Modul 1000 =A3+03-30S2 verriegelt
        /// </summary>
        public virtual bool ST1A30330S2Verr { get; set; }

        /// <summary>
        /// Schutztür Modul 200 =A3+03-30S1 verriegelt
        /// </summary>
        public virtual bool ST2A30330S1Verr { get; set; }

        /// <summary>
        /// Schutztür Modul 600 =A2+02-30S2 verriegelt
        /// </summary>
        public virtual bool ST3A20230S2Verr { get; set; }

        /// <summary>
        /// Schutztür hinten, rechts =A2+02-30S1 verriegelt
        /// </summary>
        public virtual bool ST4A20230S1Verr { get; set; }

        /// <summary>
        /// Schutztür hinten, links =A1+01-30S3 verriegelt
        /// </summary>
        public virtual bool ST5A10130S3Verr { get; set; }

        /// <summary>
        /// Schutztür Modul 800 =A1+01-30S2 verriegelt
        /// </summary>
        public virtual bool ST6A10130S2Verr { get; set; }

        /// <summary>
        /// Schutztüren 1-6 verriegelt
        /// </summary>
        public virtual bool ST16Verr { get; set; }

        /// <summary>
        /// Schutztür Modul 100 Eingabe =A3+03-31S5 geschlossen
        /// </summary>
        public virtual bool ST7A30331S5Geschl { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.30 nicht erreichbar
        /// </summary>
        public virtual bool BusNioFestoContrM400 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.32 nicht erreichbar
        /// </summary>
        public virtual bool BusNioFestoContrM1000 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.34 nicht erreichbar
        /// </summary>
        public virtual bool BusNioLinmotContrM700 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.68 nicht erreichbar
        /// </summary>
        public virtual bool BusNioFestoInsel1A303 { get; set; }

        /// <summary>
        /// Profibnetteilnehmer 192.168.0.14 nicht erreichbar
        /// </summary>
        public virtual bool BusNioFestoInsel2A101 { get; set; }

        /// <summary>
        /// Profibnetteilnehmer 192.168.0.10 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA1011 { get; set; }

        /// <summary>
        /// Profibnetteilnehmer 192.168.0.11 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA1012 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.20 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSiemensIOA202 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.22 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA2021 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.23 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA2022 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.60 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSiemensIOA303 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.62 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA3031 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.63 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA3032 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.64 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA3033 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.150 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSiemensIOA505 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.152 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA5051 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.170 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSiemensIOA606 { get; set; }

        /// <summary>
        /// Profinetteilnehmer 192.168.0.192 nicht erreichbar
        /// </summary>
        public virtual bool BusNioSwitchA8081 { get; set; }

        /// <summary>
        /// Profinusteilnehmer 10 nicht erreichbar
        /// </summary>
        public virtual bool BusNioAmada { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UserLevel1Active { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UserLevel2Active { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UserLevel3Active { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UserLevel4Active { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual short UserLevel { get; set; }

        /// <summary>
        /// Schutztüren 1-6 verriegeln
        /// </summary>
        public virtual bool ST16verr { get; set; }

        /// <summary>
        /// Schutztüren 1-6 entriegeln
        /// </summary>
        public virtual bool ST16entr { get; set; }
    }
}
