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
  public class FieldTests
  {
    private readonly ITestOutputHelper output;

    public FieldTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Ensures that the <see cref="Field"/> constructor works as espected.
    /// </summary>
    [Fact]
    public void FieldValidConfiguration()
    {
      Field<string> field = new("TestField", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!");
      Assert.Equal("TestField", field.FieldName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", field.Description);
    }

    /// <summary>
    /// Ensures that the <see cref="Field"/> constructor will not accept an empty string as a name.
    /// </summary>
    [Fact]
    public void FieldInvalidName() => Assert.Throws<ArgumentNullException>(() => new Field<string>("", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!"));

    /// <summary>
    /// Ensures that the constructor with the Optional parameter works properly.
    /// </summary>
    [Fact]
    public void FieldValidConfigurationWithOptional()
    {
      Field<string> field = new("TestField", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", "This is what you get if you don't put anything in for TestField!");
      Assert.Equal("TestField", field.FieldName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", field.Description);
      Assert.Equal("This is what you get if you don't put anything in for TestField!", field.DefaultValue);
    }

    /// <summary>
    /// Ensures that a <see cref="Field"/> will not accept more than one of the same type argument.
    /// </summary>
    [Fact]
    public void FieldDuplicateTypeArgumentsThrows()
    {
      Assert.Throws<ArgumentException>(() => new Field<string, string>("Test Field", "Uh oh."));
    }

    /// <summary>
    /// Ensures that ToJProperty() works as expected without constraints.
    /// </summary>
    [Fact]
    public void ToJPropertyTest()
    {
      Field<string> field = new("TestField", "This is a tautological description.");
      JProperty expected = new("TestField", new JObject() { { "Constraints", new JObject() { { "Type", "String" } } }, { "Description", "This is a tautological description." } });
      Assert.Equal(expected, field.ToJProperty());
    }

    /// <summary>
    /// Ensures that ToJProperty() works as expected with a constraint.
    /// </summary>
    [Fact]
    public void ToJPropertyWithConstraintTest()
    {
      Field<string> field = new("TestField", "Ceci n'est pas une description", new Constraint<string>[] { AllowValues("Russia", "United States", "Georgia", "Chad") });
      JProperty expected = new("TestField", new JObject() { { "Constraints", new JObject() { { "Type", "String" }, { "AllowValues", new JArray() { "Russia", "United States", "Georgia", "Chad" } } } }, { "Description", "Ceci n'est pas une description" } });
      Assert.Equal(expected, field.ToJProperty());
    }

    /// <summary>
    /// Ensures that AddNewType works correctly by building a <see cref="Field"/>, adding a new type, and validating a valid value. Start with a Field{int} and add the string type with an AllowValues constraint whose requirements are met. Then, pass a string field for validation via Schema.
    /// </summary>
    [Fact]
    public void AddNewTypeValidTest()
    {
      JObject testConfig = new() { { "Test Field", "time." } };
      Field<int> field = new("Test Field", "Once more into the breach.", new Constraint<int>[] { ConstrainValue(40, 50) });
      Field newField = field.AddNewType(new Constraint<string>[] { AllowValues("One", "more", "time.") });
      Schema testSchema = new(newField);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.False(testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that AddNewType works correctly by building a field, adding a new type, and validating an invalid value. Start with a Field{int} and add the string type with an AllowValues constraint whose requirements are met. Then, pass a string field for validation via Schema.
    /// </summary>
    [Fact]
    public void AddNewTypeInvalidTest()
    {
      JObject testConfig = new() { { "Test Field", "Surprise! an invalid value!" } };
      Field<int> field = new("Test Field", "Once more into the breach.", new Constraint<int>[] { ConstrainValue(40, 50) });
      Field newField = field.AddNewType(new Constraint<string>[] { AllowValues("One", "more", "time.") });
      Schema testSchema = new(newField);
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
      JObject testConfig = new() { { "Test Field", 45 } };
      Field<string> field = new("Test Field", "Once more into the breach.", new Constraint<string>[] { AllowValues("One", "more", "time.") });
      Field newField = field.AddNewType(new Constraint<int>[] { ConstrainValue(40, 50) });
      Schema testSchema = new(newField);
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
      Field<string> field = new("Test Field", "An innocent and carefree string Field.");
      Assert.Throws<ArgumentException>(() => field.AddNewType<string>());
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
      Field<DateTime> field = new("Test Field", "The date by which you will finish counting an indescribable number of lima beans.", new Constraint<DateTime>[] { ConstrainValue(DateTime.Parse("3021-01-01"), DateTime.Parse("3021-12-01")), ConstrainDateTimeFormat("yyyy-MM-dd") });
      JObject testConfig = new() { { "Test Field", input } };
      Schema testSchema = new(field);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.Equal(expectedResult, !testSchema.ErrorList.AnyFatal());
    }

    [Theory]
    [InlineData(true,"20211205")]
    [InlineData(false,"20211213")]
    public void FieldValidateDateTimeTest(bool expectedResult, string input)
    {
      Field<DateTime> field = new("Test Field", "From the moment I understood the weakness of my flesh, it disgusted me.",new Constraint<DateTime>[] { ConstrainDateTimeFormat("yyyyddMM") });
      JObject testConfig = new() { { "Test Field", input } };
      Schema testSchema = new(field);
      testSchema.Validate(testConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', testSchema.ErrorList));
      Assert.Equal(expectedResult, !testSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that passing a DefaultValue that cannot be cast to a <see cref="Field"/>'s type will throw an exception.
    /// </summary>
    [Fact]
    public void InvalidDefaultValueThrows()
    {
      Field<DateTime> field;
      Assert.Throws<ArgumentException>(() => field = new("Test Field", "A field that desperately desires to contain a date, but alas, it does not.", "pudding"));
    }

    /// <summary>
    /// Ensures that passing a string DefaultValue that can be cast to the <see cref="Field"/>'s type does not throw an exception.
    /// </summary>
    [Fact]
    public void ValidDefaultValueDoesNotThrow()
    {
      Field<DateTime> field = new("Test Field", "A field that is thankful to have been provided a date.", "2021-09-03");
      Assert.True(true);
    }


    /// <summary>
    /// Ensures that passing a DefaultValue of the <see cref="Field"/>'s type does not throw an exception.
    /// </summary>
    [Fact]
    public void ValidDateTimeDefaultValueDoesNotThrow()
    {
      Field<DateTime> field = new("Test Field", "A field that is thankful to have been provided a date.", new DateTime(2021,09,03));
      Assert.True(true);
    }
  }
}
