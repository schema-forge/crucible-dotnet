using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaTests
{
  [Trait("Crucible", "")]
  public class ConfigTokenTests
  {
    private readonly ITestOutputHelper output;

    public ConfigTokenTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Ensures that the ConfigToken constructor works as espected.
    /// </summary>
    [Fact]
    public void ConfigTokenValidConfiguration()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.Description);
    }

    /// <summary>
    /// Ensures that the ConfigToken constructor will not accept an empty string as a name.
    /// </summary>
    [Fact]
    public void ConfigTokenInvalidName() => Assert.Throws<ArgumentNullException>(() => new ConfigToken<string>("", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!"));

    /// <summary>
    /// Ensures that the constructor with the Optional parameter works properly.
    /// </summary>
    [Fact]
    public void ConfigTokenValidConfigurationWithOptional()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", "This is what you get if you don't put anything in for TestToken!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.Description);
      Assert.Equal("This is what you get if you don't put anything in for TestToken!", token.DefaultValue);
    }

    /// <summary>
    /// Ensures that a ConfigToken will not accept more than one of the same type argument.
    /// </summary>
    [Fact]
    public void ConfigTokenDuplicateTypeArgumentsThrows()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken<string, string>("Test Token", "Uh oh."));
    }

    /// <summary>
    /// Ensures that ToJProperty() works as expected without constraints.
    /// </summary>
    [Fact]
    public void ToJPropertyTest()
    {
      ConfigToken<string> token = new("TestToken", "This is a tautological description.");
      JProperty expected = new("TestToken", new JObject() { { "Constraints", new JObject() { { "Type", "String" } } }, { "Description", "This is a tautological description." } });
      Assert.Equal(expected, token.ToJProperty());
    }

    /// <summary>
    /// Ensures that ToJProperty() works as expected with a constraint.
    /// </summary>
    [Fact]
    public void ToJPropertyWithConstraintTest()
    {
      ConfigToken<string> token = new("TestToken", "Ceci n'est pas une description", new Constraint<string>[] { AllowValues("Russia", "United States", "Georgia", "Chad") });
      JProperty expected = new("TestToken", new JObject() { { "Constraints", new JObject() { { "Type", "String" }, { "AllowValues", new JArray() { "Russia", "United States", "Georgia", "Chad" } } } }, { "Description", "Ceci n'est pas une description" } });
      Assert.Equal(expected, token.ToJProperty());
    }

    /// <summary>
    /// Ensures that AddNewType works correctly by building a token, adding a new type, and validating a valid value. Start with a ConfigToken{int} and add the string type with an AllowValues constraint whose requirements are met. Then, pass a string token for validation via Schema.
    /// </summary>
    [Fact]
    public void AddNewTypeValidTest()
    {
      JObject testConfig = new() { { "Test Token", "time." } };
      ConfigToken<int> token = new("Test Token", "Once more into the breach.", new Constraint<int>[] { ConstrainValue(40, 50) });
      ConfigToken newToken = token.AddNewType(new Constraint<string>[] { AllowValues("One", "more", "time.") });
      Schema testSchema = new(newToken);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.False(testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that AddNewType works correctly by building a token, adding a new type, and validating an invalid value. Start with a ConfigToken{int} and add the string type with an AllowValues constraint whose requirements are met. Then, pass a string token for validation via Schema.
    /// </summary>
    [Fact]
    public void AddNewTypeInvalidTest()
    {
      JObject testConfig = new() { { "Test Token", "Surprise! an invalid value!" } };
      ConfigToken<int> token = new("Test Token", "Once more into the breach.", new Constraint<int>[] { ConstrainValue(40, 50) });
      ConfigToken newToken = token.AddNewType(new Constraint<string>[] { AllowValues("One", "more", "time.") });
      Schema testSchema = new(newToken);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.True(testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// This test references the fact that casts and applied constraints are attempted in order; in this case, casting "45" to String will succeed, but "45" is not in the list of allowed values.
    /// </summary>
    [Fact]
    public void AddNewTypeInvalidTypeOrderTest()
    {
      JObject testConfig = new() { { "Test Token", 45 } };
      ConfigToken<string> token = new("Test Token", "Once more into the breach.", new Constraint<string>[] { AllowValues("One", "more", "time.") });
      ConfigToken newToken = token.AddNewType(new Constraint<int>[] { ConstrainValue(40, 50) });
      Schema testSchema = new(newToken);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.True(testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that AddNewType does not allow a user to add a type that is already present.
    /// </summary>
    [Fact]
    public void AddNewTypeThrowsIfTypeAlreadyPresent()
    {
      ConfigToken<string> token = new("Test Token", "An innocent and carefree string ConfigToken.");
      Assert.Throws<ArgumentException>(() => token.AddNewType<string>());
    }

    /// <summary>
    /// Tests Format Constraints as well as testing Standard constraints. Format constraints apply to a given value as if it were a string; standard constraints apply to a given value as its actual value type.
    /// In this case, the format constraint constrains how a datetime value has been presented.
    /// </summary>
    /// <param name="expectedResult">Expected outcome of the test condition.</param>
    /// <param name="input">Value to test against the constraints.</param>
    [Theory]
    [InlineData(true, "3021-05-03")] // Passes both.
    [InlineData(false, "3022-08-01")] // Fails ConstrainValue.
    [InlineData(false, "3021-05-03T12:00:00")] // Fails ConstrainDateTimeFormat.
    [InlineData(false, "3022-08-01T23:59:59")] // Fails both.
    public void MixedFormatAndStandardConstraintsTest(bool expectedResult, string input)
    {
      ConfigToken<DateTime> token = new("Test Token", "The date by which you will finish counting an indescribable number of lima beans.", new Constraint<DateTime>[] { ConstrainValue(DateTime.Parse("3021-01-01"), DateTime.Parse("3021-12-01")), ConstrainDateTimeFormat("yyyy-MM-dd") });
      JObject testConfig = new() { { "Test Token", input } };
      Schema testSchema = new(token);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.Equal(expectedResult, !testSchema.ErrorList.AnyFatal());
    }

    [Theory]
    [InlineData(true,"20211205")]
    [InlineData(false,"20211213")]
    public void ConfigTokenValidateDateTimeTest(bool expectedResult, string input)
    {
      ConfigToken<DateTime> token = new("Test Token", "From the moment I understood the weakness of my flesh, it disgusted me.",new Constraint<DateTime>[] { ConstrainDateTimeFormat("yyyyddMM") });
      JObject testConfig = new() { { "Test Token", input } };
      Schema testSchema = new(token);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.Equal(expectedResult, !testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that passing a DefaultValue that cannot be cast to a ConfigToken's type will throw an exception.
    /// </summary>
    [Fact]
    public void InvalidDefaultValueThrows()
    {
      ConfigToken<DateTime> token;
      Assert.Throws<ArgumentException>(() => token = new("Test Token", "A token that desperately desires to contain a date, but alas, it does not.", "pudding"));
    }

    /// <summary>
    /// Ensures that passing a string DefaultValue that can be cast to the ConfigToken's type does not throw an exception.
    /// </summary>
    [Fact]
    public void ValidDefaultValueDoesNotThrow()
    {
      ConfigToken<DateTime> token = new("Test Token", "A token that is thankful to have been provided a date.", "2021-09-03");
      Assert.True(true);
    }


    /// <summary>
    /// Ensures that passing a DefaultValue of the ConfigToken's type does not throw an exception.
    /// </summary>
    [Fact]
    public void ValidDateTimeDefaultValueDoesNotThrow()
    {
      ConfigToken<DateTime> token = new("Test Token", "A token that is thankful to have been provided a date.", new DateTime(2021,09,03));
      Assert.True(true);
    }
  }
}
