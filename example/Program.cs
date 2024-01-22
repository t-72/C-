using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //парсинг json, разворот дерева объектов данных
            //для приёма и интерпритации массива байтов modbus

            byte[] buf1 = new byte[4096];
            buf1[3] = 93;
            buf1[26] = 1;
            buf1[27] = 1;

            DataRegister dr = new DataRegister("data.json");
            dr.SetData(buf1);

            Console.WriteLine(dr["TOperData.CMD_PRM3"].IValue);

            dr["TOperData.CMD_PRM3"].IValue = 553;
            byte[] buf2 = dr.Data;
            Array.Copy(buf2, 0, buf1, 0, buf2.Length);

            Console.WriteLine("byte 1: " + buf1[26] + " + byte 2: " + buf1[27] + " =");
            Console.WriteLine(dr["TOperData.CMD_PRM3"].IValue);
            Console.ReadLine();
        }
    }
}
