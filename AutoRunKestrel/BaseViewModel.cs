using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.ViewModel
{
    /// <summary>
    /// 1.0.0 - 2021-02-03 12:11:01
    /// 由 baseViewModel中 拷贝出一份副本供给 .net framework 4.0 使用
    /// </summary>
    public class BaseViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        #region INotifyPropertyChanged成员

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
