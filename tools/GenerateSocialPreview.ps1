# Render the brand cover only; does not alter app icons or drawing code.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Brush([string]$hex){[Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($hex))}
$visual=[Windows.Media.DrawingVisual]::new()
$dc=$visual.RenderOpen()
$dc.DrawRectangle((Brush '#0D111C'),$null,[Windows.Rect]::new(0,0,1280,640))
$glow=[Windows.Media.RadialGradientBrush]::new()
$glow.GradientStops.Add([Windows.Media.GradientStop]::new([Windows.Media.ColorConverter]::ConvertFromString('#30304F'),0))
$glow.GradientStops.Add([Windows.Media.GradientStop]::new([Windows.Media.ColorConverter]::ConvertFromString('#0D111C'),1))
$dc.DrawEllipse($glow,$null,[Windows.Point]::new(1010,300),410,350)
function Label([string]$s,[double]$size,[string]$color,[double]$x,[double]$y,[bool]$bold=$false){
 $weight=if($bold){[Windows.FontWeights]::SemiBold}else{[Windows.FontWeights]::Normal}
 $face=[Windows.Media.Typeface]::new([Windows.Media.FontFamily]::new('Segoe UI'),[Windows.FontStyles]::Normal,$weight,[Windows.FontStretches]::Normal)
 $text=[Windows.Media.FormattedText]::new($s,[Globalization.CultureInfo]::InvariantCulture,[Windows.FlowDirection]::LeftToRight,$face,$size,(Brush $color),1.0)
 $dc.DrawText($text,[Windows.Point]::new($x,$y))
}
Label 'OPEN SOURCE / WINDOWS' 17 '#B8A1E5' 80 65 $true
Label 'ScreenWisp' 82 '#F5F2FC' 75 126 $true
Label 'Draw anywhere.' 48 '#F5F2FC' 80 263
Label 'Release Alt. Gone.' 48 '#BC92F1' 80 323 $true
Label 'Temporary screen annotations' 21 '#A9AEC1' 82 433
$dc.DrawLine([Windows.Media.Pen]::new((Brush '#33364A'),1),[Windows.Point]::new(80,520),[Windows.Point]::new(1200,520))
Label 'dontpalevo / ScreenWisp' 18 '#BCC1D0' 80 550
Label 'Native WPF  /  MIT licensed' 17 '#969EB3' 927 552
$logo=[Windows.Media.Imaging.BitmapImage]::new()
$logo.BeginInit();$logo.CacheOption=[Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$logo.UriSource=[Uri]::new((Join-Path $root 'branding\png\ScreenWisp-1024.png'))
$logo.EndInit();$logo.Freeze()
$dc.DrawImage($logo,[Windows.Rect]::new(795,88,400,400))
$dc.Close()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(1280,640,96,96,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($visual)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$path=Join-Path $root 'branding\ScreenWisp-social-preview.png'
$stream=[IO.File]::Create($path);try{$encoder.Save($stream)}finally{$stream.Dispose()}
Write-Output 'Rendered 1280x640 brand cover; approved logo and app icons unchanged.'
