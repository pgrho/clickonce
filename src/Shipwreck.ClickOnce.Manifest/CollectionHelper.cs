using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shipwreck.ClickOnce.Manifest
{
    internal static class CollectionHelper
    {
        public static Collection<T> GetOrCreate<T>(ref Collection<T> field)
            => field ??= new Collection<T>();

        public static void Set<T>(ref Collection<T> field, IEnumerable<T> value)
        {
            if (field != value)
            {
                field?.Clear();
                if (value != null)
                {
                    foreach (var s in value)
                    {
                        GetOrCreate(ref field).Add(s);
                    }
                }
            }
        }
    }
}