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
    [Fact]
    public void ConfigTokenValidConfiguration()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.Description);
    }

    [Fact]
    public void ConfigTokenInvalidName() => Assert.Throws<ArgumentNullException>(() => new ConfigToken<string>("", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!"));

    //[Fact]
    //public void ConfigTokenInvalidHelpString() => Assert.Throws<ArgumentNullException>(() => new ConfigToken<string>("BestPracticesOrDie", ""));

    [Fact]
    public void ConfigTokenValidConfigurationWithOptional()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", "This is what you get if you don't put anything in for TestToken!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.Description);
      Assert.Equal("This is what you get if you don't put anything in for TestToken!", token.DefaultValue);
    }

    [Fact]
    public void ConfigTokenDuplicateTypeArgumentsThrows()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken<string, string>("Test Token", "Uh oh."));
    }

    [Fact]
    public void ToJPropertyTest()
    {
      ConfigToken<string> token = new("TestToken", "This is a tautological description.");
      JProperty expected = new("TestToken", new JObject() { { "Constraints", new JObject() { { "Type", "String" } } }, { "Description", "This is a tautological description." } });
      Assert.Equal(expected, token.ToJProperty());
    }

    [Fact]
    public void ToJPropertyWithConstraintTest()
    {
      ConfigToken<string> token = new("TestToken", "Ceci n'est pas une description", new Constraint<string>[] { AllowValues("Russia", "United States", "Georgia", "Chad") });
      JProperty expected = new("TestToken", new JObject() { { "Constraints", new JObject() { { "Type", "String" }, { "AllowValues", new JArray() { "Russia", "United States", "Georgia", "Chad" } } } }, { "Description", "Ceci n'est pas une description" } });
      Assert.Equal(expected, token.ToJProperty());
    }

    // Start with a ConfigToken<int> and add the string type with an AllowValues constraint whose requirements are met. Then, pass a string token for validation via Schema.
    // Expected not to generate a fatal error.
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

    // Start with a ConfigToken<int> and add the string type with an AllowValues constraint whose requirements are not met. Then, pass a string token for validation via Schema.
    // Expected to generate a fatal error.
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

    // This should generate a fatal error because it creates a ConfigToken<string,int>. The string cast will succeed, and the token will be tested against AllowValues, which will fail.
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

    [Fact]
    public void AddNewTypeThrowsIfTypeAlreadyPresent()
    {
      ConfigToken<string> token = new("Test Token", "An innocent and carefree string ConfigToken.");
      Assert.Throws<ArgumentException>(() => token.AddNewType<string>());
    }

    // Testing mixing together Standard constraints and Format constraints.
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
  }
}
