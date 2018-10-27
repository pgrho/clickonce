using System.Xml.Linq;

namespace Shipwreck.ClickOnce.Manifest
{
    internal static class XElementHelper
    {
        public static XElement GetOrAdd(this XContainer parent, XName name)
        {
            var e = parent.Element(name);
            if (e == null)
            {
                e = new XElement(name);
                parent.Add(e);
            }
            return e;
        }
    }
}