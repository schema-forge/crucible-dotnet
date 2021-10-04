using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaForge.Crucible.Extensions
{

  /// <summary>
  /// Contains extensions to the <see cref="IEnumerable"/> class for ease-of-use in SchemaForge.
  /// </summary>
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
    /// Checks to see if any error in the calling error collection has severity Fatal.
    /// </summary>
    /// <param name="errorCollection">Error collection to search.</param>
    /// <returns>Bool indicating if the error collection contains a fatal error.</returns>
    public static bool AnyFatal(this IEnumerable<Error> errorCollection)
    {
      List<Severity> fatalTypes = new() { Severity.Fatal };
      return errorCollection.Any(x => fatalTypes.Contains(x.ErrorSeverity));
    }

    /// <summary>
    /// If the source is ICollection, returns Count property. Otherwise instantiates an enumerator and retrieves the count through iteration.
    /// </summary>
    /// <param name="source">IEnumerable to count.</param>
    /// <returns>Number of elements in the IEnumerable.</returns>
    public static int Count(this IEnumerable source)
    {
      if (source is ICollection col)
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
        if (resource is IDisposable d)
          d.Dispose();
      }
    }
  }
}

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Contains AnyFatal, an extension method that indicates whether or not a collection of Errors contains any of Severity.Fatal.
  /// An important method for consumers of the library.
  /// </summary>
  public static class IEnumerableExtensionsPublic
  {
    /// <summary>
    /// Checks to see if any error in the calling error collection has severity Fatal.
    /// </summary>
    /// <param name="errorCollection">Error collection to search.</param>
    /// <returns>Bool indicating if the error collection contains a fatal error.</returns>
    public static bool AnyFatal(this IList<Error> errorCollection)
    {
      for (int i = errorCollection.Count; i-- > 0;)
      {
        if (errorCollection[i].ErrorSeverity == Severity.Fatal)
        {
          return true;
        }
      }
      return false;
    }
  }
}
