using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator
{
    public static class EnumeratorExtension
    {
        public static List<T> ToList<T>(this IEnumerator<T> enumerator)
        {
            List<T> result = new List<T>();

            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }
    }

}
