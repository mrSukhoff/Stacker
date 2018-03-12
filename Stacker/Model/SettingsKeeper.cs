using System;
using System.Windows;

namespace Stacker.Model
{
    public class SettingsKeeper
    {
        //orders
        //места хранения файлов заявлок и архива
        public string OrdersFile { get; }
        public string ArchiveFile { get; }
        public string WrongOrdersFile { get; }
        //интервал чтения заявок
        public ushort ReadingInterval { get; }

        //закрыть при ошибке открытия порта
        public bool CloseOrInform { get; }
        
       
        
        //имя порта, к которому подключен контроллер
        public string ComPort { get; }

        //размеры штабелера
        public ushort StackerDepth { get; } = 29;
        public ushort StackerHight { get; } = 16;
        
        //Максимальные значения координат
        public int MaxX { get; } = 55000;
        public int MaxY { get; } = 14000;

        //имена стеллажей
        public char LeftRackName { get; }
        public char RightRackName { get; }

        //имя файла с координатами ячеек
        public string CellsFile { get; }
        
        //Максимальный вес груза
        public UInt16 MaxWeight { get; }
        
        //view
        //показывать вкладку "взвесить"
        public bool ShowWeightTab { get; }

        //коэффициенты для пересчета тока ПЧ в вес
        public UInt16 WeightAlpha1 { get; }
        public UInt16 WeightBeta1 { get; }
        public UInt16 WeightAlpha2 { get; }
        public UInt16 WeightBeta2 { get; }
        
        //Читаем настройки
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
                CellsFile = manager.GetPrivateString("General", "CellsFile");
                CloseOrInform = Convert.ToBoolean(manager.GetPrivateString("General", "CloseOrInform"));
                ShowWeightTab = Convert.ToBoolean(manager.GetPrivateString("General", "ShowWeightTab"));
                ReadingInterval = Convert.ToUInt16(manager.GetPrivateString("General", "ReadingInterval"));

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

                manager = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SettingsKeeper");
            }
        }
    }

}
