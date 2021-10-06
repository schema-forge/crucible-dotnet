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
  public class DateTimeConstraintTests
  {
    private readonly ITestOutputHelper output;

    public DateTimeConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests providing specific date formats.
    /// </summary>
    /// <param name="expectedResult">Expected result of the test condition.</param>
    /// <param name="dateString">Date to be processed, as a string.</param>
    /// <param name="formatString">Format to use to parse the date string.</param>
    [Theory]
    [InlineData(true,"2021-01-05","yyyy-MM-dd")]
    [InlineData(false, "2021-13-05", "yyyy-MM-dd")]
    public void ConstrainDateTimeFormatTests(bool expectedResult, string dateString, string formatString)
    {
      Constraint<DateTime> testConstraint = ConstrainDateTimeFormat(formatString);

      Assert.Equal(expectedResult, !testConstraint.FormatFunction(dateString, "Test Token").AnyFatal());
    }
  }
}
