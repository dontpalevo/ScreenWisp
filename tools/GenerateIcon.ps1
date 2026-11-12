$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$assetDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ScreenWisp\Assets'))
$brandDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\branding'))
[IO.Directory]::CreateDirectory((Join-Path $brandDir 'png')) | Out-Null
# Exact silhouette traced from the approved S stroke; separate micro geometry avoids tiny notches.
$full = 'M530,65 C440,-12 192,65 101,144 C25,215 99,265 242,280 C383,295 445,326 351,391 C321,412 288,427 240,447 C122,492 132,527 232,542 C285,550 342,546 385,540 C330,541 255,528 224,514 C177,494 260,470 320,446 C432,405 482,348 452,305 C432,259 354,250 242,237 C126,224 98,194 167,144 C268,76 450,24 530,65 Z'
$micro = 'M530,65 C440,5 205,70 105,150 C12,225 95,265 250,282 C380,296 442,331 345,395 C295,425 225,447 190,467 C123,505 188,555 375,540 C285,535 210,519 210,497 C210,480 270,465 325,443 C440,401 477,348 452,306 C430,264 353,253 247,240 C132,225 105,199 170,150 C275,83 450,28 530,65 Z'
$gradient = [Windows.Media.LinearGradientBrush]::new()
$gradient.StartPoint = [Windows.Point]::new(0,0);$gradient.EndPoint = [Windows.Point]::new(0.6,1)
foreach ($stop in @(@('#63718A',0),@('#53617A',0.48),@('#8C72AF',0.78),@('#B079E8',1))) {
 $gradient.GradientStops.Add([Windows.Media.GradientStop]::new([Windows.Media.ColorConverter]::ConvertFromString($stop[0]),[double]$stop[1]))
}
$edge = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#C5BFE0'))
$accent = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#AA7DDF'))
function Render-Mark([int]$size) {
 $visual = [Windows.Media.DrawingVisual]::new();$context = $visual.RenderOpen()
 $context.PushTransform([Windows.Media.ScaleTransform]::new($size/600.0,$size/600.0))
 $small = $size -le 24
 $path = if($small){$micro}else{$full}
 $outlineWidth = if($small){600.0*0.45/$size}else{2.5}
 $context.DrawGeometry($gradient,[Windows.Media.Pen]::new($edge,$outlineWidth),[Windows.Media.Geometry]::Parse($path))
 $particles = [System.Collections.Generic.List[object]]::new()
 if($size -eq 16){$particles.Add([double[]]@(410,532,42))}
 elseif($size -eq 20){$particles.Add([double[]]@(410,532,36));$particles.Add([double[]]@(459,514,29))}
 elseif($size -eq 24){$particles.Add([double[]]@(410,532,32));$particles.Add([double[]]@(456,515,27));$particles.Add([double[]]@(492,493,22))}
 else{$particles.Add([double[]]@(410,532,30));$particles.Add([double[]]@(452,518,24));$particles.Add([double[]]@(484,500,18));$particles.Add([double[]]@(510,480,11))}
 foreach($particle in $particles) {
  $x=$particle[0];$y=$particle[1];$radius=$particle[2]/2.0
  $diamond=[Windows.Media.Geometry]::Parse("M$($x-$radius),$y L$x,$($y-$radius) L$($x+$radius),$y L$x,$($y+$radius) Z")
  $context.DrawGeometry($accent,[Windows.Media.Pen]::new($edge,$outlineWidth*0.6),$diamond)
 }
 $context.Pop();$context.Close()
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
 $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[IO.MemoryStream]::new();$encoder.Save($stream);$bytes=$stream.ToArray();$stream.Dispose()
 [IO.File]::WriteAllBytes((Join-Path $brandDir "png\ScreenWisp-$size.png"),$bytes)
 return @{Size=$size;Bytes=$bytes}
}
function Write-Ico($frames,[string]$path) {
 $writer=[IO.BinaryWriter]::new([IO.File]::Create($path))
 try{
  $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
  $offset=6+16*$frames.Count
  foreach($frame in $frames){
   $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
   $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
   $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset);$offset+=$frame.Bytes.Length
  }
  foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
 }finally{$writer.Dispose()}
}
$frames=@();foreach($size in @(16,20,24,32,48,64,128,256)){$frames+=Render-Mark $size}
Write-Ico $frames (Join-Path $assetDir 'screenwisp.ico')
Write-Ico $frames (Join-Path $brandDir 'ScreenWisp.ico')
Write-Ico @($frames | Where-Object Size -le 32) (Join-Path $brandDir 'ScreenWisp-tray.ico')
$null=Render-Mark 1024
$svg=@"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 600"><defs><linearGradient id="ink" x2=".6" y2="1"><stop stop-color="#63718A"/><stop offset=".48" stop-color="#53617A"/><stop offset=".78" stop-color="#8C72AF"/><stop offset="1" stop-color="#B079E8"/></linearGradient></defs><path d="$full" fill="url(#ink)" stroke="#C5BFE0" stroke-width="2.5"/><g fill="#AA7DDF" stroke="#C5BFE0" stroke-width="1.5"><path d="M395 532L410 517L425 532L410 547Z"/><path d="M440 518L452 506L464 518L452 530Z"/><path d="M475 500L484 491L493 500L484 509Z"/><path d="M504.5 480L510 474.5L515.5 480L510 485.5Z"/></g></svg>
"@
[IO.File]::WriteAllText((Join-Path $assetDir 'screenwisp.svg'),$svg)
[IO.File]::WriteAllText((Join-Path $brandDir 'ScreenWisp-mark.svg'),$svg)
