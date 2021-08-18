using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaForge.Crucible
{
  public class Schema
  {
    /*
    
    "Integer":
    {
      "Type": must be integer
      "Domain": [[56], [20, 25]]
    }

     */



    /// <summary>
    /// Set of token rules to use when a Json is passed to Validate().
    /// </summary>
    private readonly HashSet<ConfigToken> ConfigTokens = new();
    /// <summary>
    /// Contains all errors generated during validation and the associated HelpStrings of each token that was marked invalid.
    /// Should be printed to console or returned as part of an HTTP 400 response.
    /// </summary>
    public List<Error> ErrorList { get; } = new();

    public Schema()
    {
      
    }

    public Schema(IEnumerable<ConfigToken> tokens)
    {
      AddTokens(tokens);
    }

    public void AddToken(ConfigToken token)
    {
      if (!ConfigTokens.Add(token))
      {
        throw new ArgumentException($"ConfigToken set already contains a ConfigToken named {token.TokenName}");
      }
    }

    public void AddTokens(IEnumerable<ConfigToken> tokens)
    {
      foreach (ConfigToken token in tokens)
      {
        if (!ConfigTokens.Add(token))
        {
          throw new ArgumentException($"ConfigToken set already contains a ConfigToken named {token.TokenName}");
        }
      }
    }

    public int Count() => ConfigTokens.Count;

    /// <summary>
    /// Checks config against the ConfigToken HashSet.
    /// If name and type are provided, the message
    /// "Validation for <paramref name="type"/> <paramref name="name"/> failed."
    /// will be added to ErrorList on validation failure.
    /// </summary>
    /// <param name="config">Config object to check using the ConfigToken rules set in ConfigTokens.</param>
    /// <param name="name">If name and type are provided, the message 
    /// "Validation for <paramref name="type"/> <paramref name="name"/> failed."
    /// will be added to ErrorList on validation failure.</param>
    /// <param name="type">If name and type are provided, the message 
    /// "Validation for <paramref name="type"/> <paramref name="name"/> failed."
    /// will be added to ErrorList on validation failure.</param>
    /// <param name="allowUnrecognized">If false, unrecognized tokens will raise
    /// a <see cref="Severity.Fatal"/> error. If true, unrecognized tokens will
    /// raise a <see cref="Severity.Info"/> error.</param>
    public virtual List<Error> Validate<TCollectionType,TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType,TValueType> translator, string name = null, string type = null, bool allowUnrecognized = false)
    {
      string message = " ";
      // This option is included in case a sub-configuration is being validated; this allows the ErrorList to indicate the exact configuration that has the issue.
      // Name is usually the token name of the sub-configuration.
      if (!(string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(type)))
      {
        message = $"Validation for {type} {name} failed.";
      }
      foreach (ConfigToken token in ConfigTokens)
      {
        if (!translator.CollectionContains(collection, token.TokenName))
        {
          if(token.Required)
          {
            if (message.IsNullOrEmpty())
            {
              ErrorList.Add(new Error($"Input collection is missing required token {token.TokenName}\n{token.Description}"));
            }
            else
            {
              ErrorList.Add(new Error($"Input {type} {name} is missing required token {token.TokenName}\n{token.Description}"));
            }
          }
          else if(token.ContainsDefaultValue)
          {
            token.InsertDefaultValue(collection, translator); // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
          }
          else
          {
            ErrorList.Add(new Error($"Input collection is missing optional token {token.TokenName}",Severity.Info));
          }
        }
        else if (!token.Validate(collection,translator))
        {
          ErrorList.AddRange(token.ErrorList);
          ErrorList.Add(new Error(token.Description,Severity.Info));
        }
      }
      /*

      The decision to invalidate the config due to unrecognized tokens stems from the possibility that an end user might misspell an optional token 
          when forming their configuration file or request.

      If the user includes an optional token with a typo in the token name, it will not be flagged as a missing required token,
          but it will also not have the effect the user intended from including the optional token.

      Such a problem would be very frustrating and possibly difficult to debug;
          therefore, we invalidate the config file if there are any tokens that are not accounted for in ConfigTokens.

      */
      List<string> collectionKeys = translator.GetCollectionKeys(collection);
      foreach (string key in collectionKeys)
      {
        if (!ConfigTokens.Select(x => x.TokenName).Contains(key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new Error($"Input json file contains unrecognized token: {key}",allowUnrecognized?Severity.Info:Severity.Fatal));
          }
          else
          {
            ErrorList.Add(new Error($"Input {type} {name} contains unrecognized token: {key}", allowUnrecognized?Severity.Info:Severity.Fatal));
          }
        }
      }
      if (ErrorList.AnyFatal() && !string.IsNullOrWhiteSpace(message))
      {
        ErrorList.Add(new Error(message,Severity.Info));
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
    /// <returns>String version of a JObject representation of the current schema controller.</returns>
    public override string ToString()
    {
      JObject schemaJson = new();
      foreach(ConfigToken token in ConfigTokens)
      {
        schemaJson.Add(token.TokenName, token.JsonConstraint);
      }
      return schemaJson.ToString();
    }

    /*
    
    -----------------------------------------
    END CONSTRUCTION ZONE
    
    */

    /// <summary>
    /// This method can be used to generate a new example request or configuration file with all the required and optional tokens along with their HelpStrings.
    /// </summary>
    /// <returns>A JObject file with all tokens in RequiredConfigTokens and OptionalConfigTokens.
    /// If the HelpStrings are well-written, the return value will serve as an excellent example for an end user to fill in.</returns>
    public JObject GenerateEmptyConfig()
    {
      JObject newConfig = new();
      foreach (ConfigToken token in ConfigTokens)
      {
        if(token.Required)
        {
          newConfig.Add(token.TokenName, token.Description);
        }
        else
        {
          newConfig.Add(token.TokenName, "Optional - " + token.Description);
        }
      }
      return newConfig;
    }

    public Schema Clone()
    {
      Schema newSchema = new();
      foreach(ConfigToken token in ConfigTokens)
      {
        newSchema.AddToken(token);
      }
      return newSchema;
    }
  }
}
