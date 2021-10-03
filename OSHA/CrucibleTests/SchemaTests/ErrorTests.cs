using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace SchemaTests
{
  public class ErrorTests
  {
    /// <summary>
    /// Ensures that the Error constructor populates values correctly.
    /// </summary>
    [Fact]
    public void Error_ConstructorInitializesErrorMessage()
    {
      Error error = new("You dun goofed!");
      Assert.Equal("You dun goofed!", error.ErrorMessage);
      Assert.Equal(Severity.Fatal, error.ErrorSeverity);
      Assert.Equal("You dun goofed!", error.ErrorMessage);
    }

    /// <summary>
    /// Ensures that passing a custom error severity works correctly.
    /// </summary>
    [Fact]
    public void Error_ConstructorInitializesErrorSeverity()
    {
      Error error = new("You should not crossed the streams.", Severity.Warning);
      Assert.Equal(Severity.Warning, error.ErrorSeverity);
    }

    /// <summary>
    /// Ensures that the Error constructor throws on an empty error string.
    /// </summary>
    [Fact]
    public void Error_ConstructorThrowsOnEmptyString() => Assert.Throws<ArgumentNullException>(() => new Error(""));
  }
}
