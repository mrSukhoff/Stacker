using System.Runtime.InteropServices;
using System.Text;

namespace Stacker.Model
{
    //Компонен не мой.
    internal class INIManager
    {
        //Конструктор, принимающий путь к INI-файлу
        internal INIManager(string aPath)
        {
            path = aPath;
        }

        //Возвращает значение из INI-файла (по указанным секции и ключу) 
        internal string GetPrivateString(string aSection, string aKey)
        {
            //Для получения значения
            StringBuilder buffer = new StringBuilder(SIZE);

            //Получить значение в buffer
            GetPrivateString(aSection, aKey, null, buffer, SIZE, path);

            //Вернуть полученное значение
            return buffer.ToString();
        }
 
        //Поля класса
        private const int SIZE = 1024; //Максимальный размер (для чтения значения из файла)
        private string path = null; //Для хранения пути к INI-файлу

        //Импорт функции GetPrivateProfileString (для чтения значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);
    }
}
