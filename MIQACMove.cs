using EvilDICOM.Core.Helpers;
using EvilDICOM.Network;
using EvilDICOM.Network.DIMSE.IOD;
using EvilDICOM.Network.SCUOps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvilDICOM.Network.DIMSE;
using EvilDICOM.Core.Enums;
using System.Threading;
using System.Runtime.InteropServices;
using EvilDICOM.Core;
using System.Xml.Linq;
using EvilDICOM.Core.IO.Writing;
using EvilDICOM.Core.Element;

namespace DICOM_Communication_101
{
    /// <summary>
    /// This tutorial is outlined in chapter 4 of Scripting in RT for Physicists (C-Move to Self)
    /// </summary>
    public class MIQACMove
    {
        private Entity local;
        private Entity daemon;
        private Entity miqa;
        private DICOMSCU client;
        private CFinder finder;
        private IEnumerable<CFindSeriesIOD> series;

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
            //Store the details of the daemon (Ae Title, IP, port)
            miqa = new Entity("MIQA_PACS", "10.30.103.68", 10104);
            //Set up a client (DICOM SCU = Service Class User)
            client = new DICOMSCU(local);
            //Set up a receiver to catch the files as they come in
            var receiver = new DICOMSCP(local);
            //Let the daemon know we can take anything it sends
            receiver.SupportedAbstractSyntaxes = AbstractSyntax.ALL_RADIOTHERAPY_STORAGE;
            //Set up storage location
            var storagePath = @"\\sltvmiqa1\Incoming";

            UniqueIdentifier languageCode = new UniqueIdentifier
            {
                DData = "ISO_IR 100",
                Tag = TagHelper.SpecificCharacterSet
            };

            //Set the action when a DICOM files comes in
            receiver.DIMSEService.CStoreService.CStorePayloadAction = (dcm, asc) =>
            {
                var path = Path.Combine(storagePath, dcm.GetSelector().SOPInstanceUID.Data + ".dcm");
                //dcm.Replace(languageCode);
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
            series = finder.FindSeries(studies); //Öppnar serier för patienten

            var DICOMs = series.Where(s => s.Modality == SOPname) //Plockar ut alla DICOMs med samma modalitet
                .SelectMany(ser => finder.FindImages(ser));
            var DICOM = DICOMs.FirstOrDefault(s => s.SOPInstanceUID.Equals(SeriesUID)); //Plockar ut alla DICOMs med samma UID som scriptet

            SendingDICOM(DICOM); //Skickar DICOM till MIQA
        }
        public void MultipleSeriesUID(string SeriesUID, int n, string SeriesName, string PatientID, string LogFilePath) //Samma som tidigare men för multipla serier 
        {
            var studies = finder.FindStudies(PatientID);
            series = finder.FindSeries(studies);

            var DICOMs = series.Where(s => s.SeriesInstanceUID == SeriesUID) //Plockar ut alla DICOMs med seriens UID
                .SelectMany(ser => finder.FindImages(ser));
            SendingDICOM(DICOMs.Count(), n, DICOMs, SeriesName, LogFilePath); //Plockar ut alla DICOMs med modaliteten CT
        }

        private void SendingDICOM(CFindInstanceIOD DICOM) //Skickar 1 DICOM
        {
            var mover = client.GetCMover(daemon); //CMove av DICOM från Eclipse till MIQA
            
            ushort msgId = 1;

            CMoveResponse response;
            response = mover.SendCMove(DICOM, local.AeTitle, ref msgId); //Utför operationen CMove
        }
        private void SendingDICOM(int lengthOfSeris, int n, IEnumerable<CFindInstanceIOD> DICOMs, string name, string LogFilePath) //Skickar flera DICOM
        {
            var mover = client.GetCMover(daemon); //CMove av DICOM från Eclipse till MIQA
            
            ushort msgId = 1;
            int success = 0, failed = 0, remaining = 0, warnings = 0;
            CMoveResponse response;
            foreach (var DICOM in DICOMs) //loopar genom alla DICOM-filer
            {
                response = mover.SendCMove(DICOM, local.AeTitle, ref msgId);
                if (response.NumberOfCompletedOps == 1)
                    success++;
                if (response.NumberOfFailedOps == 1)
                    failed++;
                if (response.NumberOfRemainingOps == 1)
                    remaining++;
                if (response.NumberOfWarningOps == 1)
                    warnings++;
                Console.SetCursorPosition(0, n);
                Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
                Console.WriteLine($"Number of Completed Operations : {success} / {lengthOfSeris}             ");
                Console.SetCursorPosition(0, n);
            }
            Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
            Console.WriteLine($"Number of Completed Operations : {success} / {lengthOfSeris}              ");
            Console.SetCursorPosition(0, n);
            File.AppendAllText(LogFilePath, Environment.NewLine + $"DICOM C-Move Results for {name} :                           ");
        }
    }
}