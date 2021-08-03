using System;
using System.Collections.Generic;
using System.Linq;
using schemaforge.Crucible;
using schemaforge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace SchemaControllerTests
{
  public class ErrorTests
  {
    [Fact]
    public void Error_ConstructorInitializesErrorMessage()
    {
      Error error = new("You dun goofed!");
      Assert.Equal("You dun goofed!", error.ErrorMessage);
      Assert.Equal(Severity.Fatal, error.ErrorSeverity);
      Assert.Equal("You dun goofed!", error.ErrorMessage);
    }

    [Fact]
    public void Error_ConstructorInitializesErrorSeverity()
    {
      Error error = new Error("You should not crossed the streams.", Severity.Warning);
      Assert.Equal(Severity.Warning, error.ErrorSeverity);
    }

    [Fact]
    public void Error_ConstructorThrowsOnEmptyString()
    {
      Assert.Throws<ArgumentNullException>(() => new Error(""));
    }
  }
}
