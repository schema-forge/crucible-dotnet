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
  public class JObjectConstraintTests : Schema
  {
    private readonly ITestOutputHelper output;

    public JObjectConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealtokenbutitwasme':'DIO'}")]
    public void ConstrainJsonTokensTests(bool expectedResult, string constrainedJson)
    {
      ConfigToken TestToken =
        new ConfigToken("FruitProperties", "Json: Additional properties of the fruit in question.", ApplyConstraints<JObject>(ConstrainJsonTokens(
          new ConfigToken[] {
            new ConfigToken("Ripe","Bool: Indicates whether or not the fruit is ripe.",ApplyConstraints<bool>()),
            new ConfigToken("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",ApplyConstraints<int>(ConstrainNumericValue((0,5),(10,15))))
          })));
      bool testResult = TestToken.Validate(JObject.Parse(constrainedJson));
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3,'Color':'bloo'}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'Color':['youthoughtitwasarealcolorbutitwasmeDIO']}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealtokenbutitwasme':'DIO'}")]
    public void ConstrainJsonTokensWithOptionalTests(bool expectedResult, string constrainedJson)
    {
      ConfigToken TestToken =
        new ConfigToken("FruitProperties", "Json: Additional properties of the fruit in question.", ApplyConstraints<JObject>(ConstrainJsonTokens(
          new ConfigToken[]
            {
              new ConfigToken("Ripe","Bool: Indicates whether or not the fruit is ripe.",ApplyConstraints<bool>()),
              new ConfigToken("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",ApplyConstraints<int>(ConstrainNumericValue((0,5),(10,15))))
            },
          new ConfigToken[] 
            {
              new ConfigToken("Color","String: Indicates the color of the fruit.",ApplyConstraints<string>())
            })));
      bool testResult = TestToken.Validate(JObject.Parse(constrainedJson));
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [InlineData(true, "{'DecoyProperty':''}", 1)]
    [InlineData(false, "{}", 1)]
    [InlineData(true, "{'DecoyProperty':'','AnotherDecoyProperty':'','AThirdDecoyProperty':''}", 1, 3)]
    [InlineData(false, "{}", 1, 3)]
    [InlineData(false, "{'DecoyProperty':'','AnotherDecoyProperty':'','AThirdDecoyProperty':'','OneTooMany':''}", 1, 3)]
    [InlineData(false, "{}", 3, 1)] // Exception test.
    public void ConstrainPropertyCountTests(bool expectedResult, string constrainedJson, params int[] constraints)
    {
      bool testResult;
      if (constraints.Length == 1)
      {
        testResult = new ConfigToken("TestToken", "Eat the ice cream.", ConstrainPropertyCount(constraints[0])).Validate(JObject.Parse(constrainedJson));
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Eat the ice cream.", ConstrainPropertyCount(constraints[0], constraints[1])));
          testResult = false;
        }
        else
        {
          testResult = new ConfigToken("TestToken", "Eat the ice cream.", ConstrainPropertyCount(constraints[0], constraints[1])).Validate(JObject.Parse(constrainedJson));
        }
      }
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Equal(testResult, expectedResult);
    }
  }
}
