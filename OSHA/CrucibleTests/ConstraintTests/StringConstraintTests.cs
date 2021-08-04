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
  public class StringConstraintTests
  {
    private readonly ITestOutputHelper output;

    public StringConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Theory]
    [InlineData(true, "AcceptableString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    [InlineData(false, "HowDareYouFeedMeThisString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    public void ConstrainStringValuesInnerFunctionTest(bool expectedResult, string constrainedString, params string[] acceptableStrings)
    {
      ConfigToken testToken = new ConfigToken("TestToken", "There Is No String", ApplyConstraints<string>(ConstrainStringValues(acceptableStrings)));
      bool testResult = testToken.Validate(constrainedString);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [MemberData(nameof(ConstrainRegexTestData))]
    public void ConstrainStringRegexExactInnerFunctionTest(bool expectedResult, string constrainedString, params Regex[] patterns)
    {
      ConfigToken testToken = new ConfigToken("TestToken", "There is only yourself.", ApplyConstraints<string>(ConstrainStringWithRegexExact(patterns)));
      bool testResult = testToken.Validate(constrainedString);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    public static IEnumerable<object[]> ConstrainRegexTestData
    {
      get
      {
        return new[]
        {
          new object[] { true, "IPassTheTest", new Regex("([A-z])+") },
          new object[] { false, "IDoNotPass :(", new Regex("([A-z])+") },
          new object[] { false, "IAlsoDoNotPass :(", new Regex("([A-z])+"), new Regex("([1-9])+")}
        };
      }
    }

    [Theory]
    [InlineData(true, "APerfectlyWellBehavedString", 3)]
    [InlineData(false, "AnInsubordinateAndChurlishString", 33)]
    [InlineData(true, "DelightfulStringFromDownTheLane", 3, 31)]
    [InlineData(false, "TheStringNextDoor", 3, 5)]
    [InlineData(false, "IRanOutOfKNDReferences", 25, 45)]
    [InlineData(false, "ThisTestWasWrittenIncorrectly", 8, 5)]
    public void ConstrainStringLengthInnerFunctionTest(bool expectedResult, string constrainedString, params int[] passedConstraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (passedConstraints.Length == 1)
      {
        testToken = new ConfigToken("TestToken", "There Is No String", ApplyConstraints<string>(ConstrainStringLength(passedConstraints[0])));
        testResult = testToken.Validate(constrainedString);
      }
      else
      {
        if (passedConstraints[0] > passedConstraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Doomed", ApplyConstraints<string>(ConstrainStringLength(passedConstraints[0], passedConstraints[1]))));
          return;
        }
        else
        {
          testToken = new ConfigToken("TestToken", "Another Movie Quote", ApplyConstraints<string>(ConstrainStringLength(passedConstraints[0], passedConstraints[1])));
          testResult = testToken.Validate(constrainedString);
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [InlineData(true, "GoodBoy", '/', '*')]
    [InlineData(false, "Why, father?", 'W')]
    [InlineData(false, "Doomed")]
    public void ForbidStringCharactersInnerFunctionTest(bool expectedResult, string constrainedString, params char[] forbiddenChars)
    {
      if (forbiddenChars.Length > 0)
      {
        ConfigToken testToken = new ConfigToken("TestToken", "One Million Watts", ApplyConstraints<string>(ForbidStringCharacters(forbiddenChars)));
        bool testResult = testToken.Validate(constrainedString);
        output.WriteLine(string.Join('\n', testToken.ErrorList));
        Assert.Equal(testResult, expectedResult);
      }
      else
      {
        Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "There Is No String", ApplyConstraints<string>(ForbidStringCharacters(forbiddenChars))));
      }
    }

    [Fact]
    public void ConstrainStringValuesPropertyTest()
    {
      Constraint testConstraint = ConstrainStringValues("Hello", "dlrow");
      JProperty expected = new("ConstrainStringValues", new JArray() { "Hello", "dlrow" });
      Assert.Equal(expected, testConstraint.Property);
    }

    [Fact]
    public void ConstrainStringLengthLowerBoundPropertyTest()
    {
      Constraint testConstraint = ConstrainStringLength(3);
      JProperty expected = new("ConstrainStringValues", 3);
      Assert.Equal(expected, testConstraint.Property);
    }

    [Fact]
    public void ConstrainStringLowerAndUpperBoundLengthPropertyTest()
    {
      Constraint testConstraint = ConstrainStringLength(3,25);
      JProperty expected = new("ConstrainStringValues", "3, 25");
      Assert.Equal(expected, testConstraint.Property);
    }
  }
}
