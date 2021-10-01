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
  public class JArrayConstraintTests
  {
    private readonly ITestOutputHelper output;

    public JArrayConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests for ConstrainCollectionCount on JArray.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json containing the array that will be tested.</param>
    /// <param name="constraints">Upper and lower bounds to pass to ConstrainCollectionCount.</param>
    [Theory]
    [InlineData(true, "{'TestArray':[1,'fish']}", 2)] // Equal to lower.
    [InlineData(false, "{'TestArray':['beat']}", 2)] // Too few.
    [InlineData(true, "{'TestArray':[2,'fish']}", 1, 2)] //Equal to upper.
    [InlineData(true, "{'TestArray':['red','fish']}", 2, 2)] // Exactly equal.
    [InlineData(false, "{'TestArray':['blue','fish']}", 3, 5)] // Too few.
    [InlineData(false, "{'TestArray':['arm1','arm2','leg1','leg2','theforbiddenone']}", 1, 3)] // Too many.
    [InlineData(false, "{'TestArray':['doomed']}", 4, 2)] // Exception test.
    public void ConstrainCollectionCountTests(bool expectedResult, string constrainedJson, params int[] constraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken<JArray>("TestArray", "Eat the ice cream.", new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(constraints[0]) });
        testResult = testToken.Validate(JObject.Parse(constrainedJson), new JObjectTranslator());
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken<JArray>("TestToken", "I don't want any more.", new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(constraints[0], constraints[1]) }));
          return;
        }
        else
        {
          testToken = new ConfigToken<JArray>("TestArray", "Humans require ice cream.", new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(constraints[0], constraints[1]) });
          testResult = testToken.Validate(JObject.Parse(constrainedJson), new JObjectTranslator());
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests applying constraints to all values of an array.
    /// In this case, checks to ensure that all the values are valid ints.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json containing the array that will be tested.</param>
    [Theory]
    [InlineData(true, "{'TestArray':[1,5,15]}")]
    [InlineData(false, "{'TestArray':[]}")]
    [InlineData(false, "{'TestArray':[1,'Cake and grief counseling will be available at the conclusion of the test.',15]}")]
    public void ApplyTypeConstraintsToAllArrayValuesTests(bool expectedResult, string constrainedJson)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken<JArray>("TestArray", "Where's David?", new Constraint<JArray>[] { ApplyConstraintsToJArray<int>() });
      testResult = testToken.Validate(JObject.Parse(constrainedJson), new JObjectTranslator());
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests applying constraints to all values of an array.
    /// In this case, checks for int type and gives a minimum value.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json containing the array that will be tested.</param>
    [Theory]
    [InlineData(true, "{'TestArray':[5,15]}")]
    [InlineData(false, "{'TestArray':[1,5,15]}")]
    [InlineData(false, "{'TestArray':[]}")]
    [InlineData(false, "{'TestArray':[1,'Please do not attempt to remove testing apparatus from the testing area.',15]}")]
    public void ApplyOneConstraintToAllArrayValuesTests(bool expectedResult, string constrainedJson)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken<JArray>("TestArray", "Everyone you love is gone.", new Constraint<JArray>[] { ApplyConstraintsToJArray(ConstrainValueLowerBound(5)) });
      testResult = testToken.Validate(JObject.Parse(constrainedJson), new JObjectTranslator());
      output.WriteLine($"Input array: {string.Join(",", JObject.Parse(constrainedJson)["TestArray"])}");
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(expectedResult, testResult);
    }

    /// <summary>
    /// Tests applying two constraints to all values of an array. In this case, contains minimum string length and forbids / ? and !.
    /// </summary>
    /// <param name="expectedResult">Expected validation result.</param>
    /// <param name="constrainedJson">Json containing the array that will be tested.</param>
    [Theory]
    [InlineData(true, "{'TestArray':['Do not touch the operational end of The Device.','Do not look directly at the operational end of The Device.']}")] // Passes all constraints.
    [InlineData(false, "{'TestArray':['Well done! Your OSHA compliance score is currently 7/129. Keep going, sport!']}")] // Fails one constraint.
    [InlineData(false, "{'TestArray':[{'MessageFollows':'Here is a complimentary invalid value in your array, to keep you on your toes.'},'And here is a valid value, to ensure that your OSHA compliance score continues to rise.']}")]
    [InlineData(false, "{'TestArray':['Invalid value!','A much calmer and more relaxed valid value.']}")] // First value fails both constraints.
    public void ApplyTwoConstraintsToAllArrayValuesTests(bool expectedResult, string constrainedJson)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken<JArray>("TestArray", "There is only ice cream.", new Constraint<JArray>[] { ApplyConstraintsToJArray(ConstrainStringLength(15), ForbidSubstrings("/", "?", "!")) });
      testResult = testToken.Validate(JObject.Parse(constrainedJson), new JObjectTranslator());
      output.WriteLine($"Input array: {string.Join(",", JObject.Parse(constrainedJson)["TestArray"])}");
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }
  }
}
