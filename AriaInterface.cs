using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MIQAexport
{
    public static class AriaInterface
    {
        private static SqlConnection connection = null; //Deklarerar sql connection

        public static void Connect()
        {
            string filename = @"\\ltvastmanland.se\ltv\shares\vradiofy\RADIOFYSIK NYSTART\Ö_Erik\02 Programmering\C# scripting\aria_account_information.txt";//txtfilen med inloggningsuppgifterna till Arias databas
            StreamReader sr = new StreamReader(filename, false); //deklarerar en reader för txtfilen
            string connectionStr = sr.ReadLine(); //läser txtfilen
            sr.Close(); //stänger filen
            connection = new SqlConnection(connectionStr); //ansätter sql connection till inloggningssträngen

            connection.Open(); //Öppnar kopplingen mellan scriptet och Arias DB
        }

        public static void Disconnect()
        {
            connection.Close(); //Stänger kopplingen mellan Aria och scriptet
        }
        public static DataTable Query(string queryString) //Förfrågan mot Arias DB
        {
            DataTable dataTable = new DataTable(); //deklarerar nytt dataTabel
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(queryString, connection) { MissingMappingAction = MissingMappingAction.Passthrough, MissingSchemaAction = MissingSchemaAction.Add }; //Frågar databasen med strängen
                adapter.Fill(dataTable); //Sparar datan från förfrågan i datatable
                adapter.Dispose(); //Skräpar förfrågan efter den är klar
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "SQL Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //skriver ut felmeddelande vid krasch för lättare felsökning
            }
            return dataTable;
        }
        public static DataTable GetPatientIDList(DateTime startDate, DateTime endDate)
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                        Patient.PatientId
                                                    FROM
                                                        Patient,
                                                        TreatmentRecord
                                                    WHERE
                                                        TreatmentRecord.PatientSer=Patient.PatientSer AND
                                                        TreatmentRecord.TreatmentRecordDateTime BETWEEN '" + startDate.ToString() + @"' AND '" + endDate.ToString() + @"'
                                                    ORDER BY
                                                        Patient.PatientId
                                                        ");
            return datatable;
        }
        public static DataTable GetPatientIDList(string date) //Returnerar en lista över patienter som har slutfört "Sista behandling" eller sista fraktionen enligt ordination
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                            Patient.PatientId
                                                        FROM 
                                                            Activity,
                                                            Patient,
                                                            ScheduledActivity,
                                                            RadiationHstry,
                                                            ActivityInstance,
                                                            TreatmentRecord,
                                                            Prescription,
                                                            PlanSetup,
                                                            RTPlan
                                                        WHERE
                                                            Patient.PatientSer=ScheduledActivity.PatientSer AND
                                                            ScheduledActivity.ActivityInstanceSer=ActivityInstance.ActivityInstanceSer AND
                                                            Activity.ActivitySer=ActivityInstance.ActivitySer AND
                                                            ScheduledActivity.ActualEndDate >= '" + date + @"' AND
                                                            YEAR(ScheduledActivity.ActualEndDate) = YEAR(RadiationHstry.TreatmentEndTime) AND
                                                            MONTH(ScheduledActivity.ActualEndDate) = MONTH(RadiationHstry.TreatmentEndTime) AND
                                                            DAY(ScheduledActivity.ActualEndDate) = DAY(RadiationHstry.TreatmentEndTime) AND
                                                            (Activity.ActivitySer LIKE '" + 537 + @"' OR
                                                            Prescription.NumberOfFractions = RadiationHstry.FractionNumber) AND
                                                            RadiationHstry.TreatmentEndTime >= '" + date + @"' AND
                                                            TreatmentRecord.TreatmentRecordSer = RadiationHstry.TreatmentRecordSer AND
                                                            TreatmentRecord.PatientSer = Patient.PatientSer AND
                                                            TreatmentRecord.RTPlanSer = RTPlan.RTPlanSer AND
                                                            RTPlan.PlanSetupSer = PlanSetup.PlanSetupSer AND
                                                            Prescription.PrescriptionSer = PlanSetup.PrescriptionSer
                                                            ");
            return datatable;
            //Activity.ActivitySer LIKE '" + 847 + @"' OR
        }
        public static DataTable GetCTSliceUIDs(string RTPlanSer) //Returnerar CT-snittens UID baserat på planens UID
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                        Slice.SliceUID
                                                    FROM
                                                        Slice,
                                                        Image,
                                                        StructureSet,
                                                        PlanSetup,
                                                        RTPlan
                                                    WHERE
                                                        RTPlan.RTPlanSer = '" + RTPlanSer + @"' AND
                                                        RTPlan.PlanSetupSer = PlanSetup.PlanSetupSer AND
                                                        PlanSetup.StructureSetSer = StructureSet.StructureSetSer AND
                                                        StructureSet.ImageSer = Image.ImageSer AND
                                                        Image.SeriesSer = Slice.SeriesSer AND
                                                        Slice.SliceModality = 'CT'
                                                        ");
            return datatable;
        }

        public static string GetPatientSer(string patientID) // översätter patient ID till patientens serienummer i Aria
        {
            DataTable datatable = AriaInterface.Query(@"SELECT
                                                            Patient.PatientSer
                                                        FROM
                                                            Patient
                                                        WHERE                    
                                                            Patient.PatientId LIKE '" + patientID + @"'
                                                        GROUP BY
                                                            Patient.PatientSer");
            if (!datatable.Rows[0].IsNull(0)) // om första elementet inte är tomt
                return datatable.Rows[0]["PatientSer"].ToString(); //skicka tillbaka patientens serienummer
            return string.Empty;
        }
        public static DataTable GetCourseSer(string PatienSer, DateTime date) //Returnera alla kursers serienummer baserat på patient och datum
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                            Course.CourseSer
                                                        FROM
                                                            Series,
                                                            RTPlan,
                                                            TreatmentRecord,
                                                            PlanSetup,
                                                            Patient,
                                                            Course
                                                        WHERE
                                                            PlanSetup.CourseSer=Course.CourseSer AND
                                                            PlanSetup.PlanSetupSer=RTPlan.PlanSetupSer AND
                                                            RTPlan.RTPlanSer=TreatmentRecord.RTPlanSer AND
                                                            TreatmentRecord.PatientSer=Patient.PatientSer AND
                                                            TreatmentRecord.TreatmentRecordDateTime >= '" + date + @"' AND
                                                            Patient.PatientSer= '" + PatienSer + @"'
                                                            ");
            return datatable;
        }
        public static DataTable GetRTPlanUID(string PatienId, DateTime date) //Returnerar planernas UIDs baserat på patient och datum
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                            RTPlan.PlanUID,
                                                            RTPlan.RTPlanSer
                                                        FROM
                                                            Series,
                                                            RTPlan,
                                                            TreatmentRecord,
                                                            Patient
                                                        WHERE
                                                            RTPlan.RTPlanSer=TreatmentRecord.RTPlanSer AND
                                                            TreatmentRecord.PatientSer=Patient.PatientSer AND
                                                            TreatmentRecord.TreatmentRecordDateTime >= '" + date + @"' AND
                                                            Patient.PatientId= '" + PatienId + @"'
                                                            ");
            return datatable;
        }
        //public static DataTable GetRTPlanUID(string CourseSer) //Returnerar planernas UIDs och serienummer baserat på kursserienummer
        //{
        //    DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
        //                                                    RTPlan.PlanUID,
        //                                                    RTPlan.RTPlanSer
        //                                                FROM
        //                                                    Series,
        //                                                    RTPlan,
        //                                                    TreatmentRecord,
        //                                                    PlanSetup,
        //                                                    Patient,
        //                                                    Course
        //                                                WHERE
        //                                                    PlanSetup.CourseSer=Course.CourseSer AND
        //                                                    PlanSetup.PlanSetupSer=RTPlan.PlanSetupSer AND
        //                                                    RTPlan.RTPlanSer=TreatmentRecord.RTPlanSer AND
        //                                                    Course.CourseSer= '" + CourseSer + @"'
        //                                                    ");
        //    return datatable;
        //}
        public static DataTable GetRTRecordUIDs(string PatienId, string RTPlanSer) //Returnerar RTRecord UIDs baserat på patienter och planens serienummer
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                        TreatmentRecord.TreatmentRecordUID
                                                    FROM
                                                        Patient,
                                                        RTPlan,
                                                        Series,
                                                        TreatmentRecord,
                                                        RadiationHstry
                                                    WHERE
                                                        Series.SeriesSer=TreatmentRecord.SeriesSer AND
                                                        RadiationHstry.TreatmentRecordSer=TreatmentRecord.TreatmentRecordSer AND
                                                        TreatmentRecord.PatientSer=Patient.PatientSer AND
                                                        Patient.PatientId= '" + PatienId + @"' AND
                                                        RTPlan.RTPlanSer=TreatmentRecord.RTPlanSer AND
                                                        RTPlan.RTPlanSer = '" + RTPlanSer + @"'
                                                        ");
            return datatable;
        }

        public static DataTable GetCTUIDs(string RTPlanSer) //Returnerar CT UIDs baserat på planens serienummer
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                        Series.SeriesUID
                                                    FROM
                                                        Series,
                                                        Image,
                                                        StructureSet,
                                                        RTPlan,
                                                        PlanSetup
                                                    WHERE
                                                        Series.SeriesSer=Image.SeriesSer AND
                                                        Image.ImageSer=StructureSet.ImageSer AND
                                                        StructureSet.StructureSetSer=PlanSetup.StructureSetSer AND
                                                        PlanSetup.PlanSetupSer=RTPlan.PlanSetupSer AND
                                                        RTPlan.RTPlanSer = '" + RTPlanSer + @"' AND
                                                        Series.SeriesModality = 'CT'
                                                        ");
            return datatable;
        }
        public static DataTable GetRTStructureSetUIDs(string RTPlanSer) //Returnerar RTStructureSet UIDs baserat på planens serienummer
        {
            DataTable datatable = AriaInterface.Query(@"SELECT
                                                        StructureSet.StructureSetUID
                                                    FROM
                                                        StructureSet,
                                                        RTPlan,
                                                        PlanSetup
                                                    WHERE
                                                        StructureSet.StructureSetSer=PlanSetup.StructureSetSer AND
                                                        PlanSetup.PlanSetupSer=RTPlan.PlanSetupSer AND
                                                        RTPlan.RTPlanSer = '" + RTPlanSer + @"'
                                                        ");
            return datatable;
        }
        public static DataTable GetRTDose(string RTPlanSer) //Returnerar RTDose UIDs baserat på planens serienummer
        {
            DataTable datatable = AriaInterface.Query(@"SELECT DISTINCT
                                                        DoseMatrix.DoseUID
                                                    FROM
                                                        PlanSetup,
                                                        RTPlan,
                                                        DoseMatrix
                                                    WHERE
                                                        DoseMatrix.PlanSetupSer=PlanSetup.PlanSetupSer AND
                                                        PlanSetup.PlanSetupSer=RTPlan.PlanSetupSer AND
                                                        RTPlan.RTPlanSer = '" + RTPlanSer + @"'
                                                        ");
            return datatable;
        }
    }
}
