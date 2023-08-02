using EvilDICOM.Core;
using EvilDICOM.Network;
using EvilDICOM.Network.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICOM_Communication_101
{
    public class CStore
    {
        /// <summary>
        /// This tutorial is outlined in chapter 4 of Scripting in RT for Physicists (C-Store)
        /// </summary>
        public static string Run(int consoleLine)
        {
            //MIQA_PACS IP: 192.168.110.23
            //VMSDBD IP: 192.168.113.57
            //VMSFSD IP: 192.168.113.41

            //Store the details of the MIQA (Ae Title, IP, port)
            var daemon = new Entity("MIQA_PACS", "10.30.103.68", 10104);
            //Store the details of the client (Ae Title, port) -> IP address is determined by CreateLocal() method
            var local = Entity.CreateLocal("DCSCRIPT", 104);
            //Set up a client (DICOM SCU = Service Class User)
            var client = new DICOMSCU(local);
            var storer = client.GetCStorer(daemon);

            var desktopPath = @"\\ltvastmanland.se\ltv\shares\vradiofy\z_Erik\MIQA\";
            var storagePath = Path.Combine(desktopPath, "DICOM Storage");

            ushort msgId = 1;
            var dcmFiles = Directory.GetFiles(storagePath);
            int success = 1;
            string line = "";
            foreach (var path in dcmFiles)
            {
                //Reads DICOM object into memory
                var dcm = DICOMObject.Read(path);
                var response = storer.SendCStore(dcm, ref msgId);
                //Write results to console
                if (response != null)
                {
                    if ((Status)response.Status == Status.SUCCESS)
                        success++;
                    Console.SetCursorPosition(0, consoleLine);
                    line = $"DICOM C-Store from {local.AeTitle} => " +
                            $"{daemon.AeTitle} @{daemon.IpAddress}:{daemon.Port}:" +
                            $"{(Status)response.Status}" +
                            $" : {success}" + "/" + dcmFiles.Length;
                    Console.WriteLine(line);
                    Console.SetCursorPosition(0, consoleLine);
                }
            }

            System.IO.DirectoryInfo di = new DirectoryInfo(storagePath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            return line;
        }
    }
}
