﻿using System;
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
  /// Interprets <see cref="JObject"/> objects for a <see cref="Schema"/> object to validate.
  /// Requires no constructor parameters.
  /// </summary>
  public class JObjectTranslator : ISchemaTranslator<JObject>
  {
    /// <inheritdoc/>
    public bool TryCastValue<TCastType>(JObject collection, string valueName, out TCastType outputValue)
    {
      if (typeof(TCastType) == typeof(DateTime))
      {
        bool result = Conversions.TryConvertDateTime(collection[valueName].ToString(), out DateTime outDateTime);
        outputValue = (TCastType)(object)outDateTime; // This syntax is required because, even though we're in an
                                                      //   If statement that enforces the rule that TCastType must be DateTime, the compiler is silly.
        return result;
      }
      else
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
    }
    /// <inheritdoc/>
    public bool FieldValueIsNullOrEmpty(JObject collection, string valueName) => collection[valueName].IsNullOrEmpty();
    /// <inheritdoc/>
    public JObject InsertFieldValue<TDefaultValueType>(JObject collection, string valueName, TDefaultValueType newValue)
    {
      collection.Add(valueName, new JValue(newValue));
      return collection;
    }
    /// <inheritdoc/>
    public bool CollectionContains(JObject collection, string valueName) => collection.ContainsKey(valueName);
    /// <inheritdoc/>
    public List<string> GetCollectionKeys(JObject collection) => collection.Properties().Select(x => x.Name).ToList();
    /// <inheritdoc/>
    public string CollectionValueToString(JObject collection, string valueName) => collection[valueName].ToString();
    /// <inheritdoc/>
    public string GetEquivalentType(string cSharpType) => Conversions.GetEquivalentJsonType(cSharpType);
  }
}
