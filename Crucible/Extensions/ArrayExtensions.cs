using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaForge.Crucible.Extensions
{
  /// <summary>
  /// Contains extensions to the <see cref="Array"/> class for ease-of-use in SchemaForge.
  /// </summary>
  public static class ArrayExtensions
  {
    /// <summary>
    /// Shallow copies the <paramref name="source"/> array.
    /// </summary>
    /// <typeparam name="T">Type of the data contained in the array.</typeparam>
    /// <param name="source">Source array to be shallow copied.</param>
    /// <returns>Copy of the <paramref name="source"/> array.</returns>
    public static T[] CloneArray<T>(this T[] source)
    {
      _ = source ?? throw new ArgumentNullException(nameof(source));
      return (T[])source.Clone();
    }

    /// <summary>
    /// Performs an in-place reversal on the <paramref name="source"/>
    /// and returns it.
    /// </summary>
    /// <typeparam name="T">Type of the data contained in the array.</typeparam>
    /// <param name="source">Source array, modified in place.</param>
    /// <exception cref="ArgumentNullException">
    /// If the <paramref name="source"/> array is null.
    /// </exception>
    /// <returns>The <paramref name="source"/> array.</returns>
    public static T[] Reverse<T>(this T[] source)
    {
      Array.Reverse(source);
      return source;
    }
  }
}
