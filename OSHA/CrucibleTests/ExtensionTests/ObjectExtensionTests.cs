using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Xunit;

namespace Extensions
{
  public class ObjectExtensionTests
  {
    [Fact]
    public void Exists_ReturnsTrueIfNotNull()
    {
      string testString = "I'm a real boy!";
      Assert.True(testString.Exists());
    }

    [Fact]
    public void Exists_ReturnsTrueIfNotNullForRegex()
    {
      Regex pattern = new Regex("([A-z])+");
      Assert.True(pattern.Exists());
    }


    [Fact]
    public void Exists_ReturnsTrueForEmptyString()
    {
      string aLonelyAndSadStringWithNoContents = "";
      Assert.True(aLonelyAndSadStringWithNoContents.Exists());
    }

    [Fact]
    public void Exists_ReturnsFalseForNullString()
    {
      string aStringThatIsNotOnlyEmptyInsideButIsActuallyNullForReal = null;
      Assert.False(aStringThatIsNotOnlyEmptyInsideButIsActuallyNullForReal.Exists());
    }

    [Fact]
    public void Exists_ReturnsFalseIfNullForRegex()
    {
      Regex pattern = null;
      Assert.False(pattern.Exists());
    }

    [Fact]
    public void Exists_NullableStructExistsIfNotNull()
    {
      bool? existentialBool = true;
      Assert.True(existentialBool.Exists());
    }

    [Fact]
    public void Exists_NullableStructDoesNotExistIfNull()
    {
      bool? existentialBool = null;
      Assert.False(existentialBool.Exists());
    }
  }
}
