using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
using Drawing=System.Drawing;

namespace MathSymbolPad {
    public sealed class PadWindow : Window {
        public const double PanelWidth=344,PanelHeight=330,BallSize=64,PanelRadius=24;
        readonly SettingsStore store;readonly Preferences preferences;
        readonly MorphBorder shell;readonly Grid panel,ball,appearanceView;
        readonly UniformGrid keys;readonly StackPanel tabs;
        readonly Button appearanceButton;readonly TextBlock alphaLabel;
        readonly Slider alphaSlider;readonly DispatcherTimer saveTimer,errorTimer;
        readonly List<Button> tabButtons=new List<Button>(),themeButtons=new List<Button>();
        readonly ToolTip errorTip=new ToolTip{Focusable=false,StaysOpen=true};
        Forms.NotifyIcon tray;IntPtr hwnd;HwndSource source;
        bool loaded,closing,dragging,moved,animating,appearanceOpen;
        Native.Point dragStart;Native.Rect dragRect;IntPtr pressedTarget;string pressedReason;
        public readonly Dictionary<string,Button> SymbolButtons=new Dictionary<string,Button>();
        public IntPtr Handle {get{return hwnd;}}
        public bool IsCollapsed {get{return preferences.Collapsed;}}
        public Preferences CurrentPreferences {get{return preferences;}}
        public PadWindow(SettingsStore settings,bool useTray=true){
            store=settings;preferences=store.Read();
            if(preferences.Version<2&&preferences.Collapsed&&preferences.X.HasValue)
                preferences.X-=(int)Math.Round((PanelWidth-BallSize)*Native.ScaleAt(preferences.X.Value,preferences.Y??0));
            preferences.Version=2;
            // A single, fixed HWND avoids reallocating the transparent backing bitmap during morphs.
            Title="数学符号助手";Width=PanelWidth;Height=PanelHeight;
            WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
            ShowInTaskbar=false;Topmost=true;ShowActivated=false;Focusable=false;UseLayoutRounding=true;
            FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI");FontSize=12;
            TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
            Resources["Foreground"]=Brushes.Black;Resources["Muted"]=Brushes.Gray;
            Icon=Branding.WindowIcon();
            var canvas=new Grid();Content=canvas;
            shell=new MorphBorder{Width=PanelWidth-16,Height=PanelHeight-16,Margin=new Thickness(8),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,
                Radius=PanelRadius,BorderThickness=new Thickness(1)};
            var shadow=new RoundedShadow{Margin=new Thickness(8),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top};
            shadow.SetBinding(FrameworkElement.WidthProperty,new Binding("Width"){Source=shell});
            shadow.SetBinding(FrameworkElement.HeightProperty,new Binding("Height"){Source=shell});
            shadow.SetBinding(RoundedShadow.RadiusProperty,new Binding("Radius"){Source=shell});
            shadow.SetBinding(RoundedShadow.ExpansionProperty,new Binding("Expansion"){Source=shell});
            canvas.Children.Add(shadow);
            canvas.Children.Add(shell);
            var surface=new Grid();shell.Child=surface;
            panel=new Grid{Width=PanelWidth-40,Height=PanelHeight-40,Margin=new Thickness(11),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
            surface.Children.Add(panel);
            panel.RowDefinitions.Add(new RowDefinition{Height=new GridLength(50)});
            panel.RowDefinitions.Add(new RowDefinition{Height=new GridLength(36)});
            panel.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            var grip=new StackPanel{Orientation=Orientation.Horizontal,Cursor=Cursors.SizeAll,Background=Brushes.Transparent};
            var logo=Branding.Mark(28,true);logo.Margin=new Thickness(0,0,9,0);logo.VerticalAlignment=VerticalAlignment.Center;grip.Children.Add(logo);
            var titleGroup=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
            var title=Text("数学符号",15);title.FontFamily=new FontFamily("Microsoft YaHei UI, Segoe UI");title.FontWeight=FontWeights.SemiBold;title.LineHeight=18;title.LineStackingStrategy=LineStackingStrategy.BlockLineHeight;titleGroup.Children.Add(title);
            var hint=Text("先点输入框，再点符号输入。",11);hint.FontFamily=title.FontFamily;hint.FontWeight=FontWeights.Normal;hint.LineHeight=13;hint.LineStackingStrategy=LineStackingStrategy.BlockLineHeight;hint.SetResourceReference(TextBlock.ForegroundProperty,"Muted");hint.Margin=new Thickness(0,2,0,0);titleGroup.Children.Add(hint);grip.Children.Add(titleGroup);
            AttachDrag(grip,false);header.Children.Add(grip);
            var controls=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(controls,1);header.Children.Add(controls);
            appearanceButton=MakeButton("◐",28,28,"面板内的外观设置",false);AutomationProperties.SetAutomationId(appearanceButton,"appearance");appearanceButton.Click+=(s,e)=>ShowAppearance();controls.Children.Add(appearanceButton);
            var fold=MakeButton("−",28,28,"收成浮球",false);AutomationProperties.SetAutomationId(fold,"collapse");fold.Click+=(s,e)=>ToggleCollapsed();controls.Children.Add(fold);
            var close=MakeButton("×",28,28,"退出助手",false);AutomationProperties.SetAutomationId(close,"exit");close.Click+=(s,e)=>Close();controls.Children.Add(close);
            panel.Children.Add(header);
            tabs=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetRow(tabs,1);panel.Children.Add(tabs);
            for(int i=0;i<Catalog.Pages.Length;i++){
                int page=i;var b=MakeButton(Catalog.Pages[i].Name,57,28,Catalog.Pages[i].Name+"符号",false);b.Margin=new Thickness(0,0,3,0);b.FontSize=11;
                b.Click+=(s,e)=>{preferences.Page=page;ShowPage();QueueSave();};AutomationProperties.SetAutomationId(b,"page-"+i);tabs.Children.Add(b);tabButtons.Add(b);
            }
            keys=new UniformGrid{Rows=4,Margin=new Thickness(-3,3,-3,0)};Grid.SetRow(keys,2);panel.Children.Add(keys);
            appearanceView=new Grid{Visibility=Visibility.Collapsed,Opacity=0,IsHitTestVisible=false,Margin=new Thickness(3,12,3,4)};
            Grid.SetRow(appearanceView,1);Grid.SetRowSpan(appearanceView,2);panel.Children.Add(appearanceView);
            var settingsContent=new StackPanel();appearanceView.Children.Add(settingsContent);
            var caption=Text("外观",14);caption.FontWeight=FontWeights.SemiBold;caption.Margin=new Thickness(0,0,0,16);settingsContent.Children.Add(caption);
            var themeRow=new UniformGrid{Columns=3,Rows=1,Margin=new Thickness(-3,0,-3,24)};settingsContent.Children.Add(themeRow);
            string[] names={"跟随系统","浅色","深色"},modes={"system","light","dark"};
            for(int i=0;i<modes.Length;i++){
                string mode=modes[i];var b=MakeButton(names[i],double.NaN,38,names[i]+"配色",true);b.Margin=new Thickness(3);b.FontSize=12;
                AutomationProperties.SetAutomationId(b,"theme-"+mode);b.Click+=(s,e)=>{preferences.Theme=mode;ApplyTheme();QueueSave();};themeButtons.Add(b);themeRow.Children.Add(b);
            }
            alphaLabel=Text("背景不透明度  "+Math.Round(preferences.BackgroundOpacity*100)+"%",12);settingsContent.Children.Add(alphaLabel);
            alphaSlider=new Slider{Minimum=70,Maximum=95,Value=preferences.BackgroundOpacity*100,TickFrequency=1,IsSnapToTickEnabled=true,IsMoveToPointEnabled=true,Focusable=false,IsTabStop=false,Height=24,Margin=new Thickness(0,12,0,5)};
            alphaSlider.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Slider}'>
              <Grid Background='Transparent'><Border Height='4' CornerRadius='2' Background='{DynamicResource SliderRail}' VerticalAlignment='Center'/>
                <Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' Orientation='Horizontal'>
                  <Track.DecreaseRepeatButton><RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False' IsTabStop='False'><RepeatButton.Template><ControlTemplate TargetType='{x:Type RepeatButton}'><Border Height='4' CornerRadius='2' Background='{DynamicResource Accent}' VerticalAlignment='Center'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
                  <Track.Thumb><Thumb Width='16' Height='24' Focusable='False'><Thumb.Template><ControlTemplate TargetType='{x:Type Thumb}'><Grid Background='Transparent'><Border Width='16' Height='16' CornerRadius='8' Background='{DynamicResource Accent}' VerticalAlignment='Center'/><Border Width='6' Height='6' CornerRadius='3' Background='{DynamicResource Foreground}' Opacity='.22' VerticalAlignment='Center'/></Grid></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
                  <Track.IncreaseRepeatButton><RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False' IsTabStop='False'><RepeatButton.Template><ControlTemplate TargetType='{x:Type RepeatButton}'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
                </Track>
              </Grid></ControlTemplate>");
            alphaSlider.SetResourceReference(Control.ForegroundProperty,"Accent");AutomationProperties.SetAutomationId(alphaSlider,"background-opacity");settingsContent.Children.Add(alphaSlider);
            var limits=new Grid();var low=Text("70%",10);low.SetResourceReference(TextBlock.ForegroundProperty,"Muted");limits.Children.Add(low);var high=Text("95%",10);high.HorizontalAlignment=HorizontalAlignment.Right;high.SetResourceReference(TextBlock.ForegroundProperty,"Muted");limits.Children.Add(high);settingsContent.Children.Add(limits);
            alphaSlider.ValueChanged+=(s,e)=>{preferences.BackgroundOpacity=alphaSlider.Value/100;alphaLabel.Text="背景不透明度  "+Math.Round(alphaSlider.Value)+"%";ApplyTheme();QueueSave();};
            ball=new Grid{Width=46,Height=46,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Background=Brushes.Transparent,Cursor=Cursors.Hand};
            var ballMark=Branding.Mark(34,false);ballMark.HorizontalAlignment=HorizontalAlignment.Center;ballMark.VerticalAlignment=VerticalAlignment.Center;ball.Children.Add(ballMark);surface.Children.Add(ball);
            AttachDrag(ball,true);ToolTipService.SetToolTip(ball,"点击展开数学符号 · 拖动可移动");
            shell.ExpansionChanged+=(s,e)=>ApplyMorphProgress();
            errorTip.PlacementTarget=shell;errorTip.Placement=PlacementMode.Bottom;
            errorTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};errorTimer.Tick+=(s,e)=>{errorTimer.Stop();errorTip.IsOpen=false;};
            saveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};saveTimer.Tick+=(s,e)=>{saveTimer.Stop();SavePosition();};
            SourceInitialized+=(s,e)=>{hwnd=new WindowInteropHelper(this).Handle;source=HwndSource.FromHwnd(hwnd);source.AddHook(WindowHook);Native.SetWindowLong(hwnd,Native.GWL_EXSTYLE,Native.GetWindowLong(hwnd,Native.GWL_EXSTYLE)|Native.WS_EX_NOACTIVATE|Native.WS_EX_TOOLWINDOW);SetInitialPosition();};
            Loaded+=(s,e)=>{loaded=true;EnsureVisible();if(useTray)CreateTray();};
            LocationChanged+=(s,e)=>{if(loaded&&!dragging&&!animating)QueueSave();};
            Closed+=(s,e)=>{closing=true;errorTip.IsOpen=false;errorTimer.Stop();saveTimer.Stop();SavePosition();if(tray!=null){tray.Visible=false;var oldIcon=tray.Icon;tray.Dispose();if(oldIcon!=null)oldIcon.Dispose();}SystemEvents.UserPreferenceChanged-=OnSystemTheme;SystemEvents.DisplaySettingsChanged-=OnDisplayChanged;};
            SystemEvents.UserPreferenceChanged+=OnSystemTheme;SystemEvents.DisplaySettingsChanged+=OnDisplayChanged;
            ApplyTheme();ShowPage();ApplyCollapsedVisual();
        }
        TextBlock Text(string text,double size){var t=new TextBlock{Text=text,FontSize=size};t.SetResourceReference(TextBlock.ForegroundProperty,"Foreground");return t;}
        Button MakeButton(string label,double width,double height,string tip,bool key){
            var b=new Button{Content=label,Width=width,Height=height,Focusable=false,IsTabStop=false,Cursor=Cursors.Hand,FontSize=key?19:14,FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI"),ToolTip=tip,BorderThickness=new Thickness(1),Padding=new Thickness(2)};
            b.SetResourceReference(Control.ForegroundProperty,"Foreground");b.SetResourceReference(Control.BackgroundProperty,key?"Key":"Chrome");b.SetResourceReference(Control.BorderBrushProperty,key?"KeyBorder":"Chrome");
            var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(key?9:7));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));border.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));
            var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);
            var template=new ControlTemplate(typeof(Button)){VisualTree=border};var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Control.BackgroundProperty,new DynamicResourceExtension("Hover")));template.Triggers.Add(hover);
            var press=new Trigger{Property=ButtonBase.IsPressedProperty,Value=true};press.Setters.Add(new Setter(Control.BackgroundProperty,new DynamicResourceExtension("AccentSoft")));template.Triggers.Add(press);
            b.Template=template;AutomationProperties.SetName(b,tip);return b;
        }
        void ShowPage(){
            keys.Children.Clear();SymbolButtons.Clear();var page=Catalog.Pages[preferences.Page];keys.Columns=page.Columns;
            for(int i=0;i<tabButtons.Count;i++){tabButtons[i].SetResourceReference(Control.BackgroundProperty,i==preferences.Page?"Selected":"Chrome");tabButtons[i].SetResourceReference(Control.ForegroundProperty,i==preferences.Page?"Accent":"Muted");}
            foreach(var symbol in page.Items){var b=MakeButton(symbol.Label,double.NaN,double.NaN,symbol.Label+"："+symbol.Hint,true);b.HorizontalAlignment=HorizontalAlignment.Stretch;b.VerticalAlignment=VerticalAlignment.Stretch;b.Margin=new Thickness(3);b.FontFamily=new FontFamily("Cambria Math, Segoe UI, Microsoft YaHei UI");if(symbol.Label.Length>2)b.FontSize=14;if(symbol.Label.Length>4)b.FontSize=12;
                AutomationProperties.SetAutomationId(b,"symbol-"+symbol.Id);b.PreviewMouseLeftButtonDown+=(s,e)=>{pressedTarget=Native.InputTarget(out pressedReason);};b.Click+=(s,e)=>InsertSymbol(symbol);keys.Children.Add(b);SymbolButtons[symbol.Id]=b;}
        }
        void InsertSymbol(Symbol symbol){
            if(animating)return;
            if(pressedTarget==IntPtr.Zero){ShowError(pressedReason??"请先点击要输入的聊天框");return;}
            string error;if(!Native.Insert(symbol,pressedTarget,out error))ShowError(error);pressedTarget=IntPtr.Zero;
        }
        void ShowError(string text){errorTip.Content=text;errorTip.IsOpen=true;errorTimer.Stop();errorTimer.Start();}
        IntPtr WindowHook(IntPtr handle,int message,IntPtr w,IntPtr l,ref bool handled){
            if(message==Native.WM_MOUSEACTIVATE||message==Native.WM_POINTERACTIVATE){handled=true;return new IntPtr(Native.MA_NOACTIVATE);}
            if(message==0x84){
                Native.Point p;Native.Rect b;
                if(Native.GetCursorPos(out p)&&Native.GetWindowRect(hwnd,out b)){
                    Rect view=shell.DisplayBounds;double scale=b.Width/PanelWidth,left=b.Left+(8+view.X)*scale,top=b.Top+(8+view.Y)*scale,right=left+view.Width*scale,bottom=top+view.Height*scale;
                    double radius=Math.Min(shell.Radius,Math.Min(view.Width,view.Height)/2)*scale;
                    double cx=Math.Max(left+radius,Math.Min(p.X,right-radius)),cy=Math.Max(top+radius,Math.Min(p.Y,bottom-radius));
                    if(p.X<left||p.X>right||p.Y<top||p.Y>bottom||Math.Pow(p.X-cx,2)+Math.Pow(p.Y-cy,2)>radius*radius){handled=true;return new IntPtr(-1);}
                }
            }
            if(message==0x02E0)Dispatcher.BeginInvoke(new Action(()=>{if(!closing&&!animating)EnsureVisible();}),DispatcherPriority.Background);
            return IntPtr.Zero;
        }
        void AttachDrag(UIElement element,bool expandOnClick){
            element.PreviewMouseLeftButtonDown+=(s,e)=>{if(animating)return;dragging=true;moved=false;Native.GetCursorPos(out dragStart);Native.GetWindowRect(hwnd,out dragRect);element.CaptureMouse();e.Handled=true;};
            element.PreviewMouseMove+=(s,e)=>{if(!dragging||e.LeftButton!=MouseButtonState.Pressed)return;Native.Point p;Native.GetCursorPos(out p);int dx=p.X-dragStart.X,dy=p.Y-dragStart.Y;if(Math.Abs(dx)>4||Math.Abs(dy)>4)moved=true;if(moved)Native.SetWindowPos(hwnd,Native.HWND_TOPMOST,dragRect.Left+dx,dragRect.Top+dy,0,0,Native.SWP_NOSIZE|Native.SWP_NOACTIVATE);};
            element.PreviewMouseLeftButtonUp+=(s,e)=>{if(!dragging)return;dragging=false;element.ReleaseMouseCapture();if(expandOnClick&&!moved)ToggleCollapsed();else{EnsureVisible();SavePosition();}e.Handled=true;};element.LostMouseCapture+=(s,e)=>{dragging=false;};
        }
        Native.Rect WorkingArea(int x,int y){var r=Forms.Screen.FromPoint(new Drawing.Point(x,y)).WorkingArea;return new Native.Rect{Left=r.Left,Top=r.Top,Right=r.Right,Bottom=r.Bottom};}
        void SetInitialPosition(){
            var screen=Forms.Screen.PrimaryScreen.WorkingArea;double scale=Native.ScaleAt(preferences.X??screen.Right-40,preferences.Y??screen.Top+40);
            int width=(int)Math.Round(PanelWidth*scale),height=(int)Math.Round(PanelHeight*scale),x=preferences.X??(screen.Right-width-(int)(28*scale)),y=preferences.Y??(screen.Top+(screen.Height-height)/2);
            Native.SetWindowPos(hwnd,Native.HWND_TOPMOST,x,y,width,height,Native.SWP_NOACTIVATE);EnsureVisible();
        }
        public void EnsureVisible(){
            if(hwnd==IntPtr.Zero||closing||animating)return;Native.Rect b;if(!Native.GetWindowRect(hwnd,out b))return;
            double scale=b.Width/PanelWidth;int width=(int)Math.Round((preferences.Collapsed?BallSize:PanelWidth)*scale),height=(int)Math.Round((preferences.Collapsed?BallSize:PanelHeight)*scale),offset=b.Width-width;
            var area=WorkingArea(b.Left+offset+width/2,b.Top+height/2);var fit=Placement.Clamp(area,b.Left+offset,b.Top,width,height);
            if(fit.Left!=b.Left+offset||fit.Top!=b.Top)Native.SetWindowPos(hwnd,Native.HWND_TOPMOST,fit.Left-offset,fit.Top,0,0,Native.SWP_NOSIZE|Native.SWP_NOACTIVATE);
        }
        static DoubleAnimation Motion(double target,int milliseconds,int delay=0){return new DoubleAnimation{To=target,Duration=TimeSpan.FromMilliseconds(milliseconds),BeginTime=TimeSpan.FromMilliseconds(delay),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};}
        public void ToggleCollapsed(){
            if(animating)return;errorTip.IsOpen=false;saveTimer.Stop();bool collapse=!preferences.Collapsed;preferences.Collapsed=collapse;animating=true;panel.IsHitTestVisible=false;ball.IsHitTestVisible=false;
            ToolTipService.SetIsEnabled(ball,false);ToolTipService.SetIsEnabled(panel,false);
            double target=collapse?0:1;int duration=SystemParameters.ClientAreaAnimation?(collapse?280:380):0;
            var progress=new DoubleAnimation{From=shell.Expansion,To=target,Duration=TimeSpan.FromMilliseconds(duration)};
            progress.Completed+=(s,e)=>{
                // Commit the final base value first. Removing the clock can never reveal an old frame.
                shell.Expansion=target;shell.BeginAnimation(MorphBorder.ExpansionProperty,null);ApplyMorphProgress();
                animating=false;panel.IsHitTestVisible=!collapse;ball.IsHitTestVisible=collapse;ToolTipService.SetIsEnabled(ball,true);ToolTipService.SetIsEnabled(panel,true);EnsureVisible();SavePosition();
            };shell.BeginAnimation(MorphBorder.ExpansionProperty,progress);
        }
        void ApplyMorphProgress(){double p=Math.Max(0,Math.Min(1,shell.Expansion));panel.Opacity=MorphGeometry.Smooth((p-MorphGeometry.RevealEnd)/(1-MorphGeometry.RevealEnd));ball.Opacity=1-MorphGeometry.Smooth((p-.18)/(MorphGeometry.RevealEnd-.18));}
        void ApplyCollapsedVisual(){bool c=preferences.Collapsed;shell.Expansion=c?0:1;ApplyMorphProgress();panel.IsHitTestVisible=!c;ball.IsHitTestVisible=c;}
        void QueueSave(){if(closing||!loaded||animating)return;saveTimer.Stop();saveTimer.Start();}
        void SavePosition(){if(hwnd==IntPtr.Zero)return;Native.Rect b;if(Native.GetWindowRect(hwnd,out b)){preferences.X=b.Left;preferences.Y=b.Top;}if(!store.Write(preferences)&&!closing)ShowError("偏好未能保存，仍可继续使用");}
        public void Reveal(){if(!IsVisible)Show();if(preferences.Collapsed)ToggleCollapsed();else EnsureVisible();}
        void OnSystemTheme(object sender,UserPreferenceChangedEventArgs e){if(!closing)Dispatcher.BeginInvoke(new Action(ApplyTheme));}
        void OnDisplayChanged(object sender,EventArgs e){if(!closing)Dispatcher.BeginInvoke(new Action(EnsureVisible));}
        bool IsDark(){if(preferences.Theme=="dark")return true;if(preferences.Theme=="light")return false;try{using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))return Convert.ToInt32(k==null?1:k.GetValue("AppsUseLightTheme",1))==0;}catch{return false;}}
        void Brush(string key,string hex){Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));}
        void ApplyTheme(){
            bool dark=IsDark();Brush("Foreground",dark?"#EEF3FC":"#16243A");Brush("Muted",dark?"#A0ACC0":"#63738A");Brush("Accent",dark?"#97B2FF":"#4D64DA");Brush("AccentSoft",dark?"#445A78FF":"#275A78FF");Brush("Selected",dark?"#355A78FF":"#215A78FF");Brush("Hover",dark?"#304F81BD":"#55CED8FF");Brush("Chrome","#00000000");Brush("Key",dark?"#16FFFFFF":"#99FFFFFF");Brush("KeyBorder",dark?"#15FFFFFF":"#D9FFFFFF");
            Brush("SliderRail",dark?"#28FFFFFF":"#223E4C75");
            Color c=(Color)ColorConverter.ConvertFromString(dark?"#1D2335":"#F3F5FC");c.A=(byte)Math.Round(preferences.BackgroundOpacity*255);shell.Background=new SolidColorBrush(c);shell.BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#38FFFFFF":"#E6FFFFFF"));
            if(themeButtons!=null)for(int i=0;i<themeButtons.Count;i++){bool active=preferences.Theme==(i==0?"system":i==1?"light":"dark");themeButtons[i].SetResourceReference(Control.BackgroundProperty,active?"Selected":"Key");themeButtons[i].SetResourceReference(Control.ForegroundProperty,active?"Accent":"Foreground");}
        }
        void CreateTray(){
            var trayIcon=Branding.TrayIcon();
            tray=new Forms.NotifyIcon{Text="数学符号助手",Icon=trayIcon,Visible=true};var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("显示面板",null,(s,e)=>Dispatcher.BeginInvoke(new Action(Reveal)));menu.Items.Add("外观设置",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>{Reveal();SetAppearance(true);})));menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("退出",null,(s,e)=>Dispatcher.BeginInvoke(new Action(Close)));tray.ContextMenuStrip=menu;tray.MouseDoubleClick+=(s,e)=>Dispatcher.BeginInvoke(new Action(Reveal));
        }
        void ShowAppearance(){SetAppearance(!appearanceOpen);}
        public void SetAppearance(bool open,bool animate=true){
            appearanceOpen=open;appearanceButton.Content=open?"←":"◐";appearanceButton.ToolTip=open?"返回符号":"面板内的外观设置";AutomationProperties.SetName(appearanceButton,(string)appearanceButton.ToolTip);
            tabs.IsHitTestVisible=keys.IsHitTestVisible=!open;appearanceView.Visibility=open?Visibility.Visible:Visibility.Collapsed;appearanceView.IsHitTestVisible=open;
            if(animate){tabs.BeginAnimation(UIElement.OpacityProperty,Motion(open?0:1,140));keys.BeginAnimation(UIElement.OpacityProperty,Motion(open?0:1,140));appearanceView.BeginAnimation(UIElement.OpacityProperty,Motion(open?1:0,160));}
            else{tabs.Opacity=keys.Opacity=open?0:1;appearanceView.Opacity=open?1:0;}
        }
        public void RenderPreview(string path){var root=(FrameworkElement)Content;if(!IsLoaded){root.Measure(new Size(Width,Height));root.Arrange(new Rect(0,0,Width,Height));}root.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Width,(int)Height,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);}
        public void SetPreviewExpansion(double expansion){shell.Expansion=Math.Max(0,Math.Min(1,expansion));ApplyMorphProgress();}
    }
}
