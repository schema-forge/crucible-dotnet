using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;
using OSHA.TestUtilities;

namespace TranslatorTests
{
  [Trait("Crucible", "")]
  public class JObjectTranslatorTests
  {
    private readonly ITestOutputHelper output;

    public JObjectTranslatorTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests the capabilites of TryCastToken using reflection.
    /// </summary>
    /// <param name="expectedResult">Expected result of the conversion; true if conversion should succeed, false if it should not.</param>
    /// <param name="type">Full name of the type to attempt to which <paramref name="valueToConvert"/> will be cast.</param>
    /// <param name="valueToConvert">Value to cast to <paramref name="type"/></param>
    [Theory]
    [InlineData(true,"System.String","I'm a string!")]
    [InlineData(false,"System.Int32","I'm still a string!")]
    [InlineData(true,"System.Int32","57")]
    [InlineData(true,"System.DateTime","2021-10-09")]
    [InlineData(false,"System.DateTime","20211021")] //This is not a format that DateTime.Parse can recognize.
    [InlineData(true,"System.DateTime","10202109")] //This format is normally not recognized by DateTime.Parse, but is added in the test by RegisterDateTimeFormat.
    public void ConversionTests(bool expectedResult, string type, string valueToConvert)
    {
      JObjectTranslator testTranslator = new();
      SchemaForge.Crucible.Utilities.Conversions.RegisterDateTimeFormat("MMyyyydd"); // Add a new recognized DateTime format.
      MethodInfo methodInfo = typeof(JObjectTranslator).GetMethod("TryCastToken");
      MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(Type.GetType(type));
      Assert.Equal(expectedResult, genericMethodInfo.Invoke(testTranslator, new object[] { new JObject() { { "TestToken", valueToConvert } }, "TestToken", null }));
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.TokenIsNullOrEmpty(JObject, string)"/>
    /// method.
    /// </summary>
    /// <param name="expectedResult">Expected result of TokenIsNullOrEmpty.</param>
    /// <param name="testValue">Value to evaluate.</param>
    [Theory]
    [InlineData(true,"   ")]
    [InlineData(false,"I'm here, I'm here!")]
    public void TokenIsNullOrEmptyTest(bool expectedResult, string testValue)
    {
      JObjectTranslator testTranslator = new();
      Assert.Equal(expectedResult,testTranslator.TokenIsNullOrEmpty(new JObject() { { "TestToken", testValue } }, "TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.InsertToken{TDefaultValueType}(JObject, string, TDefaultValueType)"/>
    /// method by inserting a token.
    /// </summary>
    [Fact]
    public void InsertTokenTest()
    {
      JObjectTranslator testTranslator = new();
      JObject testObject = new() { { "TestToken", "I'm a token!" } };
      testTranslator.InsertToken(testObject, "Another token", "Me too!");
      Assert.True(testObject.ContainsKey("Another token"));
      Assert.True(testObject["Another token"].Value<string>() == "Me too!");
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.CollectionContains(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionContainsTest()
    {
      JObjectTranslator testTranslator = new();
      JObject testObject = new() { { "TestToken", "I'm a token!" } };
      Assert.True(testTranslator.CollectionContains(testObject, "TestToken"));
      Assert.False(testTranslator.CollectionContains(testObject, "You thought it was a real token but it was me, Dio!"));
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.GetCollectionKeys(JObject)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetCollectionKeysTest()
    {
      JObjectTranslator testTranslator = new();
      JObject testObject = new() { { "TestToken", "I'm a token!" }, { "SecondTestToken", "Me too!" },{ "ThirdTestToken", "Me... three?" } };
      List<string> expectedList = new() { "TestToken", "SecondTestToken", "ThirdTestToken" };
      List<string> resultList = testTranslator.GetCollectionKeys(testObject);
      output.WriteLine($"Expected list: {expectedList.Join(", ")}");
      output.WriteLine($"Actual list: {resultList.Join(", ")}");

      Assert.Equal(expectedList, resultList);
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.CollectionValueToString(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionValueToStringTest()
    {
      JObjectTranslator testTranslator = new();
      JObject testObject = new() { { "TestToken", 38 } };
      Assert.Equal("38",testTranslator.CollectionValueToString(testObject,"TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="JObjectTranslator.GetEquivalentType(string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetEquivalentTypeTest()
    {
      JObjectTranslator testTranslator = new();
      Assert.Equal("Json Number", testTranslator.GetEquivalentType("Int32"));
    }

    /// <summary>
    /// This final test is a second-order test; it tests the interactions between <see cref="DictionaryTranslator"/>
    /// and all <see cref="Constraint"/> types. If all the tests in <see cref="ConstraintTests"/> pass,
    /// then a failure here means that there is a bad interaction between this type of translator and
    /// one or more constraints.
    /// </summary>
    [Fact]
    public void ApplyTestConfig()
    {
      Schema testSchema = TestUtilities.GetTestSchema();
      testSchema.Validate(JObject.Parse(TestUtilities.GetTestJson()), new JObjectTranslator());
      output.WriteLine(testSchema.ErrorList.Join("\n"));
      Assert.True(!testSchema.ErrorList.AnyFatal());
    }
  }
}
