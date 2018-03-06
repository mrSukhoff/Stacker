using Modbus.Device;
using System;
using System.IO.Ports;

namespace Stacker.Model
{
    internal class Controller:IDisposable
    {
        //Com-порт к которому подсоединен контроллер
        private SerialPort ComPort = null;

        //интерфейс контроллера
        private IModbusMaster PLC;

        private bool disposed;
        internal Controller(string port)
        {
            //создаем порт
            ComPort = new SerialPort(port, 115200, Parity.Even, 7, StopBits.One);
            //открываем его
            ComPort.Open();
            //создаем modbus-устройство
            PLC = ModbusSerialMaster.CreateAscii(ComPort);
        }

        ~Controller() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            // подавляем финализацию
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    PLC?.Dispose();
                    ComPort?.Dispose();
                }
                disposed = true;
            }
        }

        internal ushort[] ReadHoldingRegisters(ushort address, ushort num)
        {
            return PLC.ReadHoldingRegisters(1, address, num);
        }

        //Записывает 32-битное число в контроллер
        internal bool WriteDword(int adr, int d)
        {
            ushort dlo = (ushort)(d % 0x10000);
            ushort dhi = (ushort)(d / 0x10000);
            UInt16 address = Convert.ToUInt16(adr);
            address += 0x1000;
            PLC.WriteSingleRegister(1, address, dlo);
            PLC.WriteSingleRegister(1, ++address, dhi);
            return true;
        }

        //Читает 32-битное число из контроллера
        internal bool ReadDword(ushort address, out int d)
        {
            d = 0;
            address += 0x1000;
            ushort[] x = PLC.ReadHoldingRegisters(1, address, 2);
            d = x[0] + x[1] * 0x10000;
            return true;
        }

        //метод читает меркер из ПЛК
        internal bool ReadMerker(ushort address, out bool m)
        {
            bool[] ms;
            address += 0x800;
            ms = PLC.ReadCoils(1, address, 1);
            m = ms[0];
            return true;
        }

        //метод устанавливает меркер в ПЛК
        internal bool SetMerker(ushort address, bool m)
        {
            address += 0x800;
            PLC.WriteSingleCoil(1, address, m);
            return true;
        }
    }
}
