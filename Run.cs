using DICOM_Communication_101;
using EvilDICOM.Core.Modules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MIQAexport
{
    public class Run
    {
        private List<string> logFile; //Deklarerar loggfil
        private string LogFilePath; //Deklarerar loggfil
        int n;
        public void Execute()
        {
            logFile = new List<string>();//initierar loggfil
            LogFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\MIQA logfiler\" + DateTime.Now.ToString("yy/MM/dd").Replace("-", "") + "_MIQA_logfile.txt"; //Sökväg till mapp på skrivbordet för loggfil
            try //Testa att köra scriptet, om krasch gå till catch
            {
                var startDate = DateTime.Today.AddDays(-91); //tidsintervallet för färdigbehandlade patienter, 00:00 samma dag
                var endDate = DateTime.Today.AddDays(-90); //tidsintervallet för färdigbehandlade patienter, 00:00 samma dag
                //logFile.Add("Datum: " + startDate.ToString() + " - " + endDate.ToString()); //lägger till datum i loggfilen //"yyyy-MM-dd HH:mm"
                File.AppendAllText(LogFilePath, Environment.NewLine + "Datum: " + startDate.ToString() + " - " + endDate.ToString());

                AriaInterface.Connect(); //Öppnar kopplingen till Arias DB

                DataTable patientIDdt = AriaInterface.GetPatientIDList(startDate, endDate);

                AriaInterface.Disconnect(); //Stänger kopplingen till Arias DB

                List<string> PatientIDList = new List<string>();
                PatientIDList = rowExtraction(patientIDdt, PatientIDList); //Omvandlar rows från DataTable till strängar
                List<string> NewPatientIDList = new List<string>();

                int currentPat = 1; //Räknestart för patienter som ska skickas
                int TotalPat = patientIDdt.Rows.Count;
                MIQACMove move = new MIQACMove(); //Initierar klassen MIQACMove
                DataTable RTPlansUIDs = new DataTable();
                bool removed;

                foreach (string patient in PatientIDList)
                {
                    if (patient.Length == 12)
                    {
                        List<string> RTPlanUIDsList = new List<string>();

                        RTPlansUIDs = AriaInterface.GetRTPlanUID(patient, startDate); //Hämtar RTPlan UIDs
                        RTPlanUIDsList = rowExtraction(RTPlansUIDs, RTPlanUIDsList); //Omvandlar rows från DataTable till strängar i List
                        RTPlanUIDsList = NewPlanList(RTPlanUIDsList, out removed); //Tar bort planer som redan skickats till MIQA

                        if (!removed)
                            NewPatientIDList.Add(patient);
                    }
                }

                if (NewPatientIDList.Count == 0)
                {
                    //logFile.Add("Det finns inga nya patienter att skicka");
                    File.AppendAllText(LogFilePath, Environment.NewLine + "Det finns inga nya patienter att skicka");
                }

                n = 1; //Radbrytning för loggfil

                foreach (string patientID in NewPatientIDList) //Loopar genom varje patient från listan av patienter
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    string prevPlanUIDfilepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\MIQA logfiler\planUIDs.txt"; //Sökväg till mapp på skrivbordet för loggfil
                    Console.SetCursorPosition(0, 0); //Hoppar tillbaka till början av terminalen för varje patient

                    AriaInterface.Connect(); //kopplar till Arias DB
                    //string patSer = AriaInterface.GetPatientSer(patientID); //Hämtar patientens serienummer

                    //var listOfCourseSer = AriaInterface.GetCourseSer(patSer, yesterday); //Hämtar listan över kursers serienummer för patienten.

                    DataTable RTRecordsUIDS = new DataTable();
                    DataTable RTDosesUIDs = new DataTable();
                    DataTable RTStructureSetUIDs = new DataTable();
                    DataTable CTUID = new DataTable();
                    List<string> RTPlanSeriesUIDs = new List<string>();
                    List<string> RTRecordSeriesUIDs = new List<string>();
                    List<string> RTDoseSeriesUIDs = new List<string>();
                    List<string> RTStructureSetSeriesUIDs = new List<string>();
                    List<string> CTSeriesUIDs = new List<string>();

                    Console.WriteLine("Sending patient " + currentPat + "/" + NewPatientIDList.Count); //Skriver ut numret på nuvarande patient som processas
                    //logFile.Add("Patient: " + currentPat.ToString() + "/" + NewPatientIDList.Count); //lägger till infon i loggfilen
                    File.AppendAllText(LogFilePath, Environment.NewLine + "Patient: " + currentPat.ToString() + "/" + NewPatientIDList.Count);
                    //string courseSer = row[0].ToString(); //Deklarerar courseSer som sträng
                    RTPlansUIDs = AriaInterface.GetRTPlanUID(patientID, startDate); //Hämtar RTPlan UIDS och Serienummer
                    RTPlanSeriesUIDs = rowExtraction(RTPlansUIDs, RTPlanSeriesUIDs); //Omvandlar rows från DataTable till strängar
                    foreach (DataRow planRow in RTPlansUIDs.Rows) //Loopar genom alla behandlade planer för patient
                    {

                        string RTPlanSer = planRow[1].ToString();

                        RTRecordsUIDS = AriaInterface.GetRTRecordUIDs(patientID, RTPlanSer); //Hämtar RTRecord UIDS
                        CTUID = AriaInterface.GetCTUIDs(RTPlanSer); //Hämtar CT UIDS
                        //CTUID = AriaInterface.GetCTSliceUIDs(RTPlanSer); //Hämtar CT UIDS
                        RTStructureSetUIDs = AriaInterface.GetRTStructureSetUIDs(RTPlanSer); //Hämtar RTStructureSet UIDS
                        RTDosesUIDs = AriaInterface.GetRTDose(RTPlanSer); //Hämtar RTDose UIDS

                        RTDoseSeriesUIDs = rowExtraction(RTDosesUIDs, RTDoseSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        RTRecordSeriesUIDs = rowExtraction(RTRecordsUIDS, RTRecordSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        RTStructureSetSeriesUIDs = rowExtraction(RTStructureSetUIDs, RTStructureSetSeriesUIDs); //Omvandlar rows från DataTable till strängar
                        CTSeriesUIDs = rowExtraction(CTUID, CTSeriesUIDs); //Omvandlar rows från DataTable till strängar

                        File.AppendAllLines(prevPlanUIDfilepath, RTPlanSeriesUIDs);
                    }
                    
                    AriaInterface.Disconnect(); //Stängar koppling till Arias DB

                    MoveDCMs(RTDoseSeriesUIDs, move, "RTDOSE", patientID); //Skickar förfrågan att skicka DICOM-filer för RTDose
                    //MoveDCMs(CTSeriesUIDs, move, "CT", patientID); //Skickar förfrågan att skicka DICOM-filer för RTDose
                    CTSeriesUIDs = CTSeriesUIDs.Distinct().ToList(); //Skapar en lista av unika CT-serie UIDs för enskilda bilder i CTn
                    foreach (string s in CTSeriesUIDs)
                        move.MultipleSeriesUID(s, n, "CT", patientID, LogFilePath); //Skickar förfrågan att skicka DICOM-filer för CT
                    n += 2;
                    MoveDCMs(RTRecordSeriesUIDs, move, "RTRECORD", patientID); //Skickar förfrågan att skicka DICOM-filer för RTRECORD
                    MoveDCMs(RTPlanSeriesUIDs, move, "RTPLAN", patientID); //Skickar förfrågan att skicka DICOM-filer för RTPLAN
                    MoveDCMs(RTStructureSetSeriesUIDs, move, "RTSTRUCT", patientID); //Skickar förfrågan att skicka DICOM-filer för RTSTRUCT

                    currentPat++; //ökar patienträknaren med 1
                    n++;

                    sw.Stop();
                    File.AppendAllText(LogFilePath, Environment.NewLine + "Elapsed time: " + sw.Elapsed.TotalSeconds.ToString("0") + " s" + Environment.NewLine);
                }
                //writeToTxt();
                Console.WriteLine("Done!");
            }
            catch (Exception e) //vid krasch
            {
                //logFile.Add("{0} Exception caught." + e); //Skriver felmeddelandet i loggfilen
                File.AppendAllText(LogFilePath, Environment.NewLine + $"Exception caught. {e}");
                writeToTxt();
                Console.WriteLine("Ett fel har uppstått. Försök igen eller kontakta Erik Fura. Skicka gärna med logfilen :)");
                Console.Read();
            }
        }
        private void MoveDCMs(List<string> RTSeriesUIDs, MIQACMove move, string name, string PatientID) //Skickar förfrågan till Eclipse att skicka DICOMS baserat på UID
        {
            if (RTSeriesUIDs.Count != 0)
            {
                int iteration = 0;
                RTSeriesUIDs = RTSeriesUIDs.Distinct().ToList(); //Välj ut Unika UIDs om det skulle råka finnas dubbleter
                foreach (string s in RTSeriesUIDs) //Loopar genom alla UIDs
                {
                    iteration++;
                    move.SOPUID(s, name, PatientID); //Skickar DICOMs till MIQA
                    DICOMmoveResult(RTSeriesUIDs, name, iteration);//Skriver resultatet av flytten i terminalen
                }
                DICOMmoveResultToLog(RTSeriesUIDs, name, iteration);//Skriver resultatet av flytten i loggfilen
                n += 2; //Storleken på steg för att texten ska formateras rätt i loggfilen/terminalen
            }
        }
        private void DICOMmoveResult(List<string> RTDoseSeriesUIDs, string name, int iteration) //Skriver resultatet i terminalen
        {
            Console.SetCursorPosition(0, n); //Ställer in raden för terminalen
            Console.WriteLine($"DICOM C-Move Results for {name} :                           ");
            Console.WriteLine($"Number of Completed Operations : {iteration} / {RTDoseSeriesUIDs.Count}            ");
        }
        private void DICOMmoveResultToLog(List<string> RTDoseSeriesUIDs, string name, int iteration)//Skriver resultatet av flytten i loggfilen
        {
            //logFile.Add($"DICOM C-Move Results for {name} :                           ");
            File.AppendAllText(LogFilePath, Environment.NewLine + $"DICOM C-Move Results for {name} :                           ");
            //logFile.Add($"Number of Completed Operations : {iteration} / {RTDoseSeriesUIDs.Count}            ");
            File.AppendAllText(LogFilePath, Environment.NewLine + $"Number of Completed Operations : {iteration} / {RTDoseSeriesUIDs.Count}            ");
        }
        private List<string> rowExtraction(DataTable dt, List<string> SeriesUID) //Översätter dataRow till sträng 
        {
            foreach (DataRow row in dt.Rows)
            {
                SeriesUID.Add(row[0].ToString());
            }
            return SeriesUID;
        }

        private List<string> NewPlanList(List<string> RTPlanUIDList, out bool removed) //Tar bort planer som redan skickats till MIQA 
        {
            removed = false;
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\MIQA logfiler\planUIDs.txt"; //Sökväg till mapp på skrivbordet för loggfil
            string[] RTPlansInMIQA = File.ReadAllLines(filepath);

            foreach (string RTPlan in RTPlanUIDList.ToList())
                if (RTPlansInMIQA.Contains(RTPlan))
                {
                    RTPlanUIDList.Remove(RTPlan);
                    removed = true;
                }

            return RTPlanUIDList;
        }

        private void writeToTxt() //Genererar txtfilen
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\MIQA logfiler\" + DateTime.Now.ToString("yy/MM/dd").Replace("-", "") + "_MIQA_logfile.txt"; //Sökväg till mapp på skrivbordet för loggfil
            Writer.WriteTxt(filepath, logFile); //Skriver txtfilen
        }
    }
}
