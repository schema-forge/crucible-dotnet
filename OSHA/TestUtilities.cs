using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;

using static SchemaForge.Crucible.Constraints;

namespace OSHA.TestUtilities
{
  internal class JTokenTranslator : ISchemaTranslator<JToken>
  {
    public bool TryCastValue<TCastType>(JToken collection, string valueName, out TCastType newValue)
    {
      try
      {
        JToken token = collection;
        newValue = token.Value<TCastType>();
        return true;
      }
      catch
      {
        newValue = default;
        return false;
      }
    }
    public bool FieldValueIsNullOrEmpty(JToken collection, string valueName) => collection.IsNullOrEmpty();
    public JToken InsertFieldValue<TDefaultValueType>(JToken collection, string valueName, TDefaultValueType newValue) => throw new NotImplementedException("Cannot insert a value into a JToken. Use JObjectTranslator instead.");
    public bool CollectionContains(JToken collection, string valueName) => collection.Contains(valueName);
    public string CollectionValueToString(JToken collection, string valueName) => collection.ToString();
    public List<string> GetCollectionKeys(JToken collection) => throw new NotImplementedException("A JToken does not always have keys.");
    public string GetEquivalentType(string cSharpType) => cSharpType;
  }
  internal class ObjectTranslator :ISchemaTranslator<Object>
  {
    public bool TryCastValue<TCastType>(object collection, string valueName, out TCastType newValue)
    {
      try
      {
        newValue = (TCastType)Convert.ChangeType(collection,typeof(TCastType));
        return true;
      }
      catch
      {
        newValue = default;
        return false;
      }
    }
    public bool FieldValueIsNullOrEmpty(object collection, string valueName) => false;
    public object InsertFieldValue<TDefaultValueType>(object collection, string valueName, TDefaultValueType newValue) => throw new NotImplementedException("Cannot insert a value into an object. Use DictionaryTranslator instead.");
    public bool CollectionContains(object collection, string valueName) => true;
    public string CollectionValueToString(object collection, string valueName) => collection.ToString();
    public List<string> GetCollectionKeys(object collection) => throw new NotImplementedException("An object does not always have keys.");
    public string GetEquivalentType(string cSharpType) => cSharpType;
  }
  public class TestUtilities
  {
    public static Schema GetTestSchema()
    {
      Schema SubSchema = new(
        new Field<string>("Season1", "The first season."),
        new Field<string>("Season2", "Second season."),
        new Field<string>("Season3", "Third season.")
      );
      return new(new HashSet<Field>()
      {
          new Field<string>("RequiredField","String: A Very Important Field[tm]"),
          new Field<string>("UnrequiredField","String: Too lazy to show up.", required: false),
          new Field<string>("AFieldThatIsNotRequiredButNonethelessHasAValueIfNotIncluded","Indeed.","Default Value"),
          new Field<string>("AllowValues","String: Constrained string.",new Constraint<string>[] { AllowValues("This Is The End","(if you want it)") }),
          new Field<string>("ConstrainStringWithRegexExactPatterns","String: Constrained by Regex patterns [A-z] or [1-3].",new Constraint<string>[] { ConstrainStringWithRegexExact(new Regex("[A-z]*"), new Regex("[1-3]*")) }),
          new Field<string>("ConstrainStringLengthLowerBound", "String: Minimum length of 3.", new Constraint<string>[] { ConstrainStringLengthLowerBound(3) }),
          new Field<string>("ConstrainStringLengthUpperBound", "String: Maximum length of 3.", new Constraint<string>[] { ConstrainStringLengthUpperBound(3) }),
          new Field<string>("ConstrainStringLength", "String: Length must be between 3 and 10.", new Constraint<string>[] { ConstrainStringLength(3, 10) }),
          new Field<string>("ForbidSubstrings", "String: Characters / and * are forbidden.", new Constraint<string>[] { ForbidSubstrings("/", "*") }),
          new Field<int>("ConstrainValueLowerBound", "Int: Number at least 10.", new Constraint<int>[] { ConstrainValueLowerBound(10) }),
          new Field<int>("ConstrainValue", "Int: Number at least 10 and at most 50.", new Constraint<int>[] { ConstrainValue(10, 50) }),
          new Field<int>("ConstrainValueUpperBound", "Int: Number at least 10 and at most 50.", new Constraint<int>[] { ConstrainValueUpperBound(50) }),
          new Field<int>("ConstrainValueDomains", "Int: Within ranges 10-50 or 100-150.", new Constraint<int>[] { ConstrainValue((10, 50), (100, 150)) }),
          new Field<double>("ConstrainDigits", "Double: Number with a maximum of 2 digits.", new Constraint<double>[] { ConstrainDigits<double>(2) }),
          new Field<JObject>("ConstrainCollectionCountLowerBound", "Why have you scrolled this far? The heat of the forge sears me. I can no longer remember the coolness of a summer day.", new Constraint<JObject>[] { ConstrainCollectionCountLowerBound<JObject>(1) }),
          new Field<JObject>("ConstrainCollectionCount", "Burn with me.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(1, 3) }),
          new Field<JObject>("ApplySchema", "Applies the SubSchema object to the value of this field.", new Constraint<JObject>[] { ApplySchema(SubSchema) }),
          new Field<JArray>("ConstrainArrayCountLowerBound", "BURN WITH ME, MARTHA.", new Constraint<JArray>[] { ConstrainCollectionCountLowerBound<JArray>(1) }),
          new Field<JArray>("ConstrainArrayCount", "Anything. Anything for just a moment of relief. Anything to lay my head upon sunbaked asphalt and feel its cold touch. Anything.", new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(1, 5) }),
          new Field<JArray>("ApplyConstraintsToArrayElements", "hurry", new Constraint<JArray>[] { ApplyConstraintsToJArray(AllowValues("under the smelters", "in the tunnels beneath", "help us")) }),
          new Field<DateTime>("ConstrainDateTimeFormat","String input must be a date in ddMMyyyy format.",new Constraint<DateTime>[] { ConstrainDateTimeFormat("ddMMyyyy") }),
          new Field<int>("MatchAnyConstraint","Int: Must match at least one criteria; at most 3, between 5 and 10, at least 12.",new Constraint<int>[] { MatchAnyConstraint(ConstrainValueUpperBound(3),ConstrainValue(5,10),ConstrainValueLowerBound(12)) })
      });
    }

    public static string GetTestJson()
    {
      return @"{
      'RequiredField':'Payable on delivery.',
      'AllowValues':'This Is The End',
      'ConstrainStringWithRegexExactPatterns':'32113',
      'ConstrainStringLengthLowerBound':'Is this long enough, father?',
      'ConstrainStringLength':'Maybe.',
      'ConstrainStringLengthUpperBound':'No.',
      'ForbidSubstrings':'This string does not use any forbidden characters or substrings. Stop looking at it. I promise it contains none of the characters that have been forbidden. You are making it nervous by scrolling this far to the right. The string is self-conscious now, and it is all your fault. I hope you are happy.',
      'ConstrainValueLowerBound':37,
      'ConstrainValue':37,
      'ConstrainValueDomains':37,
      'ConstrainValueUpperBound':37,
      'ConstrainDigits':37.55,
      'ConstrainCollectionCountLowerBound':{'OneItem':'.', 'TweItems':'..', 'ThreeItems':'...'},
      'ConstrainCollectionCount':{'OneItem':'.', 'TweItems':'..', 'ThreeItems':'...'},
      'ApplySchema':{'Season1':'Phantom Blood','Season2':'Stardust Crusaders','Season3':'Diamond Is Unbreakable'},
      'ConstrainArrayCountLowerBound':[1, 2, 3],
      'ConstrainArrayCount':[4, 5, 6],
      'ApplyConstraintsToArrayElements':['under the smelters', 'in the tunnels beneath', 'help us'],
      'ConstrainDateTimeFormat':'28112021',
      'MatchAnyConstraint':5
    }";
    }
  }
}
