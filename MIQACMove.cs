using EvilDICOM.Core.Helpers;
using EvilDICOM.Network;
using EvilDICOM.Network.DIMSE.IOD;
using EvilDICOM.Network.SCUOps;
using EvilDICOM.Network.DIMSE;
using EvilDICOM.Core.Element;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvilDICOM.Core;
using System.Threading;

namespace MIQAexport
{
    public class MIQACMove
    {
        private Entity local;
        private Entity daemon;
        private DICOMSCU client;
        private CFinder finder;

        public MIQACMove()
        {
            Initialize();
        }
        public void Initialize()
        {
            //Store the details of the client (Ae Title, port) -> IP address is determined by CreateLocal() method
            local = Entity.CreateLocal("DCSCRIPT", 104);
            //Store the details of the daemon (Ae Title, IP, port)
            daemon = new Entity("DB_Service", "10.30.53.103", 55404);
            //Set up a client (DICOM SCU = Service Class User)
            client = new DICOMSCU(local);
            //Set up a receiver to catch the files as they come in
            var receiver = new DICOMSCP(local);
            //Let the daemon know we can take anything it sends
            receiver.SupportedAbstractSyntaxes = AbstractSyntax.ALL_RADIOTHERAPY_STORAGE;
            //Set up storage location
            var storagePath = @"\\sltvmiqa1\Incoming";

            //Set the action when a DICOM files comes in
            receiver.DIMSEService.CStoreService.CStorePayloadAction = (dcm, asc) =>
            {
                var path = Path.Combine(storagePath, dcm.GetSelector().SOPInstanceUID.Data + ".dcm");
                dcm.Write(path);
                return true; // Lets daemom know if you successfully wrote to drive
            };
            receiver.ListenForIncomingAssociations(true);

            client.ConnectionTimeout = 600_000; // in ms
            client.IdleTimeout = 600_000; // in ms

            //Build a finder class to help with C-FIND operations
            finder = client.GetCFinder(daemon);
        }

        public void SOPUID(string SeriesUID, string SOPname, string PatientID) //Filter series by modality, then create list of DICOMs
        {
            var studies = finder.FindStudies(PatientID); //Öppnar studier för patienten
            var series = finder.FindSeries(studies); //Öppnar serier för patienten

            var DICOMs = series.Where(s => s.Modality == SOPname) //Plockar ut alla DICOMs med samma modalitet
                .SelectMany(ser => finder.FindImages(ser));
            var DICOM = DICOMs.FirstOrDefault(s => s.SOPInstanceUID.Equals(SeriesUID)); //Plockar ut alla DICOMs med samma UID som scriptet

            SendingDICOM(DICOM); //Skickar DICOM till MIQA
        }
        public void MultipleSeriesUID(string SeriesUID, int n, string SeriesName, string PatientID, out int iteration, out int totDcms) //Samma som tidigare men för multipla serier 
        {
            var studies = finder.FindStudies(PatientID);
            var series = finder.FindSeries(studies);
            var DICOMs = series.Where(s => s.SeriesInstanceUID == SeriesUID) //Plockar ut alla DICOMs med seriens UID
                .SelectMany(ser => finder.FindImages(ser));
            SendingDICOM(DICOMs.Count(), n, DICOMs, SeriesName, out iteration); //Plockar ut alla DICOMs med modaliteten CT
            totDcms = DICOMs.Count();
        }

        private void SendingDICOM(CFindInstanceIOD DICOM) //Skickar 1 DICOM
        {
            var mover = client.GetCMover(daemon); //CMove av DICOM från Eclipse till MIQA
            
            ushort msgId = 1;

            CMoveResponse response;
            response = mover.SendCMove(DICOM, local.AeTitle, ref msgId); //Utför operationen CMove
            if (response.NumberOfCompletedOps == 0)
            {
                mover.Dispose();
                Thread.Sleep(30000);
            }
        }
        private void SendingDICOM(int lengthOfSeris, int n, IEnumerable<CFindInstanceIOD> DICOMs, string name, out int success) //Skickar flera DICOM
        {
            var mover = client.GetCMover(daemon); //CMove av DICOM från Eclipse till MIQA
            
            ushort msgId = 1;
            success = 0;
            int failed = 0, remaining = 0, warnings = 0;
            CMoveResponse response;
            
            foreach (var DICOM in DICOMs) //loopar genom alla DICOM-filer
            {
                response = mover.SendCMove(DICOM, local.AeTitle, ref msgId);
                if (response.NumberOfCompletedOps == 1)
                    success++;
                else if (response.NumberOfFailedOps == 1)
                    failed++;
                else if (response.NumberOfWarningOps == 1)
                    warnings++;
                else if (response.NumberOfRemainingOps == 1)
                    remaining++;
                else
                    break;
                Console.SetCursorPosition(0, n);
                Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
                Console.WriteLine($"Number of Completed Operations : {success} / {lengthOfSeris}             ");
                Console.SetCursorPosition(0, n);
            }
            Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
            Console.WriteLine($"Number of Completed Operations : {success} / {lengthOfSeris}              ");
            Console.SetCursorPosition(0, n);
            if (success == 0) //Ifall den inte lyckas skicka DICOM-filer trots att de finns (ex korrupta filer) stängs connection och väntar 30 s innan den går vidare
            {
                mover.Dispose();
                Thread.Sleep(30000);
            }
        }
    }
}