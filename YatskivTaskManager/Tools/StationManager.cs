using System;
using System.Windows;

namespace YatskivTaskManager.Tools
{
    internal static class StationManager
    {
        public static event Action KillThreads;

        internal static void CloseApp()
        {
            MessageBox.Show("Closing the app...");
            KillThreads?.Invoke();
            Environment.Exit(1);
        }
    }
}
