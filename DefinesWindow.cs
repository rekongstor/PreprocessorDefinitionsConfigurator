using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace PreprocessorDefinitionsConfigurator
{
   /// <summary>
   /// This class implements the tool window exposed by this package and hosts a user control.
   /// </summary>
   /// <remarks>
   /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
   /// usually implemented by the package implementer.
   /// <para>
   /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
   /// implementation of the IVsUIElementPane interface.
   /// </para>
   /// </remarks>
   [Guid("e5488938-8f5b-4466-b863-a06df43b3282")]
   public class DefinesWindow : ToolWindowPane, IVsRunningDocTableEvents
   {
      private uint rdtCookie;
      private DefinesWindowControl definesWindowControl;
      /// <summary>
      /// Initializes a new instance of the <see cref="DefinesWindow"/> class.
      /// </summary>
      public DefinesWindow() : base(null)
      {
         this.Caption = "Defines Window";

         // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
         // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
         // the object returned by the Content property.
         this.Content = definesWindowControl = new DefinesWindowControl();
      }

      protected override void Initialize()
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         IVsRunningDocumentTable rdt = (IVsRunningDocumentTable)this.GetService(typeof(SVsRunningDocumentTable));
         rdt.AdviseRunningDocTableEvents(this, out rdtCookie);
      }

      protected override void Dispose(bool disposing)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         // Release the RDT cookie.
         IVsRunningDocumentTable rdt = (IVsRunningDocumentTable)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsRunningDocumentTable));
         rdt.UnadviseRunningDocTableEvents(rdtCookie);

         base.Dispose(disposing);
      }

      public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
      {
         return VSConstants.S_OK;
      }

      public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
      {
         return VSConstants.S_OK;
      }

      public int OnAfterSave(uint docCookie)
      {
         return VSConstants.S_OK;
      }

      public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
      {
         return VSConstants.S_OK;
      }

      private bool ParsePreprocessorDefinesIfStartsWith(string preprocessor, string line, ref List<string> list, int outCount = int.MaxValue)
      {
         if (line.StartsWith(preprocessor)) {
            string[] parsedDefines = line.Substring(preprocessor.Length).Split();
            for (int i = 0; i < Math.Min(parsedDefines.Length, outCount); ++i) {
               string define = parsedDefines[i];
               if (define.Contains("//")) {
                  break;
               }
               define = define.Replace('!', ' ');
               define = define.Replace("defined(", "");
               define = define.Replace(')', ' ');
               // TODO: Parse out comments #define SOMETHING// Commented
               list.Add(define.Trim());
            }
            return true;
         }
         return false;
      }

      private void ParseFile(string path, ref List<string> defines, ref List<string> ifdefs)
      {
         if (path.Contains("pdc-defines.h")) {
            return;
         }
         const String DEFINE = "#define ";
         const String IFDEF = "#ifdef ";
         const String IFNDEF = "#ifndef ";
         const String IF = "#if ";
         const String ELIF = "#elif ";
         const String INCLUDE = "#include ";
         string[] lines = System.IO.File.ReadAllLines(path);
         for (int i = 0; i < lines.Length; ++i) {
            string line = lines[i];
            if (line.Length > 0 && line[0] == '#') { // preprocessor
               if (ParsePreprocessorDefinesIfStartsWith(DEFINE, line, ref defines, 1)) continue;
               if (ParsePreprocessorDefinesIfStartsWith(IFDEF, line, ref ifdefs)) continue;
               if (ParsePreprocessorDefinesIfStartsWith(IFNDEF, line, ref ifdefs)) continue;
               if (ParsePreprocessorDefinesIfStartsWith(IF, line, ref ifdefs)) continue;
               if (ParsePreprocessorDefinesIfStartsWith(ELIF, line, ref ifdefs)) continue;

               if (line.StartsWith(INCLUDE)) { // parse included file
                  string includee = line.Substring(INCLUDE.Length).Trim(new char[] { ' ', '<', '>', '"' });
                  string includeePath = System.IO.Path.GetDirectoryName(path) + "\\" + includee;
                  ParseFile(includeePath, ref defines, ref ifdefs);
               }
            }
         }
      }

      private string lastUsedFileName;

      public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame pFrame)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         EnvDTE.Window win = VsShellUtilities.GetWindowObject(pFrame);
         string ext = Path.GetExtension(win.Document.Name).ToLower();
         if (ext == ".hlsl" && lastUsedFileName != win.Document.Name) {
            lastUsedFileName = win.Document.Name;
            this.Caption = "Defines Window: " + win.Document.Name;

            List<string> defines = new List<string>();
            List<string> ifdefs = new List<string>();

            ParseFile(win.Document.FullName, ref defines, ref ifdefs);
            defines = defines.Distinct().ToList();
            ifdefs = ifdefs.Distinct().ToList();
            // Merge defines and ifdefs
            int deleted = ifdefs.RemoveAll(x => defines.Contains(x));
            definesWindowControl.controlVM.definesList.Clear();
            foreach (var id in ifdefs) {
               definesWindowControl.controlVM.definesList.Add(new DefineCheckboxVM(id));
            }
            definesWindowControl.controlVM.projectName = win.Document.Path;
         }
         return VSConstants.S_OK;
      }

      public int OnAfterDocumentWindowHide(uint docCookie, Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame pFrame)
      {

         return VSConstants.S_OK;
      }
   }
}
