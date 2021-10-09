using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using static SchemaForge.Crucible.Constraints;

namespace Crucible
{
  public class TestConfig
  {
    public static Schema testSchema = new(new HashSet<ConfigToken>()
    {
        new ConfigToken<string>("RequiredToken","String: A Very Important Token[tm]"),
        new ConfigToken<string>("Unrequired Token","String: Too lazy to show up.", required: false),
        new ConfigToken<string>("AllowValues","String: Constrained string.",new Constraint<string>[] { AllowValues("This Is The End","(if you want it)") }),
        new ConfigToken<string>("ConstrainStringWithRegexExactPatterns","String: Constrained by Regex patterns [A-z] or [1-3].",new Constraint<string>[] { ConstrainStringWithRegexExact(new Regex("[A-z]"), new Regex("[1-3]")) }),
        new ConfigToken<string>("ConstrainStringLengthLowerBound", "String: Minimum length of 3.", new Constraint<string>[] { ConstrainStringLengthLowerBound(3) }),
        new ConfigToken<string>("ConstrainStringLength", "String: Length must be between 3 and 10.", new Constraint<string>[] { ConstrainStringLength(3, 10) }),
        new ConfigToken<string>("ForbidSubstrings", "String: Characters / and * are forbidden.", new Constraint<string>[] { ForbidSubstrings("/", "*") }),
        new ConfigToken<int>("ConstrainValueLowerBound", "Int: Number at least 10.", new Constraint<int>[] { ConstrainValueLowerBound(10) }),
        new ConfigToken<int>("ConstrainValue", "Int: Number at least 10 and at most 50.", new Constraint<int>[] { ConstrainValue(10, 50) }),
        new ConfigToken<int>("ConstrainValueDomains", "Int: Within ranges 10-50 or 100-150.", new Constraint<int>[] { ConstrainValue((10, 50), (100, 150)) }),
        new ConfigToken<JObject>("ConstrainCollectionCountLowerBound", "Why have you scrolled this far? The heat of the forge sears me. I can no longer remember the coolness of a summer day.", new Constraint<JObject>[] { ConstrainCollectionCountLowerBound<JObject>(1) }),
        new ConfigToken<JObject>("ConstrainCollectionCount", "Burn with me.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(1, 3) }),
        new ConfigToken<JArray>("ConstrainArrayCountLowerBound", "BURN WITH ME, MARTHA.", new Constraint<JArray>[] { ConstrainCollectionCountLowerBound<JArray>(1) }),
        new ConfigToken<JArray>("ConstrainArrayCount", "Anything. Anything for just a moment of relief. Anything to lay my head upon sunbaked asphalt and feel its cold touch. Anything.", new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(1, 5) }),
        new ConfigToken<JArray>("ApplyConstraintsToArrayElements", "hurry", new Constraint<JArray>[] { ApplyConstraintsToJArray(AllowValues("under the smelters", "in the tunnels beneath", "help us")) })
    });

    public static string testJson =
  @"{
      'RequiredToken':'Payable on delivery.',
      'AllowValues':'This Is The End',
      'ConstrainStringWithRegexExactPatterns':'32113',
      'ConstrainStringLengthLowerBound':'Is this long enough, father?',
      'ContrainStringLength':'Maybe.',
      'ForbidSubstrings':'This string does not use any forbidden characters or substrings. Stop looking at it. I promise it contains none of the characters that have been forbidden. You're making it nervous by scrolling this far to the right. The string is self-conscious now, and it's all your fault. I hope you're happy.',
      'ConstrainValueLowerBound':37,
      'ConstrainValue':37,
      'ConstrainValueDomains':37,
      'ConstrainCollectionCountLowerBound':{'One item':'.', 'Two items':'..', 'Three items':'...'},
      'ConstrainCollectionCount':{'One item':'.', 'Two items':'..', 'Three items':'...'},
      'ConstrainArrayCountLowerBound':[1, 2, 3],
      'ConstrainArrayCount':[4, 5, 6],
      'ApplyConstraintsToArrayElements':['under the smelters', 'in the tunnels beneath', 'help us']
    }";
  }
}
