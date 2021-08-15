using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaTests
{
  [Trait("Crucible", "")]
  public class ConfigTokenTests
  {
    private readonly ITestOutputHelper output;

    public ConfigTokenTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void ConfigTokenValidConfiguration()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.HelpString);
    }

    [Fact]
    public void ConfigTokenInvalidName()
    {
      Assert.Throws<ArgumentNullException>(() => new ConfigToken<string>("", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!"));
    }

    [Fact]
    public void ConfigTokenInvalidHelpString()
    {
      Assert.Throws<ArgumentNullException>(() => new ConfigToken<string>("BestPracticesOrDie", ""));
    }

    [Fact]
    public void ConfigTokenValidConfigurationWithOptional()
    {
      ConfigToken<string> token = new("TestToken", "Hi, this is what this value does and hopefully what you did wrong in order to see this message!", "This is what you get if you don't put anything in for TestToken!");
      Assert.Equal("TestToken", token.TokenName);
      Assert.Equal("Hi, this is what this value does and hopefully what you did wrong in order to see this message!", token.HelpString);
      Assert.Equal("This is what you get if you don't put anything in for TestToken!", token.DefaultValue);
    }
  }
}
