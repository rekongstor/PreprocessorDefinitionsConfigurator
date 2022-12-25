using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PreprocessorDefinitionsConfigurator
{
   public class DefineCheckboxVM : System.ComponentModel.INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private bool _isSet;
      private string _defineName;

      public DefineCheckboxVM(string defineName)
      {
         _defineName = defineName;
      }

      public bool isSet { get => _isSet; set {
            _isSet = value;
            OnPropertyChanged(nameof(isSet));
         } }

      public string defineName { get => _defineName; set {
            _defineName = value;
            OnPropertyChanged(nameof(defineName));
         }
      }
   }
}
