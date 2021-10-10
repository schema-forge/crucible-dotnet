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
  public class SimpleTestClass
  {
    public string TestToken { get; set; }
  }

  public class SimpleTestClassTwoTokens
  {
    public string TestToken { get; set; }
    public string AnotherToken { get; set; }
  }

  public class SimpleTestClassThreeTokens
  {
    public string TestToken { get; set; }
    public string SecondTestToken { get; set; }
    public string ThirdTestToken { get; set; }
  }

  public class ListTestClass
  {
    public List<int> TestList { get; set; }
  }

  #region TestConfigAsClass
  public class TestConfigAsClass
  {
    public string RequiredToken { get; set; }
    public string UnrequiredToken { get; set; }
    public string ATokenThatIsNotRequiredButNonethelessHasAValueIfNotIncluded { get; set; }
    public string AllowValues { get; set; }
    public string ConstrainStringWithRegexExactPatterns { get; set; }
    public string ConstrainStringLengthLowerBound { get; set; }
    public string ConstrainStringLength { get; set; }
    public string ConstrainStringLengthUpperBound { get; set; }
    public string ForbidSubstrings { get; set; }
    public int ConstrainValueLowerBound { get; set; }
    public int ConstrainValue { get; set; }
    public int ConstrainValueDomains { get; set; }
    public int ConstrainValueUpperBound { get; set; }
    public double ConstrainDigits { get; set; }
    public ConstrainCollectionCountLowerBound ConstrainCollectionCountLowerBound { get; set; }
    public ConstrainCollectionCount ConstrainCollectionCount { get; set; }
    public ApplySchema ApplySchema { get; set; }
    public List<int> ConstrainArrayCountLowerBound { get; set; }
    public List<int> ConstrainArrayCount { get; set; }
    public List<string> ApplyConstraintsToArrayElements { get; set; }
    public string ConstrainDateTimeFormat { get; set; }
    public int MatchAnyConstraint { get; set; }
  }
  public class ConstrainCollectionCountLowerBound
  {
    public string OneItem { get; set; }

    public string TwoItems { get; set; }

    public string ThreeItems { get; set; }
  }

  public class ConstrainCollectionCount
  {
    public string OneItem { get; set; }

    public string TwoItems { get; set; }

    public string ThreeItems { get; set; }
  }

  public class ApplySchema
  {
    public string Season1 { get; set; }

    public string Season2 { get; set; }

    public string Season3 { get; set; }
  }
  #endregion

  [Trait("Crucible", "")]
  public class ClassTranslatorTests
  {
    private readonly ITestOutputHelper output;

    public ClassTranslatorTests(ITestOutputHelper output)
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
    [InlineData(false,"System.DateTime","20211021")] //This is not a format that DateTime.Parse can recognize.
    [InlineData(true,"System.DateTime","10202109")] //This format is normally not recognized by DateTime.Parse, but is added in the test by RegisterDateTimeFormat.
    public void ConversionTests(bool expectedResult, string type, string valueToConvert)
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'" + valueToConvert + "'}";
      SimpleTestClass testClass = JsonConvert.DeserializeObject<SimpleTestClass>(testString);
      SchemaForge.Crucible.Utilities.Conversions.RegisterDateTimeFormat("MMyyyydd"); // Add a new recognized DateTime format.
      MethodInfo methodInfo = typeof(ClassTranslator).GetMethod("TryCastToken");
      MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(Type.GetType(type));
      Assert.Equal(expectedResult, genericMethodInfo.Invoke(testTranslator, new object[] { testClass, "TestToken", null }));
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.TokenIsNullOrEmpty(JObject, string)"/>
    /// method.
    /// </summary>
    /// <param name="expectedResult">Expected result of TokenIsNullOrEmpty.</param>
    /// <param name="testValue">Value to evaluate.</param>
    [Theory]
    [InlineData(true,"   ")]
    [InlineData(false,"I live!")]
    public void TokenIsNullOrEmptyTest(bool expectedResult, string testValue)
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'" + testValue + "'}";
      SimpleTestClass testClass = JsonConvert.DeserializeObject<SimpleTestClass>(testString);
      Assert.Equal(expectedResult,testTranslator.TokenIsNullOrEmpty(testClass, "TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.InsertToken{TDefaultValueType}(JObject, string, TDefaultValueType)"/>
    /// method by inserting a token.
    /// </summary>
    [Fact]
    public void InsertTokenTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!'}";
      SimpleTestClassTwoTokens testClass = JsonConvert.DeserializeObject<SimpleTestClassTwoTokens>(testString);
      testTranslator.InsertToken(testClass, "AnotherToken", "Me too!");
      Assert.True(testClass.AnotherToken == "Me too!");
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.CollectionContains(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionContainsTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!'}";
      SimpleTestClass testClass = JsonConvert.DeserializeObject<SimpleTestClass>(testString);
      Assert.True(testTranslator.CollectionContains(testClass, "TestToken"));
      Assert.False(testTranslator.CollectionContains(testClass, "You thought it was a real token but it was me, Dio!"));
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.GetCollectionKeys(JObject)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetCollectionKeysTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'I am a token!','SecondTestToken':'Me too!','ThirdTestToken':'Me... three?'}";
      SimpleTestClassThreeTokens testClass = JsonConvert.DeserializeObject<SimpleTestClassThreeTokens>(testString);
      List<string> expectedList = new() { "TestToken", "SecondTestToken", "ThirdTestToken" };
      List<string> resultList = testTranslator.GetCollectionKeys(testClass);
      output.WriteLine($"Expected list: {expectedList.Join(", ")}");
      output.WriteLine($"Actual list: {resultList.Join(", ")}");

      Assert.Equal(expectedList, resultList);
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.CollectionValueToString(JObject, string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void CollectionValueToStringTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestToken':'38'}";
      SimpleTestClass testClass = JsonConvert.DeserializeObject<SimpleTestClass>(testString);
      Assert.Equal("38",testTranslator.CollectionValueToString(testClass,"TestToken"));
    }

    /// <summary>
    /// Tests the <see cref="ClassTranslator.GetEquivalentType(string)"/>
    /// method.
    /// </summary>
    [Fact]
    public void GetEquivalentTypeTest()
    {
      ClassTranslator testTranslator = new();
      Assert.Equal("Json Number", testTranslator.GetEquivalentType("Int32"));
    }

    /// <summary>
    /// This final test is a second-order test; it tests the interactions between <see cref="ClassTranslator"/>
    /// and all <see cref="Constraint"/> types. If all the tests in <see cref="ConstraintTests"/> pass,
    /// then a failure here means that there is a bad interaction between this type of translator and
    /// one or more constraints.
    /// </summary>
    [Fact]
    public void ApplyTestConfig()
    {
      Schema testSchema = TestUtilities.GetTestSchema();
      TestConfigAsClass testClass = JsonConvert.DeserializeObject<TestConfigAsClass>(TestUtilities.GetTestJson());
      testSchema.Validate(testClass, new ClassTranslator());
      output.WriteLine(testSchema.ErrorList.Join("\n"));
      Assert.True(!testSchema.ErrorList.AnyFatal());
    }

    [Fact]
    public void GenericListTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestList':[5, 13, 22]}";
      ListTestClass testClass = JsonConvert.DeserializeObject<ListTestClass>(testString);
      Schema testSchema = new(new ConfigToken<List<int>>("TestList", "A matter of reflection."));
      output.WriteLine(testSchema.Validate(testClass, testTranslator).Join("\n"));
      Assert.True(!testSchema.ErrorList.AnyFatal());
    }

    [Fact]
    public void GenericListWithCastTest()
    {
      ClassTranslator testTranslator = new();
      string testString = "{'TestList':[5, 13, 22]}";
      ListTestClass testClass = JsonConvert.DeserializeObject<ListTestClass>(testString);
      Schema testSchema = new(new ConfigToken<List<string>>("TestList", "A matter of reflection."));
      output.WriteLine(testSchema.Validate(testClass, testTranslator).Join("\n"));
      Assert.True(!testSchema.ErrorList.AnyFatal());
    }
  }
}
