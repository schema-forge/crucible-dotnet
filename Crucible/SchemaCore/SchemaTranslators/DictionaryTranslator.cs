using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using SchemaForge.Crucible.Utilities;

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Interprets (string, object) <see cref="Dictionary{TKey, TValue}"/> objects for a <see cref="Schema"/>
  /// object to validate.
  /// </summary>
  public class DictionaryTranslator : ISchemaTranslator<Dictionary<string, object>>
  {
    /// <inheritdoc/>
    public bool CollectionContains(Dictionary<string, object> collection, string valueName) => collection.ContainsKey(valueName);
    /// <inheritdoc/>
    public string CollectionValueToString(Dictionary<string, object> collection, string valueName) => collection[valueName].ToString();
    /// <inheritdoc/>
    public List<string> GetCollectionKeys(Dictionary<string, object> collection) => collection.Keys.ToList();
    /// <inheritdoc/>
    public string GetEquivalentType(string cSharpType) => Conversions.GetEquivalentJsonType(cSharpType);
    /// <inheritdoc/>
    public Dictionary<string, object> InsertFieldValue<TDefaultValueType>(Dictionary<string, object> collection, string valueName, TDefaultValueType newValue)
    {
      collection.Add(valueName, newValue);
      return collection;
    }
    /// <inheritdoc/>
    public bool FieldValueIsNullOrEmpty(Dictionary<string, object> collection, string valueName) => collection[valueName].ToString().IsNullOrEmpty();
    /// <inheritdoc/>
    public bool TryCastValue<TCastType>(Dictionary<string, object> collection, string valueName, out TCastType outputValue)
    {
      if (typeof(TCastType) == typeof(DateTime))
      {
        string value = collection[valueName].ToString();
        bool result = Conversions.TryConvertDateTime(value, out DateTime outDateTime);
        outputValue = (TCastType)(object)outDateTime; // This syntax is required because, even though we're in an
                                                      //   If statement that enforces the rule that TCastType must be DateTime, the compiler is silly.
        return result;
      }
      else if (typeof(TCastType) == typeof(JArray))
      {
        if(collection[valueName].GetType() == typeof(JArray))
        {
          outputValue = (TCastType)collection[valueName];
          return true;
        }
        else
        {
          outputValue = default;
          return false;
        }
      }
      else if (typeof(TCastType) == typeof(JObject))
      {
        try
        {
          string value = collection[valueName].ToString();
          outputValue = (TCastType)(object)JObject.Parse(value);
          return true;
        }
        catch
        {
          outputValue = default;
          return false;
        }
      }
      else
      {
        try
        {
          outputValue = (TCastType)Convert.ChangeType(collection[valueName], typeof(TCastType));
          return true;
        }
        catch
        {
          outputValue = default;
          return false;
        }
      }
    }
  }
}
