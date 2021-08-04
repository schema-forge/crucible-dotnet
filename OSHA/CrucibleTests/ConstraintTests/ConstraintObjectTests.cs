using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ConstraintTests
{
  public class ConstraintObjectTests
  {
    Func<JToken, string, List<Error>> TestFunction = (JToken input, string inputName) => new List<Error>();
    [Fact]
    public void ConstraintContainer_ConstructorValid()
    {
      ConstraintContainer newContainer = new(TestFunction, new JArray() { new JObject() { { "Type", "AnyType" } } });
      Assert.NotEmpty(newContainer.JsonConstraints);
    }

    [Fact]
    public void ConstraintContainer_ConstructThrowsOnInvalidArray()
    {
      Assert.Throws<ArgumentException>(() => new ConstraintContainer(TestFunction, new JArray() { new JArray() }));
    }

    [Fact]
    public void ConstraintContainer_ConstructorThrowsWhenMissingType()
    {
      Assert.Throws<ArgumentException>(() => new ConstraintContainer(TestFunction, new JArray() { new JObject() }));
    }

    [Fact]
    public void Constraint_ConstructorValid()
    {
      Constraint constraint = new(TestFunction, new JProperty("ConstraintName", "Bill"));
      Assert.NotNull(constraint.Function);
    }

    [Fact]
    public void Constraint_ConstructorThrowsOnNullName()
    {
      Assert.Throws<ArgumentNullException>(() => new Constraint(TestFunction, new JProperty("", "Ghost Property")));
    }

    [Fact]
    public void Constraint_ConstructorThrowsOnNullValue()
    {
      Assert.Throws<ArgumentNullException>(() => new Constraint(TestFunction, new JProperty("EmptyInside", "")));
    }
  }
}
