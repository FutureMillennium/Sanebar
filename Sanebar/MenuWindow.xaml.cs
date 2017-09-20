using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sanebar
{
    /// <summary>
    /// Interaction logic for MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
		internal WindowInteropHelper this32;

		public MenuWindow()
        {
            InitializeComponent();
        }

		private void quitButton_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.Hide();
			e.Cancel = true;
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			this.Hide();
		}

		private void hideCheckbox_Click(object sender, RoutedEventArgs e)
		{
			SanebarWindow.primarySanebarWindow.ExceptionChange();
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			this32 = new WindowInteropHelper(this);
		}
	}
}
