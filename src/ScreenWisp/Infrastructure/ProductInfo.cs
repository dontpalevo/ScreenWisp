using System.Reflection;
namespace ScreenWisp.Infrastructure;
internal static class ProductInfo {
 internal const string Name="ScreenWisp";
 internal const string Developer="dontpalevo";
 internal const string GitHub="https://github.com/dontpalevo";
 internal static string Version=>typeof(ProductInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
}
