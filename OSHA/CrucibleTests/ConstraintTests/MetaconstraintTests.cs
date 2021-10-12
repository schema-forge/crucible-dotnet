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
  public class MetaConstraintTests
  {
    private readonly ITestOutputHelper output;

    public MetaConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests <see cref="MatchAnyConstraint{TValueType}(Constraint{TValueType}[])"/> with three constraints and values that pass the first, last, middle, and all constraints, or fail them all.
    /// </summary>
    /// <param name="expectedResult">Expected validation result from the tested method.</param>
    /// <param name="constrainedValue">Value to validate against the test conditions.</param>
    [Theory]
    [InlineData(true, 15)] // Pass only first constraint.
    [InlineData(true, 45)] // Pass only last constraint.
    [InlineData(true, 101)] // Pass only middle constraint.
    [InlineData(true, 10)] // Pass two constraints.
    [InlineData(false, 40)] // Fail all constraints.
    public void AnyConstraintTests(bool expectedResult, int constrainedValue)
    {
      // Conditions of constraint: Value must be less than or equal to 15, greater than or equal to 100, or 10, 35, 45, or 55.
      Constraint<int> newConstraint = MatchAnyConstraint(ConstrainValueUpperBound(15), ConstrainValueLowerBound(100), AllowValues(10, 35, 45, 55));
      List<SchemaError> testResult = newConstraint.Function(constrainedValue, "Test Value");
      output.WriteLine($"Test value: {constrainedValue}");
      output.WriteLine(string.Join("\n", testResult));
      Assert.Equal(expectedResult, !testResult.AnyFatal());
    }
  }
}
