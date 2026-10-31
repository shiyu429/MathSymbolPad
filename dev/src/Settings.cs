using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace MathSymbolPad {
    public sealed class Preferences {
        public int Version=2;
        public int? X,Y;
        public bool Collapsed=false;
        public double BackgroundOpacity=0.86;
        public string Theme="system";
        public int Page=0;
        public void Normalize(){BackgroundOpacity=double.IsNaN(BackgroundOpacity)?0.86:Math.Max(0.70,Math.Min(0.95,BackgroundOpacity));if(Theme!="light"&&Theme!="dark")Theme="system";Page=Math.Max(0,Math.Min(Catalog.Pages.Length-1,Page));}
    }
    public sealed class SettingsStore {
        public readonly string DirectoryPath;
        public string FilePath {get{return Path.Combine(DirectoryPath,"settings.json");}}
        readonly JavaScriptSerializer json=new JavaScriptSerializer();
        public SettingsStore(string dir){DirectoryPath=dir;}
        public Preferences Read(){try{var p=json.Deserialize<Preferences>(File.ReadAllText(FilePath,Encoding.UTF8));p.Normalize();return p;}catch{return new Preferences();}}
        public bool Write(Preferences prefs){
            try{prefs.Normalize();Directory.CreateDirectory(DirectoryPath);string temp=FilePath+".tmp";File.WriteAllText(temp,json.Serialize(prefs),new UTF8Encoding(false));if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);return true;}catch{return false;}
        }
    }
    public static class Placement {
        public static Native.Rect Clamp(Native.Rect area,int x,int y,int width,int height){
            width=Math.Max(1,Math.Min(width,area.Width));height=Math.Max(1,Math.Min(height,area.Height));
            x=Math.Max(area.Left,Math.Min(x,area.Right-width));y=Math.Max(area.Top,Math.Min(y,area.Bottom-height));
            return new Native.Rect{Left=x,Top=y,Right=x+width,Bottom=y+height};
        }
    }
}
