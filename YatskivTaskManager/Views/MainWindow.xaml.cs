using YatskivTaskManager.ViewModels;
using YatskivTaskManager.Tools;
using System.ComponentModel;

namespace YatskivTaskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow 
    {
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
       
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            StationManager.CloseApp();
        }
    }
}
