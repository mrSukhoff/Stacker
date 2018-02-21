using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stacker
{
    class SettingsKeeper
    {
        public string  OrdersFile { get; }
        public string ArchiveFile { get; }
        public string WrongOrdersFile { get; }
        public bool CloseOrInform { get; }
        public bool ShowWeightTab { get; }
        public char LeftRackName { get; }
        public char RightRackName { get; }
        public string ComPort { get; }
        public UInt16 WeightAlpha1 { get; }
        public UInt16 WeightBeta1 { get; }
        public UInt16 WeightAlpha2 { get; }
        public UInt16 WeightBeta2 { get; }
        public UInt16 MaxWeight { get; }
        
        //Читаем первоначальные настройки
        public SettingsKeeper()
        {
            string path = Environment.CurrentDirectory + "\\Stacker.ini";
            try
            {
                INIManager manager = new INIManager(path);
                //общие
                OrdersFile = manager.GetPrivateString("General", "OrderFile");
                ArchiveFile = manager.GetPrivateString("General", "ArchiveFile");
                WrongOrdersFile = manager.GetPrivateString("General", "WrongOrdersFile");
                CloseOrInform = Convert.ToBoolean(manager.GetPrivateString("General", "CloseOrInform"));
                ShowWeightTab = Convert.ToBoolean(manager.GetPrivateString("General", "ShowWeightTab"));

                //свойства стеллажей
                LeftRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "LeftRackName"));
                RightRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "RightRackName"));

                //настройки порта
                ComPort = manager.GetPrivateString("PLC", "ComPort");
                
                //настройка весов
                WeightAlpha1 = Convert.ToUInt16(manager.GetPrivateString("Weigh", "alfa1"));
                WeightBeta1 = Convert.ToUInt16(manager.GetPrivateString("Weigh", "beta1"));
                WeightAlpha2 = Convert.ToUInt16(manager.GetPrivateString("Weigh", "alfa2"));
                WeightBeta2 = Convert.ToUInt16(manager.GetPrivateString("Weigh", "beta2"));
                MaxWeight = (UInt16)(Convert.ToUInt16(manager.GetPrivateString("Weigh", "MaxWeight")) * WeightBeta1 / 100 + WeightAlpha1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SettingsKeeper");
            }
        }
    }

}
