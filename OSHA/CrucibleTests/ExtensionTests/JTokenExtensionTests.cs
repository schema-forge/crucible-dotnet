using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Extensions
{
  [Trait("Crucible", "")]
  public class JTokenExtensionTests
  {
    /// <summary>
    /// Tests all conditions under which <see cref="JTokenExtensions.IsNullOrEmpty(JToken)"/> should return true for a <see cref="JToken"/>.
    /// </summary>
    /// <param name="input">JToken to test for emptiness.</param>
    [Theory]
    [MemberData(nameof(JTokenIsNullOrEmptyData))]
    public void JTokenIsNullOrEmpty(JToken input)
    {
      Assert.True(input.IsNullOrEmpty());
    }

    public static IEnumerable<object[]> JTokenIsNullOrEmptyData =>
    new List<object[]>
    {
      new object[] { null },
      new object[] { new JArray() },
      new object[] { new JObject() },
      new object[] { new JProperty("something").Value },
      new object[] { new JProperty("","avalue") },
      new object[] { new JProperty("avalue","") },
      new object[] { JObject.Parse("{}") }
    };

    /// <summary>
    /// Tests several conditions under which <see cref="JTokenExtensions.IsNullOrEmpty(JToken)"/> should return false for a <see cref="JToken"/>.
    /// </summary>
    /// <param name="input">JToken to test for emptiness.</param>
    [Theory]
    [MemberData(nameof(JTokenIsNotNullOrEmptyData))]
    public void JTokenIsNotNullOrEmpty(JToken input)
    {
      Assert.False(input.IsNullOrEmpty());
    }

    static JObject JTokenNotNullTestObject = JObject.Parse(@"{'SuddenlyABool':true}");

    public static IEnumerable<object[]> JTokenIsNotNullOrEmptyData =>
    new List<object[]>
    {
      new object[] { new JArray() { true } },
      new object[] { new JProperty("avalue","something") },
      new object[] { JObject.Parse(@"{'AProperty':''}") },
      new object[] { JTokenNotNullTestObject["SuddenlyABool"] }
    };


    /// <summary>
    /// Tests all conditions under which <see cref="JTokenExtensions.Contains(JToken, object)"/> should return true for a <see cref="JToken"/>.
    /// </summary>
    /// <param name="input">JToken to test for its contents. It can be a singular JValue or a collection of some kind.</param>
    /// <param name="searchTerm">Object to search for in the JToken.</param>
    [Theory]
    [MemberData(nameof(JTokenContainsData))]
    public void JTokenContains(JToken input, object searchTerm)
    {
      Assert.True(input.Contains(searchTerm));
    }

    static JObject JTokenContainsTestObject = JObject.Parse(
      @"{
        'AnArray':[15, 23, 55],
        'ASubObject':{ 'AnotherArray':['HelpIAmTrappedInATestFactory'] },
        'AString':'Blue',
        'AnInt':25
      }");

    public static IEnumerable<object[]> JTokenContainsData =>
    new List<object[]>
    {
      new object[] { JTokenContainsTestObject, "AnArray" },
      new object[] { JTokenContainsTestObject["ASubObject"], "AnotherArray" },
      new object[] { JTokenContainsTestObject["AString"], "l" }
    };

    // Separated out due to some weird interactions with XUnit MemberData and Contains.

    [Fact]
    public void JArrayContains()
    {
      JObject thingy = JObject.Parse(@"{'Array':[15, 21, 13]}");
      Assert.True(thingy["Array"].Contains(15));
    }

    /// <summary>
    /// Tests <see cref="JTokenExtensions.Add{T}(JToken, T)"/> for JArrays.
    /// </summary>
    [Fact]
    public void JTokenAddToArrayValid()
    {
      JObject thingy = JObject.Parse(@"{'Array':[20]}");
      thingy["Array"].Add(25);
      Assert.True(((JArray)thingy["Array"]).Count == 2);
    }

    /// <summary>
    /// Tests <see cref="JTokenExtensions.Add{T}(JToken, T)"/> for non-JArrays.
    /// </summary>
    [Fact]
    public void JTokenAddToArrayInvalid()
    {
      JObject thingy = JObject.Parse(@"{'NotArray':'[Pretty close, though.]'}");
      Assert.Throws<ArgumentException>(() => thingy["NotArray"].Add(25));
    }

    [Fact]
    public void JTokenAddToObjectValid()
    {
      JObject thingy = JObject.Parse(@"{'SubObject':{'FirstProperty':'FirstValue'}}");
      thingy["SubObject"].Add("SecondProperty", "SecondValue");
      Assert.True(thingy["SubObject"]["SecondProperty"].ToString() == "SecondValue");
    }

    [Fact]
    public void JTokenAddToObjectInvalid()
    {
      JObject thingy = JObject.Parse(@"{'DecoySubObject':''}");
      Assert.Throws<ArgumentException>(() => thingy["DecoySubObject"].Add("NewProperty", "NewValue"));
    }

    [Fact]
    public void JTokenAddToObjectAlsoInvalid()
    {
      JObject thingy = JObject.Parse(@"{'SubObject':{'FirstProperty':'FirstValue'}}");
      Assert.Throws<ArgumentException>(() => thingy["SubObject"].Add(" ", "NewValue"));
    }
  }
}
