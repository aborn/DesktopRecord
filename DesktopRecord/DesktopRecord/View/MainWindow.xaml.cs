using DesktopRecord.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DesktopRecord.View
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainVM _vm = new MainVM();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _vm.WaterMaker = textBox.Text;
            }
        }
    }
}
