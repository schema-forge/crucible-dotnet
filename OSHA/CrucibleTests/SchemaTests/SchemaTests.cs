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
    /// Ensures that the empty constructor works and allows <see cref="Field"/>s to be added.
    /// </summary>
    [Fact]
    public void ConstructorTest()
    {
      Schema newSchema = new();
      newSchema.AddFields(new HashSet<Field>()
        {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      Assert.Equal(3, newSchema.Count());
    }

    /// <summary>
    /// Ensures that constructing a schema with an IEnumerable and Schema.Count() both work properly.
    /// </summary>
    [Fact]
    public void ConstructorWithFieldsTest()
    {
      Schema newSchema = new(new HashSet<Field>()
        {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      Assert.Equal(3,newSchema.Count());
    }

    /// <summary>
    /// Ensures Validate works properly.
    /// </summary>
    [Fact]
    public void ValidSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
        {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });
      
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that unrecognized <see cref="Field"/>s result in a fatal error.
    /// </summary>
    [Fact]
    public void UnrecognizedFieldSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });
      
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that unrecognized <see cref="Field"/>s do not result in a fatal error when permitted.
    /// </summary>
    [Fact]
    public void UnrecognizedFieldWithAllowUnrecognizedSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(),allowUnrecognized: true);
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that missing required <see cref="Field"/>s result in a fatal error.
    /// </summary>
    [Fact]
    public void MissingRequiredFieldSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
        new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new Field<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(300,300) }),
        new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });
            
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that a null or empty <see cref="Field"/> value results in a fatal error.
    /// </summary>
    [Fact]
    public void NullOrEmptyFieldSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
        {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer."),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      TestConfig["Kids"] = "";
            
      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.True(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that a null or empty <see cref="Field"/> value does not result in a fatal error when permitted.
    /// </summary>
    [Fact]
    public void NullOrEmptyFieldSchemaWithNullAllowedTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
        {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",allowNull:true),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
        });

      TestConfig["Kids"] = "";

      TestSchema.Validate(TestConfig, new JObjectTranslator());
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when there is an unrecognized <see cref="Field"/>.
    /// </summary>
    [Fact]
    public void UnrecognizedFieldWithCustomNameSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig,new JObjectTranslator(),"He of Many Fields");
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input He of Many Fields contains unrecognized field: Kids", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.Contains($"Validation for He of Many Fields failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }

    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when there is an unrecognized <see cref="Field"/>.
    /// </summary>
    [Fact]
    public void UnrecognizedFieldWithCustomNameAllowUnrecognizedSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
          new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
          new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(), "He of Many Fields", true);
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input He of Many Fields contains unrecognized field: Kids", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.DoesNotContain($"Validation for He of Many Fields failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }


    /// <summary>
    /// Ensures that the type and name arguments of Validate work properly when a required <see cref="Field"/> is missing.
    /// </summary>
    [Fact]
    public void MissingRequiredFieldWithCustomNameSchemaTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
        new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new Field<JArray>("Lincolnshire Poacher","The first 300 numbers read out on the Lincolnshire Poacher station.",new Constraint<JArray>[] { ConstrainCollectionCount<JArray>(300,300) }),
        new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
      });

      TestSchema.Validate(TestConfig, new JObjectTranslator(), "EmptyInside :(");
      output.WriteLine(string.Join('\n', TestSchema.ErrorList));
      Assert.Contains($"Input EmptyInside :( is missing required field Lincolnshire Poacher\nThe first 300 numbers read out on the Lincolnshire Poacher station.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
      Assert.Contains($"Validation for EmptyInside :( failed.", TestSchema.ErrorList.Select(x => x.ErrorMessage));
    }

    /// <summary>
    /// Ensures that AddField works.
    /// </summary>
    [Fact]
    public void ValidSchemaAddFieldTest()
    {
      TestSchema.AddField(new Field<string>("ExtraCustomField", "Super special!"));

      Assert.False(TestSchema.ErrorList.AnyFatal());
    }

    /// <summary>
    /// Ensures that GenerateEmptyConfig() works as expected and generates a set of <see cref="Field"/> names and their respective <see cref="Field.Description"/>s.
    /// </summary>
    [Fact]
    public void GenerateEmptyConfigTest()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
        new Field<string>("The First of Three","A sample required field."),
        new Field<string>("The Second of Three","Another sample required field."),
        new Field<int>("The Third of Three","Finally, a sample optional field.",required: false)
      });

      string configString = TestSchema.GenerateEmptyJson().ToString();
      string expectedString = JObject.Parse("{'The First of Three':'A sample required field.','The Second of Three':'Another sample required field.','The Third of Three':'Optional - Finally, a sample optional field.'}").ToString();
      Assert.Equal(expectedString, configString);
    }

    /// <summary>
    /// Ensures that <see cref="Schema.AddField(Field)"/> throws an argument exception if adding a duplicate <see cref="Field"/> is attempted.
    /// </summary>
    [Fact]
    public void AddField_ThrowsWhenDuplicate()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
        new Field<string>("The First of Three","A sample required field."),
        new Field<string>("The Second of Three","Another sample required field."),
        new Field<int>("The Third of Three","Finally, a sample optional field.",required: false)
      });
      Assert.Throws<ArgumentException>(() => TestSchema.AddField(new Field<string>("The First of Three", "Defective, fake, insubordinate, and churlish.")));
    }

    /// <summary>
    /// Ensures that <see cref="Schema.AddFields(IEnumerable{Field})"/> throws an argument exception if there is a duplicate <see cref="Field"/> in the set being added.
    /// </summary>
    [Fact]
    public void AddFields_ThrowsWhenDuplicate()
    {
      TestSchema.AddFields(new HashSet<Field>()
      {
        new Field<string>("The First of Three","A sample required field."),
        new Field<string>("The Second of Three","Another sample required field."),
        new Field<int>("The Third of Three","Finally, a sample optional field.",required: false)
      });
      Assert.Throws<ArgumentException>(() => TestSchema.AddFields(new Field[] { new Field<string>("A brand new field!", "Completely original!"),
                                                                                      new Field<string>("The First of Three", "Once again thinks it's the most important because it was added first.") }));
    }

    /// <summary>
    /// Ensures that the constructor throws an exception when there are duplicate <see cref="Field"/>s in the passed set of <see cref="Field"/>s.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWithDuplicateFieldsTest()
    {
      Assert.Throws<ArgumentException>(() => new Schema(new Field[]
        {
        new Field<string>("August Burns Red","The commit author's favorite August Burns Red song.",new Constraint<string>[] { AllowValues("Spirit Breaker","Provision","The Wake", "Empire (Midi)") }),
        new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new Field<string>("Kids","The best remix of the MGMT song 'Kids'. There is only one correct answer.",new Constraint<string>[] { AllowValues("Paramond Extended Mix") }),
        new Field<int>("ourfathers.","The number of songs by ourfathers. the commit author has given a 5/5 rating in his music library.",required: false)
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

      Schema movieSchema = new (new Field[]
      {
        new Field<int>("MoviesWatched","Indicates how many movies the user has watched simultaneously in the past 24 hours."),
        new Field<int>("MoviesEnjoyed","Indicates the number of simultaneous movies the user enjoyed."),
        new Field<string>("LeastEnjoyedMovie","Indicates that, even in the overwhelming cacophany and sense-overload of thirty seven simultaneous movies, the live-action Avatar film still stands out as the worst of them all."),
        new Field<bool>("There is hope for the movie industry", "Indicates whether or not the user believes that good movies are still being made.",true)
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

      Schema movieSchema = new(new Field[]
      {
        new Field<int>("MoviesWatched","Indicates how many movies the user has watched simultaneously in the past 24 hours."),
        new Field<int>("MoviesEnjoyed","Indicates the number of simultaneous movies the user enjoyed."),
        new Field<string>("LeastEnjoyedMovie","Indicates that, even in the overwhelming cacophany and sense-overload of thirty seven simultaneous movies, the live-action Avatar film still stands out as the worst of them all."),
        new Field<bool>("There is hope for the movie industry", "Indicates whether or not the user believes that good movies are still being made.",true)
      });

      movieSchema.Validate(testConfig, new JObjectTranslator());

      Assert.True(testConfig.ContainsKey("There is hope for the movie industry") && !bool.Parse(testConfig["There is hope for the movie industry"].ToString()));
    }
  }
}
