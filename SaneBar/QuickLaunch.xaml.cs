using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
	/// Interaction logic for QuickLaunch.xaml
	/// </summary>
	public partial class QuickLaunch : Window
	{
		internal WindowInteropHelper this32;
		internal System.Drawing.Point? prevCursorPosition = null;
		
        Button[,] buttons;
		string[,] actions;
		Brush hoverBrush = new SolidColorBrush(Color.FromArgb(0x4C, 0xFF, 0xFF, 0xFF));
		int lastX, lastY;

		TextBlock emptyMessageTextBlock = null;

		public QuickLaunch()
		{
			bool isEmpty = true;

			InitializeComponent();

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
					{
						SetIcon(i, j);
						isEmpty = false;
					}

					iconGrid.Children.Add(buttons[i,j]);
                    Grid.SetRow(buttons[i, j], j);
                    Grid.SetColumn(buttons[i, j], i);
                }
            
			if (isEmpty)
			{
				emptyMessageTextBlock = new TextBlock()
				{
					Text = "Drag your favourite apps here!",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Padding = new Thickness(15),
					Foreground = Brushes.LightGray,
					Background = SanebarWindow.defaultBackground,
				};
				mainGrid.Children.Add(emptyMessageTextBlock);
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
			if (SanebarWindow.hideQuickLaunchTimer != null)
				SanebarWindow.hideQuickLaunchTimer.Stop();
		}

		private void Window_DragLeave(object sender, DragEventArgs e)
		{
			if (SanebarWindow.hideQuickLaunchTimer != null)
				SanebarWindow.hideQuickLaunchTimer.Start();

			//ButtonHoverOnMouseMove(new Point(-1, -1));
		}

		void Done()
		{
			if (prevCursorPosition != null)
			{
				System.Windows.Forms.Cursor.Clip = new System.Drawing.Rectangle();
				System.Windows.Forms.Cursor.Position = (System.Drawing.Point)prevCursorPosition;

				prevCursorPosition = null;
			}
			this.Hide();
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

			Done();
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
					string fileName = files[0];

					actions[lastX, lastY] = fileName;

					SetIcon(lastX, lastY);

					if (emptyMessageTextBlock != null)
					{
						mainGrid.Children.Remove(emptyMessageTextBlock);
						emptyMessageTextBlock = null;
					}
				}
			}

			Done();
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

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			this32 = new WindowInteropHelper(this);
		}

		private void Window_MouseMove(object sender, MouseEventArgs e)
		{
			ButtonHoverOnMouseMove(e.GetPosition(this));
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Done();
		}

		void SetIcon(int X, int Y)
		{
			string fileName = actions[X, Y];

			var icon = WinAPI.GetFileIcon(fileName);

			if (icon != null)
			{
				FileVersionInfo fileVersionInfo;
				FileAttributes attr = File.GetAttributes(fileName);

				if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				{
					fileVersionInfo = null;
				}
				else
				{
					fileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
				}
				
				string name;

				if (fileVersionInfo == null)
					name = System.IO.Path.GetFileName(fileName);
				else if (fileVersionInfo.FileDescription == null)
					name = System.IO.Path.GetFileNameWithoutExtension(fileName);
				else
					name = fileVersionInfo.FileDescription;

				buttons[X, Y].Content = new StackPanel()
				{
					Children = {
						new Image()
						{
							Source = WinAPI.ToImageSource(icon),
							Stretch = Stretch.None,
						},
						new TextBlock()
						{
							Text = name,
							TextWrapping = TextWrapping.Wrap,
							TextAlignment = TextAlignment.Center,
							Margin = new Thickness(5),
						}
					},
				};
			}
			else
			{
				buttons[X, Y].Content = new TextBlock()
				{
					Text = fileName,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = TextAlignment.Center,
					Margin = new Thickness(5),
				};
			}
		}
	}
}
