using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaForge.Crucible
{
  /// <summary>
  /// An <see cref="ISchemaTranslator{TCollectionType, TValueType}"/>
  /// interprets a <typeparamref name="TCollectionType"/>
  /// for a <see cref="Schema"/> so that it can be validated.
  /// </summary>
  /// <typeparam name="TCollectionType"></typeparam>
  public interface ISchemaTranslator<TCollectionType>
  {
    /// <summary>
    /// Extracts a value from <paramref name="collection"/> with the designation
    /// <paramref name="valueName"/> and attempts to cast that value to <typeparamref name="TCastType"/>
    /// </summary>
    /// <typeparam name="TCastType">The extracted value will be cast to this type.</typeparam>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <returns>(bool, <typeparamref name="TCastType"/>) where bool indicates
    /// success of the cast and <typeparamref name="TCastType"/> is the cast
    /// value if successful, default otherwise.</returns>
    public bool TryCastToken<TCastType>(TCollectionType collection, string valueName, out TCastType outputValue);

    /// <summary>
    /// Extracts a value from <paramref name="collection"/> with the designation
    /// <paramref name="valueName"/> and determines whether this value is null or empty.
    /// </summary>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <returns>bool indicating if the value is null or empty.</returns>
    public bool TokenIsNullOrEmpty(TCollectionType collection, string valueName);

    /// <summary>
    /// Inserts <paramref name="newValue"/> into <paramref name="collection"/>
    /// with string designation <paramref name="valueName"/> and returns a
    /// new <typeparamref name="TCollectionType"/> with the value inserted.
    /// </summary>
    /// <typeparam name="TDefaultValueType">Type of the value to be inserted.</typeparam>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <param name="newValue">The new value to be inserted.</param>
    /// <returns>New <typeparamref name="TCollectionType"/> with value inserted.</returns>
    public TCollectionType InsertToken<TDefaultValueType>(TCollectionType collection, string valueName, TDefaultValueType newValue);

    /// <summary>
    /// Searches <paramref name="collection"/> for a value with string designation <paramref name="valueName"/>
    /// </summary>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <returns>bool indicating whether or not <paramref name="collection"/> contains a value with string designation <paramref name="valueName"/></returns>
    public bool CollectionContains(TCollectionType collection, string valueName);

    /// <summary>
    /// Extracts a value from <paramref name="collection"/> with the designation
    /// <paramref name="valueName"/> and returns its string representation.
    /// </summary>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <returns>string representation of <paramref name="valueName"/> extracted from <paramref name="collection"/></returns>
    public string CollectionValueToString(TCollectionType collection, string valueName);

    /// <summary>
    /// Extracts a list of all string representations of tokens inside <paramref name="collection"/>.
    /// </summary>
    /// <param name="collection">The collection from which to extract all keys.</param>
    /// <returns>List{string} containing all collection keys.</returns>
    public List<string> GetCollectionKeys(TCollectionType collection);

    /// <summary>
    /// Takes the name of a valid C# data type and translates it to the
    /// equivalent data type in <see cref="TCollectionType"/>
    /// </summary>
    /// <param name="cSharpType">Name of a valid C# data type.</param>
    /// <returns>Name of the corresponding type in <see cref="TCollectionType"/></returns>
    public string GetEquivalentType(string cSharpType);
  }
  /// <summary>
  /// Interprets <see cref="JObject"/> objects for a <see cref="Schema"/> object to validate.
  /// Requires no parameters.
  /// </summary>
  public class JObjectTranslator : ISchemaTranslator<JObject>
  {
    Dictionary<string, string> TypeMap = new()
    {
      { "Byte", "number" },
      { "SByte", "number" },
      { "Single", "number" },
      { "Double", "number" },
      { "Decimal", "number" },
      { "Int16", "number" },
      { "UInt16", "number" },
      { "Int32", "number" },
      { "UInt32", "number" },
      { "Int64", "number" },
      { "UInt64", "number" },
      { "JObject", "object" },
      { "Boolean", "boolean" },
      { "DateTime", "string" },
      { "JArray", "array" },
      { "String", "string" },
      { "Char", "string" }
    };
    public bool TryCastToken<TCastType>(JObject collection, string valueName, out TCastType outputValue)
    {
      try
      {
        JToken token = collection[valueName];
        outputValue = token.Value<TCastType>();
        return true;
      }
      catch
      {
        outputValue = default;
        return false;
      }
    }
    public bool TokenIsNullOrEmpty(JObject collection, string valueName) => collection[valueName].IsNullOrEmpty();
    public JObject InsertToken<TDefaultValueType>(JObject collection, string valueName, TDefaultValueType newValue)
    {
      collection.Add(valueName, new JValue(newValue));
      return collection;
    }
    public bool CollectionContains(JObject collection, string valueName) => collection.ContainsKey(valueName);
    public List<string> GetCollectionKeys(JObject collection) => collection.Properties().Select(x => x.Name).ToList();
    public string CollectionValueToString(JObject collection, string valueName) => collection[valueName].ToString();
    public string GetEquivalentType(string cSharpType) => $"Json " + (TypeMap.ContainsKey(cSharpType) ? TypeMap[cSharpType] : cSharpType.Contains("[]") ? "array" : "null");
  }
}
