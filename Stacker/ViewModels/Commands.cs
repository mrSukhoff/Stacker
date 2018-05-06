using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Stacker.ViewModels
{
    class Commands : RoutedUICommand

    {
        public Commands()
        {
        }

        public static RoutedUICommand BringCommand = new RoutedUICommand();
    }
}
