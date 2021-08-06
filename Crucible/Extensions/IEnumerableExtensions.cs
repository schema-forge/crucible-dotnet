using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaForge.Crucible.Extensions
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
    public static string Join<T>(this IEnumerable<T> enumerable, char separator) => string.Join(separator, enumerable);

    /// <summary>
    /// Performs a <see cref="string.Join{T}(string, IEnumerable{T})"/>
    /// operation on enumerable using the given <see cref="string"/>
    /// seperator. <see cref="object.ToString"/> is called on each item.
    /// </summary>
    /// <typeparam name="T">Underlying type in <paramref name="enumerable"/></typeparam>
    /// <param name="enumerable">Enumerable object to be joined.</param>
    /// <param name="separator">Separator to be used to delimit items.</param>
    /// <returns><paramref name="separator"/>-delimited <see cref="string"/></returns>
    public static string Join<T>(this IEnumerable<T> enumerable, string separator) => string.Join(separator, enumerable);

    /// <summary>
    /// Checks to see if any error in the calling error collection has severity Fatal or NullOrEmpty unless NullOrEmpty is explicitly allowed.
    /// </summary>
    /// <param name="errorCollection">Error collection to search.</param>
    /// <param name="nullOrEmptyIsFatal">If true, NullOrEmpty is a fatal error.</param>
    /// <returns>Bool indicating if the error collection contains a fatal error.</returns>
    public static bool AnyFatal(this IEnumerable<Error> errorCollection, bool nullOrEmptyIsFatal = true)
    {
      List<Severity> fatalTypes = new() { Severity.Fatal };
      if(nullOrEmptyIsFatal)
      {
        fatalTypes.Add(Severity.NullOrEmpty);
      }
      return errorCollection.Any(x => fatalTypes.Contains(x.ErrorSeverity));
    }

    /// <summary>
    /// Checks to see if any error in the calling error collection has severity Fatal or NullOrEmpty unless NullOrEmpty is explicitly allowed.
    /// </summary>
    /// <param name="errorCollection">Error collection to search.</param>
    /// <param name="nullOrEmptyIsFatal">If true, NullOrEmpty is a fatal error.</param>
    /// <returns>Bool indicating if the error collection contains a fatal error.</returns>
    public static bool AnyFatal(this IList<Error> errorCollection, bool nullOrEmptyIsFatal = true)
    {
      List<Severity> fatalTypes = new() { Severity.Fatal };
      if (nullOrEmptyIsFatal)
      {
        fatalTypes.Add(Severity.NullOrEmpty);
      }
      for(int i=errorCollection.Count; i-- > 0;)
      {
        if(fatalTypes.Contains(errorCollection[i].ErrorSeverity))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// If the source is ICollection, returns Count property. Otherwise instantiates an enumerator and retrieves the count through iteration.
    /// </summary>
    /// <param name="source">IEnumerable to count.</param>
    /// <returns>Number of elements in the IEnumerable.</returns>
    public static int Count(this IEnumerable source)
    {
      ICollection col = source as ICollection;
      if (col != null)
        return col.Count;

      int c = 0;
      IEnumerator e = source.GetEnumerator();
      DynamicUsing(e, () =>
      {
        while (e.MoveNext())
          c++;
      });

      return c;
    }
    private static void DynamicUsing(object resource, Action action)
    {
      try
      {
        action();
      }
      finally
      {
        IDisposable d = resource as IDisposable;
        if (d != null)
          d.Dispose();
      }
    }
  }
}
