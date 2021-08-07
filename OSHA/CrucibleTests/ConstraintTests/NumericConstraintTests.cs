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
  public class NumericConstraintTests
  {
    private readonly ITestOutputHelper output;

    public NumericConstraintTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Tests ConstrainValue on ints.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedValue">Value to check with ConstrainValue.</param>
    /// <param name="constraints">Arguments to be passed to ConstrainValue.</param>
    [Theory]
    [InlineData(true, 15, 15)]
    [InlineData(false, 15, 25)]
    [InlineData(true, 15, 10, 15)]
    [InlineData(true, 15, 15, 15)]
    [InlineData(false, 15, 3, 7)]
    [InlineData(false, 15, 7, 3)]
    public void ConstrainApplyInnerFunctionIntTests(bool expectedResult, int constrainedValue, params int[] constraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken("TestToken", "Angry String", ApplyConstraints(ConstrainValue(constraints[0])));
        testResult = testToken.Validate(constrainedValue);
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Relaxed String", ApplyConstraints<int>(ConstrainValue(constraints[0], constraints[1]))));
          return;
        }
        else
        {
          testToken = new ConfigToken("TestToken", "Nervous String", ApplyConstraints<int>(ConstrainValue(constraints[0], constraints[1])));
          testResult = testToken.Validate(constrainedValue);
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests ConstrainValue on doubles.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedValue">Value to check with ConstrainValue.</param>
    /// <param name="constraints">Arguments to be passed to ConstrainValue.</param>
    [Theory]
    [InlineData(true, 15.3, 15.3)]
    [InlineData(false, 15.7, 15.8)]
    [InlineData(true, 15.0, 14.9, 15.0)]
    [InlineData(true, 15.0, 15.0, 15.0)]
    [InlineData(false, 15.5, 3.5, 3.5)]
    [InlineData(false, 15.5, 3.3, 3.2)]
    public void ConstrainValueApplyInnerFunctionDoubleTests(bool expectedResult, double constrainedValue, params double[] constraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<double>(ConstrainValue(constraints[0])));
        testResult = testToken.Validate(constrainedValue);
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "I think", ApplyConstraints<double>(ConstrainValue(constraints[0], constraints[1]))));
          return;
        }
        else
        {
          testToken = new ConfigToken("TestToken", "we've done this before", ApplyConstraints<double>(ConstrainValue(constraints[0], constraints[1])));
          testResult = testToken.Validate(constrainedValue);
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    /// <summary>
    /// Tests ConstrainValue with domain ranges on values.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedValue">Value to check with ConstrainValue.</param>
    /// <param name="domains">Domains to be passed to ConstrainValue.</param>
    [Theory]
    [MemberData(nameof(ConstrainValueIntDomainData))]
    public void ConstrainValueApplyInnerFunctionIntDomainTests(bool expectedResult, int constrainedValue, params (int, int)[] domains)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<int>(ConstrainValue(domains)));
      testResult = testToken.Validate(constrainedValue);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    public static IEnumerable<object[]> ConstrainValueIntDomainData
    {
      get
      {
        return new[]
        {
          new object[] { true, 15, (14, 16) },
          new object[] { false, 15, (13, 14) },
          new object[] { true, 15, (10, 12), (3, 5), (14, 16) },
          new object[] { false, 13, (10, 12), (3, 5), (14, 16) }
        };
      }
    }


    /// <summary>
    /// Tests ConstrainValue with domain ranges on values.
    /// </summary>
    /// <param name="expectedResult">Expected result from validation.</param>
    /// <param name="constrainedValue">Value to check with ConstrainValue.</param>
    /// <param name="domains">Domains to be passed to ConstrainValue.</param>
    [Theory]
    [MemberData(nameof(ConstrainValueDoubleDomainData))]
    public void ConstrainValueApplyInnerFunctionDoubleDomainTests(bool expectedResult, int constrainedValue, params (double, double)[] domains)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<double>(ConstrainValue(domains)));
      testResult = testToken.Validate(constrainedValue);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    public static IEnumerable<object[]> ConstrainValueDoubleDomainData
    {
      get
      {
        return new[]
        {
          new object[] { true, 15, (14.0, 16.0) },
          new object[] { false, 15, (13.0, 14.0) },
          new object[] { true, 15, (10.0, 12.0), (3.0, 5.0), (14.0, 16.0) },
          new object[] { false, 13, (10.0, 12.0), (3.0, 5.0), (14.0, 16.0) }
        };
      }
    }

    /// <summary>
    /// Ensures that passing a domain with a higher upper bound than a lower bound will throw an exception. Invalid inputs will be punished.
    /// </summary>
    [Fact]
    public void ConstrainNumericDomain_InvalidBounds()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Doomed Token", ApplyConstraints<int>(ConstrainValue((15, 13)))));
    }

    /// <summary>
    /// Ensures that the JProperty passed back from the constraint function with lower bound is as expected.
    /// </summary>
    [Fact]
    public void ConstrainValuePropertyTestLowerBound()
    {
      Constraint testConstraint = ConstrainValue(25);
      JProperty expected = new("ConstrainValue", 25);
      Assert.Equal(expected, testConstraint.Property);
    }

    /// <summary>
    /// Ensures that the JProperty passed back from the constraint function with a lower bound and upper bound is as expected.
    /// </summary>
    [Fact]
    public void ConstrainValuePropertyTestUpperAndLowerBound()
    {
      Constraint testConstraint = ConstrainValue(25,75);
      JProperty expected = new("ConstrainValue", "25, 75");
      Assert.Equal(expected, testConstraint.Property);
    }


    /// <summary>
    /// Ensures that the JProperty passed back from the constraint function with domains is as expected.
    /// </summary>
    [Fact]
    public void ConstrainValuePropertyTestIntDomains()
    {
      Constraint testConstraint = ConstrainValue((25, 75), (1, 3));
      JProperty expected = new("ConstrainValue", new JArray() { "(25, 75)", "(1, 3)" });
      Assert.Equal(expected, testConstraint.Property);
    }


    /// <summary>
    /// Ensures that the JProperty passed back from the constraint function with double domains is as expected.
    /// </summary>
    [Fact]
    public void ConstrainValuePropertyTestDoubleDomains()
    {
      Constraint testConstraint = ConstrainValue((25.3, 75.7), (1.6, 3.1));
      JProperty expected = new("ConstrainValue", new JArray() { "(25.3, 75.7)", "(1.6, 3.1)" });
      Assert.Equal(expected, testConstraint.Property);
    }

    [Theory]
    [InlineData(true,35.55,2)]
    [InlineData(true, 35.55, 3)]
    [InlineData(false, 35.5535, 3)]
    [InlineData(false, 35.5, 0)]
    public void ConstrainDigitsTests(bool expected, double constrainedValue, int arg)
    {
      Constraint<double> testConstraint = ConstrainDigits(arg);
      JProperty expectedProperty = new("ConstrainDigits", arg);
      Assert.Equal(expectedProperty, testConstraint.Property);
      Assert.Equal(expected, !testConstraint.Function(constrainedValue, "Test Token").AnyFatal());
    }
  }
}
