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
  public interface ISchemaTranslator<TCollectionType, TValueType>
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
    public (bool, TCastType) TryCastToken<TCastType>(TCollectionType collection, string valueName);

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
    /// <paramref name="newValue"/> should be interpreted and cast to
    /// <typeparamref name="TValueType"/>.
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
  }
  public class JObjectTranslator : ISchemaTranslator<JObject, JToken>
  {
    public (bool, TCastType) TryCastToken<TCastType>(JObject collection, string valueName)
    {
      try
      {
        JToken token = collection[valueName];
        TCastType result = token.Value<TCastType>();
        return (true, result);
      }
      catch
      {
        return (false, default);
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
  }
}
