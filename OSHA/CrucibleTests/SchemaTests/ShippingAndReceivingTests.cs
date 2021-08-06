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

namespace SchemaTests
{
  public class ShippingAndReceivingTests
  {
    [Theory]
    [MemberData(nameof(DeserializeTypeTestData))]
    public static void DeserializeTypeTests(bool expected, string typeString, JToken inputValue, params Constraint[] constraints)
    {
      ConstraintContainer container = ShippingAndReceiving.DeserializeType(typeString, constraints);
      List<Error> resultList = container.ApplyConstraints(inputValue,"Test Token");
      Assert.Equal(expected, !resultList.AnyFatal());
    }

    public static IEnumerable<object[]> DeserializeTypeTestData
    {
      get
      {
        return new[]
        {
          new object[] { true, "Integer", 25 }, // Applies type constraint to valid input.
          new object[] { false, "Integer", "bleventeen" }, // Applies type constraint to invalid input.
          new object[] { true, "Integer", 25, ConstrainValue<long>(22) }, // Applies actual constraint to valid input.
          new object[] { true, "Integer", 25, ConstrainValue<long>(22,35) }, // Applies actual constraint to valid input.
          new object[] { false, "Integer", 25, ConstrainValue<long>(26,35) }, // Applies actual constraint to invalid input.
          new object[] { true, "String", "A string!"}, // Applies string type constraint to valid input.
          new object[] { true, "String", "Another string, yay!", AllowValues("Another string, yay!", "Another option that will go unused forever and ever!") }, // Applies actual string constraint to valid input.
          new object[] { false, "String", "Another option that will go unused forever and ever!", AllowValues("Another string, yay!", "Just kidding, that one's not valid anymore!") }, // Applies actual string constraint to invalid input.
          new object[] { false, "String", new JArray() { 6, 6, 4, 7, 5 } }, // Applies string type constraint to invalid input.
          new object[] { true, "Decimal", 25.3 }, // Applies decimal type constraint to valid input.
          new object[] { true, "Decimal", 25.3, ConstrainValue<double>(22) }, // Applies actual decimal constraint to valid input.
          new object[] { false, "Decimal", "astrong" }, // Applies decimal type constraint to invalid input.
          new object[] { true, "Array", new JArray() { 3 } }, // Applies array type constraint to valid input.
          new object[] { true, "Array", new JArray() { 3 }, ApplyConstraintsToCollection<int>() }, // Applies actual array constraint to valid input.
          new object[] { false, "Array", "youthoughtitwasarealarraybutitwasmeDIO" } // Applies array type constraint to invalid input.
        };
      }
    }

    [Theory]
    [MemberData(nameof(GetConstraintsForTypeTestData))]
    public static void GetConstraintsForTypeTests(bool expected, JToken inputValue, params Constraint[] constraints)
    {
      ConstraintContainer container = ShippingAndReceiving.GetConstraintsForType<long>(constraints);
      List<Error> resultList = container.ApplyConstraints(inputValue, "Test Token");
      Assert.Equal(expected, !resultList.AnyFatal());
    }

    public static IEnumerable<object[]> GetConstraintsForTypeTestData
    {
      get
      {
        return new[]
        {
          new object[] { true, 25 }, // Applies type constraint to valid input.
          new object[] { false, "bleventeen" }, // Applies type constraint to invalid input.
          new object[] { true, 25, ConstrainValue<long>(22) }, // Applies actual constraint to valid input.
          new object[] { true, 25, ConstrainValue<long>(22,35) }, // Applies actual constraint to valid input.
          new object[] { false, 25, ConstrainValue<long>(26,35) }, // Applies actual constraint to invalid input.
        };
      }
    }

    [Fact]
    public static void TypeMapTests()
    {
      Assert.Equal("Integer", ShippingAndReceiving.TypeMap("Int64"));
    }

    [Fact]
    public static void GetSupportedTypesTest()
    {
      Assert.Contains("UInt64", ShippingAndReceiving.GetSupportedTypes());
    }
  }
}
