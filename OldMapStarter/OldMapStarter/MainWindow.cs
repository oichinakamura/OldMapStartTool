using System;
using System.Windows;
using System.Windows.Controls;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OldMapStarter
{
    public class MainWindow : Window
    {
        private StartupConfig startupConfig;
        private ListBox list;

        public MainWindow()
        {
            var panel = new DockPanel()
            {
            };
            this.Content = panel;

            {
                var messA = new TextBlock() { FontSize = 14, TextAlignment = TextAlignment.Center, Foreground = System.Windows.Media.Brushes.Black, Background = System.Windows.Media.Brushes.White, TextWrapping = TextWrapping.Wrap };
                messA.Text = $"システムの更新";
                DockPanel.SetDock(messA, Dock.Top);
                panel.Children.Add(messA);
            }


            var ignore = new ButtonEx(Ignore_Click) { Content = "無視", ClickMode = ClickMode.Press };
            var cancel = new ButtonEx(Cancel_Click) { Content = "キャンセル", ClickMode = ClickMode.Press };

            var bottomBar = new BottomBar() {Orientation=Orientation.Horizontal };
            DockPanel.SetDock(bottomBar, Dock.Bottom);

            bottomBar.Children.Add(ignore); 
            bottomBar.Children.Add(cancel);
            panel.Children.Add(bottomBar);

            var progress = new ProgressView() { };
            DockPanel.SetDock(progress, Dock.Bottom);
            panel.Children.Add(progress);


            var st = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            startupConfig = new StartupConfig(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            if (startupConfig.LocalConfigExists())
            {

                startupConfig.LocalLoad();
                startupConfig.ProgBar = progress;
                var serverConfigPath = System.IO.Path.Combine(startupConfig.ServerPath, "StartupConfig.xml");

                if (startupConfig.ServerConfigExists())
                {

                    if (startupConfig.CompareConfigFile() < 0)
                    {
                        var mess = new TextBlock() { MinHeight = 80, Foreground = System.Windows.Media.Brushes.White, TextWrapping = TextWrapping.Wrap };
                        mess.Text = $"システムの更新または最新のデータがサーバーにあります。\n※更新にはしばらくかかります。\n<古いファイル：{startupConfig.LocalConfigLastWrite}>\n<新しいファイル：{startupConfig.ServerConfigLastWrite}>\n ダウンロードしますか？";
                        DockPanel.SetDock(mess, Dock.Top);
                        panel.Children.Add(mess);

                        var update = new ButtonEx(Update_Click) { Content = "読み込み", ClickMode = ClickMode.Press };
                        DockPanel.SetDock(update, Dock.Top);
                        panel.Children.Add(update);

                        list = new ListBox();
                        panel.Children.Add(list);
                    }
                    else
                    {
                        startupConfig.Execute();
                        Environment.Exit(0);
                    }
                }
                else
                {
                    MessageBox.Show("サーバーに[StartupConfig.xml]が見つかりません", "起動の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
            }
            else
            {
                MessageBox.Show("StartupConfig.xmlが見つかりません", "起動の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }


        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            startupConfig.ServerLoad();
            startupConfig.UpdateFiles(list);

            startupConfig.LocalSave();
            MessageBox.Show("更新が完了しました。");

            startupConfig.Execute();
            Environment.Exit(0);
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            startupConfig.Execute();
            Environment.Exit(0);
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }


        [STAThread]
        static void Main(string[] args)
        {
            var app = new Application();
            app.Run(new MainWindow
            {
                Title = "サンプル",
                Width = 300,
                Height = 300,
                Background = System.Windows.Media.Brushes.Navy,
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            });
        }
    }

    public class ProgressView : DockPanel
    {
        private TextBlock textStatus;
        private ProgressBar progressBar;
        public ProgressView()
        {
            textStatus = new TextBlock() {Foreground=System.Windows.Media.Brushes.White };
            DockPanel.SetDock(textStatus, Dock.Left);
            Children.Add(textStatus);
            progressBar = new ProgressBar { Maximum = 100, Minimum = 0 };
            Children.Add(progressBar);
            
        }

        public string FileName
        {
            set
            {
                textStatus.Text = value;
                DoEvents();
            }
        }

        public double Value
        {
            get => progressBar.Value;
            set
            {
                DoEvents();
                progressBar.Value = value;
            }

        }
        private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
              new DispatcherOperationCallback(ExitFrames), frame);
            Dispatcher.PushFrame(frame);
        }

        private object ExitFrames(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }
    }

    public class ButtonEx : Button
    {
        public ButtonEx(RoutedEventHandler eventHandler)
        {
            Click += eventHandler;

        }

    }

    public class BottomBar : StackPanel
    {
        public BottomBar()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            SizeChanged += BottomBar_SizeChanged;
        }

        private void BottomBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Children.Count > 0)
            {
                var w = ActualWidth / Children.Count;
                foreach (Control ctrl in Children)
                {
                    ctrl.Width = w;
                }
            }
        }


    }
}