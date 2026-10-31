$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$appRoot = [IO.Path]::GetFullPath((Join-Path $sourceRoot '..'))
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'
$iconPath = Join-Path $sourceRoot '.brand-build.ico'
if (Test-Path -LiteralPath $iconPath) { throw '存在未清理的图标构建文件，请确认没有其他构建正在运行。' }
try {
Add-Type -AssemblyName System.Drawing
$iconStream = [IO.File]::Create($iconPath)
$writer = [IO.BinaryWriter]::new($iconStream)
$images = @()
foreach ($size in @(16,32,48,256)) {
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform($size/64.0,$size/64.0)
    $tileRect = [Drawing.RectangleF]::new(2,2,60,60)
    $blue = [Drawing.Drawing2D.LinearGradientBrush]::new($tileRect,[Drawing.ColorTranslator]::FromHtml('#3A6DFF'),[Drawing.ColorTranslator]::FromHtml('#704BFF'),45)
    $ink = [Drawing.SolidBrush]::new([Drawing.Color]::White)
    $graphics.Clear([Drawing.Color]::Transparent)
    $round = [Drawing.Drawing2D.GraphicsPath]::new()
    $round.AddArc(2,2,34,34,180,90);$round.AddArc(28,2,34,34,270,90);$round.AddArc(28,28,34,34,0,90);$round.AddArc(2,28,34,34,90,90);$round.CloseFigure()
    $graphics.FillPath($blue,$round)
    $pen = [Drawing.Pen]::new([Drawing.Color]::White,5.6)
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $points = [Drawing.PointF[]]@([Drawing.PointF]::new(12,33),[Drawing.PointF]::new(21,43),[Drawing.PointF]::new(31,20),[Drawing.PointF]::new(51,20))
    $graphics.DrawLines($pen,$points)
    $dot = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#98E8FF'))
    $graphics.FillEllipse($dot,45,38,6.6,6.6)
    $memory = [IO.MemoryStream]::new()
    $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $images += ,$memory.ToArray()
    $memory.Dispose();$ink.Dispose();$dot.Dispose();$pen.Dispose();$round.Dispose();$blue.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$writer.Write([UInt16]0);$writer.Write([UInt16]1);$writer.Write([UInt16]4)
$offset = 6 + 16*4
for ($index=0; $index -lt 4; $index++) {
    $dimension = @(16,32,48,0)[$index]
    $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
    $writer.Write([UInt16]1);$writer.Write([UInt16]32);$writer.Write([UInt32]$images[$index].Length);$writer.Write([UInt32]$offset)
    $offset += $images[$index].Length
}
foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
$writer.Dispose();$iconStream.Dispose()
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll',"$wpf\WindowsBase.dll","$wpf\PresentationCore.dll","$wpf\PresentationFramework.dll","$wpf\UIAutomationTypes.dll","$wpf\UIAutomationProvider.dll")
$source = @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'src') -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$arguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/main:MathSymbolPad.Program',"/out:$appRoot\MathSymbolPad.exe","/win32manifest:$sourceRoot\app.manifest","/win32icon:$iconPath","/resource:$iconPath,MathSymbolPad.Brand.ico")
foreach ($reference in $references) { $arguments += "/reference:$reference" }
& $compiler @arguments @source
if ($LASTEXITCODE -ne 0) { throw '悬浮助手编译失败' }
Write-Output (Join-Path $appRoot 'MathSymbolPad.exe')
} finally {
    if (Test-Path -LiteralPath $iconPath) { Remove-Item -LiteralPath $iconPath -Force }
}
