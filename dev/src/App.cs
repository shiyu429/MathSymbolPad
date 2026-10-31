using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

[assembly: System.Reflection.AssemblyTitle("Math Symbol Pad")]
[assembly: System.Reflection.AssemblyProduct("Math Symbol Pad")]
[assembly: System.Reflection.AssemblyDescription("Floating math symbol input tool for Windows")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace MathSymbolPad {
    public static class Program {
        [STAThread] public static int Main(string[] args){
            bool created;bool testing=args.Contains("--test-mode");string suffix=testing?"Test":"Main";
            using(var mutex=new Mutex(true,@"Local\MathSymbolPad_"+suffix,out created)){
                if(!created){try{using(var signal=EventWaitHandle.OpenExisting(@"Local\MathSymbolPad_Show_"+suffix))signal.Set();}catch{}return 0;}
                using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\MathSymbolPad_Show_"+suffix)){
                    try{
                        string dir=Argument(args,"--settings-dir")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MathSymbolPad");
                        var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};
                        var window=new PadWindow(new SettingsStore(dir),!testing);app.MainWindow=window;
                        string preview=Argument(args,"--preview");
                        if(preview!=null){
                            if(args.Contains("--appearance-preview"))window.SetAppearance(true,false);
                            double expansion;if(double.TryParse(Argument(args,"--preview-morph"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out expansion))window.SetPreviewExpansion(expansion);
                            window.RenderPreview(preview);window.Close();return 0;
                        }
                        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(s,e)=>{if(signal.WaitOne(0))window.Reveal();};timer.Start();
                        app.Run(window);timer.Stop();return 0;
                    }catch(Exception ex){try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log"),DateTime.Now.ToString("s")+" "+ex+Environment.NewLine);}catch{}MessageBox.Show("助手未能启动："+ex.Message,"数学符号助手");return 1;}
                    finally{try{mutex.ReleaseMutex();}catch{}}
                }
            }
        }
        static string Argument(string[] args,string name){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
    }
}
