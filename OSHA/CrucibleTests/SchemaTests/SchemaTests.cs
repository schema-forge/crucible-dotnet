﻿using System;
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
    readonly Schema TestSchema = new();
    readonly JObject TestConfig = JObject.Parse(
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
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });
      
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that unrecognized tokens result in a fatal error.
    /// </summary>
    [Fact]
    public void UnrecognizedTokenWithAllowUnrecognizedSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(),allowUnrecognized: true);
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that missing required tokens result in a fatal error.
    /// </summary>
    [Fact]
    public void MissingRequiredTokenSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new ConfigToken<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(300,300) }),
        new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer."),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      TestConfig["Kids"] = "";
            
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that a null or empty token results in a fatal error.
    /// </summary>
    [Fact]
    public void NullOrEmptyTokenSchemaWithNullAllowedTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
        {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",allowNull:true),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      TestConfig["Kids"] = "";

      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when there is an unrecognized token.
    /// </summary>
    [Fact]
    public void UnrecognizedTokenWithCustomNameSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig,new JObjectTranslator(),"He of Many Tokens","BloatedConfig");
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input BloatedConfig He of Many Tokens contains unrecognized token: Kids", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.Contains($"Validation for BloatedConfig He of Many Tokens failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }

    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when there is an unrecognized token.
    /// </summary>
    [Fact]
    public void UnrecognizedTokenWithCustomNameAllowUnrecognizedSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
          new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(), "He of Many Tokens", "BloatedConfig",true);
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input BloatedConfig He of Many Tokens contains unrecognized token: Kids", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.DoesNotContain($"Validation for BloatedConfig He of Many Tokens failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }


    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when a required token is missing.
    /// </summary>
    [Fact]
    public void MissingRequiredTokenWithCustomNameSchemaTest()
    {
      TestSchema.AddTokens(new HashSet<ConfigToken>()
      {
        new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new ConfigToken<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(300,300) }),
        new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",required: false)
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
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",required: false)
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
        new ConfigToken<int>("The Third of Three","Finally, a sample optional token.",required: false)
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
        new ConfigToken<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new ConfigToken<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new ConfigToken<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        }));
    }

    /// <summary>
    /// Ensures that, when a default value is provided and not included in the config being validated, it is successfully inserted.
    /// </summary>
    [Fact]
    public void DefaultValueSuccessfullyInserts()
    {
      JObject testConfig = JObject.Parse(
        @"{
          'MoviesWatched':37,
          'MoviesEnjoyed':'36',
          'LeastEnjoyedMovie':'Avatar: The Last Airbender, courtesy of famed director M. Night Shyamalan.'
        }");

      Schema movieSchema = new (new ConfigToken[]
      {
        new ConfigToken<int>("MoviesWatched","Indicates how many movies the user has watched simultaneously in the past 24 hours."),
        new ConfigToken<int>("MoviesEnjoyed","Indicates the number of simultaneous movies the user enjoyed."),
        new ConfigToken<string>("LeastEnjoyedMovie","Indicates that, even in the overwhelming cacophany and sense-overload of thirty seven simultaneous movies, the live-action Avatar film still stands out as the worst of them all."),
        new ConfigToken<bool>("There is hope for the movie industry", "Indicates whether or not the user believes that good movies are still being made.",true)
      });

      movieSchema.Validate(testConfig, new JObjectTranslator());

      Assert.True(testConfig.ContainsKey("There is hope for the movie industry") && bool.Parse(testConfig["There is hope for the movie industry"].ToString()));
    }

    /// <summary>
    /// Ensures that, when a default value is provided and is included in the provided config, the default value does not override the original value or throw an exception from being added.
    /// </summary>
    [Fact]
    public void DefaultValueSuccessfullyDoesNotInsert()
    {
      JObject testConfig = JObject.Parse(
        @"{
          'MoviesWatched':37,
          'MoviesEnjoyed':'36',
          'LeastEnjoyedMovie':'Avatar: The Last Airbender, courtesy of famed director M. Night Shyamalan.',
          'There is hope for the movie industry':false
        }");

      Schema movieSchema = new(new ConfigToken[]
      {
        new ConfigToken<int>("MoviesWatched","Indicates how many movies the user has watched simultaneously in the past 24 hours."),
        new ConfigToken<int>("MoviesEnjoyed","Indicates the number of simultaneous movies the user enjoyed."),
        new ConfigToken<string>("LeastEnjoyedMovie","Indicates that, even in the overwhelming cacophany and sense-overload of thirty seven simultaneous movies, the live-action Avatar film still stands out as the worst of them all."),
        new ConfigToken<bool>("There is hope for the movie industry", "Indicates whether or not the user believes that good movies are still being made.",true)
      });

      movieSchema.Validate(testConfig, new JObjectTranslator());

      Assert.True(testConfig.ContainsKey("There is hope for the movie industry") && !bool.Parse(testConfig["There is hope for the movie industry"].ToString()));
    }
  }
}
