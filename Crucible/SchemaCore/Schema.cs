using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using static SchemaForge.Crucible.Constraints;

namespace SchemaForge.Crucible
{
  public interface ISchemaTranslator<TCollectionType, TValueType>
  {
    public (bool,TCastType) TryCastToken<TCastType>(TCollectionType collection, string valueName);
    public bool TokenIsNullOrEmpty(TCollectionType collection, string valueName);
    public TCollectionType InsertToken(TCollectionType collection, string valueName, string newValue);
    public bool CollectionContains(TCollectionType collection, string valueName);
    public string CollectionValueToString(TCollectionType collection, string valueName);
    public List<string> GetCollectionKeys(TCollectionType collection);
  }
  class JsonTranslator : ISchemaTranslator<JObject,JToken>
  {
    public (bool, TCastType) TryCastToken<TCastType>(JObject collection, string valueName)
    {
      try
      {
        JToken token = collection[valueName];
        TCastType result = token.Value<TCastType>();
        return (true, result);
      }
      catch
      {
        return (false, default);
      }
    }
    public bool TokenIsNullOrEmpty(JObject collection, string valueName) => collection[valueName].IsNullOrEmpty();
    public JObject InsertToken(JObject collection, string valueName, string newValue)
    {
      collection.Add(valueName, newValue);
      return collection;
    }
    public bool CollectionContains(JObject collection, string valueName) => collection.ContainsKey(valueName);
    public List<string> GetCollectionKeys(JObject collection) => collection.Properties().Select(x => x.Name).ToList();
    public string CollectionValueToString(JObject collection, string valueName) => collection[valueName].ToString();
  }
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
    /// Checks config against the ConfigToken HashSets required and optional.
    /// If name and type are provided, the message "Validation for [type] [name] failed." will be added to ErrorList on validation failure.
    /// </summary>
    /// <param name="config">Config object to check using the ConfigToken rules set in ConfigTokens.</param>
    public virtual List<Error> Validate<TCollectionType,TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType,TValueType> translator, string name = null, string type = null)
    {
      string message = " ";
      // This option is included in case a sub-JObject of another configuration is being validated; this allows the ErrorList to indicate the exact configuration that has the issue.
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
              ErrorList.Add(new Error($"Input json is missing required token {token.TokenName}\n{token.HelpString}"));
            }
            else
            {
              ErrorList.Add(new Error($"Input {type} {name} is missing required token {token.TokenName}\n{token.HelpString}"));
            }
          }
          else if(!token.DefaultValue.IsNullOrEmpty())
          {
            collection = translator.InsertToken(collection, token.TokenName, token.DefaultValue); // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
          }
        }
        else if (translator.TokenIsNullOrEmpty(collection,token.TokenName))
        {
          ErrorList.Add(new Error($"Value of token {token.TokenName} is null or empty.",Severity.Null));
        }
        else if (!token.Validate(config[token.TokenName]))
        {
          ErrorList.AddRange(token.ErrorList);
          ErrorList.Add(new Error(token.HelpString,Severity.Info));
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
            ErrorList.Add(new Error($"Input json file contains unrecognized token: {key}"));
          }
          else
          {
            ErrorList.Add(new Error($"Input {type} {name} contains unrecognized token: {key}"));
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
        schemaJson.Add(token.TokenName, token.Constraints.ToJToken());
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
          newConfig.Add(token.TokenName, token.HelpString);
        }
        else
        {
          newConfig.Add(token.TokenName, "Optional - " + token.HelpString);
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
