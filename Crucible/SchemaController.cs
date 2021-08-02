using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using schemaforge.Crucible.Extensions;

namespace schemaforge.Crucible
{
  public abstract class SchemaController
  {
    // In child config definitions, RequiredConfigTokens and OptionalConfigTokens are set by merging a new HashSet of config tokens with the parent.
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
    public List<string> ErrorList { get; set; } = new List<string>();
    /// <summary>
    /// Should be populated with the JObject that is being checked with RequiredConfigTokens and OptionalConfigTokens.
    /// </summary>
    public JObject UserConfig { get; set; }
    /// <summary>
    /// Indicates if any step of validation failed.
    /// </summary>
    public bool Valid { get; set; } = true;

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
      foreach (var token in required)
      {
        if (!config.ContainsKey(token.TokenName))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add($"User config is missing required token {token.TokenName}\n{token.HelpString}");
          }
          else
          {
            ErrorList.Add($"{type} {name} is missing required token {token.TokenName}\n{token.HelpString}");
          }
          Valid = false;
        }
        else if (config[token.TokenName].IsNullOrEmpty())
        {
          ErrorList.Add($"Value of token {token.TokenName} is null or empty.");
          Valid = false;
        }
        else if (!token.Validate(config[token.TokenName]))
        {
          ErrorList.Add(token.HelpString);
          Valid = false;
        }
      }
      foreach (var token in optional)
      {
        if (!config.ContainsKey(token.TokenName) && token.DefaultValue != null)
        {
          config[token.TokenName] = token.DefaultValue; // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
        }
        else if (config.ContainsKey(token.TokenName) && !token.Validate(config[token.TokenName]))
        {
          ErrorList.Add(token.HelpString);
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
      foreach (var property in config)
      {
        if (!required.Select(x => x.TokenName).Contains(property.Key) && !optional.Select(x => x.TokenName).Contains(property.Key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add($"User config file contains unrecognized token: {property.Key}");
          }
          else
          {
            ErrorList.Add($"{type} {name} contains unrecognized token: {property.Key}");
          }
          Valid = false;
        }
      }
      if (!Valid && !string.IsNullOrWhiteSpace(message))
      {
        ErrorList.Add(message);
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
      foreach (var token in RequiredConfigTokens)
      {
        configJson.Add(token.TokenName, UserConfig.ContainsKey(token.TokenName) ? UserConfig[token.TokenName].ToString() : "");
      }
      foreach (var token in OptionalConfigTokens)
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
      foreach (var token in RequiredConfigTokens)
      {
        newConfig.Add(token.TokenName, token.HelpString);
      }
      foreach (var token in OptionalConfigTokens)
      {
        newConfig.Add(token.TokenName, "Optional - " + token.HelpString);
      }
      return newConfig;
    }

    #region Apply Constraints

    /*

    The following method will be used to produce ValidationFunction members for ConfigTokens. It serves two purposes: First, to enforce type checking by ensuring
    that the given JToken can be parsed as type T, and second, to apply any additional constraints the developer requires.

    */
    /// <summary>
    /// Produces ValidationFunction members for ConfigTokens, which are executed on the corresponding values found in the UserConfig property.
    /// It first checks the type of the value with T, then executes all passed constraints on the value.
    /// </summary>
    /// <typeparam name="T">Type that the token value will be cast to.</typeparam>
    /// <param name="constraints">Functions to execute on the token value after cast is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints. Can be used in the constructor of a ConfigToken.</returns>
    public Func<JToken, string, bool> ApplyConstraints<T>(params Func<JToken, string, bool>[] constraints)
    {
      bool ValidationFunction(JToken inputToken, string tokenName)
      {
        if (inputToken.IsNullOrEmpty())
        {
          ErrorList.Add($"The value of token {tokenName} is empty or null.");
          return false;
        }
        bool validToken = true;
        try
        {
          T castValue = inputToken.Value<T>();
          foreach (var constraint in constraints)
          {
            if (!constraint(inputToken, tokenName))
            {
              validToken = false;
            }
          }
          return validToken;
        }
        catch
        {
          ErrorList.Add($"Token {tokenName} with value {inputToken} is an incorrect type. Expected value type: {typeof(T)}");
          return false;
        }
      }
      return ValidationFunction;
    }

    /// <summary>
    /// Produces ValidationFunction members for ConfigTokens, which are executed on the corresponding values found in the UserConfig property.
    /// If the value of the token in the user's config can be cast as T1, then constraintsIfT1 are executed.
    /// If not, then if the value can be cast as T2, constraintsIfT2 will be executed.
    /// WARNING: Casts will be attempted IN ORDER. For example, ApplyConstraints string, int will NEVER treat the passed token as an int!
    /// </summary>
    /// <typeparam name="T1">First type to check against the token value in the returned function.</typeparam>
    /// <typeparam name="T2">Second type to check against the token value in the returned function.</typeparam>
    /// <param name="constraintsIfT1">Functions to execute on the token value if casting to T1 is successful.</param>
    /// <param name="constraintsIfT2">Functions to execute on the token value if casting to T2 is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints. Can be used in the constructor of a ConfigToken.</returns>
    public Func<JToken, string, bool> ApplyConstraints<T1, T2>(Func<JToken, string, bool>[] constraintsIfT1 = null, Func<JToken, string, bool>[] constraintsIfT2 = null)
    {
      bool ValidationFunction(JToken inputToken, string tokenName)
      {
        if (inputToken.IsNullOrEmpty())
        {
          ErrorList.Add($"The value of token {tokenName} is empty or null.");
          return false;
        }
        bool validToken = true;
        try
        {
          T1 castValue = inputToken.Value<T1>();
          if (!(constraintsIfT1 == null))
          {
            foreach (var constraint in constraintsIfT1)
            {
              if (!constraint(inputToken, tokenName))
              {
                validToken = false;
              }
            }
            return validToken;
          }
          else
          {
            return true;
          }
        }
        catch
        {
          try
          {
            T2 castValue = inputToken.Value<T2>();
            if (!(constraintsIfT2 == null))
            {
              foreach (var constraint in constraintsIfT2)
              {
                if (!constraint(inputToken, tokenName))
                {
                  validToken = false;
                }
              }
              return validToken;
            }
            else
            {
              return true;
            }
          }
          catch
          {
            ErrorList.Add($"Token {tokenName} with value {inputToken} is an incorrect type. Expected one of: {typeof(T1)}, {typeof(T2)}");
            return false;
          }
        }
      }
      return ValidationFunction;
    }

    #endregion Apply Constraints

    #region String Constraints

    /// <param name="acceptableValues">List of values used to build the returned function.</param>
    /// <returns>Function checking to ensure that the value of the passed JToken is one of acceptableValues.</returns>
    protected Func<JToken, string, bool> ConstrainStringValues(params string[] acceptableValues)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        if (!acceptableValues.Contains(inputToken.ToString())) //Returns false if inputString is not in provided list
        {
          ErrorList.Add($"Input {inputName} with value {inputToken} is not valid. Valid values: {string.Join(", ", acceptableValues)}"); // Tell the user what's wrong and how to fix it.
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <param name="pattern">Valid Regex pattern(s) used in the returned function.</param>
    /// <returns>Function checking to ensure that the whole JToken matches at least one of the provided pattern strings.</returns>
    protected Func<JToken, string, bool> ConstrainStringWithRegexExact(params Regex[] patterns)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        string inputString = inputToken.ToString();
        bool matchesAtLeastOne = false;
        foreach (var pattern in patterns)
        {
          if (pattern.IsMatch(inputString))
          {
            if (Regex.Replace(inputString, pattern.ToString(), "").Length == 0)
            {
              matchesAtLeastOne = true;
            }
          }
        }
        if (!matchesAtLeastOne)
        {
          if (patterns.Length == 1)
          {
            ErrorList.Add($"Token {inputName} with value {inputToken} is not an exact match to pattern {patterns[0]}");
          }
          else
          {
            ErrorList.Add($"Token {inputName} with value {inputToken} is not an exact match to any pattern: {string.Join<Regex>(" ", patterns)}");
          }
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains length of string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <returns>Function that ensures the length of a string is at least lowerBound.</returns>
    protected Func<JToken, string, bool> ConstrainStringLength(int lowerBound)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        string inputString = inputToken.ToString();
        if (inputString.Length < lowerBound)
        {
          ErrorList.Add($"Token {inputName} with value {inputToken} must have a length of at least {lowerBound}. Actual length: {inputString.Length}");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains length of string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <param name="upperBound">Maximum length of passed string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if lowerBound is greater than upperBound.</exception>
    /// <returns>Function that ensures the length of a string is at least lowerBound and at most upperBound.</returns>
    protected Func<JToken, string, bool> ConstrainStringLength(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainStringLength lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        string inputString = inputToken.ToString();
        if (inputString.Length < lowerBound || inputString.Length > upperBound)
        {
          ErrorList.Add($"Token {inputName} with value {inputToken} must have a length of at least {lowerBound} and at most {upperBound}. Actual length: {inputString.Length}");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Forbids characters from a string.
    /// </summary>
    /// <param name="forbiddenCharacters">Characters that cannot occur in the input string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if no chars are passed.</exception>
    /// <returns>Function that ensures the input string does not contain any of the passed characters.</returns>
    protected Func<JToken, string, bool> ForbidStringCharacters(params char[] forbiddenCharacters)
    {
      if (forbiddenCharacters.Length == 0)
      {
        throw new ArgumentException("ForbidStringCharacters must have at least one parameter.");
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        string inputString = inputToken.ToString();
        bool containsForbidden = false;
        foreach (var forbiddenCharacter in forbiddenCharacters)
        {
          if (inputString.IndexOf(forbiddenCharacter) != -1)
          {
            containsForbidden = true;
          }
        }
        if (containsForbidden)
        {
          ErrorList.Add($"Token {inputName} with value {inputToken} contains at least one of a forbidden character: {string.Join(" ", forbiddenCharacters)}");
        }
        return !containsForbidden;
      }
      return InnerMethod;
    }

    #endregion String Constraints

    #region Numeric Constraints

    /// <summary>
    /// Constrains numeric values with only a lower bound.
    /// </summary>
    /// <param name="lowerBound">Double used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed JToken is greater than the provided lower bound.</returns>
    protected Func<JToken, string, bool> ConstrainNumericValue(double lowerBound)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        if ((double)inputToken < lowerBound)
        {
          ErrorList.Add($"Token {inputName} with value {inputToken} is less than enforced lower bound {lowerBound}");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains numeric values with a lower bound and an upper bound.
    /// </summary>
    /// <param name="lowerBound">Double used as the lower bound in the returned function, inclusive.</param>
    /// <param name="upperBound">Double used as the upper bound in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if upperBound is greater than lowerBound.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is greater than the provided lower bound.</returns>
    protected Func<JToken, string, bool> ConstrainNumericValue(double lowerBound, double upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainNumericValue lower bound must be less than or equal to upper bound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        if ((double)inputToken < lowerBound || (double)inputToken > upperBound)
        {
          ErrorList.Add($"Token {inputName} with value {inputToken} is invalid. Value must be greater than or equal to {lowerBound} and less than or equal to {upperBound}");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains numeric values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <param name="domains">(double, double) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is within at least one of the provided domains.</returns>
    protected Func<JToken, string, bool> ConstrainNumericValue(params (double, double)[] domains)
    {
      foreach (var domain in domains)
      {
        if (domain.Item1 > domain.Item2)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound)");
        }
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        double inputValue = (double)inputToken;
        bool matchesAtLeastOne = false;
        foreach (var domain in domains)
        {
          if (inputValue >= domain.Item1 && inputValue <= domain.Item2)
          {
            matchesAtLeastOne = true;
          }
        }
        if (!matchesAtLeastOne)
        {
          ErrorList.Add($"Token {inputName} with value {inputValue} is invalid. Value must fall within one of the following domains, inclusive: {string.Join(" ", domains.Select(x => x.ToString()))}");
        }
        return matchesAtLeastOne;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains numeric values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <param name="domains">(int, int) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is within at least one of the provided domains.</returns>
    protected Func<JToken, string, bool> ConstrainNumericValue(params (int, int)[] domains)
    {
      foreach (var domain in domains)
      {
        if (domain.Item1 > domain.Item2)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound).");
        }
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        double inputValue = (double)inputToken;
        bool matchesAtLeastOne = false;
        foreach (var domain in domains)
        {
          if (inputValue >= domain.Item1 && inputValue <= domain.Item2)
          {
            matchesAtLeastOne = true;
          }
        }
        if (!matchesAtLeastOne)
        {
          ErrorList.Add($"Token {inputName} with value {inputValue} is invalid. Value must fall within one of the following domains, inclusive: {string.Join(" ", domains.Select(x => x.ToString()))}");
        }
        return matchesAtLeastOne;
      }
      return InnerMethod;
    }

    #endregion Numeric Constraints

    #region JObject Constraints

    // This constraint method allows nesting of Json objects inside one another without resorting to defining additional config types.

    /// <summary>
    /// Allows nested Json property checking.
    /// </summary>
    /// <param name="requiredTokens">Array of required ConfigToken objects that will be applied to the passed Json object.</param>
    /// <returns>Function ensuring the passed JObject contains all tokens in requiredTokens and all validation functions are passed.</returns>
    protected Func<JToken, string, bool> ConstrainJsonTokens(params ConfigToken[] requiredTokens)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        Initialize(requiredTokens.ToHashSet(), new HashSet<ConfigToken>(), inputJson, inputName, "Value of token");
        return Valid;
      }
      return InnerMethod;
    }


    /// <summary>
    /// Allows nested Json property checking.
    /// </summary>
    /// <param name="requiredTokens">Array of required ConfigToken objects that will be applied to the passed Json object.</param>
    /// <param name="optionalTokens">Array of optional ConfigToken objects that will be applied to the passed Json object.</param>
    /// <returns>Function ensuring the passed JObject contains all tokens in requiredTokens and all validation functions are passed for both token arrays.</returns>
    protected Func<JToken, string, bool> ConstrainJsonTokens(ConfigToken[] requiredTokens, ConfigToken[] optionalTokens)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        Initialize(requiredTokens.ToHashSet(), optionalTokens.ToHashSet(), inputJson, inputName, "Value of token");
        return Valid;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains the number of properties in a JObject with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of properties the target JObject must contain.</param>
    /// <returns>Function ensuring a JObject has at least lowerBound properties.</returns>
    protected Func<JToken, string, bool> ConstrainPropertyCount(int lowerBound)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        if (inputJson.Count < lowerBound)
        {
          ErrorList.Add($"Value of token {inputName} is invalid. Value has {inputJson.Count} properties, but must have at least {lowerBound} properties.");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains the number of properties in a JObject with a lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of properties the target JObject must contain.</param>
    /// <param name="upperBound">Maximum number of properties the target JObject can contain.</param>
    /// <returns>Function ensuring a JObject has at least lowerBound and at most upperBound properties.</returns>
    protected Func<JToken, string, bool> ConstrainPropertyCount(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainPropertyCount lowerBound must be less than upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JObject inputJson = (JObject)inputToken;
        if (inputJson.Count < lowerBound || inputJson.Count > upperBound)
        {
          ErrorList.Add($"Value of token {inputName} is invalid. Value has {inputJson.Count} properties, but must have at least {lowerBound} properties and at most {upperBound} properties.");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    #endregion JObject Constraints

    #region JArray Constraints

    /// <summary>
    /// Constrains the number of items in a JArray with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target JArray.</param>
    /// <returns>Function ensuring a JArray has at least lowerBound items.</returns>
    protected Func<JToken, string, bool> ConstrainArrayCount(int lowerBound)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JArray inputArray = (JArray)inputToken;
        if (inputArray.Count < lowerBound)
        {
          ErrorList.Add($"Value of token {inputName} contains {inputArray.Count} values, but must contain at least {lowerBound} values.");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains the number of items in a JArray with a lower bound and upper bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target JArray.</param>
    /// <param name="upperBound">Maximum number of items in the target JArray.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if lowerBound is greater than upperBound.</exception>
    /// <returns>Function ensuring a JArray has at least lowerBound and at most upperBound items.</returns>
    protected Func<JToken, string, bool> ConstrainArrayCount(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainArrayCount lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: " + upperBound);
      }
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JArray inputArray = (JArray)inputToken;
        if (inputArray.Count < lowerBound || inputArray.Count > upperBound)
        {
          ErrorList.Add($"Value of token {inputName} contains {inputArray.Count} values, but must contain between {lowerBound} and {upperBound} values.");
          return false;
        }
        return true;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Ensures all items in the target JArray are of type T and pass all provided constraints.
    /// </summary>
    /// <typeparam name="T">Type of all items in the target JArray.</typeparam>
    /// <param name="constraints">List of functions to run on all items in the JArray individually.</param>
    /// <returns>Function ensuring that all items in the target JArray are of type T and pass all provided constraints.</returns>
    protected Func<JToken, string, bool> ApplyConstraintsToAllArrayValues<T>(params Func<JToken, string, bool>[] constraints)
    {
      bool InnerMethod(JToken inputToken, string inputName)
      {
        JArray inputArray = (JArray)inputToken;
        bool allPassed = true;
        foreach (var value in inputArray)
        {
          try
          {
            T castValue = value.Value<T>();
            foreach (var constraint in constraints)
            {
              if (!constraint(value, "in array " + inputName))
              {
                allPassed = false;
              }
            }
          }
          catch
          {
            ErrorList.Add($"Value {value} in array {inputName} is an incorrect type. Expected value type: {typeof(T)}");
            allPassed = false;
          }
        }
        return allPassed;
      }
      return InnerMethod;
    }

    #endregion JArray Constraints
  }
}
