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
  public class StringConstraintTests
  {
    private readonly ITestOutputHelper output;

    public StringConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Ensures that AllowValues functions with strings with two tests.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to test against acceptable values.</param>
    /// <param name="acceptableStrings">Acceptable strings.</param>
    [Theory]
    [InlineData(true, "AcceptableString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    [InlineData(false, "HowDareYouFeedMeThisString", "AcceptableString", "AnotherAcceptableString", "YetAnotherAcceptableString")]
    public void AllowValuesInnerFunctionTest(bool expectedResult, string constrainedString, params string[] acceptableStrings)
    {
      Field TestField = new Field<string>("TestField", "There Is No String", new Constraint<string>[] { AllowValues(acceptableStrings) });
      bool testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that ForbidValues functions with strings with two tests.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to test against acceptable values.</param>
    /// <param name="forbiddenStrings">Unacceptable strings.</param>
    [Theory]
    [InlineData(true, "AcceptableString", "ForbiddenString", "AnotherForbiddenString", "YetAnotherForbiddenString")]
    [InlineData(false, "ForbiddenString", "ForbiddenString", "AnotherForbiddenString", "YetAnotherForbiddenString")]
    public void ForbidValuesInnerFunctionTest(bool expectedResult, string constrainedString, params string[] forbiddenStrings)
    {
      Field TestField = new Field<string>("TestField", "There Is No String", new Constraint<string>[] { ForbidValues(forbiddenStrings) });
      bool testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that ConstrainStringRegex functions properly by checking the entire string and not only part of it.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to test against acceptable values.</param>
    /// <param name="patterns">Pattern or patterns to test with.</param>
    [Theory]
    [MemberData(nameof(ConstrainRegexTestData))]
    public void ConstrainStringRegexExactInnerFunctionTest(bool expectedResult, string constrainedString, params Regex[] patterns)
    {
      Field TestField = new Field<string>("TestField", "There is only yourself.", new Constraint<string>[] { ConstrainStringWithRegexExact(patterns) });
      bool testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
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

    /// <summary>
    /// Ensures that <see cref="ConstrainStringLengthLowerBound(int)"/> functions properly with one, two, and invalid arguments.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to validate with <see cref="ConstrainStringLengthLowerBound(int)"/>.</param>
    /// <param name="lowerBound">Upper bound to pass to <see cref="ConstrainStringLengthLowerBound(int)"/>.</param>
    [Theory]
    [InlineData(true, "APerfectlyWellBehavedString", 27)] // Length is exactly equal to lower bound. Should be permitted.
    [InlineData(false, "AnInsubordinateAndChurlishString", 33)]
    public void ConstrainStringLengthLowerBoundTest(bool expectedResult, string constrainedString, int lowerBound)
    {
      Field TestField;
      bool testResult;
      TestField = new Field<string>("TestField", "There Is No String", new Constraint<string>[] { ConstrainStringLengthLowerBound(lowerBound) });
      testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that ConstrainStringLength functions properly with one, two, and invalid arguments.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to validate with ConstrainStringLength.</param>
    /// <param name="passedConstraints">Arguments to pass to ConstrainStringLength.</param>
    [Theory]
    [InlineData(true, "DelightfulStringFromDownTheLane", 3, 31)]
    [InlineData(false, "TheStringNextDoor", 3, 5)]
    [InlineData(false, "IRanOutOfKNDReferences", 25, 45)]
    [InlineData(false, "ThisTestWasWrittenIncorrectly", 8, 5)]
    public void ConstrainStringLengthTest(bool expectedResult, string constrainedString, int lowerBound, int upperBound)
    {
      Field TestField;
      bool testResult;
      if (lowerBound > upperBound)
      {
        Assert.Throws<ArgumentException>(() => new Field<string>("TestField", "Doomed", new Constraint<string>[] { ConstrainStringLength(lowerBound, upperBound) }));
        return;
      }
      else
      {
        TestField = new Field<string>("TestField", "Another Movie Quote", new Constraint<string>[] { ConstrainStringLength(lowerBound, upperBound) });
        testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      }
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that <see cref="ConstrainStringLengthUpperBound(int)"/> functions properly with one, two, and invalid arguments.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to validate with <see cref="ConstrainStringLengthUpperBound(int)"/>.</param>
    /// <param name="upperBound">Upper bound to pass to <see cref="ConstrainStringLengthUpperBound(int)"/>.</param>
    [Theory]
    [InlineData(true, "APerfectlyWellBehavedString", 27)] // Length is exactly equal to upper bound. Should be permitted.
    [InlineData(false, "AnInsubordinateAndChurlishString", 3)]
    public void ConstrainStringLengthUpperBoundTest(bool expectedResult, string constrainedString, int upperBound)
    {
      Field TestField;
      output.WriteLine($"String length: {constrainedString.Length}\nUpper bound: {upperBound}");
      bool testResult;
      TestField = new Field<string>("TestField", "There Is No String", new Constraint<string>[] { ConstrainStringLengthUpperBound(upperBound) });
      testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that ForbidSubstrings functions properly with one, multiple, and no arguments.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedString">String to test against forbidden substrings.</param>
    /// <param name="forbiddenSubstrings">Arguments to pass to ForbidSubstrings.</param>
    [Theory]
    [InlineData(true, "ModestyPrevails", "V")]
    [InlineData(true, "GoodBoy", "/", "*")]
    [InlineData(true, "AnotherGoodBoy", "Bad", "lBoy")]
    [InlineData(false, "Why, father?", "W")]
    [InlineData(false, "This is the end.", "nd.")]
    [InlineData(false, "Or, perhaps, the beginning.", "Or,")]
    [InlineData(false, "Doomed")]
    public void ForbidSubstringsInnerFunctionTest(bool expectedResult, string constrainedString, params string[] forbiddenSubstrings)
    {
      if (forbiddenSubstrings.Length > 0)
      {
        Field<string> TestField = new("TestField", "One Million Watts", new Constraint<string>[] { ForbidSubstrings(forbiddenSubstrings) });
        bool testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
        output.WriteLine(string.Join('\n', TestField.ErrorList));
        Assert.Equal(testResult, expectedResult);
      }
      else
      {
        Assert.Throws<ArgumentException>(() => new Field<string>("TestField", "There Is No String", new Constraint<string>[] { ForbidSubstrings(forbiddenSubstrings) }));
      }
    }

    [Theory]
    [InlineData(true, "GoodString")]
    [InlineData(false, " Bad String")]
    [InlineData(false, "AnotherBad\nString")]
    public void ForbidWhiteSpaceInnerFunctionTest(bool expectedResult, string constrainedString)
    {
      Field<string> TestField = new("TestField", "Deo Dona Nobis Pacem", new Constraint<string>[] { ForbidWhiteSpace() });
      bool testResult = TestField.Validate(new JValue(constrainedString), new JTokenTranslator());
      output.WriteLine(string.Join('\n', TestField.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Ensures that the AllowValues constraint passes the expected JProperty.
    /// </summary>
    [Fact]
    public void AllowValuesPropertyTest()
    {
      Constraint testConstraint = AllowValues("Hello", "dlrow");
      JProperty expected = new("AllowValues", new JArray() { "Hello", "dlrow" });
      Assert.Equal(expected, testConstraint.Property);
    }

    /// <summary>
    /// Ensures that ConstrainStringLength passes the expected JProperty when given a lower bound.
    /// </summary>
    [Fact]
    public void ConstrainStringLengthLowerBoundPropertyTest()
    {
      Constraint testConstraint = ConstrainStringLengthLowerBound(3);
      JProperty expected = new("AllowValues", 3);
      Assert.Equal(expected, testConstraint.Property);
    }

    /// <summary>
    /// Ensures that ConstrainStringLength passes the expected JProperty when given a lower and upper bound.
    /// </summary>
    [Fact]
    public void ConstrainStringLowerAndUpperBoundLengthPropertyTest()
    {
      Constraint testConstraint = ConstrainStringLength(3,25);
      JProperty expected = new("AllowValues", "3, 25");
      Assert.Equal(expected, testConstraint.Property);
    }
  }
}
