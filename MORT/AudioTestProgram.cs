using System;
using System.Windows.Forms;

namespace MORT
{
    class AudioTestProgram
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AudioDeviceTestForm());
        }
    }
}
