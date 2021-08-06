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
  public class ApplyConstraintsTests
  {
    private readonly ITestOutputHelper output;

    public ApplyConstraintsTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Ensures that type checking works properly.
    /// </summary>
    [Fact]
    public void ApplyTypeConstraintValid()
    {
      bool testResult = new ConfigToken("TestToken", "Allons-y!", ApplyConstraints<string>()).Validate("A string! :D");
      Assert.True(testResult);
    }

    /// <summary>
    /// Ensures that type checking invalidation works properly.
    /// </summary>
    [Fact]
    public void ApplyTypeConstraintInvalidType()
    {
      bool testResult = new ConfigToken("TestToken", "Jeronimo!", ApplyConstraints<bool>()).Validate("Unfalsifiable! :D");
      Assert.False(testResult);
    }

    /// <summary>
    /// Ensures that that empty tokens are properly flagged by ApplyConstraints.
    /// </summary>
    [Fact]
    public void ApplyTypeConstraintNullOrEmpty()
    {
      bool testResult = new ConfigToken("TestToken", "Fantastic!", ApplyConstraints<string>()).Validate("");
      Assert.False(testResult);
    }

    /// <summary>
    /// Executes two constraints on a string that will pass both constarints.
    /// </summary>
    [Fact]
    public void ApplyConstraintsValid()
    {
      Assert.True(new ConfigToken("TestToken", "I'm sorry. I'm so sorry.", ApplyConstraints<string>(ConstrainStringLength(5), ForbidStringCharacters(',', '\n'))).Validate("Valid string!"));
    }

    /// <summary>
    /// Executes two constraints on a string that fails both conditions.
    /// </summary>
    [Fact]
    public void ApplyConstraintsInvalid()
    {
      Assert.False(new ConfigToken("TestToken", "Bow ties are cool.", ApplyConstraints<string>(ConstrainStringLength(5), ForbidStringCharacters('.', '\n'))).Validate("yup."));
    }

    /// <summary>
    /// Tests applying constraints to a token that can be one of two types.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="inputValue">Value to validate against the constraint (must be either bool or int)</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, 45)]
    [InlineData(false, "Whitaker")] // Will fail, because the input value must be either bool or int.
    public void ApplyTwoTypeConstraintTest(bool expectedResult, object inputValue)
    {
      ConfigToken testToken = new("TestToken", "Silence in the Library Part 1", ApplyConstraints<bool, int>());
      bool testResult = testToken.Validate(new JValue(inputValue));
      output.WriteLine(testToken.ErrorList.Join('\n'));
      Assert.Equal(expectedResult, testResult);
    }

    /// <summary>
    /// Tests applying different constraints to different types.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="inputValue">Value to validate using the constraints. If int, it must be between 3 and 15. If string, must be one of Tennant, Smith, and Eccleston.</param>
    [Theory]
    [InlineData(true, "Tennant")]
    [InlineData(true, 12)]
    [InlineData(false, "Whitaker")]
    [InlineData(false, 45)]
    public void ApplyTwoTypeConstraintPlusIndividualConstraintsTest(bool expectedResult, object inputValue)
    {
      Assert.Equal(expectedResult, new ConfigToken("TestToken", "Silence in the Library Part 2", ApplyConstraints<int, string>(
      constraintsIfType1: new Constraint<int>[]
        {
          ConstrainValue(3,15)
        },
      constraintsIfType2: new Constraint<string>[]
        {
          AllowValues("Tennant", "Smith", "Eccleston")
        })).Validate(new JValue(inputValue)));
    }

    /// <summary>
    /// Ensures that ApplyConstraints throws ArgumentException if duplicate constraints are passed.
    /// </summary>
    [Fact]
    public void ApplyConstraintsThrowsWithDuplicateConstraints()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Don't Blink", ApplyConstraints(AllowValues("Please", "just"),AllowValues("put", "us", "in", "one", "constraint"))));
    }
  }
}
