#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace schemaforge.Crucible.Extensions
{
  public static class ObjectExtensions
  {
    /// <summary>
    /// Syntactic sugar to check for null.
    /// </summary>
    /// <param name="obj">Object to check its null status.</param>
    /// <returns>True if not null.</returns>
    public static bool Exists(this object obj) => obj is not null;

    /// <summary>
    /// Syntactic sugar to check for null.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="strct">Object to check.</param>
    /// <returns>True if not null, or false if it is.</returns>
    public static bool Exists<T>(this T? strct)
      where T : struct => strct is not null;
  }
}
