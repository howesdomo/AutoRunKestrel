using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = $"AutoRunKestrel - V {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
            this.initEvent();
        }
        void initEvent()
        {
            this.Loaded += Frm_Loaded;
            this.Closing += Frm_Closing;
        }
        void Frm_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ViewModel.MainWindow_ViewModel)
            {
                var vm = this.DataContext as ViewModel.MainWindow_ViewModel;
                vm.mOwner = this;
                vm.mUcConsole = this.ucConsole;
                vm.initNotifyIcon();
                vm.Run();
            }

            this.NewHide();
        }

        void Frm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.NewHide();
        }

        public void NewHide()
        {
            this.ShowInTaskbar = false;
            this.Hide();
        }

        public void NewShow()
        {
            this.ShowInTaskbar = true;
            this.Show();
        }
    }
}

namespace Client.ViewModel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class MainWindow_ViewModel : BaseViewModel
    {
        public MainWindow mOwner { get; set; }

        public ListBox mUcConsole { get; set; }

        /// <summary>
        /// 任务栏图标
        /// </summary>
        System.Windows.Forms.NotifyIcon NotifyIcon { get; set; }

        public void initNotifyIcon()
        {
            #region 设置任务栏图标

            var menuExit = new System.Windows.Forms.MenuItem("退出", (s, e) =>
            {
                if (mProcess != null)
                {
                    this.Stop();
                }

                NotifyIcon.Visible = false;
                Application.Current.Shutdown();
            });

            var menuSetting = new System.Windows.Forms.MenuItem("设置", (s, e) =>
            {
                if (mOwner.WindowState == WindowState.Minimized)
                {
                    mOwner.WindowState = WindowState.Normal;
                }
                mOwner.NewShow();
            });

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[]
            {
                menuSetting,
                menuExit
            });

            NotifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Images/LOGO.ico", UriKind.RelativeOrAbsolute)).Stream),
                ContextMenu = menu,
                Visible = true
            };

            #endregion
        }

        public MainWindow_ViewModel()
        {
            initCMD();
        }

        void initCMD()
        {
            this.CMD_Run = new Command(Run);
            this.CMD_Stop = new Command(Stop);
        }

        private bool _IsRunning;
        public bool IsRunning
        {
            get { return _IsRunning; }
            set
            {
                _IsRunning = value;
                this.OnPropertyChanged(nameof(IsRunning));
            }
        }

        public Command CMD_Run { get; private set; }
        public void Run()
        {
            this.IsRunning = true;
            Task.Factory.StartNew(taskWebServer);
        }

        Process mProcess { get; set; }

        StreamWriter mInputWriter { get; set; }

        StreamReader mOutputReader { get; set; }

        void taskWebServer()
        {
            string WebServerDll_FilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebServer.dll");
            var fi = new System.IO.FileInfo(WebServerDll_FilePath);

            // 创建一个新的进程启动信息对象
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet", // dotnet CLI 的可执行文件名
                Arguments = $"exec {fi.FullName}",
                WorkingDirectory = fi.Directory.FullName, // 工作目录 用于获取配置文件
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            // 启动进程，并获取它的标准输入和输出流
            mProcess = Process.Start(startInfo);
            if (mProcess == null)
            {
                Console.WriteLine("未安装.NET");
                this.IsRunning = false;

                AutoRunKestrel.App.BeginInvokeOnMainThread(() =>
                {
                    this.mUcConsole.Items.Add("未安装 .NET");
                    this.IsRunning = false;

                }, System.Windows.Threading.DispatcherPriority.DataBind);
                return;
            }

            // 读取输出流
            mOutputReader = mProcess.StandardOutput;

            // 读取命令提示符的输出
            string line = string.Empty;
            while ((line = mOutputReader.ReadLine()) != null)
            {
                Console.WriteLine(line);

                if (IsAutoRunSuccess == false)
                {
                    if (Regex.IsMatch(line, RunSuccessPattern) == true)
                    {
                        IsAutoRunSuccess = true;
                        using (var w = new WebClient())
                        {
                            w.DownloadStringAsync(new Uri("http://localhost:8081/")); // TODO 配置
                        }
                    }
                }

                AutoRunKestrel.App.BeginInvokeOnMainThread(() =>
                {
                    this.mUcConsole.Items.Add(line);
                }, System.Windows.Threading.DispatcherPriority.DataBind);
            }

            // 等待进程退出
            mProcess.WaitForExit();
            mProcess.Close();
            mProcess = null;
        }

        /// <summary>
        /// 有 Ctrl+C to Shun down 字眼标识服务成功运行
        /// </summary>
        const string RunSuccessPattern = "Ctrl\\+C"; // !!!遇到的坑!!! + 号需要转换

        private bool _IsAutoRunSuccess;
        public bool IsAutoRunSuccess
        {
            get { return _IsAutoRunSuccess; }
            set
            {
                _IsAutoRunSuccess = value;
                this.OnPropertyChanged(nameof(IsAutoRunSuccess));
            }
        }


        public Command CMD_Stop { get; private set; }
        void Stop()
        {
            if (mProcess == null)
            {
                return;
            }

            try
            {
                mProcess.Kill();
                this.mUcConsole.Items.Add("关闭服务器");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            this.IsRunning = false;
        }
    }
}
