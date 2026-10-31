using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MathSymbolPad {
    public static class Branding {
        static System.IO.Stream OpenIcon() {
            var stream=typeof(Branding).Assembly.GetManifestResourceStream("MathSymbolPad.Brand.ico");
            if(stream==null)throw new System.InvalidOperationException("程序内置图标缺失，请重新构建。");
            return stream;
        }
        public static ImageSource WindowIcon() {
            using(var stream=OpenIcon()){
                var decoder=new System.Windows.Media.Imaging.IconBitmapDecoder(stream,System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var image=decoder.Frames[decoder.Frames.Count-1];image.Freeze();return image;
            }
        }
        public static System.Drawing.Icon TrayIcon() {
            using(var stream=OpenIcon())
            using(var icon=new System.Drawing.Icon(stream,32,32))return (System.Drawing.Icon)icon.Clone();
        }
        public static FrameworkElement Mark(double size,bool tile) {
            var host=new Grid{Width=size,Height=size,IsHitTestVisible=false};
            if(tile)host.Children.Add(new Border{CornerRadius=new CornerRadius(size*0.29),Background=new LinearGradientBrush(Color.FromRgb(58,109,255),Color.FromRgb(112,75,255),45)});
            var drawing=new Canvas{Width=24,Height=24};
            var root=new Path{Data=Geometry.Parse("M 4,12.5 L 7.5,16.5 L 11.5,7.5 L 20,7.5"),StrokeThickness=2.35,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round};
            if(tile)root.Stroke=Brushes.White;else root.SetResourceReference(Shape.StrokeProperty,"Accent");
            drawing.Children.Add(root);
            var dot=new Ellipse{Width=2.7,Height=2.7,Fill=tile?new SolidColorBrush(Color.FromRgb(152,232,255)):Brushes.Transparent};
            if(!tile)dot.SetResourceReference(Shape.FillProperty,"Accent");
            Canvas.SetLeft(dot,17.7);Canvas.SetTop(dot,14.4);drawing.Children.Add(dot);
            host.Children.Add(new Viewbox{Child=drawing,Margin=new Thickness(tile?size*0.05:0)});return host;
        }
    }
    public static class MorphGeometry {
        public const double RevealEnd=.68;
        public static Rect Bounds(double width,double height,double expansion){
            width=System.Math.Max(48,width);height=System.Math.Max(48,height);expansion=System.Math.Max(0,System.Math.Min(1,expansion));
            double shape=Smooth(expansion/RevealEnd);
            double shownWidth=48+(width-48)*shape,shownHeight=48+(height-48)*shape;
            return new Rect(width-shownWidth,0,shownWidth,shownHeight);
        }
        public static double Smooth(double value){value=System.Math.Max(0,System.Math.Min(1,value));return value*value*(3-2*value);}
    }
    public sealed class MorphBorder : Border {
        public static readonly DependencyProperty RadiusProperty=DependencyProperty.Register("Radius",typeof(double),typeof(MorphBorder),new FrameworkPropertyMetadata(24.0,FrameworkPropertyMetadataOptions.AffectsRender,Changed));
        public static readonly DependencyProperty ExpansionProperty=DependencyProperty.Register("Expansion",typeof(double),typeof(MorphBorder),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender,ProgressChanged));
        public double Radius {get{return (double)GetValue(RadiusProperty);}set{SetValue(RadiusProperty,value);}}
        public double Expansion {get{return (double)GetValue(ExpansionProperty);}set{SetValue(ExpansionProperty,value);}}
        public event System.EventHandler ExpansionChanged;
        public Rect DisplayBounds {get{return MorphGeometry.Bounds(ActualWidth>0?ActualWidth:Width,ActualHeight>0?ActualHeight:Height,Expansion);}}
        public MorphBorder(){CornerRadius=new CornerRadius(24);SizeChanged+=(s,e)=>UpdateClip();LayoutUpdated+=(s,e)=>UpdateClip();}
        static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e){var b=(MorphBorder)d;b.CornerRadius=new CornerRadius((double)e.NewValue);b.UpdateClip();}
        static void ProgressChanged(DependencyObject d,DependencyPropertyChangedEventArgs e){var b=(MorphBorder)d;b.UpdateClip();if(b.ExpansionChanged!=null)b.ExpansionChanged(b,System.EventArgs.Empty);}
        static void RoundClip(UIElement element,Rect rect,double radius){
            radius=System.Math.Max(0,System.Math.Min(radius,System.Math.Min(rect.Width,rect.Height)/2));var existing=element.Clip as RectangleGeometry;
            if(existing!=null&&existing.Rect==rect&&existing.RadiusX==radius&&existing.RadiusY==radius)return;
            var geometry=new RectangleGeometry(rect,radius,radius);geometry.Freeze();element.Clip=geometry;
        }
        void UpdateClip(){
            // The layout stays full size. Only this rounded reveal changes, so glyphs
            // never get remeasured, scaled or repositioned while the surface expands.
            Rect view=DisplayBounds;RoundClip(this,view,Radius);
            if(Child==null)return;
            double insetX=BorderThickness.Left+Padding.Left,insetY=BorderThickness.Top+Padding.Top;
            double innerWidth=System.Math.Max(0,view.Width-insetX-BorderThickness.Right-Padding.Right);
            double innerHeight=System.Math.Max(0,view.Height-insetY-BorderThickness.Bottom-Padding.Bottom);
            RoundClip(Child,new Rect(view.X,view.Y,innerWidth,innerHeight),System.Math.Max(0,Radius-System.Math.Max(insetX,insetY)));
        }
        protected override void OnRender(DrawingContext dc){
            Rect view=DisplayBounds;double edge=System.Math.Max(BorderThickness.Left,BorderThickness.Top),half=edge/2;
            var rect=new Rect(view.X+half,view.Y+half,System.Math.Max(0,view.Width-edge),System.Math.Max(0,view.Height-edge));
            Pen pen=BorderBrush==null||edge==0?null:new Pen(BorderBrush,edge);
            dc.DrawRoundedRectangle(Background,pen,rect,System.Math.Max(0,Radius-half),System.Math.Max(0,Radius-half));
        }
    }
    public sealed class RoundedShadow : FrameworkElement {
        public static readonly DependencyProperty RadiusProperty=DependencyProperty.Register("Radius",typeof(double),typeof(RoundedShadow),new FrameworkPropertyMetadata(24.0,FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ExpansionProperty=DependencyProperty.Register("Expansion",typeof(double),typeof(RoundedShadow),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
        public double Radius {get{return (double)GetValue(RadiusProperty);}set{SetValue(RadiusProperty,value);}}
        public double Expansion {get{return (double)GetValue(ExpansionProperty);}set{SetValue(ExpansionProperty,value);}}
        readonly Pen[] rings=new Pen[6];
        public RoundedShadow(){
            IsHitTestVisible=false;
            for(int i=0;i<rings.Length;i++){var brush=new SolidColorBrush(Color.FromArgb((byte)(12-i*2),0,0,0));brush.Freeze();rings[i]=new Pen(brush,1.0);rings[i].Freeze();}
        }
        protected override void OnRender(DrawingContext dc){
            base.OnRender(dc);if(ActualWidth<=0||ActualHeight<=0)return;
            Rect view=MorphGeometry.Bounds(ActualWidth,ActualHeight,Expansion);double radius=System.Math.Min(Radius,System.Math.Min(view.Width,view.Height)/2);
            // Only soft rounded outlines are painted. No opaque rectangular source bitmap.
            for(int i=rings.Length-1;i>=0;i--){double spread=i+.5;dc.DrawRoundedRectangle(null,rings[i],new Rect(view.X-spread,view.Y+1-spread,view.Width+2*spread,view.Height+2*spread),radius+spread,radius+spread);}
        }
    }
}
