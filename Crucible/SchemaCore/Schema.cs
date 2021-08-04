using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  public abstract class Schema
  {
    /// <summary>
    /// Set of tokens that must exist in the JObject set as UserConfig. Object must be added to; it cannot be replaced.
    /// </summary>
    protected HashSet<ConfigToken> RequiredConfigTokens { get; } = new();
    /// <summary>
    /// Set of tokens that optionally exist in the JObject set as UserConfig.
    /// </summary>
    protected HashSet<ConfigToken> OptionalConfigTokens { get; } = new();
    /// <summary>
    /// Contains all errors generated during validation and the associated HelpStrings of each token that was marked invalid.
    /// Should be printed to console or returned as part of an HTTP 400 response.
    /// </summary>
    public List<Error> ErrorList { get; } = new();
    /// <summary>
    /// Should be populated with the JObject that is being checked with RequiredConfigTokens and OptionalConfigTokens.
    /// </summary>
    public JObject UserConfig { get; protected set; }
    /// <summary>
    /// Indicates if any step of validation failed.
    /// </summary>
    public bool Valid { get; protected set; } = true;

    /// <summary>
    /// Checks UserConfig against RequiredConfigTokens and OptionalConfigTokens.
    /// If name and type are provided, the message "Validation for [type] [name] failed." will be added to ErrorList on validation failure.
    /// </summary>
    protected virtual void Initialize(string name = null, string type = null)
    {
      Initialize(RequiredConfigTokens, OptionalConfigTokens, UserConfig, name, type);
    }

    /// <summary>
    /// Checks UserConfig against the ConfigToken HashSets required and optional.
    /// If name and type are provided, the message "Validation for [type] [name] failed." will be added to ErrorList on validation failure.
    /// </summary>
    /// <param name="required">Collection of ConfigToken objects that must be included in UserConfig.</param>
    /// <param name="optional">Collection of ConfigToken objects that can be included in UserConfig.</param>
    protected virtual void Initialize(HashSet<ConfigToken> required, HashSet<ConfigToken> optional, string name = null, string type = null)
    {
      Initialize(required, optional, UserConfig, name, type);
    }

    /// <summary>
    /// Checks config against the ConfigToken HashSets required and optional.
    /// If name and type are provided, the message "Validation for [type] [name] failed." will be added to ErrorList on validation failure.
    /// </summary>
    /// <param name="required">Collection of ConfigToken objects that must be included in UserConfig.</param>
    /// <param name="optional">Collection of ConfigToken objects that can be included in UserConfig.</param>
    /// <param name="config">Config object to check against the ConfigToken sets.</param>
    protected virtual void Initialize(HashSet<ConfigToken> required, HashSet<ConfigToken> optional, JObject config, string name = null, string type = null)
    {
      string message = " ";
      // This option is included in case a sub-JObject of another configuration is being validated; this allows the ErrorList to indicate the exact configuration that has the issue.
      // Name is usually the token name of the sub-configuration.
      if (!(string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(type)))
      {
        message = $"Validation for {type} {name} failed.";
      }
      foreach (ConfigToken token in required)
      {
        if (!config.ContainsKey(token.TokenName))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new Error($"User config is missing required token {token.TokenName}\n{token.HelpString}"));
          }
          else
          {
            ErrorList.Add(new Error($"{type} {name} is missing required token {token.TokenName}\n{token.HelpString}"));
          }
          Valid = false;
        }
        else if (config[token.TokenName].IsNullOrEmpty())
        {
          ErrorList.Add(new Error($"Value of token {token.TokenName} is null or empty.",Severity.NullOrEmpty));
          Valid = false;
        }
        else if (!token.Validate(config[token.TokenName]))
        {
          ErrorList.AddRange(token.ErrorList);
          ErrorList.Add(new Error(token.HelpString,Severity.Info));
          Valid = false;
        }
      }
      foreach (ConfigToken token in optional)
      {
        if (!config.ContainsKey(token.TokenName) && token.DefaultValue != null)
        {
          config[token.TokenName] = token.DefaultValue; // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
        }
        else if (config.ContainsKey(token.TokenName) && !token.Validate(config[token.TokenName]))
        {
          ErrorList.AddRange(token.ErrorList);
          ErrorList.Add(new Error(token.HelpString, Severity.Info));
          Valid = false;
        }
      }
      /*

      The decision to invalidate the config due to unrecognized tokens stems from the possibility that an end user might misspell an optional token 
          when forming their configuration file or request.

      If the user includes an optional token with a typo in the token name, it will not be flagged as a missing required token,
          but it will also not have the effect the user intended from including the optional token.

      Such a problem would be very frustrating and possibly difficult to debug;
          therefore, we invalidate the config file if there are any tokens that are not accounted for in Required and Optional put together.

      */
      foreach (KeyValuePair<string, JToken> property in config)
      {
        if (!required.Select(x => x.TokenName).Contains(property.Key) && !optional.Select(x => x.TokenName).Contains(property.Key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new Error($"User config file contains unrecognized token: {property.Key}"));
          }
          else
          {
            ErrorList.Add(new Error($"{type} {name} contains unrecognized token: {property.Key}"));
          }
          Valid = false;
        }
      }
      if (!Valid && !string.IsNullOrWhiteSpace(message))
      {
        ErrorList.Add(new Error(message,Severity.Info));
      }
    }

    /// <summary>
    /// Returns the current schema controller as a stringified Json object,
    /// filling in values for required and optional config tokens if UserConfig has already been populated.
    /// </summary>
    /// <returns>String version of a JObject representation of the current schema controller.</returns>
    public override string ToString()
    {
      JObject configJson = new();
      foreach (ConfigToken token in RequiredConfigTokens)
      {
        configJson.Add(token.TokenName, UserConfig.ContainsKey(token.TokenName) ? UserConfig[token.TokenName].ToString() : "");
      }
      foreach (ConfigToken token in OptionalConfigTokens)
      {
        configJson.Add(token.TokenName, UserConfig.ContainsKey(token.TokenName) ? UserConfig[token.TokenName].ToString() : token.DefaultValue ?? "");
      }
      return configJson.ToString();
    }

    /// <summary>
    /// This method can be used to generate a new example request or configuration file with all the required and optional tokens along with their HelpStrings.
    /// </summary>
    /// <returns>A JObject file with all tokens in RequiredConfigTokens and OptionalConfigTokens.
    /// If the HelpStrings are well-written, the return value will serve as an excellent example for an end user to fill in.</returns>
    public JObject GenerateEmptyConfig()
    {
      JObject newConfig = new();
      foreach (ConfigToken token in RequiredConfigTokens)
      {
        newConfig.Add(token.TokenName, token.HelpString);
      }
      foreach (ConfigToken token in OptionalConfigTokens)
      {
        newConfig.Add(token.TokenName, "Optional - " + token.HelpString);
      }
      return newConfig;
    }

    #region JObject Constraints

    // This constraint method allows nesting of Json objects inside one another without resorting to defining additional config types.

    /// <summary>
    /// Allows nested Json property checking.
    /// </summary>
    /// <param name="requiredTokens">Array of required ConfigToken objects that will be applied to the passed Json object.</param>
    /// <returns>Function ensuring the passed JObject contains all tokens in requiredTokens and all validation functions are passed.</returns>
    protected Func<JToken, string, List<Error>> ConstrainJsonTokens(params ConfigToken[] requiredTokens)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        Initialize(requiredTokens.ToHashSet(), new HashSet<ConfigToken>(), inputJson, inputName, "Value of token");
        return ErrorList;
      }
      return InnerMethod;
    }


    /// <summary>
    /// Allows nested Json property checking.
    /// </summary>
    /// <param name="requiredTokens">Array of required ConfigToken objects that will be applied to the passed Json object.</param>
    /// <param name="optionalTokens">Array of optional ConfigToken objects that will be applied to the passed Json object.</param>
    /// <returns>Function ensuring the passed JObject contains all tokens in requiredTokens and all validation functions are passed for both token arrays.</returns>
    protected Func<JToken, string, List<Error>> ConstrainJsonTokens(ConfigToken[] requiredTokens, ConfigToken[] optionalTokens)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        Initialize(requiredTokens.ToHashSet(), optionalTokens.ToHashSet(), inputJson, inputName, "Value of token");
        return ErrorList;
      }
      return InnerMethod;
    }

    #endregion JObject Constraints
  }
}
