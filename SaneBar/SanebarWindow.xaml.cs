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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sanebar
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class SanebarWindow : Window
	{
		SanebarWindow[] sanebarWindows;

		public SanebarWindow() : this(true)
		{
			
		}

		public SanebarWindow(bool primary = true)
		{
			if (primary)
			{
				sanebarWindows = new SanebarWindow[System.Windows.Forms.Screen.AllScreens.Length];
				sanebarWindows[0] = this;
				int i = 1;

				foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
				{
					if (screen.Primary == true)
					{
						this.Top = screen.WorkingArea.Top;
						this.Left = screen.WorkingArea.Left;
						//this.Width = screen.WorkingArea.Right - screen.WorkingArea.Left;
						this.Width = 1920;
					}
					else
					{
						sanebarWindows[i] = new SanebarWindow(false);
						sanebarWindows[i].Top = screen.WorkingArea.Top;
						sanebarWindows[i].Left = screen.WorkingArea.Left;
						sanebarWindows[i].Width = screen.WorkingArea.Right - screen.WorkingArea.Left;
						sanebarWindows[i].Show();
						i++;
					}
				}
			}
			this.Height = SystemParameters.CaptionHeight;

			InitializeComponent();
		}
	}
}
