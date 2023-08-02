using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Data;


namespace MIQAexport
{
    public class Writer
    {
        public static void WriteTxt(string fileName, List<string> strOut) //Skriver ner text i en txtfil från lista av strängar
        {
            StreamWriter sw = new StreamWriter(fileName, false);
            foreach (string text in strOut) //loopar genom alla strängar
            {
                sw.WriteLine(text); //Skriver ner sträng i txtfil
            }
            sw.Close();
        }
    }
}
