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
   public class DefinesWindowControlVM : System.ComponentModel.INotifyPropertyChanged
   {
      public DefinesWindowControlVM()
      {
         definesList.ListChanged += DefinesList_ListChanged;
      }

      public event PropertyChangedEventHandler PropertyChanged;


      private void OnPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void DefinesList_ListChanged(object sender, ListChangedEventArgs e)
      {
         StringBuilder defines = new StringBuilder();
         foreach(var d in definesList) {
            if (d.isSet)
            {
               defines.Append("#define " + d.defineName + "\n");
            }
         }

         System.IO.File.WriteAllText("E:\\RoH\\Code\\RoH\\Engine_vs2013\\Layers\\xrRender_R5_Shaders\\pdc-defines.h", defines.ToString());
      }

      public BindingList<DefineCheckboxVM> definesList { get; set; } = new BindingList<DefineCheckboxVM>();
      public string projectName { get; set; }
   }
}
