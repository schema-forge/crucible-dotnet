using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace schemaforge.Crucible.Extensions
{
  public static class IEnumerableExtensions
  {
    /// <summary>
    /// Performs a <see cref="string.Join{T}(char, IEnumerable{T})"/> operation
    /// on enumerable using the given <see cref="char"/> seperator.
    /// <see cref="object.ToString"/> is called on each item.
    /// </summary>
    /// <typeparam name="T">Underlying type in <paramref name="enumerable"/></typeparam>
    /// <param name="enumerable">Enumerable object to be joined.</param>
    /// <param name="separator">Separator to be used to delimit items.</param>
    /// <returns><paramref name="separator"/>-delimited <see cref="string"/>.</returns>
    public static string Join<T>(this IEnumerable<T> enumerable, char separator)
    {
      return string.Join(separator, enumerable);
    }

    /// <summary>
    /// Performs a <see cref="string.Join{T}(string, IEnumerable{T})"/>
    /// operation on enumerable using the given <see cref="string"/>
    /// seperator. <see cref="object.ToString"/> is called on each item.
    /// </summary>
    /// <typeparam name="T">Underlying type in <paramref name="enumerable"/></typeparam>
    /// <param name="enumerable">Enumerable object to be joined.</param>
    /// <param name="separator">Separator to be used to delimit items.</param>
    /// <returns><paramref name="separator"/>-delimited <see cref="string"/></returns>
    public static string Join<T>(this IEnumerable<T> enumerable, string separator)
    {
      return string.Join(separator, enumerable);
    }
  }
}
