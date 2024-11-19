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
        public static DataTable GetRTRecordUIDs(string PatienId, string RTPlanUID) //Returnerar RTRecord UIDs baserat på patienter och planens serienummer
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
                                                            RTPlan.PlanUID = '" + RTPlanUID + @"'
                                                            ");
            return datatable;
        }
        public static DataTable GetCTUIDs(string RTPlanUID) //Returnerar CT UIDs baserat på planens serienummer
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
                                                            RTPlan.PlanUID = '" + RTPlanUID + @"' AND
                                                            Series.SeriesModality = 'CT'
                                                            ");
            return datatable;
        }
        public static DataTable GetRTStructureSetUIDs(string RTPlanUID) //Returnerar RTStructureSet UIDs baserat på planens serienummer
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
                                                            RTPlan.PlanUID = '" + RTPlanUID + @"'
                                                            ");
            return datatable;
        }
        public static DataTable GetRTDose(string RTPlanUID) //Returnerar RTDose UIDs baserat på planens serienummer
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
                                                            RTPlan.PlanUID = '" + RTPlanUID + @"'
                                                            ");
            return datatable;
        }
    }
}
