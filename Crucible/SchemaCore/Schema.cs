using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using static SchemaForge.Crucible.Constraints;

/*

TODO:

Expose iteration over fields and more fine tuning functions, for the grim darkness of the future when Schemas are passed from program to program.
 
*/

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Schema objects contain a set of <see cref="Field"/>s that define each value that should be
  /// contained in an object passed to its Validate method.
  /// </summary>
  public class Schema
  {
    /// <summary>
    /// Set of <see cref="Field"/>s to use when a collection is passed to
    /// <see cref="Validate{TCollectionType}(TCollectionType, ISchemaTranslator{TCollectionType}, string, bool)"/>.
    /// </summary>
    private readonly Dictionary<string, Field> Fields = new Dictionary<string, Field>();
    /// <summary>
    /// Contains all errors generated during validation and the associated
    /// <see cref="Field.Description"/> of each <see cref="Field"/> value that was marked invalid.
    /// </summary>
    public List<SchemaError> ErrorList { get; } = new List<SchemaError>();

    /// <summary>
    /// Constructs an empty <see cref="Schema"/> with no <see cref="Field"/> objects.
    /// </summary>
    public Schema()
    {
      
    }

    /// <summary>
    /// Instantiates a <see cref="Schema"/> object with a set of <see cref="Field"/> objects to use.
    /// </summary>
    /// <param name="fields"><see cref="Field"/>s to add to the <see cref="Field"/> set.</param>
    public Schema(params Field[] fields)
    {
      AddFields(fields);
    }

    /// <summary>
    /// Instantiates a <see cref="Schema"/> object with a set of <see cref="Field"/> objects to use.
    /// </summary>
    /// <param name="fields"><see cref="Field"/>s to add to the <see cref="Field"/> set.</param>
    public Schema(IEnumerable<Field> fields)
    {
      AddFields(fields);
    }

    /// <summary>
    /// Adds a <see cref="Field"/> to the <see cref="Schema"/> object's set of <see cref="Field"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if the Schema already contains a <see cref="Field"/> with the same name.</exception>
    /// <param name="field"><see cref="Field"/> to add. The name must be different from all <see cref="Field"/> objects currently in the Schema.</param>
    public void AddField(Field field)
    {
      if (Fields.ContainsKey(field.FieldName))
      {
        throw new ArgumentException($"Field set already contains a Field named {field.FieldName}");
      }
      Fields.Add(field.FieldName, field);
    }

    /// <summary>
    /// Adds a set of <see cref="Field"/>s to the <see cref="Schema"/> object's set of <see cref="Field"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if the Schema already contains a <see cref="Field"/> with the same name as one or more of the <see cref="Field"/>s in <paramref name="fields"/>.</exception>
    /// <param name="fields">Collection of <see cref="Field"/>s to add. There must be no <see cref="Field"/>s in the set that have a name identical to something already in the Schema's <see cref="Field"/> set.</param>
    public void AddFields(IEnumerable<Field> fields)
    {
      foreach (Field field in fields)
      {
        if (Fields.ContainsKey(field.FieldName))
        {
          throw new ArgumentException($"Field set already contains a Field named {field.FieldName}");
        }
        Fields.Add(field.FieldName, field);
      }
    }

    /// <summary>
    /// Removes one <see cref="Field"/> from the <see cref="Schema"/> object's set of <see cref="Field"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a <see cref="Field"/> not already in the set.</exception>
    /// <param name="fieldName">Name of the <see cref="Field"/> to remove; corresponds to <see cref="Field.FieldName"/>.</param>
    public void RemoveField(string fieldName)
    {
      if(Fields.ContainsKey(fieldName))
      {
        Fields.Remove(fieldName);
      }
      else
      {
        throw new ArgumentException($"Attempted to remove field {fieldName} from Schema, but Schema did not contain {fieldName}");
      }
    }

    /// <summary>
    /// Removes all <see cref="Field"/>s from the <see cref="Schema"/> object's set of <see cref="Field"/> objects
    /// where <see cref="Field.FieldName"/> is found in <paramref name="fieldNames"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a <see cref="Field"/> not already in the set.</exception>
    /// <param name="fieldNames">List of <see cref="Field"/> names to remove; corresponds to <see cref="Field.FieldName"/>.</param>
    public void RemoveFields(IEnumerable<string> fieldNames)
    {
      foreach (string fieldName in fieldNames)
      {
        if (Fields.ContainsKey(fieldName))
        {
          Fields.Remove(fieldName);
        }
        else
        {
          throw new ArgumentException($"Attempted to remove field {fieldName} from Schema, but Schema did not contain {fieldName}");
        }
      }
    }

    /// <summary>
    /// Removes all <see cref="Field"/>s from the <see cref="Schema"/> object's set of <see cref="Field"/> objects
    /// where <see cref="Field.FieldName"/> is found in <paramref name="fieldNames"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a <see cref="Field"/> not already in the set.</exception>
    /// <param name="fieldNames">List of <see cref="Field"/> names to remove; corresponds to <see cref="Field.FieldName"/>.</param>
    public void RemoveFields(params string[] fieldNames)
    {
      foreach (string fieldName in fieldNames)
      {
        if (Fields.ContainsKey(fieldName))
        {
          Fields.Remove(fieldName);
        }
        else
        {
          throw new ArgumentException($"Attempted to remove field {fieldName} from Schema, but Schema did not contain {fieldName}");
        }
      }
    }

    /// <summary>
    /// Returns the number of <see cref="Field"/>s contained in the <see cref="Schema"/>.
    /// </summary>
    /// <returns>The number of <see cref="Field"/>s contained in the <see cref="Schema"/>.</returns>
    public int Count() => Fields.Count;

    /// <summary>
    /// Checks <paramref name="collection"/> using the set of <see cref="Fields"/>.
    /// If name and type are provided, the message
    /// "Validation for <paramref name="name"/> failed."
    /// will be added to <see cref="ErrorList"/> on validation failure.
    /// </summary>
    /// <param name="collection">Collection object to check using the <see cref="Field"/>
    /// rules set in <see cref="Fields"/>.</param>
    /// <param name="translator"><see cref="ISchemaTranslator{TCollectionType}"/>
    /// used to interpret the collection for the <see cref="Schema"/> and extract values.</param>
    /// <param name="name">If name and type are provided, the message 
    /// "Validation for <paramref name="name"/> failed."
    /// will be added to ErrorList on validation failure.</param>
    /// <param name="allowUnrecognized">If false, unrecognized <see cref="Field"/>s (that is,
    /// <see cref="Field"/>s present in the object being validated but not in the <see cref="Schema"/>) will raise
    /// a <see cref="Severity.Fatal"/> error. If true, unrecognized <see cref="Field"/>s will
    /// raise a <see cref="Severity.Info"/> error.</param>
    public virtual List<SchemaError> Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator, string name = null, bool allowUnrecognized = false)
    {
      string message = " ";
      // This option is included in case a sub-collection is being validated; this
      //   allows the ErrorList to indicate the exact collection that has the issue.
      if (!string.IsNullOrWhiteSpace(name))
      {
        message = $"Validation for {name} failed.";
      }
      foreach (Field field in Fields.Values)
      {
        if (!translator.CollectionContains(collection, field.FieldName))
        {
          if(field.Required)
          {
            if (message.IsNullOrEmpty())
            {
              ErrorList.Add(new SchemaError($"Input collection is missing required field {field.FieldName}\n{field.Description}"));
            }
            else
            {
              ErrorList.Add(new SchemaError($"Input {name} is missing required field {field.FieldName}\n{field.Description}"));
            }
          }
          else if(field.DefaultValue.Exists())
          {
            collection = field.InsertDefaultValue(collection, translator); // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
          }
          else
          {
            ErrorList.Add(new SchemaError($"Input collection is missing optional field {field.FieldName}",Severity.Info));
          }
        }
        else if (!field.Validate(collection,translator))
        {
          ErrorList.AddRange(field.ErrorList);
          ErrorList.Add(new SchemaError(field.Description,Severity.Info));
        }
      }
      /*

      The decision to invalidate the config due to unrecognized fields stems
        from the possibility that an end user might misspell an optional field
        when forming their configuration file or request.

      If the user includes an optional field with a typo in the field name, it
        will not be flagged as a missing required field, but it will also not
        have the effect the user intended from including the optional field.

      Such a problem would be very frustrating and possibly difficult to debug;
        therefore, we invalidate the collection if there are any tokens that
        are not accounted for in Fields by default.

      */
      List<string> collectionKeys = translator.GetCollectionKeys(collection);
      HashSet<string> fieldNames = Fields.Select(x => x.Value.FieldName).ToHashSet();
      foreach (string key in collectionKeys)
      {
        if (!fieldNames.Contains(key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new SchemaError($"Input object contains unrecognized field: {key}",allowUnrecognized?Severity.Info:Severity.Fatal));
          }
          else
          {
            ErrorList.Add(new SchemaError($"Input {name} contains unrecognized field: {key}", allowUnrecognized?Severity.Info:Severity.Fatal));
          }
        }
      }
      if (ErrorList.AnyFatal() && !string.IsNullOrWhiteSpace(message))
      {
        ErrorList.Add(new SchemaError(message,Severity.Info));
      }
      return ErrorList;
    }

    /*
    
    CONSRUCTION ZONE AHEAD
    DO NOT ENTER
    !DANGER!
    -----------------------------------------

    */

    /// <summary>
    /// Returns the current schema as a stringified Json object.
    /// </summary>
    /// <returns>String version of a <see cref="JObject"/> representation of the current schema controller.</returns>
    public override string ToString()
    {
      JObject schemaJson = new JObject();
      foreach(Field field in Fields.Values)
      {
        schemaJson.Add(field.FieldName, field.JsonConstraint);
      }
      return schemaJson.ToString();
    }

    /*
    
    -----------------------------------------
    END CONSTRUCTION ZONE
    
    */

    /// <summary>
    /// This method can be used to generate a new example request or configuration file with all the required and optional <see cref="Field"/>s along with their <see cref="Field.Description"/>.
    /// </summary>
    /// <returns>A <see cref="JObject"/> with all <see cref="Field"/>s
    /// from <see cref="Fields"/>, using <see cref="Field.FieldName"/>
    /// as the name and <see cref="Field.Description"/> as the property value.
    /// If the Descriptions are well-written, the return value will serve as an
    /// excellent example for an end user to fill in.</returns>
    public JObject GenerateEmptyJson()
    {
      JObject newConfig = new JObject();
      foreach (Field field in Fields.Values)
      {
        if(field.Required)
        {
          newConfig.Add(field.FieldName, field.Description);
        }
        else
        {
          newConfig.Add(field.FieldName, "Optional - " + field.Description);
        }
      }
      return newConfig;
    }

    /// <summary>
    /// Returns a new <see cref="Schema"/> that is a clone of the current <see cref="Schema"/>.
    /// </summary>
    /// <returns>A new <see cref="Schema"/> that is a clone of this <see cref="Schema"/>.</returns>
    public Schema Clone()
    {
      Schema newSchema = new Schema();
      foreach(Field token in Fields.Values)
      {
        newSchema.AddField(token);
      }
      return newSchema;
    }
  }
}
