using DICOM_Communication_101;
using EvilDICOM.Core.Modules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MIQAexport
{
    public class Run_linnea
    {
        private List<string> logFile; //Deklarerar loggfil
        public void Execute()
        {
            logFile = new List<string>();//initierar loggfil
            try //Testa att köra scriptet, om krasch gå till catch
            {
                //var yesterday = DateTime.Today; //tidsintervallet för färdigbehandlade patienter, 00:00 samma dag             Ändra!
                DateTime start = DateTime.Today.AddDays(-168); //början på tidsintervallet för färdigbehandlade patienter, 00:00 för 24 veckor sedan
                DateTime end = DateTime.Today.AddDays(-161); //slutet på tidsintervallet för färdigbehandlade patienter, 00:00 för 23 veckor sedan
                //DateTime start = new DateTime(2022, 1, 1, 0, 0, 0);
                //DateTime end = new DateTime(2022, 12, 31, 0, 0, 0);

                AriaInterface2.Connect(); //Öppnar kopplingen till Arias DB

                //var patientIDList = AriaInterface2.GetPatientIDList(yesterday.ToString()); //Skapar listan över färdigbehandlade patienter
                DataTable patientIDList = AriaInterface2.GetPatientIDList(start, end);

                AriaInterface2.Disconnect(); //Stänger kopplingen till Arias DB

                int currentPat = 1; //Räknestart för patienter som ska skickas
                int n = 0; //Radbrytning för loggfil
                MIQACMove move = new MIQACMove(); //Initierar klassen MIQACMove

                foreach (DataRow patient in patientIDList.Rows) //Loopar genom varje patient från listan av patienter
                {
                    Console.SetCursorPosition(0, 0); //Hoppar tillbaka till början av terminalen för varje patient
                    Console.WriteLine("Sending patient " + currentPat + "/" + patientIDList.Rows.Count); //Skriver ut numret på nuvarande patient som processas
                    logFile.Add("Patient: " + currentPat.ToString() + "/" + patientIDList.Rows.Count); //lägger till infon i loggfilen
                    string patientID = patient[0].ToString(); //Deklarerar patient ID som sträng

                    //AriaInterface2.Connect(); //kopplar till Arias DB
                    //string patSer = AriaInterface2.GetPatientSer(patientID); //Hämtar patientens serienummer

                    //var listOfCourseSer = AriaInterface2.GetCourseSer(patSer, yesterday); //Hämtar listan över kursers serienummer för patienten.

                    DataTable RTRecordsUIDS = new DataTable();
                    DataTable RTPlansUIDs = new DataTable();
                    DataTable RTDosesUIDs = new DataTable();
                    DataTable RTStructureSetUIDs = new DataTable();
                    DataTable CTUID = new DataTable();
                    List<string> RTPlanSeriesUIDs = new List<string>();
                    List<string> RTRecordSeriesUIDs = new List<string>();
                    List<string> RTDoseSeriesUIDs = new List<string>();
                    List<string> RTStructureSetSeriesUIDs = new List<string>();
                    List<string> CTSeriesUIDs = new List<string>();

                    //foreach (DataRow row in listOfCourseSer.Rows) //loopar genom alla kurser
                    //{
                    //    string courseSer = row[0].ToString(); //Deklarerar courseSer som sträng
                    //    RTPlansUIDs = AriaInterface2.GetRTPlanUID(courseSer); //Hämtar RTPlan UIDS och Serienummer
                    //    RTPlanSeriesUIDs = rowExtraction(RTPlansUIDs, RTPlanSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    //    foreach (DataRow planRow in RTPlansUIDs.Rows) //Loopar genom alla behandlade planer för patient
                    //    {
                    //        string RTPlanSer = planRow[1].ToString();

                    //        RTRecordsUIDS = AriaInterface2.GetRTRecordUIDs(patSer, RTPlanSer); //Hämtar RTRecord UIDS
                    //        CTUID = AriaInterface2.GetCTUIDs(RTPlanSer); //Hämtar CT UIDS
                    //        RTStructureSetUIDs = AriaInterface2.GetRTStructureSetUIDs(RTPlanSer); //Hämtar RTStructureSet UIDS
                    //        RTDosesUIDs = AriaInterface2.GetRTDose(RTPlanSer); //Hämtar RTDose UIDS

                    //        RTDoseSeriesUIDs = rowExtraction(RTDosesUIDs, RTDoseSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    //        RTRecordSeriesUIDs = rowExtraction(RTRecordsUIDS, RTRecordSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    //        RTStructureSetSeriesUIDs = rowExtraction(RTStructureSetUIDs, RTStructureSetSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    //        CTSeriesUIDs = rowExtraction(CTUID, CTSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    //    }
                    //}

                    RTPlansUIDs = AriaInterface2.GetRTPlanUID(patientID, start, end); //Hämtar RTPlan UIDS
                    RTPlanSeriesUIDs = rowExtraction(RTPlansUIDs, RTPlanSeriesUIDs); //Omvandlar rows från DataTable till strängar i List
                    foreach (string plan in RTPlanSeriesUIDs) //Loopar genom alla behandlade planer för patient
                    {
                        AriaInterface2.Connect();
                        RTRecordsUIDS = AriaInterface2.GetRTRecordUIDs(plan); //Hämtar RTRecord UIDs
                        //CTUID = AriaInterface2.GetCTUIDs(RTPlanSer); //Hämtar CT UIDs
                        RTStructureSetUIDs = AriaInterface2.GetRTStructureSetUIDs(plan); //Hämtar RTStructureSet UIDs
                        //RTDosesUIDs = AriaInterface.GetRTDose(RTPlanSer); //Hämtar RTDose UIDs
                        AriaInterface2.Disconnect();

                        RTRecordSeriesUIDs = rowExtraction(RTRecordsUIDS, RTRecordSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        //CTSeriesUIDs = rowExtraction(CTUID, CTSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        RTStructureSetSeriesUIDs = rowExtraction(RTStructureSetUIDs, RTStructureSetSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        //RTDoseSeriesUIDs = rowExtraction(RTDosesUIDs, RTDoseSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    }

                    //AriaInterface.Disconnect(); //Stängar koppling till Arias DB

                    //n += 2; //Storleken på steg för att texten ska formateras rätt i loggfilen/terminalen
                    //MoveDCMs(RTDoseSeriesUIDs, n, move, "RTDOSE", patientID); //Skickar förfrågan att skicka DICOM-filer för RTDose
                    //n += 2;
                    //CTSeriesUIDs = CTSeriesUIDs.Distinct().ToList(); //Skapar en lista av unika CT-serie UIDs för enskilda bilder i CTn
                    //foreach (string s in CTSeriesUIDs)
                    ////    move.MultipleSeriesUID(s, n, "CT", patientID, logFile); //Skickar förfrågan att skicka DICOM-filer för CT
                    //n += 2;
                    //MoveDCMs(RTRecordSeriesUIDs, n, move, "RTRECORD", patientID); //Skickar förfrågan att skicka DICOM-filer för RTRECORD
                    n += 2;
                    MoveDCMs(RTPlanSeriesUIDs, n, move, "RTPLAN", patientID); //Skickar förfrågan att skicka DICOM-filer för RTPLAN
                    n += 2;
                    MoveDCMs(RTStructureSetSeriesUIDs, n, move, "RTSTRUCT", patientID); //Skickar förfrågan att skicka DICOM-filer för RTSTRUCT
                    n += 2;

                    currentPat++; //ökar patienträknaren med 1
                }
                writeToTxt();
                Console.WriteLine("Done!");
            }
            catch (Exception e) //vid krasch
            {
                logFile.Add("{0} Exception caught." + e); //Skriver felmeddelandet i loggfilen
                writeToTxt();
                Console.WriteLine("Ett fel har uppstått. Försök igen eller kontakta Erik Fura. Skicka gärna med logfilen :)");
            }
        }
        private void MoveDCMs(List<string> RTDoseSeriesUIDs, int n, MIQACMove move, string name, string PatientID) //Skickar förfrågan till Eclipse att skicka DICOMS baserat på UID
        {
            int iteration = 0;
            RTDoseSeriesUIDs = RTDoseSeriesUIDs.Distinct().ToList(); //Välj ut Unika UIDs om det skulle råka finnas dubbleter
            foreach (string s in RTDoseSeriesUIDs) //Loopar genom alla UIDs
            {
                iteration++;
                move.SOPUID(s, name, PatientID); //Skickar DICOMs till MIQA
                DICOMmoveResult(RTDoseSeriesUIDs, n, name, iteration);//Skriver resultatet av flytten i terminalen
            }
            DICOMmoveResultToLog(RTDoseSeriesUIDs, n, name, iteration);//Skriver resultatet av flytten i loggfilen
        }
        private void DICOMmoveResult(List<string> RTDoseSeriesUIDs, int n, string name, int iteration) //Skriver resultatet i terminalen
        {
            Console.SetCursorPosition(0, n); //Ställer in raden för terminalen
            Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
            Console.WriteLine($"Number of Completed Operations : {iteration} / {RTDoseSeriesUIDs.Count}            ");
            Console.SetCursorPosition(0, n); //Ställer in raden för terminalen
        }
        private void DICOMmoveResultToLog(List<string> RTDoseSeriesUIDs, int n, string name, int iteration)//Skriver resultatet av flytten i loggfilen
        {
            logFile.Add($"DICOM C-Move Results for {name} :                           ");
            logFile.Add($"Number of Completed Operations : {iteration} / {RTDoseSeriesUIDs.Count}            ");
        }
        private List<string> rowExtraction(DataTable dt, List<string> SeriesUID) //Översätter dataRow till sträng 
        {
            foreach (DataRow row in dt.Rows)
            {
                SeriesUID.Add(row[0].ToString());
            }
            return SeriesUID;
        }
        private void writeToTxt() //Genererar txtfilen
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + DateTime.Now.ToString("yy/MM/dd").Replace("-", "") + "_MIQA_logfile.txt"; //Sökväg till mapp på skrivbordet för loggfil
            Writer.WriteTxt(filepath, logFile); //Skriver txtfilen
        }
    }
}
