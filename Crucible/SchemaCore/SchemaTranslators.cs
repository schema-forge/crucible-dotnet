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
    public (bool, TCastType) TryCastToken<TCastType>(TCollectionType collection, string valueName);
    public bool TokenIsNullOrEmpty(TCollectionType collection, string valueName);
    public TCollectionType InsertToken<TDefaultValueType>(TCollectionType collection, string valueName, TDefaultValueType newValue);
    public bool CollectionContains(TCollectionType collection, string valueName);
    public string CollectionValueToString(TCollectionType collection, string valueName);
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
  public class JTokenTranslator : ISchemaTranslator<JToken, JToken>
  {
    public (bool, TCastType) TryCastToken<TCastType>(JToken collection, string valueName)
    {
      try
      {
        JToken token = collection;
        TCastType result = token.Value<TCastType>();
        return (true, result);
      }
      catch
      {
        return (false, default);
      }
    }
    public bool TokenIsNullOrEmpty(JToken collection, string valueName) => collection.IsNullOrEmpty();
    public JToken InsertToken<TDefaultValueType>(JToken collection, string valueName, TDefaultValueType newValue) => throw new NotImplementedException("Cannot insert a value into a JToken. Use JObjectTranslator instead.");
    public bool CollectionContains(JToken collection, string valueName) => collection.Contains(valueName);
    public string CollectionValueToString(JToken collection, string valueName) => collection.ToString();
    public List<string> GetCollectionKeys(JToken collection) => throw new NotImplementedException("A JToken does not always have keys.");
  }
}
