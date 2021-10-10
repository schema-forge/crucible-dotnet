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

Expose iteration over config tokens and more fine tuning functions, for the grim darkness of the future when Schemas are passed from program to program.
 
*/

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Schema objects contain a set of ConfigTokens that define each value that should be
  /// contained in an object passed to its Validate method.
  /// </summary>
  public class Schema
  {
    /// <summary>
    /// Set of token rules to use when a collection is passed to
    /// <see cref="Validate{TCollectionType}(TCollectionType, ISchemaTranslator{TCollectionType}, string, bool)"/>.
    /// </summary>
    private readonly Dictionary<string, ConfigToken> ConfigTokens = new();
    /// <summary>
    /// Contains all errors generated during validation and the associated
    /// <see cref="ConfigToken.Description"/> of each token that was marked invalid.
    /// </summary>
    public List<Error> ErrorList { get; } = new();

    /// <summary>
    /// Constructs an empty <see cref="Schema"/> with no <see cref="ConfigToken"/> objects.
    /// </summary>
    public Schema()
    {
      
    }

    /// <summary>
    /// Instantiates a <see cref="Schema"/> object with a set of <see cref="ConfigToken"/> objects to use.
    /// </summary>
    /// <param name="tokens">Tokens to add to the token set.</param>
    public Schema(params ConfigToken[] tokens)
    {
      AddTokens(tokens);
    }

    /// <summary>
    /// Instantiates a <see cref="Schema"/> object with a set of <see cref="ConfigToken"/> objects to use.
    /// </summary>
    /// <param name="tokens">Tokens to add to the token set.</param>
    public Schema(IEnumerable<ConfigToken> tokens)
    {
      AddTokens(tokens);
    }

    /// <summary>
    /// Adds a token to the <see cref="Schema"/> object's set of <see cref="ConfigToken"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if the Schema already contains a <see cref="ConfigToken"/> with the same name.</exception>
    /// <param name="token">Token to add. The name must be different from all <see cref="ConfigToken"/> objects currently in the Schema.</param>
    public void AddToken(ConfigToken token)
    {
      if (ConfigTokens.ContainsKey(token.TokenName))
      {
        throw new ArgumentException($"ConfigToken set already contains a ConfigToken named {token.TokenName}");
      }
      ConfigTokens.Add(token.TokenName, token);
    }

    /// <summary>
    /// Adds a set of tokens to the <see cref="Schema"/> object's set of <see cref="ConfigToken"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if the Schema already contains a token with the same name as one or more of the tokens in <paramref name="tokens"/>.</exception>
    /// <param name="tokens">Collection of tokens to add. There must be no tokens in the set that have a name identical to something already in the Schema's token set.</param>
    public void AddTokens(IEnumerable<ConfigToken> tokens)
    {
      foreach (ConfigToken token in tokens)
      {
        if (ConfigTokens.ContainsKey(token.TokenName))
        {
          throw new ArgumentException($"ConfigToken set already contains a ConfigToken named {token.TokenName}");
        }
        ConfigTokens.Add(token.TokenName, token);
      }
    }

    /// <summary>
    /// Removes one token from the <see cref="Schema"/> object's set of <see cref="ConfigToken"/> objects.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a token not already in the set.</exception>
    /// <param name="tokenName">Name of the token to remove; corresponds to <see cref="ConfigToken.TokenName"/>.</param>
    public void RemoveToken(string tokenName)
    {
      if(ConfigTokens.ContainsKey(tokenName))
      {
        ConfigTokens.Remove(tokenName);
      }
      else
      {
        throw new ArgumentException($"Attempted to remove token {tokenName} from Schema, but Schema did not contain {tokenName}");
      }
    }

    /// <summary>
    /// Removes all tokens from the <see cref="Schema"/> object's set of <see cref="ConfigToken"/> objects
    /// where <see cref="ConfigToken.TokenName"/> is found in <paramref name="tokenNames"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a token not already in the set.</exception>
    /// <param name="tokenNames">List of token names to remove; corresponds to <see cref="ConfigToken.TokenName"/>.</param>
    public void RemoveTokens(IEnumerable<string> tokenNames)
    {
      foreach (string tokenName in tokenNames)
      {
        if (ConfigTokens.ContainsKey(tokenName))
        {
          ConfigTokens.Remove(tokenName);
        }
        else
        {
          throw new ArgumentException($"Attempted to remove token {tokenName} from Schema, but Schema did not contain {tokenName}");
        }
      }
    }

    /// <summary>
    /// Removes all tokens from the <see cref="Schema"/> object's set of <see cref="ConfigToken"/> objects
    /// where <see cref="ConfigToken.TokenName"/> is found in <paramref name="tokenNames"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Throws ArgumentException if attempting to remove a token not already in the set.</exception>
    /// <param name="tokenNames">List of token names to remove; corresponds to <see cref="ConfigToken.TokenName"/>.</param>
    public void RemoveTokens(params string[] tokenNames)
    {
      foreach (string tokenName in tokenNames)
      {
        if (ConfigTokens.ContainsKey(tokenName))
        {
          ConfigTokens.Remove(tokenName);
        }
        else
        {
          throw new ArgumentException($"Attempted to remove token {tokenName} from Schema, but Schema did not contain {tokenName}");
        }
      }
    }

    /// <summary>
    /// Returns the number of tokens contained in the <see cref="Schema"/>.
    /// </summary>
    /// <returns>The number of tokens contained in the <see cref="Schema"/>.</returns>
    public int Count() => ConfigTokens.Count;

    /// <summary>
    /// Checks <paramref name="collection"/> using the set of <see cref="ConfigTokens"/>.
    /// If name and type are provided, the message
    /// "Validation for <paramref name="name"/> failed."
    /// will be added to <see cref="ErrorList"/> on validation failure.
    /// </summary>
    /// <param name="collection">Collection object to check using the <see cref="ConfigToken"/>
    /// rules set in <see cref="ConfigTokens"/>.</param>
    /// <param name="translator"><see cref="ISchemaTranslator{TCollectionType}"/>
    /// used to interpret the collection for the <see cref="Schema"/> and extract values.</param>
    /// <param name="name">If name and type are provided, the message 
    /// "Validation for <paramref name="name"/> failed."
    /// will be added to ErrorList on validation failure.</param>
    /// <param name="allowUnrecognized">If false, unrecognized tokens (that is,
    /// tokens present in the object being validated but not in the Schea) will raise
    /// a <see cref="Severity.Fatal"/> error. If true, unrecognized tokens will
    /// raise a <see cref="Severity.Info"/> error.</param>
    public virtual List<Error> Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator, string name = null, bool allowUnrecognized = false)
    {
      string message = " ";
      // This option is included in case a sub-collection is being validated; this
      //   allows the ErrorList to indicate the exact collection that has the issue.
      if (!string.IsNullOrWhiteSpace(name))
      {
        message = $"Validation for {name} failed.";
      }
      foreach (ConfigToken token in ConfigTokens.Values)
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
              ErrorList.Add(new Error($"Input {name} is missing required token {token.TokenName}\n{token.Description}"));
            }
          }
          else if(token.DefaultValue.Exists())
          {
            collection = token.InsertDefaultValue(collection, translator); // THIS MUTATES THE INPUT CONFIG. USE WITH CAUTION.
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

      The decision to invalidate the config due to unrecognized tokens stems
        from the possibility that an end user might misspell an optional token
        when forming their configuration file or request.

      If the user includes an optional token with a typo in the token name, it
        will not be flagged as a missing required token, but it will also not
        have the effect the user intended from including the optional token.

      Such a problem would be very frustrating and possibly difficult to debug;
        therefore, we invalidate the collection if there are any tokens that
        are not accounted for in ConfigTokens by default.

      */
      List<string> collectionKeys = translator.GetCollectionKeys(collection);
      HashSet<string> configTokenNames = ConfigTokens.Select(x => x.Value.TokenName).ToHashSet();
      foreach (string key in collectionKeys)
      {
        if (!configTokenNames.Contains(key))
        {
          if (message.IsNullOrEmpty())
          {
            ErrorList.Add(new Error($"Input object contains unrecognized token: {key}",allowUnrecognized?Severity.Info:Severity.Fatal));
          }
          else
          {
            ErrorList.Add(new Error($"Input {name} contains unrecognized token: {key}", allowUnrecognized?Severity.Info:Severity.Fatal));
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
    /// <returns>String version of a <see cref="JObject"/> representation of the current schema controller.</returns>
    public override string ToString()
    {
      JObject schemaJson = new();
      foreach(ConfigToken token in ConfigTokens.Values)
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
    /// This method can be used to generate a new example request or configuration file with all the required and optional tokens along with their <see cref="ConfigToken.Description"/>.
    /// </summary>
    /// <returns>A <see cref="JObject"/> with all tokens from <see cref="ConfigTokens"/>, using <see cref="ConfigToken.TokenName"/> as the name and <see cref="ConfigToken.Description"/> as the property value.
    /// If the Descriptions are well-written, the return value will serve as an excellent example for an end user to fill in.</returns>
    public JObject GenerateEmptyJson()
    {
      JObject newConfig = new();
      foreach (ConfigToken token in ConfigTokens.Values)
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

    /// <summary>
    /// Returns a new <see cref="Schema"/> that is a clone of the current <see cref="Schema"/>.
    /// </summary>
    /// <returns>A new <see cref="Schema"/> that is a clone of this <see cref="Schema"/>.</returns>
    public Schema Clone()
    {
      Schema newSchema = new();
      foreach(ConfigToken token in ConfigTokens.Values)
      {
        newSchema.AddToken(token);
      }
      return newSchema;
    }
  }
}
