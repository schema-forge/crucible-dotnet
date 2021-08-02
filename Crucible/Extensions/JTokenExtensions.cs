using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace schemaforge.Crucible.Extensions
{
  public static class JTokenExtensions
  {
    /// <summary>
    /// Checks if the given JToken is an empty string, contains no array values, has no properties,
    /// is null or undefined, or has a JProperty value matching these conditions, depending on type.
    /// </summary>
    /// <param name="token">JToken to check. If it is a JProperty, the method will be executed on the name and the value of the JProperty.</param>
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
    /// Checks if the JToken contains the given item. If the token is array, it checks if one of the array items is the passed item. If the token is a string, it will search the string for the string version of the item. If the token is an object, it will search for a property named after the item. If the token is a property, it will run recursively on the property's value.
    /// </summary>
    /// <param name="token">Token that will be searched.</param>
    /// <param name="item">Item to search for.</param>
    /// <returns>True if the token contains the item; false if the token is not a searchable type.</returns>
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
    /// Attempts to add passed item to the given JToken. If the JToken is not an array, throws an exception.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if type of token is not JTokenType.Array</exception>
    /// <param name="token">JToken to add to if it is an array.</param>
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
    /// Attempts to add passed item to the given JToken. If the JToken is not an object, throws an exception.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if type of token is not JTokenType.Object or if name is null, empty, or whitespace.</exception>
    /// <param name="token">JToken to add to if it is an array.</param>
    /// <param name="name">Name of the new property to add to the object.</param>
    /// <param name="value">Value of the new property to add to the object.</param>
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
