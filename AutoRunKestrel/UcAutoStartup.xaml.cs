using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Microsoft.Win32;

namespace Client.Components
{
    /// <summary>
    /// 随系统自启动
    /// 
    /// V 1.0.0 - 2021-07-23 17:31:52
    /// 首次创建
    /// </summary>
    public partial class UcAutoStartup : UserControl, System.ComponentModel.INotifyPropertyChanged, System.ComponentModel.IDataErrorInfo
    {
        // TODO 仍有改进空间 例如增加一个 FlatStyle 按钮 ( 以管理员身份运行 盾牌型图标 )

        private static readonly object _LOCK_ = new object();

        /// <summary>
        /// 注册表路径 - 随系统自启动
        /// </summary>
        public string RegistryKeyName
        {
            get { return @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; }
        }

        public UcAutoStartup()
        {
            InitializeComponent();
            // check();
            this.Loaded += UcAutoStartup_Loaded;
        }

        private void UcAutoStartup_Loaded(object sender, RoutedEventArgs e)
        {
            check();

            if (IsRunAsAdministrator)
            {
                // 手工进行绑定
                // IsChecked = "{Binding ElementName=thisUc, Path=IsAutoStartup, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnDataErrors=True, NotifyOnValidationError=True}"
                Binding binding = new Binding(path: "IsAutoStartup");

                binding.ElementName = "thisUc";
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                binding.ValidatesOnDataErrors = true;
                binding.NotifyOnValidationError = true;

                this.cb.SetBinding(CheckBox.IsCheckedProperty, binding);
            }
        }

        #region [DP] Key

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register
        (
            name: "Key",
            propertyType: typeof(string),
            ownerType: typeof(UcAutoStartup),
            validateValueCallback: null, // new ValidateValueCallback((toValidate) => { return true; }),
            typeMetadata: new PropertyMetadata
            (
                defaultValue: null,
                propertyChangedCallback: null,
                coerceValueCallback: null
            )
        );

        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        #endregion

        #region [DP] Title        

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register
        (
            name: "Title",
            propertyType: typeof(string),
            ownerType: typeof(UcAutoStartup),
            validateValueCallback: null, // new ValidateValueCallback((toValidate) => { return true; }),
            typeMetadata: new PropertyMetadata
            (
                defaultValue: "随系统自启动",
                propertyChangedCallback: null,
                coerceValueCallback: null
            )
        );

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        #endregion

        #region IsRunAsAdministrator 及其相关联属性 IsCheckBoxEnabled / IsRunAdminTipsVisibility

        private bool _IsRunAsAdministrator = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        public bool IsRunAsAdministrator
        {
            get { return _IsRunAsAdministrator; }
        }

        public bool IsCheckBoxEnabled
        {
            get
            {
                return IsRunAsAdministrator;
            }
        }

        public Visibility IsRunAdminTipsVisibility
        {
            get
            {
                return IsRunAsAdministrator ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        #endregion

        private bool _IsAutoStartup;
        public bool IsAutoStartup
        {
            get { return _IsAutoStartup; }
            set
            {
                _IsAutoStartup = value;
                this.OnPropertyChanged(nameof(IsAutoStartup));
            }
        }

        string _ExePath;
        string mExePath
        {
            get
            {
                if (string.IsNullOrEmpty(_ExePath))
                {
                    lock (_LOCK_)
                    {
                        if (string.IsNullOrEmpty(_ExePath))
                        {
                            _ExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        }
                    }
                }

                return _ExePath;
            }
        }

        System.IO.FileInfo _ExeFileInfo;
        System.IO.FileInfo mExeFileInfo
        {
            get
            {
                if (_ExeFileInfo == null)
                {
                    lock (_LOCK_)
                    {
                        if (_ExeFileInfo == null)
                        {
                            _ExeFileInfo = new System.IO.FileInfo(mExePath);
                        }
                    }
                }

                return _ExeFileInfo;
            }
        }

        void check()
        {
            if (string.IsNullOrWhiteSpace(Key) == true)
            {
                throw new Exception("请设置 UcAutoStartup (开机自动启动控件) 的 Key 属性");
            }

            try
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(RegistryKeyName); // Open 无需管理员权限
                object o = registryKey.GetValue(name: Key);

                if (o != null && o is string k)
                {
                    #region 去掉一头一尾的引号

                    if (k.IndexOf('"') == 0)
                    {
                        k = k.Substring(1);
                    }

                    if (k.IndexOf('"') == (k.Length - 1))
                    {
                        k = k.Substring(0, k.Length - 1);
                    }

                    #endregion 

                    var f0 = new System.IO.FileInfo(k);
                    if (f0.FullName == mExeFileInfo.FullName)
                    {
                        if (IsRunAsAdministrator)
                        {
                            this.IsAutoStartup = true;
                        }
                        else
                        {
                            this.cb.IsChecked = true;
                        }
                        return;
                    }
                }

                if (IsRunAsAdministrator)
                {
                    this.IsAutoStartup = false;
                }
                else
                {
                    this.cb.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                string msg = $"{ex}";
                System.Diagnostics.Debug.WriteLine(msg);
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif
            }
        }

        void set(bool value)
        {
            using (RegistryKey registryKey = Registry.LocalMachine.CreateSubKey(RegistryKeyName))
            {
                if (value)
                {
                    registryKey.SetValue(Key, $"\"{mExeFileInfo.FullName}\"");
                }
                else
                {
                    if (registryKey.GetValue(Key) != null)
                    {
                        registryKey.DeleteValue(Key);
                    }
                }
            }
        }

        void btnReload_Click(object sender, RoutedEventArgs e)
        {
            check();
        }

        #region INotifyPropertyChanged成员

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDataError

        public bool IsValidated
        {
            get
            {
                return this.ErrorCollection.Count == 0;
            }
        }

        public string Error
        {
            get
            {
                return $"共 {this.ErrorCollection.Values.Count} 个错误";
            }
        }

        public System.Collections.Generic.Dictionary<string, string> ErrorCollection { get; private set; } = new System.Collections.Generic.Dictionary<string, string>();

        protected void executeErrorCollection(string columnName, string errorMsg)
        {
            if (ErrorCollection.ContainsKey(columnName))
            {
                if (string.IsNullOrWhiteSpace(errorMsg))
                {
                    ErrorCollection.Remove(columnName);
                }
                else
                {
                    ErrorCollection[columnName] = errorMsg;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(errorMsg) == false)
                {
                    ErrorCollection.Add(columnName, errorMsg);
                }
            }

            this.OnPropertyChanged("ErrorCollection");
            this.OnPropertyChanged("Error");
            this.OnPropertyChanged("IsValidated");
        }

        /// <summary>
        /// 判断errorMessage, 若符合条件则添加到 validationResultList 中
        /// </summary>
        /// <param name="validationResultList"></param>
        /// <param name="errorMessage"></param>
        protected void addValidationResult
        (
            ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResultList,
            string errorMessage
        )
        {
            if (string.IsNullOrEmpty(errorMessage) == false)
            {
                validationResultList.Add(new System.ComponentModel.DataAnnotations.ValidationResult(errorMessage));
            }
        }

        public string this[string columnName]
        {
            get
            {
                string errorMsg = null;

                // Step 1 根据 System.ComponentModel.DataAnnotations 进行校验
                var vc = new System.ComponentModel.DataAnnotations.ValidationContext(this, null, null);
                vc.MemberName = columnName;
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                System.ComponentModel.DataAnnotations.Validator.TryValidateProperty
                (
                    value: this.GetType().GetProperty(columnName).GetValue(this, null),
                    validationContext: vc,
                    validationResults: validationResults
                );

                // Step 2 根据额外编写的校验逻辑进行校验
                switch (columnName)
                {
                    case nameof(IsAutoStartup):
                        this.checkIsAutoStartup(validationResults);
                        break;

                    default:
                        break;
                }


                if (validationResults.Count > 0)
                {
                    errorMsg = string.Join("; ", validationResults);
                }

                executeErrorCollection(columnName, errorMsg);

                return errorMsg;
            }
        }

        void checkIsAutoStartup(List<System.ComponentModel.DataAnnotations.ValidationResult> l)
        {
            if (IsRunAsAdministrator == true)
            {
                try
                {
                    set(this.IsAutoStartup);
                }
                catch (Exception ex)
                {
                    l.Add(new System.ComponentModel.DataAnnotations.ValidationResult(ex.Message));
                }
            }
        }

        #endregion
    }
}




