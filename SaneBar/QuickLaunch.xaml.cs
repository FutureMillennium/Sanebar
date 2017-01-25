using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace Sanebar
{
	/// <summary>
	/// Interaction logic for QuickLaunch.xaml
	/// </summary>
	public partial class QuickLaunch : Window
	{
        Grid iconGrid;

		public QuickLaunch()
		{
			InitializeComponent();

            iconGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                },
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition(),
                },
            };

            grid.Children.Add(iconGrid);
            
        }

		private void Window_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Link;
				e.Handled = true;
			}
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			SanebarWindow.hideQuickLaunchTimer.Stop();
		}

		private void Window_DragLeave(object sender, DragEventArgs e)
		{
			SanebarWindow.hideQuickLaunchTimer.Start();
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			this.Hide();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			this.Hide();
		}
	}
}
