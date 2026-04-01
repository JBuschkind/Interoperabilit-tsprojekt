using System;

namespace Draeger.Plc
{
    // Automatisch generiert aus TIA_Export.aml
    public static class PlcTags
    {
        /// <summary>
        /// Name: Taster_Start
        /// Kommentar: Start Taster Bedienpult (Schließer)
        /// Datentyp: Bool
        /// IO-Typ: Input
        /// Adresse: 0.0
        /// Modul: DI 16x24VDC HF_1
        /// Kanal: Channel_DI_0 (0)
        /// Kanal-IO-Typ: Input
        /// Kanal-Typ: Digital
        /// </summary>
        public static PlcTag<bool> Taster_Start => new PlcTag<bool>(
            name: "Taster_Start",
            dataType: "Bool",
            ioType: "Input",
            logicalAddress: "0.0",
            comment: "Start Taster Bedienpult (Schließer)",
            moduleName: "DI 16x24VDC HF_1",
            channelName: "Channel_DI_0",
            channelNumber: 0,
            channelIoType: "Input");

        /// <summary>
        /// Name: Ventil_Stanze
        /// Kommentar: Pneumatikventil Stanze (1-Signal = senken / 0-Signal = heben)
        /// Datentyp: Bool
        /// IO-Typ: Output
        /// Adresse: 0.0
        /// Modul: DQ 16x24VDC/0.5A HF_1
        /// Kanal: Channel_DO_0 (0)
        /// Kanal-IO-Typ: Output
        /// Kanal-Typ: Digital
        /// </summary>
        public static PlcTag<bool> Ventil_Stanze => new PlcTag<bool>(
            name: "Ventil_Stanze",
            dataType: "Bool",
            ioType: "Output",
            logicalAddress: "0.0",
            comment: "Pneumatikventil Stanze (1-Signal = senken / 0-Signal = heben)",
            moduleName: "DQ 16x24VDC/0.5A HF_1",
            channelName: "Channel_DO_0",
            channelNumber: 0,
            channelIoType: "Output");

    }
}
