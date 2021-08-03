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
  public class ApplyConstraintsTests : SchemaController
  {
    private readonly ITestOutputHelper output;

    public ApplyConstraintsTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void ApplyTypeConstraintValid()
    {
      bool testResult = new ConfigToken("TestToken", "Allons-y!", ApplyConstraints<string>()).Validate("A string! :D");
      Assert.True(testResult);
    }

    [Fact]
    public void ApplyTypeConstraintInvalidType()
    {
      bool testResult = new ConfigToken("TestToken", "Jeronimo!", ApplyConstraints<bool>()).Validate("Unfalsifiable! :D");
      Assert.False(testResult);
    }

    [Fact]
    public void ApplyTypeConstraintNullOrEmpty()
    {
      bool testResult = new ConfigToken("TestToken", "Fantastic!", ApplyConstraints<string>()).Validate("");
      Assert.False(testResult);
    }

    [Fact]
    public void ApplyConstraintsValid()
    {
      Assert.True(new ConfigToken("TestToken", "I'm sorry. I'm so sorry.", ApplyConstraints<string>(ConstrainStringLength(5), ForbidStringCharacters(',', '\n'))).Validate("Valid string!"));
    }

    [Fact]
    public void ApplyConstraintsInvalid()
    {
      Assert.False(new ConfigToken("TestToken", "Bow ties are cool.", ApplyConstraints<string>(ConstrainStringLength(5), ForbidStringCharacters('.', '\n'))).Validate("yup."));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, 45)]
    [InlineData(false, "Whitaker")]
    public void ApplyTwoTypeConstraintTest(bool expectedResult, object inputValue)
    {
      Assert.Equal(expectedResult, new ConfigToken("TestToken", "Silence in the Library", ApplyConstraints<bool, int>()).Validate(new JValue(inputValue)));
    }

    [Theory]
    [InlineData(true, "Tennant")]
    [InlineData(true, 12)]
    [InlineData(false, "Whitaker")]
    [InlineData(false, 45)]
    public void ApplyTwoTypeConstraintPlusIndividualConstraintsTest(bool expectedResult, object inputValue)
    {
      Assert.Equal(expectedResult, new ConfigToken("TestToken", "Silence in the Library", ApplyConstraints<int, string>(
      constraintsIfT1: new Func<JToken, string, List<Error>>[]
        {
          ConstrainNumericValue(3,15)
        },
      constraintsIfT2: new Func<JToken, string, List<Error>>[]
        {
          ConstrainStringValues("Tennant", "Smith", "Eccleston")
        })).Validate(new JValue(inputValue)));
    }
  }
}
