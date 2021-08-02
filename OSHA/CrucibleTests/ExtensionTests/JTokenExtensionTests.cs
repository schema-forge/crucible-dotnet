using System;
using System.Collections.Generic;
using System.Linq;
using schemaforge.Crucible;
using schemaforge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Extensions
{
    [Trait("Crucible", "")]
    public class JTokenExtensionTests
    {
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

        [Fact]
        public void JTokenAddToArrayValid()
        {
            JObject thingy = JObject.Parse(@"{'Array':[20]}");
            thingy["Array"].Add(25);
            Assert.True(((JArray)thingy["Array"]).Count == 2);
        }

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
