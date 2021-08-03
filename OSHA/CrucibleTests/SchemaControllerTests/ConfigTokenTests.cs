using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaControllerTests
{
  [Trait("Crucible", "")]
  public class ConfigTokenTests : Schema
  {
    private readonly ITestOutputHelper output;

    public ConfigTokenTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void ConfigTokenValidConfiguration()
    {
      ConfigToken token = new ConfigToken("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", ApplyConstraints<string>());
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.HelpString);
    }

    [Fact]
    public void ConfigTokenInvalidName()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", ApplyConstraints<string>()));
    }

    [Fact]
    public void ConfigTokenInvalidHelpString()
    {
      Assert.Throws<ArgumentException>(() => new ConfigToken("BestPracticesOrDie", "", ApplyConstraints<string>()));
    }

    [Fact]
    public void ConfigTokenValidConfigurationWithOptional()
    {
      ConfigToken token = new ConfigToken("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", "This is what you get if you don't put anything in for TestToken!", ApplyConstraints<string>());
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.HelpString);
      Assert.Equal("This is what you get if you don't put anything in for TestToken!", token.DefaultValue);
    }
  }
}
