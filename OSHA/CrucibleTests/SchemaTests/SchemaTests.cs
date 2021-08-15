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

namespace SchemaTests
{
  [Trait("Crucible", "")]
  public class SchemaTests
  {
    private readonly ITestOutputHelper output;
    Schema TestSchema = new();
    JObject TestConfig = JObject.Parse(
        @"{
            'August Burns Red':'Spirit Breaker',
            'ourfathers.':4,
            'Kids':'Paramond Extended Mix'
          }");

    public SchemaTests(ITestOutputHelper output)
    {
      this.output = output;
    }

    /// <summary>
    /// Ensures that the empty constructor works and allows tokens to be added.
    /// </summary>
    [Fact]
    public void ConstructorTest()
    {
      Schema newSchema = new();
      newSchema.AddTokens(new HashSet<ConfigToken>()
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
        });

      Assert.Equal(3, newSchema.Count());
    }

    /// <summary>
    /// Ensures that constructing a schema with an IEnumerable and Schema.Count() both work properly.
    /// </summary>
    [Fact]
    public void ConstructorWithTokensTest()
    {
      Schema newSchema = new(new HashSet<ConfigToken>()
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
        });

      Assert.Equal(3,newSchema.Count());
    }

    /// <summary>
    /// Ensures Validate works properly.
    /// </summary>
    [Fact]
    public void ValidSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
        });
      
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that unrecognized tokens result in a fatal error.
    /// </summary>
    [Fact]
    public void UnrecognizedTokenSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
      });
      
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that missing required tokens result in a fatal error.
    /// </summary>
    [Fact]
    public void MissingRequiredTokenSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
        new ConfigToken<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",ConstrainCollectionCount<JArray>(300,300)),
        new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
      });
            
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that a null or empty token results in a fatal error.
    /// </summary>
    [Fact]
    public void NullOrEmptyTokenSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
        });

      TestConfig["Kids"] = "";
            
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when there is an unrecognized token.
    /// </summary>
    [Fact]
    public void UnrecognizedTokenWithCustomNameSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
      });

      TestSchema.Validate(TestConfig,new JObjectTranslator(),"He of Many Tokens","BloatedConfig");
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input BloatedConfig He of Many Tokens contains unrecognized token: Kids", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.Contains($"Validation for BloatedConfig He of Many Tokens failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }


    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when a required token is missing.
    /// </summary>
    [Fact]
    public void MissingRequiredTokenWithCustomNameSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
        new ConfigToken<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",ConstrainCollectionCount<JArray>(300,300)),
        new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(), "EmptyInside :(","Incomplete config");
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input Incomplete config EmptyInside :( is missing required token Lincolnshire Poacher\nThe first 300 numbers read out on the Lincolnshire Poacher station.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.Contains($"Validation for Incomplete config EmptyInside :( failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }

    /// <summary>
    /// Ensures that AddToken works.
    /// </summary>
    [Fact]
    public void ValidSchemaAddTokenTest()
    {
      TestSchema.AddToken(new ConfigToken<string>("ExtraCustomToken", "Super special!"));

      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that GenerateEmptyConfig() works as expected and generates a set of token names and their respective HelpStrings.
    /// </summary>
    [Fact]
    public void GenerateEmptyConfigTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("The First of Three","A sample required token."),
        new ConfigToken<string>("The Second of Three","Another sample required token."),
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",false)
      });

      string configString = TestSchema.GenerateEmptyConfig().ToString();
      string expectedString = JObject.Parse("{'The First of Three':'A sample required token.','The Second of Three':'Another sample required token.','The Third of Three':'Optional - Finally, a sample optional token.'}").ToString();
      Assert.Equal(expectedString, configString);
    }

    /// <summary>
    /// Ensures that AddToken throws an argument exception if adding a duplicate token is attempted.
    /// </summary>
    [Fact]
    public void AddToken_ThrowsWhenDuplicate()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("The First of Three","A sample required token."),
        new ConfigToken<string>("The Second of Three","Another sample required token."),
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",false)
      });
      Assert.Throws<ArgumentException>(() => TestSchema.AddToken(new ConfigToken<string>("The First of Three", "Defective, fake, insubordinate, and churlish.")));
    }

    /// <summary>
    /// Ensures that AddTokens throws an argument exception if there is a duplicate token in the set being added.
    /// </summary>
    [Fact]
    public void AddTokens_ThrowsWhenDuplicate()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("The First of Three","A sample required token."),
        new ConfigToken<string>("The Second of Three","Another sample required token."),
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",false)
      });
      Assert.Throws<ArgumentException>(() => TestSchema.AddTokens(new ConfigToken[] { new ConfigToken<string>("A brand new token!", "Completely original!"),
                                                                                      new ConfigToken<string>("The First of Three", "Once again thinks it's the most important because it was added first.") }));
    }

    /// <summary>
    /// Ensures that the constructor throws an exception when there are duplicate tokens in the passed set of tokens.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWithDuplicateTokensTest()
    {
      Assert.Throws<ArgumentException>(() => new Schema(new ConfigToken[]
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",AllowValues("Paramond Extended Mix")),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",false)
        }));
    }
  }
}
