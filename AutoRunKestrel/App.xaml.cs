using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace AutoRunKestrel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {        
        public App()
        {
            if (IsInstanceRunning() == true)
            {
                MessageBox.Show("AutoRunKestrel已开启，请勿重复开启", "提示");
                Environment.Exit(0);
                return;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            #region 注册事件 - 捕获未处理的异常

            // 主线程
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            // 非主线程
            System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            #endregion

            base.OnStartup(e);
        }

        #region 捕获未处理的异常

        /// <summary>
        /// 主线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException("DispatcherUnhandledException", e.Exception);
                e.Handled = true;
            }
            catch (Exception ex)
            {
#if DEBUG
                string msg = $"HandleException 发生异常。{ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg);
                if (System.Diagnostics.Debugger.IsAttached == true)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
                throw ex;
            }
        }

        /// <summary>
        /// 非主线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is System.Exception)
                {
                    HandleException("CurrentDomain.UnhandledException", e.ExceptionObject as System.Exception);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                string msg = $"HandleException 发生异常。{ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg);
                if (System.Diagnostics.Debugger.IsAttached == true)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
                throw ex;
            }
        }

        public static void HandleException(string from, Exception ex)
        {
            MessageBox.Show
            (
                caption: "捕获到以下错误，请与管理员联系以获取帮助。",
                messageBoxText: $"在 {from} 捕获到以下错误\r\n{ex.Message}"
            );

            if (App.Current.MainWindow == null || App.Current.MainWindow.IsLoaded == false)
            {
                Current.Shutdown();
            }
        }

        #endregion

        #region 单个进程

        bool IsInstanceRunning()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess(); //获取当前进程
            string currentFileName = currentProcess.MainModule.FileName;
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
            {
                try
                {
                    if (process.MainModule.FileName == currentFileName && process.Id != currentProcess.Id)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return false;
        }

        #endregion

        #region 在主线程运行

        public static bool IsMainThread
        {
            get
            {
                return System.Threading.Thread.CurrentThread.IsBackground == false;
            }
        }

        public static void BeginInvokeOnMainThread(Action action, System.Windows.Threading.DispatcherPriority dp = System.Windows.Threading.DispatcherPriority.DataBind)
        {
            if (IsMainThread)
            {
                action();
            }
            else
            {
                Current.Dispatcher.BeginInvoke(action, dp);
            }
        }

        #endregion
    }
}
