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
    public class TestConfig : SchemaController
    {
        TestConfig(JObject inputConfig)
        {
            UserConfig = inputConfig;
            RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
            {
                new ConfigToken("RequiredToken","String: A Very Important Token[tm]",ApplyConstraints<string>())
            });

            OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
            {
                new ConfigToken("ConstrainStringValues","String: Constrained string.",ApplyConstraints<string>(ConstrainStringValues("This Is The End","(if you want it"))),
                new ConfigToken("ConstrainStringWithRegexExactPatterns","String: Constrained by Regex patterns [A-z] or [1-3].",ApplyConstraints<string>(ConstrainStringWithRegexExact(new Regex("[A-z]"), new Regex("[1-3]")))),
                new ConfigToken("ConstrainStringLengthLowerBound","String: Minimum length of 3.",ApplyConstraints<string>(ConstrainStringLength(3))),
                new ConfigToken("ConstrainStringLength","String: Length must be between 3 and 10.",ApplyConstraints<string>(ConstrainStringLength(3,10))),
                new ConfigToken("ForbidStringCharacters","String: Characters / and * are forbidden.",ApplyConstraints<string>(ForbidStringCharacters('/','*'))),
                new ConfigToken("ConstrainNumericValueLowerBound","Int: Number at least 10.",ApplyConstraints<int>(ConstrainNumericValue(10))),
                new ConfigToken("ConstrainNumericValue","Int: Number at least 10 and at most 50.",ApplyConstraints<int>(ConstrainNumericValue(10,50))),
                new ConfigToken("ConstrainNumericValueDomains","Int: Within ranges 10-50 or 100-150.",ApplyConstraints<int>(ConstrainNumericValue((10, 50), (100, 150)))),
                new ConfigToken("ConstrainJsonTokensRequired","Obligatory decoy Json.",ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())}))),
                new ConfigToken("ConstrainJsonTokens","Other obligatory decoy Json.",ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())},new ConfigToken[] { new ConfigToken("OptionalInnerToken","String: Optional inner token.",ApplyConstraints<string>()) }))),
                new ConfigToken("ConstrainPropertyCountLowerBound","Why have you scrolled this far? The heat of the forge sears me. I can no longer remember the coolness of a summer day.",ApplyConstraints<JObject>(ConstrainPropertyCount(1))),
                new ConfigToken("ConstrainPropertyCount","Burn with me.",ApplyConstraints<JObject>(ConstrainPropertyCount(1,3))),
                new ConfigToken("ConstrainArrayCountLowerBound","BURN WITH ME, MARTHA.",ApplyConstraints<JArray>(ConstrainArrayCount(1))),
                new ConfigToken("ConstrainArrayCount","Anything. Anything for just a moment of relief. Anything to lay my head upon sunbaked asphalt and feel its cold touch. Anything.",ApplyConstraints<JArray>(ConstrainArrayCount(1,5))),
                new ConfigToken("ApplyConstraintsToArrayElements","hurry",ApplyConstraintsToAllArrayValues<string>(ConstrainStringValues("under the smelters", "in the tunnels beneath", "help us")))
            });
        }
    }
}
