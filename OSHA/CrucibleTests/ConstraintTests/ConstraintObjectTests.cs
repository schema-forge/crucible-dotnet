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
  public class ConstraintObjectTests
  {
    /// <summary>
    /// Empty (but valid) function to make testing easy.
    /// </summary>
    readonly Func<JToken, string, List<Error>> TestFunction = (JToken input, string inputName) => new List<Error>();

    /// <summary>
    /// Tests to ensure that the constructor populates JsonConstraints properly.
    /// </summary>
    [Fact]
    public void ConstraintContainer_ConstructorValid()
    {
      ConstraintContainer newContainer = new(TestFunction, new JArray() { new JObject() { { "Type", "AnyType" } } });
      Assert.NotEmpty(newContainer.JsonConstraints);
    }

    /// <summary>
    /// Tests to ensure that a non-JObject in the passed JArray throws ArgumentException.
    /// </summary>
    [Fact]
    public void ConstraintContainer_ConstructThrowsOnInvalidArray()
    {
      Assert.Throws<ArgumentException>(() => new ConstraintContainer(TestFunction, new JArray() { new JArray() }));
    }

    /// <summary>
    /// Tests to ensure that a JObject that does not contain Type throws ArgumentException.
    /// </summary>
    [Fact]
    public void ConstraintContainer_ConstructorThrowsWhenMissingType()
    {
      Assert.Throws<ArgumentException>(() => new ConstraintContainer(TestFunction, new JArray() { new JObject() }));
    }

    /// <summary>
    /// Tests to ensure that ToString() returns one JObject when only one set of type-based constraints is passed.
    /// </summary>
    [Fact]
    public void ConstraintContainer_ToString1ConstraintSet()
    {
      ConstraintContainer testContainer = ApplyConstraints<string>(AllowValues("Something", "or other"));
      JObject expected = new()
      {
        { "Type", "System.String" },
        { "AllowValues", new JArray() { "Something", "or other" } }
      };

      Assert.Equal(expected.ToString(), testContainer.ToString());
    }

    /// <summary>
    /// Tests to ensure that ToString() returns two JObject string representations in an array when there are two sets of type-based constraints.
    /// </summary>
    [Fact]
    public void ConstraintContainer_ToString2ConstraintSets()
    {
      ConstraintContainer testContainer = ApplyConstraints<int,string>(new Constraint<int>[] { ConstrainValue((5,15),(35,56)) },new Constraint<string>[] { AllowValues("Something", "or other") });

      JObject expected1 = new()
      {
        { "Type", "System.Int32" },
        { "ConstrainValue", new JArray() { "(5, 15)", "(35, 56)" } }
      }; 
      
      JObject expected2 = new()
      {
        { "Type", "System.String" },
        { "AllowValues", new JArray() { "Something", "or other" } }
      };

      JArray expected = new JArray() { expected1, expected2 };

      Assert.Equal(expected.ToString(), testContainer.ToString());
    }

    /// <summary>
    /// Ensures that the constructor populates Function and Property.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorValid()
    {
      Constraint<JToken> constraint = new Constraint<JToken>(TestFunction, new JProperty("ConstraintName", "Bill"));
      Assert.NotNull(constraint.Function);
      Assert.NotNull(constraint.Property);
    }

    /// <summary>
    /// Ensures that the constructor throws ArgumentNullException when the name of the JProperty is empty.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorThrowsOnNullName()
    {
      Assert.Throws<ArgumentNullException>(() => new Constraint<JToken>(TestFunction, new JProperty("", "Ghost Property")));
    }

    /// <summary>
    /// Ensures that the constructor throws ArgumentNullException when the value of the JProperty is empty.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorThrowsOnNullValue()
    {
      Assert.Throws<ArgumentNullException>(() => new Constraint<JToken>(TestFunction, new JProperty("EmptyInside", "")));
    }
  }
}
