using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;

namespace ConstraintTests
{
  [Trait("Crucible", "")]
  public class JObjectConstraintTests
  {
    private readonly ITestOutputHelper output;

    public JObjectConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests ApplySchema using a constructed schema with two required tokens.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json that will be tested against the schema.</param>
    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealtokenbutitwasme':'DIO'}")]
    public void ConstrainJsonTokensTests(bool expectedResult, string constrainedJson)
    {
      Schema appliedSchema = new(new ConfigToken[] {
            new ConfigToken<bool>("Ripe","Bool: Indicates whether or not the fruit is ripe."),
            new ConfigToken<int>("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",new Constraint<int>[] { ConstrainValue((0,5),(10,15)) })
          });
      ConfigToken<JObject> testToken = new("FruitProperties", "Json: Additional properties of the fruit in question.", new Constraint<JObject>[] { ApplySchema(appliedSchema) });
      bool testResult = testToken.Validate(JObject.Parse(constrainedJson),new JTokenTranslator());
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests ApplySchema using a constructed schema with two required tokens and one optional token.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json that will be tested against the schema.</param>
    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3,'Color':'bloo'}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'Color':['youthoughtitwasarealcolorbutitwasmeDIO']}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealtokenbutitwasme':'DIO'}")]
    public void ConstrainJsonTokensWithOptionalTests(bool expectedResult, string constrainedJson)
    {
      Schema appliedSchema = new(new ConfigToken[] {
            new ConfigToken<bool>("Ripe","Bool: Indicates whether or not the fruit is ripe."),
            new ConfigToken<int>("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",new Constraint<int>[] { ConstrainValue((0,5),(10,15)) }),
            new ConfigToken<string>("Color","String: Indicates the color of the fruit.",required: false)
          });
      ConfigToken<JObject> testToken = new("FruitProperties", "Json: Additional properties of the fruit in question.", new Constraint<JObject>[] { ApplySchema(appliedSchema) });
      bool testResult = testToken.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// ConstrainCollectionCount tests for JObjects. For JObjects, ConstrainCollectionCount counts the number of properties, since JObjects are really just dictionaries of string and JToken.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json that will be checked for its property count.</param>
    /// <param name="constraints">Arguments that will be passed to ConstrainCollectionCount.</param>
    [Theory]
    [InlineData(true, "{'DecoyProperty':''}", 1)]
    [InlineData(false, "{}", 1)]
    [InlineData(true, "{'DecoyProperty':'','AnotherDecoyProperty':'','AThirdDecoyProperty':''}", 1, 3)]
    [InlineData(false, "{}", 1, 3)]
    [InlineData(false, "{'DecoyProperty':'','AnotherDecoyProperty':'','AThirdDecoyProperty':'','OneTooMany':''}", 1, 3)]
    [InlineData(false, "{}", 3, 1)] // Exception test.
    public void ConstrainCollectionCountTests(bool expectedResult, string constrainedJson, params int[] constraints)
    {
      ConfigToken<JObject> testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken<JObject>("TestToken", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(constraints[0]) });
        testResult = testToken.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken<JObject>("TestToken", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(constraints[0], constraints[1]) }));
          return;
        }
        else
        {
          testToken = new ConfigToken<JObject>("TestToken", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(constraints[0], constraints[1]) });
          testResult = testToken.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }
  }
}
