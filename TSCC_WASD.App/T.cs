using System.Windows.Markup;
using TSCC_WASD.Core;

namespace TSCC_WASD.App;

/// <summary>XAML text in both languages: <c>Text="{app:T Zh=輸入手把, En='Input controller'}"</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class T : MarkupExtension
{
    public string Zh { get; set; } = "";
    public string En { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Zh, En);
}
