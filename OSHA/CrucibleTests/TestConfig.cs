﻿using System;
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
      Schema testSchema = new();
      testSchema.AddTokens(new HashSet<ConfigToken>()
            {
                new ConfigToken<string>("RequiredToken","String: A Very Important Token[tm]"),
                new ConfigToken<string>("AllowValues","String: Constrained string.",new Constraint<string>[] { AllowValues("This Is The End","(if you want it") }, required: false),
                new ConfigToken<string>("ConstrainStringWithRegexExactPatterns","String: Constrained by Regex patterns [A-z] or [1-3].",new Constraint<string>[] { ConstrainStringWithRegexExact(new Regex("[A-z]"), new Regex("[1-3]")) }, required: false),
                new ConfigToken<string>("ConstrainStringLengthLowerBound","String: Minimum length of 3.",new Constraint<string>[] { ConstrainStringLength(3) }, required: false),
                new ConfigToken<string>("ConstrainStringLength","String: Length must be between 3 and 10.",new Constraint<string>[] { ConstrainStringLength(3,10) }, required: false),
                new ConfigToken<string>("ForbidSubstrings","String: Characters / and * are forbidden.",new Constraint<string>[] { ForbidSubstrings("/","*") }, required: false),
                new ConfigToken<int>("ConstrainValueLowerBound","Int: Number at least 10.",new Constraint<int>[] { ConstrainValueLowerBound(10) }, required: false),
                new ConfigToken<int>("ConstrainValue","Int: Number at least 10 and at most 50.",new Constraint<int>[] { ConstrainValue(10,50) }, required: false),
                new ConfigToken<int>("ConstrainValueDomains","Int: Within ranges 10-50 or 100-150.",new Constraint<int>[] { ConstrainValue((10, 50), (100, 150)) }, required: false),
                //new ConfigToken("ConstrainJsonTokensRequired","Obligatory decoy Json.",false,ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())}))),
                //new ConfigToken("ConstrainJsonTokens","Other obligatory decoy Json.",false,ApplyConstraints<JObject>(ConstrainJsonTokens(new ConfigToken[] { new ConfigToken("RequiredInnerToken","String: Required inner token.",ApplyConstraints<string>())},new ConfigToken[] { new ConfigToken("OptionalInnerToken","String: Optional inner token.",ApplyConstraints<string>()) }))),
                new ConfigToken<JObject>("ConstrainCollectionCountLowerBound","Why have you scrolled this far? The heat of the forge sears me. I can no longer remember the coolness of a summer day.",new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(1) }, required: false),
                new ConfigToken<JObject>("ConstrainCollectionCount","Burn with me.",new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(1,3) }, required: false),
                new ConfigToken<JArray>("ConstrainCollectionCountLowerBound","BURN WITH ME, MARTHA.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(1) }, required: false),
                new ConfigToken<JArray>("ConstrainCollectionCount","Anything. Anything for just a moment of relief. Anything to lay my head upon sunbaked asphalt and feel its cold touch. Anything.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(1,5) }, required: false),
                new ConfigToken<JArray>("ApplyConstraintsToArrayElements","hurry",new Constraint<JArray>[] { ApplyConstraintsToJArray(AllowValues("under the smelters", "in the tunnels beneath", "help us")) }, required: false)
            });
    }
  }
}
