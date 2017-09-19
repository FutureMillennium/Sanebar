using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        Button[,] buttons;
		string[,] actions;
		Brush hoverBrush = new SolidColorBrush(Color.FromArgb(0x4C, 0xFF, 0xFF, 0xFF));
		int lastX, lastY;

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

            buttons = new Button[3, 3];
			actions = new string[3, 3];

			for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
					if (Properties.Settings.Default.QuickLaunch != null)
						actions[i, j] = Properties.Settings.Default.QuickLaunch[j * 3 + i];

					buttons[i, j] = new Button
                    {
                        //Content = $"{i}{j}",
						//Style = (Style)FindResource("SBButtonStyle"),
						Background = Brushes.Transparent,
						Foreground = Brushes.White,
						BorderThickness = new Thickness(0),
					};

					if (actions[i, j] != null)
						buttons[i, j].Content = actions[i, j]; // @TODO icon

					iconGrid.Children.Add(buttons[i,j]);
                    Grid.SetRow(buttons[i, j], j);
                    Grid.SetColumn(buttons[i, j], i);
                }
            
        }

		private void Window_DragOver(object sender, DragEventArgs e)
		{
			//if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				if (e.AllowedEffects == DragDropEffects.Link)
					e.Effects = DragDropEffects.Link;
				else
					e.Effects = DragDropEffects.Copy;
				e.Handled = true;
			}

			ButtonHoverOnMouseMove(e.GetPosition(this));
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			SanebarWindow.hideQuickLaunchTimer.Stop();
		}

		private void Window_DragLeave(object sender, DragEventArgs e)
		{
			SanebarWindow.hideQuickLaunchTimer.Start();

			//ButtonHoverOnMouseMove(new Point(-1, -1));
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Right)
			{
				if (lastX != -1 && lastY != -1 && actions[lastX, lastY] != null)
				{
					try
					{
						Process.Start(actions[lastX, lastY]);
					}
					catch (Exception ex)
					{
						new Thread(() => {
							MessageBox.Show(ex.Message, "Error – " + SanebarWindow.APP_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
						}).Start();
					}
				}
			}

			this.Hide();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] files = null;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				files = (string[])e.Data.GetData(DataFormats.FileDrop);
			else if (e.Data.GetDataPresent(DataFormats.UnicodeText))
				files = new string[1] { (string)e.Data.GetData(DataFormats.UnicodeText) };
			// else // @TODO error?

			if (files != null && files.Length > 0)
			{
				if (lastX != -1 && lastY != -1)
				{
					actions[lastX, lastY] = files[0];
					buttons[lastX, lastY].Content = files[0]; // @TODO
				}
			}

			this.Hide();
		}

		void ButtonHoverOnMouseMove(Point pos)
		{
			int x, y;

			if (pos.X < 0)
				x = -1;
			else if (pos.X >= this.Width)
				x = -1;
			else
				x = (int)Math.Floor(pos.X / this.Width * 3);

			if (pos.Y < 0)
				y = -1;
			else if (pos.Y >= this.Height)
				y = -1;
			else
				y = (int)Math.Floor(pos.Y / this.Height * 3);

			if (x != -1 && y != -1)
				buttons[x, y].Background = hoverBrush;

			if (lastX != x || lastY != y)
			{
				if (lastX != -1 && lastY != -1)
					buttons[lastX, lastY].Background = Brushes.Transparent;

				lastX = x;
				lastY = y;
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			WinAPI.BlurBehind(this);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.QuickLaunch = new System.Collections.Specialized.StringCollection();
			for (int j = 0; j < actions.GetLength(1); j++)
				for (int i = 0; i < actions.GetLength(0); i++)
					Properties.Settings.Default.QuickLaunch.Add(actions[i, j]);

			Properties.Settings.Default.Save();
		}

		private void Window_MouseMove(object sender, MouseEventArgs e)
		{
			ButtonHoverOnMouseMove(e.GetPosition(this));
		}
	}
}
