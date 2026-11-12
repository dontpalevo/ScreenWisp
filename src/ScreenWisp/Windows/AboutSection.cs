using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenWisp.Infrastructure;
using ScreenWisp.Localization;
namespace ScreenWisp.Windows;
internal sealed class AboutSection : Border {
 internal AboutSection(){
  Name="AboutSection";BorderBrush=new SolidColorBrush(Color.FromRgb(220,224,230));BorderThickness=new Thickness(0,1,0,0);Padding=new Thickness(0,16,0,0);Margin=new Thickness(0,20,0,0);
  var panel=new StackPanel();Child=panel;
  TextBlock Line(string key,string name="",double size=12){var t=new TextBlock{Name=name,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,6)};Localizer.Bind(t,TextBlock.TextProperty,key);panel.Children.Add(t);return t;}
  Line("About","AboutHeading",14).FontWeight=FontWeights.SemiBold;
  var header=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,2,0,10)};
  header.Children.Add(new Image{Source=BitmapFrame.Create(new Uri("pack://application:,,,/ScreenWisp;component/Assets/screenwisp.ico")),Width=28,Height=28,Margin=new Thickness(0,0,10,0)});
  header.Children.Add(new TextBlock{Text=ProductInfo.Name,FontSize=20,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});panel.Children.Add(header);
  Line("AboutDeveloper","AboutDeveloper");
  var version=new TextBlock{Name="AboutVersion",Margin=new Thickness(0,0,0,6)};panel.Children.Add(version);
  version.SetBinding(TextBlock.TextProperty,new Binding("[AboutVersion]"){Source=Localizer.Current,Converter=new VersionConverter()});
  var link=new Hyperlink(new Run(ProductInfo.GitHub)){NavigateUri=new Uri(ProductInfo.GitHub)};
  Localizer.Bind(link,System.Windows.Automation.AutomationProperties.NameProperty,"AboutGitHub");
  link.RequestNavigate+=(_,args)=>{
   args.Handled=true;
   try{Process.Start(new ProcessStartInfo(ProductInfo.GitHub){UseShellExecute=true});}
   catch(Exception ex){AppLog.Error("About GitHub link failed",ex);MessageBox.Show(Localizer.Current.Format("AboutLinkError",Localizer.Error(ex)),ProductInfo.Name,MessageBoxButton.OK,MessageBoxImage.Error);}
  };
  var github=new TextBlock{Margin=new Thickness(0,0,0,10),TextWrapping=TextWrapping.Wrap};github.Inlines.Add(new Run("GitHub: "));github.Inlines.Add(link);panel.Children.Add(github);
  Line("AboutLicense","AboutLicense");
  Line("AboutUpstream","AboutUpstream");
  Line("AboutScope","AboutScope",11).Foreground=Brushes.DimGray;
 }
 sealed class VersionConverter : IValueConverter {
  public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>string.Format(culture,(string)value,ProductInfo.Version);
  public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>throw new NotSupportedException();
 }
}
