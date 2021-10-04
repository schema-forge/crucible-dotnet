using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SchemaForge.Crucible.Extensions
{
  /// <summary>
  /// Contains extensions to the <see cref="JToken"/> class for ease-of-use in SchemaForge.
  /// </summary>
  public static class JTokenExtensions
  {
    /// <summary>
    /// Checks if the given <see cref="JToken"/> is an empty string, contains no array values, has no properties,
    /// is null or undefined, or has a <see cref="JProperty"/> value matching these conditions, depending on type.
    /// </summary>
    /// <param name="token"><see cref="JToken"/> to check. If it is a <see cref="JProperty"/>, the method will be executed on the name and the value of the <see cref="JProperty"/>.</param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(this JToken token)
    {
      if (token == null)
      {
        return true;
      }
      else
      {
        return token.Type switch
        {
          JTokenType.String => string.IsNullOrWhiteSpace(token.ToString()),
          JTokenType.Array => ((JArray)token).Count == 0,
          JTokenType.Object => ((JObject)token).Count == 0,
          JTokenType.Null => true,
          JTokenType.Undefined => true,
          JTokenType.None => string.IsNullOrWhiteSpace(token.ToString()),
          JTokenType.Property => ((JProperty)token).Name.IsNullOrEmpty() || ((JProperty)token).Value.IsNullOrEmpty(),
          _ => false
        };
      }
    }

    /// <summary>
    /// Checks if the <see cref="JToken"/> contains the given item. If the token
    /// is a <see cref="JArray"/>, it checks if one of the <see cref="JArray"/>
    /// items is the passed item. If the token is a string, it will search the
    /// string for the string version of the item. If the <see cref="JToken"/>
    /// is a <see cref="JObject"/>, it will search for a <see cref="JProperty"/> named
    /// after the item. If the token is a <see cref="JProperty"/>, it will run recursively on the Value of the <see cref="JProperty"/>.
    /// </summary>
    /// <param name="token"><see cref="JToken"/> that will be searched.</param>
    /// <param name="item">Item to search for.</param>
    /// <returns>True if the <see cref="JToken"/> contains the item; false if the <see cref="JToken"/> is not a searchable type or it does not contain <paramref name="item"/>.</returns>
    public static bool Contains<T>(this JToken token, T item)
    {
      if (token == null)
      {
        return false;
      }
      else
      {
        return token.Type switch
        {
          JTokenType.String => token.ToString().Contains(item.ToString()),
          JTokenType.Array => ((JArray)token).Contains(item),
          JTokenType.Object => ((JObject)token).ContainsKey(item.ToString()),
          JTokenType.Property => ((JProperty)token).Value.Contains(item),
          _ => false
        };
      }
    }

    /// <summary>
    /// This method is a typed Contains method for <see cref="JArray"/>s, to search for objects of specific types.
    /// </summary>
    /// <typeparam name="T">Type of item to search for.</typeparam>
    /// <param name="input">JArray to search.</param>
    /// <param name="item">Item to search for.</param>
    /// <returns>Bool indicating if the item with the correct type is contained in the array.</returns>
    private static bool Contains<T>(this JArray input, T item)
    {
      /*

      Suppose the user wants to search this JArray for int value 0: [37, "Dio", 55]

      If TryConvert returns default(U) and that is inserted into our new list without question, "Dio" becomes 0, resulting in a scenario where the method thinks there was a real 0, but it was really Dio.

      Therefore, if the attempted conversion fails, the value should not be part of the final list to search, which is why TryConvert returns a tuple, the first item of which indicates if the conversion was a success.

      */
      static (bool, T) TryConvert(JToken input)
      {
        try
        {
          return (true, input.ToObject<T>());
        }
        catch
        {
          return (false, default(T));
        }
      }
      List<T> newList = new();
      foreach (JToken i in input)
      {
        (bool, T) result = TryConvert(i);
        if (result.Item1)
        {
          newList.Add(result.Item2);
        }
      }

      return newList.Contains(item);
    }

    /// <summary>
    /// Attempts to add passed item to the given <see cref="JToken"/>. If the <see cref="JToken"/> is not an array, throws an exception.
    /// </summary>
    /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/> if type of token is not <see cref="JTokenType.Array"/></exception>
    /// <param name="token"><see cref="JToken"/> to add to if it is a <see cref="JArray"/>.</param>
    /// <param name="item">Item to add to the array.</param>
    public static void Add<T>(this JToken token, T item)
    {
      switch (token.Type)
      {
        case JTokenType.Array:
          ((JArray)token).Add(item);
          break;
        default:
          throw new ArgumentException($"Attempted to add item {item} to non-array JToken.");
      }
    }

    /// <summary>
    /// Attempts to add passed item to the given <see cref="JToken"/>. If the <see cref="JToken"/> is not an object, throws an exception.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if type of token is not <see cref="JTokenType.Object"/> or if name is null, empty, or whitespace.</exception>
    /// <param name="token"><see cref="JToken"/> to add to if it is an array.</param>
    /// <param name="name">Name of the new property to add to the <see cref="JObject"/>.</param>
    /// <param name="value">Value of the new property to add to the <see cref="JObject"/>.</param>
    public static void Add(this JToken token, string name, JToken value)
    {
      if (name.IsNullOrEmpty())
      {
        throw new ArgumentException($"Tried to add a property with a null or empty name to JObject {token}");
      }
      switch (token.Type)
      {
        case JTokenType.Object:
          ((JObject)token).Add(name, value);
          break;
        default:
          throw new ArgumentException($"Attempted to add property {name}: {value} to non-object JToken.");
      }
    }
  }
}
