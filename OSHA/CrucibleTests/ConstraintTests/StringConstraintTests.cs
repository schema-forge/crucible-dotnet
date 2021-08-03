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
  public class StringConstraintTests : Schema
  {
    private readonly ITestOutputHelper output;

    public StringConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Theory]
    [InlineData(true, "AcceptableString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    [InlineData(false, "HowDareYouFeedMeThisString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    public void ConstrainStringValuesTest(bool expectedResult, string constrainedString, params string[] acceptableStrings)
    {
      bool testResult = new ConfigToken("TestToken", "There Is No String", ConstrainStringValues(acceptableStrings)).Validate(constrainedString);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [MemberData(nameof(ConstrainRegexTestData))]
    public void ConstrainStringRegexExactTest(bool expectedResult, string constrainedString, params Regex[] patterns)
    {
      bool testResult = new ConfigToken("TestToken", "There is only yourself.", ConstrainStringWithRegexExact(patterns)).Validate(constrainedString);
      output.WriteLine(string.Join('\n', ErrorList));
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
    public void ConstrainStringLengthTest(bool expectedResult, string constrainedString, params int[] passedConstraints)
    {
      bool testResult;
      if (passedConstraints.Length == 1)
      {
        testResult = new ConfigToken("TestToken", "There Is No String", ConstrainStringLength(passedConstraints[0])).Validate(constrainedString);
      }
      else
      {
        if (passedConstraints[0] > passedConstraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Doomed", ConstrainStringLength(passedConstraints[0], passedConstraints[1])));
          testResult = false;
        }
        else
        {
          testResult = new ConfigToken("TestToken", "Another Movie Quote", ConstrainStringLength(passedConstraints[0], passedConstraints[1])).Validate(constrainedString);
        }
      }
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [InlineData(true, "GoodBoy", '/', '*')]
    [InlineData(false, "Why, father?", 'W')]
    [InlineData(false, "Doomed")]
    public void ForbidStringCharactersTest(bool expectedResult, string constrainedString, params char[] forbiddenChars)
    {
      if (forbiddenChars.Length > 0)
      {
        bool testResult = new ConfigToken("TestToken", "One Million Watts", ForbidStringCharacters(forbiddenChars)).Validate(constrainedString);
        output.WriteLine(string.Join('\n', ErrorList));
        Assert.Equal(testResult, expectedResult);
      }
      else
      {
        Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "There Is No String", ForbidStringCharacters(forbiddenChars)));
      }
    }
  }
}
