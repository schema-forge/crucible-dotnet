using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  public static class Constraints
  {

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
    public static Func<JToken, string, List<Error>> ApplyConstraints<T>(params Func<JToken, string, List<Error>>[] constraints)
    {
      List<Error> ValidationFunction(JToken inputToken, string tokenName)
      {
        List<Error> internalErrorList = new();
        if (inputToken.IsNullOrEmpty())
        {
          internalErrorList.Add(new Error($"The value of token {tokenName} is empty or null.", Severity.NullOrEmpty));
          return internalErrorList;
        }
        try
        {
          T castValue = inputToken.Value<T>();
          foreach (Func<JToken, string, List<Error>> constraint in constraints)
          {
            internalErrorList.AddRange(constraint(inputToken, tokenName));
          }
          return internalErrorList;
        }
        catch
        {
          internalErrorList.Add(new Error($"Token {tokenName} with value {inputToken} is an incorrect type. Expected value type: {typeof(T)}"));
          return internalErrorList;
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
    public static Func<JToken, string, List<Error>> ApplyConstraints<T1, T2>(Func<JToken, string, List<Error>>[] constraintsIfT1 = null, Func<JToken, string, List<Error>>[] constraintsIfT2 = null)
    {
      List<Error> ValidationFunction(JToken inputToken, string tokenName)
      {
        List<Error> internalErrorList = new();
        if (inputToken.IsNullOrEmpty())
        {
          internalErrorList.Add(new Error($"The value of token {tokenName} is empty or null.", Severity.NullOrEmpty));
          return internalErrorList;
        }
        try
        {
          T1 castValue = inputToken.Value<T1>();
          if (!(constraintsIfT1 == null))
          {
            foreach (Func<JToken, string, List<Error>> constraint in constraintsIfT1)
            {
              internalErrorList.AddRange(constraint(inputToken, tokenName));
            }
            return internalErrorList;
          }
          else
          {
            return internalErrorList;
          }
        }
        catch
        {
          try
          {
            T2 castValue = inputToken.Value<T2>();
            if (!(constraintsIfT2 == null))
            {
              foreach (Func<JToken, string, List<Error>> constraint in constraintsIfT2)
              {
                internalErrorList.AddRange(constraint(inputToken, tokenName));
              }
              return internalErrorList;
            }
            else
            {
              return internalErrorList;
            }
          }
          catch
          {
            internalErrorList.Add(new Error($"Token {tokenName} with value {inputToken} is an incorrect type. Expected one of: {typeof(T1)}, {typeof(T2)}"));
            return internalErrorList;
          }
        }
      }
      return ValidationFunction;
    }

    #endregion

    #region Numeric Constraints

    /// <summary>
    /// Constrains numeric values with only a lower bound.
    /// </summary>
    /// <param name="lowerBound">Double used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed JToken is greater than the provided lower bound.</returns>
    public static Func<JToken, string, List<Error>> ConstrainNumericValue(double lowerBound)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        if ((double)inputToken < lowerBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} is less than enforced lower bound {lowerBound}"));
          return internalErrorList;
        }
        return internalErrorList;
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
    public static Func<JToken, string, List<Error>> ConstrainNumericValue(double lowerBound, double upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainNumericValue lower bound must be less than or equal to upper bound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        if ((double)inputToken < lowerBound || (double)inputToken > upperBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} is invalid. Value must be greater than or equal to {lowerBound} and less than or equal to {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains numeric values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <param name="domains">(double, double) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is within at least one of the provided domains.</returns>
    public static Func<JToken, string, List<Error>> ConstrainNumericValue(params (double, double)[] domains)
    {
      foreach ((double, double) domain in domains)
      {
        if (domain.Item1 > domain.Item2)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound)");
        }
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        double inputValue = (double)inputToken;
        bool matchesAtLeastOne = false;
        foreach ((double, double) domain in domains)
        {
          if (inputValue >= domain.Item1 && inputValue <= domain.Item2)
          {
            matchesAtLeastOne = true;
          }
        }
        if (!matchesAtLeastOne)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is invalid. Value must fall within one of the following domains, inclusive: {string.Join(" ", domains.Select(x => x.ToString()))}"));
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains numeric values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <param name="domains">(int, int) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is within at least one of the provided domains.</returns>
    public static Func<JToken, string, List<Error>> ConstrainNumericValue(params (int, int)[] domains)
    {
      foreach ((int, int) domain in domains)
      {
        if (domain.Item1 > domain.Item2)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound).");
        }
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        double inputValue = (double)inputToken;
        bool matchesAtLeastOne = false;
        foreach ((int, int) domain in domains)
        {
          if (inputValue >= domain.Item1 && inputValue <= domain.Item2)
          {
            matchesAtLeastOne = true;
          }
        }
        if (!matchesAtLeastOne)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is invalid. Value must fall within one of the following domains, inclusive: {string.Join(" ", domains.Select(x => x.ToString()))}"));
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    #endregion

    #region String Constraints

    /// <param name="acceptableValues">List of values used to build the returned function.</param>
    /// <returns>Function checking to ensure that the value of the passed JToken is one of acceptableValues.</returns>
    public static Func<JToken, string, List<Error>> ConstrainStringValues(params string[] acceptableValues)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        if (!acceptableValues.Contains(inputToken.ToString())) //Returns false if inputString is not in provided list
        {
          internalErrorList.Add(new Error($"Input {inputName} with value {inputToken} is not valid. Valid values: {string.Join(", ", acceptableValues)}")); // Tell the user what's wrong and how to fix it.
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <param name="pattern">Valid Regex pattern(s) used in the returned function.</param>
    /// <returns>Function checking to ensure that the whole JToken matches at least one of the provided pattern strings.</returns>
    public static Func<JToken, string, List<Error>> ConstrainStringWithRegexExact(params Regex[] patterns)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        string inputString = inputToken.ToString();
        bool matchesAtLeastOne = false;
        foreach (Regex pattern in patterns)
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
            internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} is not an exact match to pattern {patterns[0]}"));
          }
          else
          {
            internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} is not an exact match to any pattern: {string.Join<Regex>(" ", patterns)}"));
          }
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains length of string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <returns>Function that ensures the length of a string is at least lowerBound.</returns>
    public static Func<JToken, string, List<Error>> ConstrainStringLength(int lowerBound)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        string inputString = inputToken.ToString();
        if (inputString.Length < lowerBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} must have a length of at least {lowerBound}. Actual length: {inputString.Length}"));
          return internalErrorList;
        }
        return internalErrorList;
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
    public static Func<JToken, string, List<Error>> ConstrainStringLength(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainStringLength lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        string inputString = inputToken.ToString();
        if (inputString.Length < lowerBound || inputString.Length > upperBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} must have a length of at least {lowerBound} and at most {upperBound}. Actual length: {inputString.Length}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Forbids characters from a string.
    /// </summary>
    /// <param name="forbiddenCharacters">Characters that cannot occur in the input string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if no chars are passed.</exception>
    /// <returns>Function that ensures the input string does not contain any of the passed characters.</returns>
    public static Func<JToken, string, List<Error>> ForbidStringCharacters(params char[] forbiddenCharacters)
    {
      if (forbiddenCharacters.Length == 0)
      {
        throw new ArgumentException("ForbidStringCharacters must have at least one parameter.");
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        string inputString = inputToken.ToString();
        bool containsForbidden = false;
        foreach (char forbiddenCharacter in forbiddenCharacters)
        {
          if (inputString.IndexOf(forbiddenCharacter) != -1)
          {
            containsForbidden = true;
          }
        }
        if (containsForbidden)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputToken} contains at least one of a forbidden character: {string.Join(" ", forbiddenCharacters)}"));
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    #endregion

    #region JArray Constraints

    /// <summary>
    /// Constrains the number of items in a JArray with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target JArray.</param>
    /// <returns>Function ensuring a JArray has at least lowerBound items.</returns>
    public static Func<JToken, string, List<Error>> ConstrainArrayCount(int lowerBound)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        JArray inputArray = (JArray)inputToken;
        if (inputArray.Count < lowerBound)
        {
          internalErrorList.Add(new Error($"Value of token {inputName} contains {inputArray.Count} values, but must contain at least {lowerBound} values."));
          return internalErrorList;
        }
        return internalErrorList;
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
    public static Func<JToken, string, List<Error>> ConstrainArrayCount(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainArrayCount lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: " + upperBound);
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        JArray inputArray = (JArray)inputToken;
        if (inputArray.Count < lowerBound || inputArray.Count > upperBound)
        {
          internalErrorList.Add(new Error($"Value of token {inputName} contains {inputArray.Count} values, but must contain between {lowerBound} and {upperBound} values."));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Ensures all items in the target JArray are of type T and pass all provided constraints.
    /// </summary>
    /// <typeparam name="T">Type of all items in the target JArray.</typeparam>
    /// <param name="constraints">List of functions to run on all items in the JArray individually.</param>
    /// <returns>Function ensuring that all items in the target JArray are of type T and pass all provided constraints.</returns>
    public static Func<JToken, string, List<Error>> ApplyConstraintsToAllArrayValues<T>(params Func<JToken, string, List<Error>>[] constraints)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        JArray inputArray = (JArray)inputToken;
        foreach (JToken value in inputArray)
        {
          try
          {
            T castValue = value.Value<T>();
            foreach (Func<JToken, string, List<Error>> constraint in constraints)
            {
              internalErrorList.AddRange(constraint(value, "in array " + inputName));
            }
          }
          catch
          {
            internalErrorList.Add(new Error($"Value {value} in array {inputName} is an incorrect type. Expected value type: {typeof(T)}"));
          }
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    #endregion

    #region JObject Constraints

    /// <summary>
    /// Constrains the number of properties in a JObject with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of properties the target JObject must contain.</param>
    /// <returns>Function ensuring a JObject has at least lowerBound properties.</returns>
    public static Func<JToken, string, List<Error>> ConstrainPropertyCount(int lowerBound)
    {
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        JObject inputJson = (JObject)inputToken;
        if (inputJson.Count < lowerBound)
        {
          internalErrorList.Add(new Error($"Value of token {inputName} is invalid. Value has {inputJson.Count} properties, but must have at least {lowerBound} properties."));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    /// <summary>
    /// Constrains the number of properties in a JObject with a lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of properties the target JObject must contain.</param>
    /// <param name="upperBound">Maximum number of properties the target JObject can contain.</param>
    /// <returns>Function ensuring a JObject has at least lowerBound and at most upperBound properties.</returns>
    public static Func<JToken, string, List<Error>> ConstrainPropertyCount(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainPropertyCount lowerBound must be less than upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(JToken inputToken, string inputName)
      {
        List<Error> internalErrorList = new();
        JObject inputJson = (JObject)inputToken;
        if (inputJson.Count < lowerBound || inputJson.Count > upperBound)
        {
          internalErrorList.Add(new Error($"Value of token {inputName} is invalid. Value has {inputJson.Count} properties, but must have at least {lowerBound} properties and at most {upperBound} properties."));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return InnerMethod;
    }

    #endregion
  }
}
