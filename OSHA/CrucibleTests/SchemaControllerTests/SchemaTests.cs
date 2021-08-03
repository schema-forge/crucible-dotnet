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

namespace SchemaControllerTests
{
  [Trait("Crucible", "")]
  public class SchemaTests : Schema
  {
    private readonly ITestOutputHelper output;

    public SchemaTests(ITestOutputHelper output)
    {
      this.output = output;
      UserConfig = JObject.Parse(
        @"{
            'August Burns Red':'Spirit Breaker',
            'ourfathers.':4,
            'Kids':'Paramond Extended Mix'
          }");
    }
    [Fact]
    public void ValidSchemaControllerTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
        {
          new ConfigToken("August Burns Red","The commit author's favorite August Burns Red song.",ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)"))),
          new ConfigToken("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix")))
        });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
        {
          new ConfigToken("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",ApplyConstraints<int>())
        });

      Initialize();
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.True(Valid);
    }
    [Fact]
    public void UnrecognizedTokenSchemaControllerTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("August Burns Red","The commit author's favorite August Burns Red song.",ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")))
      });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",ApplyConstraints<int>())
      });

      Initialize();
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void MissingRequiredTokenSchemaControllerTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("August Burns Red","The commit author's favorite August Burns Red song.",ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix"))),
        new ConfigToken("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",ApplyConstraints<JArray>(ConstrainArrayCount(300,300)))
      });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",ApplyConstraints<int>())
      });

      Initialize();
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void NullOrEmptyTokenSchemaControllerTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("August Burns Red","The commit author's favorite August Burns Red song.",ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix")))
      });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",ApplyConstraints<int>())
      });

      UserConfig["Kids"] = "";

      Initialize();
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }

    [Fact]
    public void ValidSchemaCustomTokenControllerTest()
    {
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("August Burns Red", "The commit author's favorite August Burns Red song.", ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker", "Provision", "The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids", "The best remix of the MGMT song 'Kids'. There is only one correct answer.", ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix")))
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
        new ConfigToken("ourfathers.", "The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.", ApplyConstraints<int>())
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.True(Valid);
    }
    [Fact]
    public void UnrecognizedTokenCustomTokenSchemaControllerTest()
    {
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("August Burns Red", "The commit author's favorite August Burns Red song.", ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker", "Provision", "The Wake", "Empire (Midi)")))
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
        new ConfigToken("ourfathers.", "The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.", ApplyConstraints<int>())
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void MissingRequiredTokenCustomTokenSchemaControllerTest()
    {
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("August Burns Red", "The commit author's favorite August Burns Red song.", ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker", "Provision", "The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids", "The best remix of the MGMT song 'Kids'. There is only one correct answer.", ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix"))),
        new ConfigToken("Lincolnshire Poacher", "The first 300 numbers read out on the Lincolnshire Poacher station.", ApplyConstraints<JArray>(ConstrainArrayCount(300, 300)))
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
        new ConfigToken("ourfathers.", "The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.", ApplyConstraints<int>())
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void NullOrEmptyTokenCustomTokenSchemaControllerTest()
    {
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("August Burns Red", "The commit author's favorite August Burns Red song.", ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker", "Provision", "The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids", "The best remix of the MGMT song 'Kids'. There is only one correct answer.", ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix")))
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
        new ConfigToken("ourfathers.", "The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.", ApplyConstraints<int>())
      };

      UserConfig["Kids"] = "";

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }

    [Fact]
    public void ValidSchemaCustomAllControllerTest()
    {
      JObject customConfig = JObject.Parse(@"{'ExtraCustomToken':'35'}");
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("ExtraCustomToken", "Super special!", ApplyConstraints<string>())
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens, customConfig);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.True(Valid);
    }
    [Fact]
    public void UnrecognizedTokenCustomAllSchemaControllerTest()
    {
      JObject customConfig = JObject.Parse(@"{'ExtraCustomToken':'35','IntruderToken':'Unwelcome!'}");
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("ExtraCustomToken", "Super special!", ApplyConstraints<string>())
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens, customConfig);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void MissingRequiredTokenCustomAllSchemaControllerTest()
    {
      JObject customConfig = JObject.Parse(@"{'ExtraCustomToken':'35'}");
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("ExtraCustomToken", "Super special!", ApplyConstraints<string>()),
        new ConfigToken("ASadRequiredToken", "The token is sad because it wasn't included. :(", ApplyConstraints<string>())
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens, customConfig);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }
    [Fact]
    public void NullOrEmptyTokenCustomAllSchemaControllerTest()
    {
      JObject customConfig = JObject.Parse(@"{'ExtraCustomToken':''}");
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("ExtraCustomToken", "Super special!", ApplyConstraints<string>())
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens, customConfig);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.False(Valid);
    }


    [Fact]
    public void OptionalTokenAddedControllerTest()
    {
      JObject customConfig = JObject.Parse(@"{'ExtraCustomToken':'35'}");
      HashSet<ConfigToken> CustomRequiredConfigTokens = new()
      {
        new ConfigToken("ExtraCustomToken", "Super special!", ApplyConstraints<string>())
      };
      HashSet<ConfigToken> CustomOptionalConfigTokens = new()
      {
        new ConfigToken("OptionalTokenWithDefaultValue", "Included even if you didn't want it, just like a hard pull on your credit score after trying to get a loan for a giant silver harpoon with which you could finally slay the vampire kraken that ate your uncle!", "OptionalValue", ApplyConstraints<string>())
      };

      Initialize(CustomRequiredConfigTokens, CustomOptionalConfigTokens, customConfig);
      output.WriteLine(string.Join('\n', ErrorList));
      Assert.Contains("OptionalTokenWithDefaultValue", customConfig);
    }

    [Fact]
    public void ToStringTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("August Burns Red","The commit author's favorite August Burns Red song.",ApplyConstraints<string>(ConstrainStringValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)"))),
        new ConfigToken("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",ApplyConstraints<string>(ConstrainStringValues("Paramond Extended Mix")))
      });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",ApplyConstraints<int>())
      });

      string configString = ToString();
      string expectedString = JObject.Parse(@"{'August Burns Red':'Spirit Breaker','Kids':'Paramond Extended Mix','ourfathers.':'4'}").ToString();
      Assert.Equal(expectedString, configString);
    }

    [Fact]
    public void GenerateEmptyConfigTest()
    {
      RequiredConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("The First of Three","A sample required token.",ApplyConstraints<string>()),
        new ConfigToken("The Second of Three","Another sample required token.",ApplyConstraints<string>())
      });
      OptionalConfigTokens.UnionWith(new HashSet<ConfigToken>()
      {
        new ConfigToken("The Third of Three","Finally, a sample optional token.",ApplyConstraints<int>())
      });

      string configString = GenerateEmptyConfig().ToString();
      string expectedString = JObject.Parse("{'The First of Three':'A sample required token.','The Second of Three':'Another sample required token.','The Third of Three':'Optional - Finally, a sample optional token.'}").ToString();
      Assert.Equal(expectedString, configString);
    }
  }
}
