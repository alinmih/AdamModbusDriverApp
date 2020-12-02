using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdamModbusDriver;

namespace AdamModbusDriverClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ModbusTCP modbus = new ModbusTCP("192.168.0.115", 502);

            var conn = modbus.Connected;

            ushort start = 16;
            ushort lenght =2;

            // calculate the number of bytes data needs
            int numBytes = (lenght / 8 + (lenght % 8 > 0 ? 1 : 0));
            byte[] data = new byte[numBytes];
            byte[] multipleCoils = new byte[lenght];
            multipleCoils[0] = 255;
            multipleCoils[1] = 255;

            // write data to device
            byte[] result= new byte[numBytes];
            modbus.WriteMultipleCoils(1, 1, start, lenght, multipleCoils, ref result);
            //modbus.WriteSingleCoil(1, 1, start, false, ref result);

            //read data from device
            modbus.ReadHoldingRegisters(1, 1, start, lenght, ref data);

            // convert data into int value
            int length2 = data.Length / 2 + Convert.ToInt16(data.Length % 2 > 0);
            var word = new int[length2];
            for (int x = 0; x < length2; x++)
            {
                word[x] = data[x * 2] * 256 + data[x * 2 + 1];
                Console.WriteLine(word[x]);
            }




            //convert data into bit value
            BitArray bitArray = new BitArray(data);
            int[] numbers = new int[bitArray.Count];
            for (int i = 0; i < bitArray.Length; i++)
            {
                numbers[i] = bitArray[i] ? 1 : 0;
                Console.WriteLine($"Coil {i} : {numbers[i]}");
            }

            //var data1 = Convert.ToBoolean(Convert.ToByte("3"));

            //var coils = Convert.ToString(data[0],2);
            //var reversed = coils.ToArray();
            //Array.Reverse(reversed);
            //foreach (var item in reversed)
            //{
            //    Console.WriteLine(item);
            //}

            modbus.Disconnect();

            Console.ReadLine();

        }
    }
}
