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

    [Theory]
    [InlineData(true, 15, 15)]
    [InlineData(false, 15, 25)]
    [InlineData(true, 15, 10, 15)]
    [InlineData(true, 15, 15, 15)]
    [InlineData(false, 15, 3, 7)]
    [InlineData(false, 15, 7, 3)]
    public void ConstrainNumericIntTests(bool expectedResult, int constrainedValue, params int[] constraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken("TestToken", "Angry String", ApplyConstraints<int>(ConstrainNumericValue(constraints[0])));
        testResult = testToken.Validate(constrainedValue);
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Relaxed String", ApplyConstraints<int>(ConstrainNumericValue(constraints[0], constraints[1]))));
          return;
        }
        else
        {
          testToken = new ConfigToken("TestToken", "Nervous String", ApplyConstraints<int>(ConstrainNumericValue(constraints[0], constraints[1])));
          testResult = testToken.Validate(constrainedValue);
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    [Theory]
    [InlineData(true, 15.3, 15.3)]
    [InlineData(false, 15.7, 15.8)]
    [InlineData(true, 15.0, 14.9, 15.0)]
    [InlineData(true, 15.0, 15.0, 15.0)]
    [InlineData(false, 15.5, 3.5, 3.5)]
    [InlineData(false, 15.5, 3.3, 3.2)]
    public void ConstrainNumericDoubleTests(bool expectedResult, double constrainedValue, params double[] constraints)
    {
      ConfigToken testToken;
      bool testResult;
      if (constraints.Length == 1)
      {
        testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<int>(ConstrainNumericValue(constraints[0])));
        testResult = testToken.Validate(constrainedValue);
      }
      else
      {
        if (constraints[0] > constraints[1])
        {
          Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "I think", ApplyConstraints<int>(ConstrainNumericValue(constraints[0], constraints[1]))));
          return;
        }
        else
        {
          testToken = new ConfigToken("TestToken", "we've done this before", ApplyConstraints<int>(ConstrainNumericValue(constraints[0], constraints[1])));
          testResult = testToken.Validate(constrainedValue);
        }
      }
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }
    [Theory]
    [MemberData(nameof(ConstrainNumericDoubleDomainData))]
    public void ConstrainNumericDoubleDomainTests(bool expectedResult, int constrainedValue, params (double, double)[] domains)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<int>(ConstrainNumericValue(domains)));
      testResult = testToken.Validate(constrainedValue);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    public static IEnumerable<object[]> ConstrainNumericDoubleDomainData
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

    [Theory]
    [MemberData(nameof(ConstrainNumericIntDomainData))]
    public void ConstrainNumericIntDomainTests(bool expectedResult, int constrainedValue, params (int, int)[] domains)
    {
      ConfigToken testToken;
      bool testResult;
      testToken = new ConfigToken("TestToken", "Deja Vu", ApplyConstraints<int>(ConstrainNumericValue(domains)));
      testResult = testToken.Validate(constrainedValue);
      output.WriteLine(string.Join('\n', testToken.ErrorList));
      Assert.Equal(testResult, expectedResult);
    }

    public static IEnumerable<object[]> ConstrainNumericIntDomainData
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

    [Fact]
    public void ConstrainNumericIntDomain_InvalidBounds()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Doomed Token", ApplyConstraints<int>(ConstrainNumericValue((15, 13)))));
    }


    [Fact]
    public void ConstrainNumericDoubleDomain_InvalidBounds()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("TestToken", "Doomed Token", ApplyConstraints<int>(ConstrainNumericValue((15.0, 13.0)))));
    }
  }
}
