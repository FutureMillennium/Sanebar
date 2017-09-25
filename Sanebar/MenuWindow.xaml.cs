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
			this.Title = App.APP_TITLE;

			var ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
			string verS = ver.Major + "." + ver.Minor;
			if (ver.Revision != 0)
				verS += "." + ver.Revision;
			versionRun.Text = verS;

			sanebarIconImage.Source = WinAPI.ToImageSource(Properties.Resources.Sanebar);
			// @TODO find a way to display this at the highest resolution (256×256)
			// I refuse to have the same file multiple times in the executable

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

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
		}

		private void hideButton_Click(object sender, RoutedEventArgs e)
		{
			SanebarWindow.primarySanebarWindow.HideAllToggle();
			this.Hide();
		}
		
		internal void Prepare()
		{
			if (SanebarWindow.isHidden)
			{
				if (hideCheckbox.Visibility != Visibility.Collapsed)
				{
					hideCheckbox.Visibility = Visibility.Collapsed;
					this.UpdateLayout();
				}
				hideButton.Content = "Show";
			}
			else
			{
				hideCheckbox.Visibility = Visibility.Visible;
				hideButton.Content = "Hide";
			}
		}
	}
}
