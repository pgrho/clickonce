using System.Reflection;
using System.Text;

namespace Shipwreck.ClickOnce.Manifest;

internal static class AssemblyNameHelper
{
    public static string ToAttributeValue(this byte[] array, bool lowercase = false)
        => array?.Length > 0 ? array.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString(lowercase ? "x2" : "X2"))).ToString()
        : null;

    public static string ToAttributeValue(this ProcessorArchitecture processorArchitecture)
        => processorArchitecture switch
        {
            ProcessorArchitecture.X86 => "x86",
            ProcessorArchitecture.Amd64 => "amd64",
            ProcessorArchitecture.MSIL => "msil",
            _ => "msil"
        };
}