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
using OSHA.TestUtilities;

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
    /// Tests ApplySchema using a constructed schema with two required <see cref="Field"/>s.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json that will be tested against the schema.</param>
    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealvaluebutitwasme':'DIO'}")]
    public void ApplySchemaTests(bool expectedResult, string constrainedJson)
    {
      Schema appliedSchema = new(new Field[] {
            new Field<bool>("Ripe","Bool: Indicates whether or not the fruit is ripe."),
            new Field<int>("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",new Constraint<int>[] { ConstrainValue((0,5),(10,15)) })
          });
      Field<JObject> TestField = new("FruitProperties", "Json: Additional properties of the fruit in question.", new Constraint<JObject>[] { ApplySchema(appliedSchema) });
      bool testResult = TestField.Validate(JObject.Parse(constrainedJson),new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests ApplySchema using a constructed schema with two required <see cref="Field"/>s and one optional <see cref="Field"/>.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json that will be tested against the schema.</param>
    [Theory]
    [InlineData(false, "{}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3}")]
    [InlineData(false, "{'Ripe':'Brotato','MarketValue':3}")]
    [InlineData(true, "{'Ripe':true,'MarketValue':3,'Color':'bloo'}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'Color':['youthoughtitwasarealcolorbutitwasmeDIO']}")]
    [InlineData(false, "{'Ripe':true,'MarketValue':3,'youthoughtitwasarealvaluebutitwasme':'DIO'}")]
    public void ApplySchemaWithOptionalTests(bool expectedResult, string constrainedJson)
    {
      Schema appliedSchema = new(new Field[] {
            new Field<bool>("Ripe","Bool: Indicates whether or not the fruit is ripe."),
            new Field<int>("MarketValue","Int: Average price of one pound of the fruit in question. Decimals are not allowed because everyone who appends .99 to their prices in order to trick the human brain is insubordinate and churlish.",new Constraint<int>[] { ConstrainValue((0,5),(10,15)) }),
            new Field<string>("Color","String: Indicates the color of the fruit.",required: false)
          });
      Field<JObject> TestField = new("FruitProperties", "Json: Additional properties of the fruit in question.", new Constraint<JObject>[] { ApplySchema(appliedSchema) });
      bool testResult = TestField.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
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
      Field<JObject> TestField;
      bool testResult;
      if (constraints.Length == 1)
      {
        TestField = new Field<JObject>("TestField", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCountLowerBound<JObject>(constraints[0]) });
        testResult = TestField.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new Field<JObject>("TestField", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(constraints[0], constraints[1]) }));
          return;
        }
        else
        {
          TestField = new Field<JObject>("TestField", "Eat the ice cream.", new Constraint<JObject>[] { ConstrainCollectionCount<JObject>(constraints[0], constraints[1]) });
          testResult = TestField.Validate(JObject.Parse(constrainedJson), new JTokenTranslator());
        }
      }
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(expectedResult, testResult);
    }

    [Theory]
    [InlineData(true,"{'SubObject':{'Type':'Fruit','Fruit':'Watermelon'}}")]
    [InlineData(true, "{'SubObject':{'Type':'Luxury','Luxury Good':'Silk Napkins'}}")]
    [InlineData(false, "{'SubObject':{'Type':'Fruit','Luxury Good':'Those Fake Sparkly Apples People Sometimes Put In Decorative Bowls'}}")]
    [InlineData(false, "{'SubObject':{'Type':'Vegetable','Luxury Good':'Cabbage'}}")]
    public void ApplySchemaByTypeTest(bool expectedResult, string testJson)
    {
      Schema fruitSchema = new(new Field<string>("Type","The type of this object."), new Field<string>("Fruit","A fruit name."));
      Schema luxurySchema = new(new Field<string>("Type", "The type of this object."), new Field<string>("Luxury Good", "A fruit name."));
      Dictionary<string, Schema> typeMap = new() { { "Fruit", fruitSchema }, { "Luxury", luxurySchema } };
      Schema metaSchema = new(new Field<JObject>("SubObject", "Either a fruit or a luxury good. Choose wisely.",new Constraint<JObject>[] { ApplySchema("Type", typeMap) }));
      Assert.Equal(expectedResult, !metaSchema.Validate(JObject.Parse(testJson), new JObjectTranslator()).AnyFatal());
    }
  }
}
