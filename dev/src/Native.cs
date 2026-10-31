using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MathSymbolPad {
    public static class Native {
        public const int WM_MOUSEACTIVATE=0x21, WM_POINTERACTIVATE=0x024B, MA_NOACTIVATE=3;
        public const int GWL_EXSTYLE=-20, WS_EX_NOACTIVATE=0x08000000, WS_EX_TOOLWINDOW=0x80;
        public const uint SWP_NOACTIVATE=0x10, SWP_NOSIZE=1;
        public static readonly IntPtr HWND_TOPMOST=new IntPtr(-1);
        [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; public int Width {get{return Right-Left;}} public int Height {get{return Bottom-Top;}} }
        [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
        [StructLayout(LayoutKind.Sequential)] public struct GuiInfo { public int Size,Flags;public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret;public Rect CaretRect; }
        [StructLayout(LayoutKind.Sequential)] public struct KeyInput { public ushort Vk,Scan;public uint Flags,Time;public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MouseInput { public int X,Y;public uint MouseData,Flags,Time;public UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit)] public struct InputUnion { [FieldOffset(0)]public KeyInput Key; [FieldOffset(0)] public MouseInput Mouse; }
        [StructLayout(LayoutKind.Sequential)] public struct Input { public uint Type;public InputUnion Data; }
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
        [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint thread,ref GuiInfo info);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hwnd,int index);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hwnd,int index,int value);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll",SetLastError=true)] public static extern uint SendInput(uint count,Input[] inputs,int size);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(Point point,uint flags);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd,StringBuilder text,int count);
        public static double Scale(IntPtr hwnd) { try{return Math.Max(1,GetDpiForWindow(hwnd)/96.0);}catch{return 1;} }
        public static double ScaleAt(int x,int y) {try{uint dx,dy; if(GetDpiForMonitor(MonitorFromPoint(new Point{X=x,Y=y},2),0,out dx,out dy)==0)return Math.Max(1,dx/96.0);}catch{}return 1;}
        public static Input Key(ushort scan,bool up) {return new Input{Type=1,Data=new InputUnion{Key=new KeyInput{Scan=scan,Flags=4u|(up?2u:0u)}}};}
        public static Input VirtualKey(ushort key,bool up) {return new Input{Type=1,Data=new InputUnion{Key=new KeyInput{Vk=key,Flags=(up?2u:0u)|(key==0x25?1u:0u)}}};}
        public static Input[] BuildInputs(Symbol symbol) {
            if(symbol.CaretIndex<0||symbol.CaretIndex>symbol.Text.Length)throw new ArgumentException("模板光标位置无效");
            var list=new List<Input>();
            foreach(char c in symbol.Text){if(char.IsControl(c))throw new ArgumentException("符号不可包含控制键");list.Add(Key(c,false));list.Add(Key(c,true));}
            for(int i=symbol.Text.Length;i>symbol.CaretIndex;i--){list.Add(VirtualKey(0x25,false));list.Add(VirtualKey(0x25,true));}
            return list.ToArray();
        }
        public static bool ModifiersDown() {foreach(int k in new[]{0x10,0x11,0x12,0x5B,0x5C})if((GetAsyncKeyState(k)&0x8000)!=0)return true;return false;}
        public static IntPtr InputTarget(out string reason) {
            reason=null;IntPtr target=GetForegroundWindow();uint pid;
            uint thread=GetWindowThreadProcessId(target,out pid);
            if(target==IntPtr.Zero||pid==Process.GetCurrentProcess().Id||!IsWindow(target)||!IsWindowVisible(target)||IsIconic(target)){reason="请先点击要输入的聊天框";return IntPtr.Zero;}
            var gui=new GuiInfo{Size=Marshal.SizeOf(typeof(GuiInfo))};
            if(!GetGUIThreadInfo(thread,ref gui)||gui.Focus==IntPtr.Zero){reason="请先点击要输入的聊天框";return IntPtr.Zero;}
            var klass=new StringBuilder(100);GetClassName(target,klass,klass.Capacity);
            if(klass.ToString()=="Progman"||klass.ToString()=="WorkerW"||klass.ToString()=="Shell_TrayWnd"){reason="请先点击要输入的聊天框";return IntPtr.Zero;}
            return target;
        }
        public static bool Insert(Symbol symbol,IntPtr expected,out string error) {
            error=null;string reason;IntPtr target=InputTarget(out reason);
            if(target==IntPtr.Zero){error=reason;return false;}
            if(target!=expected){error="输入位置已改变，请重新点击符号";return false;}
            if(ModifiersDown()){error="请松开 Ctrl、Alt、Shift 等按键后再点";return false;}
            var inputs=BuildInputs(symbol);
            if(GetForegroundWindow()!=expected){error="输入位置已改变，请重新点击符号";return false;}
            uint sent=SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(Input)));
            if(sent!=inputs.Length){error="未能完整输入，请检查输入框和软件权限";return false;}
            return true;
        }
    }
}
