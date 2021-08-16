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
    /// Ensures that the constructor populates Function and Property.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorValid()
    {
      Constraint<JToken> constraint = new(TestFunction, new JProperty("ConstraintName", "Bill"));
      Assert.NotNull(constraint.Function);
      Assert.NotNull(constraint.Property);
    }

    /// <summary>
    /// Ensures that the constructor throws ArgumentNullException when the name of the JProperty is empty.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorThrowsOnNullName() => Assert.Throws<ArgumentNullException>(() => new Constraint<JToken>(TestFunction, new JProperty("", "Ghost Property")));

    /// <summary>
    /// Ensures that the constructor throws ArgumentNullException when the value of the JProperty is empty.
    /// </summary>
    [Fact]
    public void Constraint_ConstructorThrowsOnNullValue() => Assert.Throws<ArgumentNullException>(() => new Constraint<JToken>(TestFunction, new JProperty("EmptyInside", "")));
  }
}
