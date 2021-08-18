using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;

namespace OSHA.TestUtilities
{
  internal class JTokenTranslator : ISchemaTranslator<JToken, JToken>
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
