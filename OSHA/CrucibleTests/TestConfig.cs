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
    TestConfig(JObject inputConfig)
    {
      Schema testSchema = new Schema();
      testSchema.AddTokens(new HashSet<ConfigToken>()
            {
                new ConfigToken("RequiredToken","String: A Very Important Token[tm]",ApplyConstraints<string>()),
                new ConfigToken("ConstrainStringValues","String: Constrained string.",false,ApplyConstraints<string>(ConstrainStringValues("This Is The End","(if you want it"))),
                new ConfigToken("ConstrainStringWithRegexExactPatterns","String: Constrained by Regex patterns [A-z] or [1-3].",false,ApplyConstraints<string>(ConstrainStringWithRegexExact(new Regex("[A-z]"), new Regex("[1-3]")))),
                new ConfigToken("ConstrainStringLengthLowerBound","String: Minimum length of 3.",false,ApplyConstraints<string>(ConstrainStringLength(3))),
                new ConfigToken("ConstrainStringLength","String: Length must be between 3 and 10.",false,ApplyConstraints<string>(ConstrainStringLength(3,10))),
                new ConfigToken("ForbidStringCharacters","String: Characters / and * are forbidden.",false,ApplyConstraints<string>(ForbidStringCharacters('/','*'))),
                new ConfigToken("ConstrainNumericValueLowerBound","Int: Number at least 10.",false,ApplyConstraints<int>(ConstrainNumericValue(10))),
                new ConfigToken("ConstrainNumericValue","Int: Number at least 10 and at most 50.",false,ApplyConstraints<int>(ConstrainNumericValue(10,50))),
                new ConfigToken("ConstrainNumericValueDomains","Int: Within ranges 10-50 or 100-150.",false,ApplyConstraints<int>(ConstrainNumericValue((10, 50), (100, 150)))),
                //new ConfigToken("ConstrainJsonTokensRequired","Obligatory decoy Json.",false,ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())}))),
                //new ConfigToken("ConstrainJsonTokens","Other obligatory decoy Json.",false,ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())},new ConfigToken[] { new ConfigToken("OptionalInnerToken","String: Optional inner token.",ApplyConstraints<string>()) }))),
                new ConfigToken("ConstrainPropertyCountLowerBound","Why have you scrolled this far? The heat of the forge sears me. I can no longer remember the coolness of a summer day.",false,ApplyConstraints<JObject>(ConstrainPropertyCount(1))),
                new ConfigToken("ConstrainPropertyCount","Burn with me.",false,ApplyConstraints<JObject>(ConstrainPropertyCount(1,3))),
                new ConfigToken("ConstrainArrayCountLowerBound","BURN WITH ME, MARTHA.",false,ApplyConstraints<JArray>(ConstrainArrayCount(1))),
                new ConfigToken("ConstrainArrayCount","Anything. Anything for just a moment of relief. Anything to lay my head upon sunbaked asphalt and feel its cold touch. Anything.",false,ApplyConstraints<JArray>(ConstrainArrayCount(1,5))),
                new ConfigToken("ApplyConstraintsToArrayElements","hurry",false,ApplyConstraints<JArray>(ApplyConstraintsToAllArrayValues(ApplyConstraints<string>(ConstrainStringValues("under the smelters", "in the tunnels beneath", "help us")))))
            });
    }
  }
}
