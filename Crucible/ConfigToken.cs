using System;
using System.Collections.Generic;
using System.Text;
using schemaforge.Crucible.Extensions;
using Newtonsoft.Json.Linq;

namespace schemaforge.Crucible
{
  public class ConfigToken
  {
    public string TokenName { get; private set; } // Name of the token; corresponds to the search term in the user's config.
    public string HelpString { get; private set; } // The HelpString is used when the user generates an empty config file and added to the error list when they enter an invalid value of some kind.
    public string DefaultValue { get; private set; } // If the DefaultValue is set and the user's config does not contain a value for this token, the UserConfig JObject stored in the JsonConfig parent will be modified to contain the token with the default value set.
    public List<Error> ErrorList { get; private set; } = new();
    protected Func<JToken, string, List<Error>> ValidationFunction { get; private set; } // This function will be executed on the value found in the user config for this token, if a value exists.

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// </summary>
    /// <exception cref="ArgumentException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputValidationFunction">Function that will be executed on the corresponding value in the input config.</param>
    public ConfigToken(string inputName, string inputHelpString, Func<JToken, string, List<Error>> inputValidationFunction)
    {
      BuildConfigToken(inputName, inputHelpString, null, inputValidationFunction);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// </summary>
    /// <exception cref="ArgumentException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue">String that will be inserted into the user config if an optional token is not provided.</param>
    /// <param name="inputValidationFunction">Function that will be executed on the corresponding value in the input config.</param>
    public ConfigToken(string inputName, string inputHelpString, string inputDefaultValue, Func<JToken, string, List<Error>> inputValidationFunction)
    {
      BuildConfigToken(inputName, inputHelpString, inputDefaultValue, inputValidationFunction);
    }

    private void BuildConfigToken(string inputName, string inputHelpString, string inputDefaultValue, Func<JToken, string, List<Error>> inputValidationFunction)
    {
      if (inputName.IsNullOrEmpty())
      {
        throw new ArgumentException("Name of ConfigToken is null or empty.");
      }
      if (inputHelpString.IsNullOrEmpty())
      {
        throw new ArgumentException($"HelpString of config token {inputName} is null or empty.");
      }
      TokenName = inputName;
      ValidationFunction = inputValidationFunction;
      HelpString = inputHelpString;
      DefaultValue = inputDefaultValue;
    }
    /*

    When Validate is called on a config token, it searches the JObject userConfig for a token with its current Name.

    If such a token is found, ValidationFunction() is executed. The value found and the token's name are passed as parameters.

    The function passed to a constructor should ideally be something created by ValidationFactory<T>(), which will enforce type checking on the user input and execute any additional Func<JToken, string, List<Error>> passed to the ValidationFactory function.

    Example below.

    */
    public bool Validate(JToken tokenValue)
    {
      ErrorList.AddRange(ValidationFunction(tokenValue, TokenName));
      return !ErrorList.AnyFatal();
    }
    public override string ToString()
    {
      return TokenName;
    }
  }
}
