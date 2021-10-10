using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;
using OSHA.TestUtilities;

namespace TranslatorTests
{
  [Trait("Crucible", "")]
  public class DictionaryTranslatorTests
  {
    private readonly ITestOutputHelper output;

    public DictionaryTranslatorTests(ITestOutputHelper output)
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
    [InlineData(true,"System.String","I am a string!")]
    [InlineData(false,"System.Int32","I am still a string!")]
    [InlineData(true,"System.Int32","57")]
    [InlineData(true,"System.DateTime","2021-10-09")]
    [InlineData(false,"System.DateTime","20211009")] //This is not a format that DateTime.Parse can recognize.
    [InlineData(true,"System.DateTime","10202109")] //This format is normally not recognized by DateTime.Parse, but is added in the test by RegisterDateTimeFormat.
    public void ConversionTests(bool expectedResult, string type, string valueToConvert)
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'" + valueToConvert + "'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      SchemaForge.Crucible.Utilities.Conversions.RegisterDateTimeFormat("MMyyyydd"); // Add a new recognized DateTime format.
      MethodInfo methodInfo = typeof(DictionaryTranslator).GetMethod("TryCastToken");
      MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(Type.GetType(type));
      Assert.Equal(expectedResult, genericMethodInfo.Invoke(testTranslator, new object[] { testDictionary, "TestToken", null }));
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.TokenIsNullOrEmpty(JObject, string)"/>
    /// method.
    /// </summary>
    /// <param name="expectedResult">Expected result of TokenIsNullOrEmpty.</param>
    /// <param name="testValue">Value to evaluate.</param>
    [Theory]
    [InlineData(true,"   ")]
    [InlineData(false,"I live!")]
    public void TokenIsNullOrEmptyTest(bool expectedResult, string testValue)
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'" + testValue + "'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      Assert.Equal(expectedResult,testTranslator.TokenIsNullOrEmpty(testDictionary, "TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.InsertToken{TDefaultValueType}(JObject, string, TDefaultValueType)"/>
    /// method by inserting a token.
    /// </summary>
    [Fact]
    public void InsertTokenTest()
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      testTranslator.InsertToken(testDictionary, "Another token", "Me too!");
      Assert.True(testDictionary.ContainsKey("Another token"));
      Assert.True(testDictionary["Another token"].ToString() == "Me too!");
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.CollectionContains(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionContainsTest()
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      Assert.True(testTranslator.CollectionContains(testDictionary, "TestToken"));
      Assert.False(testTranslator.CollectionContains(testDictionary, "You thought it was a real token but it was me, Dio!"));
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.GetCollectionKeys(JObject)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetCollectionKeysTest()
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!','SecondTestToken':'Me too!','ThirdTestToken':'Me... three?'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      List<string> expectedList = new() { "TestToken", "SecondTestToken", "ThirdTestToken" };
      List<string> resultList = testTranslator.GetCollectionKeys(testDictionary);
      output.WriteLine($"Expected list: {expectedList.Join(", ")}");
      output.WriteLine($"Actual list: {resultList.Join(", ")}");

      Assert.Equal(expectedList, resultList);
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.CollectionValueToString(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionValueToStringTest()
    {
      DictionaryTranslator testTranslator = new();
      string testString = "{'TestToken':'38'}";
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(testString);
      Assert.Equal("38",testTranslator.CollectionValueToString(testDictionary,"TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="DictionaryTranslator.GetEquivalentType(string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetEquivalentTypeTest()
    {
      DictionaryTranslator testTranslator = new();
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
      Dictionary<string, object> testDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(TestUtilities.GetTestJson());
      testSchema.Validate(testDictionary, new DictionaryTranslator());
      output.WriteLine(testDictionary["ConstrainArrayCount"].GetType().Name);
      output.WriteLine(testSchema.ErrorList.Join("\n"));
      Assert.True(!testSchema.ErrorList.AnyFatal());
    }
  }
}
