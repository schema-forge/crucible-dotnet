using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  public class Schema
  {
    /// <summary>
    /// Set of token rules to use when a Json is passed to Validate().
    /// </summary>
    private HashSet<ConfigToken> ConfigTokens = new();
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
        throw new ArgumentException("Input IEnumerable<ConfigToken> contains duplicate tokens.");
      }
    }

    public void AddTokens(IEnumerable<ConfigToken> tokens)
    {
      foreach (ConfigToken token in tokens)
      {
        if (!ConfigTokens.Add(token))
        {
          throw new ArgumentException("Input IEnumerable<ConfigToken> contains duplicate tokens.");
        }
      }
    }

    public int Count() => ConfigTokens.Count;

    /// <summary>
    /// Checks config against the ConfigToken HashSets required and optional.
    /// If name and type are provided, the message "Validation for [type] [name] failed." will be added to ErrorList on validation failure.
    /// </summary>
    /// <param name="config">Config object to check using the ConfigToken rules set in ConfigTokens.</param>
    public virtual List<Error> Validate(JObject config, string name = null, string type = null)
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
        if (!config.ContainsKey(token.TokenName))
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
            config[token.TokenName] = token.DefaultValue; // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
          }
        }
        else if (config[token.TokenName].IsNullOrEmpty())
        {
          ErrorList.Add(new Error($"Value of token {token.TokenName} is null or empty.",Severity.NullOrEmpty));
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
      foreach (KeyValuePair<string, JToken> property in config)
      {
        if (!ConfigTokens.Select(x => x.TokenName).Contains(property.Key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new Error($"Input json file contains unrecognized token: {property.Key}"));
          }
          else
          {
            ErrorList.Add(new Error($"Input {type} {name} contains unrecognized token: {property.Key}"));
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

    ///// <summary>
    ///// Returns the current schema controller as a stringified Json object,
    ///// filling in values for required and optional config tokens if UserConfig has already been populated.
    ///// </summary>
    ///// <returns>String version of a JObject representation of the current schema controller.</returns>
    //public override string ToString()
    //{
    //  JObject configJson = new();
    //  foreach (ConfigToken token in RequiredConfigTokens)
    //  {
    //    configJson.Add(token.TokenName, UserConfig.ContainsKey(token.TokenName) ? UserConfig[token.TokenName].ToString() : "");
    //  }
    //  foreach (ConfigToken token in OptionalConfigTokens)
    //  {
    //    configJson.Add(token.TokenName, UserConfig.ContainsKey(token.TokenName) ? UserConfig[token.TokenName].ToString() : token.DefaultValue ?? "");
    //  }
    //  return configJson.ToString();
    //}

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
  }
}
