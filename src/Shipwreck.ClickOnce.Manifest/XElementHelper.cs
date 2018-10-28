using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    internal static class XElementHelper
    {
        public static XElement AddElement(this XContainer parent, XName name)
        {
            var e = new XElement(name);
            parent.Add(e);

            return e;
        }

        public static XElement GetOrAdd(this XContainer parent, XName name)
            => parent.Element(name) ?? parent.AddElement(name);

        public static string ToAttributeValue(this bool b)
            => b ? "true" : "false";
    }
}