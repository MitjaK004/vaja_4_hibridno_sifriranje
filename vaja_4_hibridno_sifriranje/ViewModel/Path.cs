using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vaja_4_hibridno_sifriranje.ViewModelNamespace
{
    class Path : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        public Path() { }
        public Path(string val) { Value = val; }
        public string Value {
            get { return _value; }
            set { 
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
