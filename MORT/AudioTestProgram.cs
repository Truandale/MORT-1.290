using System;
using System.Windows.Forms;

namespace MORT
{
    class AudioTestProgram
    {
        // Тестовая точка входа для отладки аудио устройств
        // Переименована чтобы избежать конфликта с основным Main()
        [STAThread]
        static void TestMain()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AudioDeviceTestForm());
        }
    }
}
