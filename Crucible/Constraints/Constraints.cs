﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SchemaForge.Crucible.Extensions;

//using ConstraintApplicator = System.Func<SchemaForge.Crucible.Constraint[], SchemaForge.Crucible.ConstraintContainer>;

namespace SchemaForge.Crucible
{
  public abstract class Constraint
  {
    /// <summary>
    /// JProperty representation of this constraint. Will be used when saving a Schema to Json.
    /// </summary>
    public JProperty Property { get; protected set; }
    /// <summary>
    /// Used solely to retrieve the function as an object when deserializing.
    /// </summary>
    /// <returns>Function boxed in an object.</returns>
    public abstract object GetFunction();
  }

  public class Constraint<T> : Constraint
  {
    /// <summary>
    /// Function that will be applied by the constraint.
    /// </summary>
    public Func<T, string, List<Error>> Function { get; private set; }
    /// <summary>
    /// Constraint objects represent a rule that is applied to a token; the Function property is the validation function that will be executed on the token's value while the Property is the representation of the constraint as a JProperty.
    /// </summary>
    /// <param name="inputFunction">Function to execute from this constraint. The JToken in the function is the value being tested; the string is the name of the token in the object being tested.</param>
    /// <param name="inputProperty">JProperty representation of this constraint. Neither name nor value can be null or whitespace.</param>
    public Constraint(Func<T, string, List<Error>> inputFunction, JProperty inputProperty)
    {
      if (inputProperty.Name.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputProperty), $"Name of {nameof(inputProperty)} cannot be null or whitespace.");
      }
      if (inputProperty.Value.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputProperty), $"Value of {nameof(inputProperty)} cannot be null or whitespace.");
      }
      Property = inputProperty;
      Function = inputFunction;
    }
    public override object GetFunction() => Function;
  }
  public static class Constraints
  {
    #region Numeric Constraints

    /// <summary>
    /// Constrains numeric values with only a lower bound.
    /// </summary>
    /// <param name="lowerBound">Double used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed JToken is greater than the provided lower bound.</returns>
    public static Constraint<T> ConstrainValue<T>(T lowerBound) where T : IComparable, IComparable<T>, IFormattable
    {
      List<Error> InnerMethod(T inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputValue.CompareTo(lowerBound) < 0)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is less than enforced lower bound {lowerBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod,new JProperty("ConstrainValue",lowerBound));
    }

    /// <summary>
    /// Constrains numeric values with a lower bound and an upper bound.
    /// </summary>
    /// <param name="lowerBound">Double used as the lower bound in the returned function, inclusive.</param>
    /// <param name="upperBound">Double used as the upper bound in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if upperBound is greater than lowerBound.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is greater than the provided lower bound.</returns>
    public static Constraint<T> ConstrainValue<T>(T lowerBound, T upperBound) where T : IComparable, IComparable<T>, IFormattable
    {
      if (lowerBound.CompareTo(upperBound) > 0)
      {
        throw new ArgumentException($"ConstrainValue lower bound must be less than or equal to upper bound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(T inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputValue.CompareTo(lowerBound) < 0 || inputValue.CompareTo(upperBound) > 0)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is invalid. Value must be greater than or equal to {lowerBound} and less than or equal to {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty("ConstrainValue", lowerBound + ", " + upperBound));
    }

    /// <summary>
    /// Constrains numeric values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <param name="domains">(double, double) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed JToken is within at least one of the provided domains.</returns>
    public static Constraint<T> ConstrainValue<T>(params (T, T)[] domains) where T : IComparable, IComparable<T>, IFormattable
    {
      foreach ((T, T) domain in domains)
      {
        if (domain.Item1.CompareTo(domain.Item2) > 0)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound)");
        }
      }
      List<Error> InnerMethod(T inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        bool matchesAtLeastOne = false;
        foreach ((T, T) domain in domains)
        {
          if (inputValue.CompareTo(domain.Item1) > 0 && inputValue.CompareTo(domain.Item2) < 0)
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
      return new Constraint<T>(InnerMethod, new JProperty("ConstrainValue", JArray.FromObject(domains.Select(x => "(" + x.Item1 + ", " + x.Item2 + ")"))));
    }

    /// <summary>
    /// Constrains the number of digits a double has after the decimal.
    /// </summary>
    /// <param name="upperBound">Maximum number of digits after the decimal.</param>
    /// <returns>A new Constraint{double} containing a method to constrain decimal digits.</returns>
    public static Constraint<double> ConstrainDigits(int upperBound)
    {
      List<Error> InnerMethod(double inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        string doubleString = inputValue.ToString();
        if(doubleString.Contains('.'))
        {
          string[] splitDouble = doubleString.Split('.');
          if(splitDouble[1].Length > upperBound)
          {
            internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is invalid. Value can have no more than {upperBound} digits after the decimal."));
          }
        }
        return internalErrorList;
      }
      return new Constraint<double>(InnerMethod, new JProperty("ConstrainDigits", upperBound));
    }

    #endregion

    #region String Constraints

    /// <param name="acceptableValues">List of values used to build the returned function.</param>
    /// <returns>Function checking to ensure that the value of the passed item is one of acceptableValues.</returns>
    public static Constraint<T> AllowValues<T>(params T[] acceptableValues)
    {
      List<Error> InnerMethod(T inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (!acceptableValues.Contains(inputValue)) //Returns false if inputValue is not in provided list
        {
          internalErrorList.Add(new Error($"Input {inputName} with value {inputValue} is not valid. Valid values: {string.Join(", ", acceptableValues)}")); // Tell the user what's wrong and how to fix it.
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod,new JProperty("AllowValues",JArray.FromObject(acceptableValues)));
    }

    /// <param name="pattern">Valid Regex pattern(s) used in the returned function.</param>
    /// <returns>Function checking to ensure that the whole JToken matches at least one of the provided pattern strings.</returns>
    public static Constraint<string> ConstrainStringWithRegexExact(params Regex[] patterns)
    {
      List<Error> InnerMethod(string inputString, string inputName)
      {
        List<Error> internalErrorList = new();
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
            internalErrorList.Add(new Error($"Token {inputName} with value {inputString} is not an exact match to pattern {patterns[0]}"));
          }
          else
          {
            internalErrorList.Add(new Error($"Token {inputName} with value {inputString} is not an exact match to any pattern: {string.Join<Regex>(" ", patterns)}"));
          }
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty("ConstrainStringWithRegexExact",JArray.FromObject(patterns)));
    }

    /// <summary>
    /// Constrains length of string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <returns>Function that ensures the length of a string is at least lowerBound.</returns>
    public static Constraint<string> ConstrainStringLength(int lowerBound)
    {
      List<Error> InnerMethod(string inputString, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputString.Length < lowerBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputString} must have a length of at least {lowerBound}. Actual length: {inputString.Length}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod,new JProperty("ConstrainStringLength", lowerBound));
    }

    /// <summary>
    /// Constrains length of string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <param name="upperBound">Maximum length of passed string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if lowerBound is greater than upperBound.</exception>
    /// <returns>Function that ensures the length of a string is at least lowerBound and at most upperBound.</returns>
    public static Constraint<string> ConstrainStringLength(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainStringLength lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(string inputString, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputString.Length < lowerBound || inputString.Length > upperBound)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputString} must have a length of at least {lowerBound} and at most {upperBound}. Actual length: {inputString.Length}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty("ConstrainStringLength", lowerBound + ", " + upperBound));
    }

    /// <summary>
    /// Forbids characters from a string.
    /// </summary>
    /// <param name="forbiddenCharacters">Characters that cannot occur in the input string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if no chars are passed.</exception>
    /// <returns>Function that ensures the input string does not contain any of the passed characters.</returns>
    public static Constraint<string> ForbidStringCharacters(params char[] forbiddenCharacters)
    {
      if (forbiddenCharacters.Length == 0)
      {
        throw new ArgumentException("ForbidStringCharacters must have at least one parameter.");
      }
      List<Error> InnerMethod(string inputString, string inputName)
      {
        List<Error> internalErrorList = new();
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
          internalErrorList.Add(new Error($"Token {inputName} with value {inputString} contains at least one of a forbidden character: {string.Join(" ", forbiddenCharacters)}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty("ForbidStringCharacters",JArray.FromObject(forbiddenCharacters.ToArray())));
    }

    #endregion

    #region JArray Constraints

    /// <summary>
    /// Constrains the number of items in an enumerable object with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target enumerable.</param>
    /// <returns>Constraint ensuring an enumerable has at least lowerBound items.</returns>
    public static Constraint<T> ConstrainCollectionCount<T>(int lowerBound) where T: IEnumerable
    {
      List<Error> InnerMethod(T inputArray, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputArray.Count() < lowerBound)
        {
          internalErrorList.Add(new Error($"Collection {inputName} contains {inputArray.Count()} values, but must contain at least {lowerBound} values."));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty("ConstrainCollectionCount", lowerBound));
    }

    /// <summary>
    /// Constrains the number of items in a JArray with a lower bound and upper bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target JArray.</param>
    /// <param name="upperBound">Maximum number of items in the target JArray.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if lowerBound is greater than upperBound.</exception>
    /// <returns>Function ensuring a JArray has at least lowerBound and at most upperBound items.</returns>
    public static Constraint<T> ConstrainCollectionCount<T>(int lowerBound, int upperBound) where T: IEnumerable
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"ConstrainArrayCount lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: " + upperBound);
      }
      List<Error> InnerMethod(T inputArray, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputArray.Count() < lowerBound || inputArray.Count() > upperBound)
        {
          internalErrorList.Add(new Error($"Collection {inputName} contains {inputArray.Count()} values, but must contain between {lowerBound} and {upperBound} values."));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty("ConstrainCollectionCount", lowerBound + ", " + upperBound));
    }
    /// <summary>
    /// Encapsulates the process of typechecking and applying all passed constraints.
    /// </summary>
    /// <typeparam name="TValueType">Expected type that the JToken will be cast to.</typeparam>
    /// <param name="inputToken">Input JToken.</param>
    /// <param name="tokenName">Name of the input JToken.</param>
    /// <param name="constraints">Constraints to apply to the input token.</param>
    /// <returns>List{error} generated by applying all of the constraints.</returns>
    private static List<Error> ApplyConstraintsHelper<TValueType>(JToken inputToken, string tokenName, Constraint<TValueType>[] constraints)
    {
      List<Error> internalErrorList = new();
      try
      {
        TValueType castValue = inputToken.Value<TValueType>();
        if (constraints.Exists())
        {
          foreach (Constraint<TValueType> constraint in constraints)
          {
            internalErrorList.AddRange(constraint.Function(castValue, tokenName));
          }
        }
        return internalErrorList;
      }
      catch
      {
        throw new ArgumentException($"Type cast failed; deferring to next possible type cast.");
      }
    }

    /// <summary>
    /// Ensures all items in the target JArray are of type <typeparam name="TElementType"/> and pass all provided constraints.
    /// </summary>
    /// <typeparam name="TElementType">Type of all items in the target JArray.</typeparam>
    /// <param name="constraints">List of functions to run on all items in the JArray individually.</param>
    /// <returns>Function ensuring that all items in the target JArray are of type T and pass all provided constraints.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType>(params Constraint<TElementType>[] constraints)
    {
      List<Error> InnerMethod(JArray inputArray, string inputName)
      {
        List<Error> internalErrorList = new();
        foreach(JToken token in inputArray)
        {
          internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName,constraints));
        }
        return internalErrorList;
      }
      JArray constraintArray = new();
      constraintArray.Add(ConfigToken.GetConstraintObject(constraints));
      return new Constraint<JArray>(InnerMethod, new JProperty("ApplyConstraintsToCollection", constraintArray));
    }

    /// <summary>
    /// Ensures all items in the target JArray are of type
    /// <typeparam name="TElementType1"/> or <typeparam name="TElementType2"/>
    /// and applies all constraints on the type to which the element corresponds.
    /// WARNING: Casts will be attempted IN ORDER. For example, <see cref="ApplyConstraintsToJArray{string, int}"/>
    /// will NEVER treat the passed token as an int!
    /// </summary>
    /// <typeparam name="TElementType1">First type to check against the token value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType2">First type to check against the token value
    /// in the returned function.</typeparam>
    /// <param name="constraintsIfTElementType1">Constraints to execute if cast to
    /// <typeparam name="TElementType1"/> is successful.</param>
    /// <param name="constraintsIfTElementType2">Constraints to execute if cast to
    /// <typeparam name="TElementType2"/> is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints.
    /// Can be used in the constructor of a ConfigToken.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType1,TElementType2>(Constraint<TElementType1>[] constraintsIfTElementType1 = null, Constraint<TElementType2>[] constraintsIfTElementType2 = null)
    {
      List<Error> InnerMethod(JArray inputArray, string inputName)
      {
        List<Error> internalErrorList = new();
        foreach (JToken token in inputArray)
        {
          try
          {
            internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraintsIfTElementType1));
          }
          catch
          {
            try
            {
              internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraintsIfTElementType2));
            }
            catch
            {
              internalErrorList.Add(new Error($"Token {token} in collection {inputName} is an incorrect type. Expected one of: {typeof(TElementType1).Name}, {typeof(TElementType2).Name}"));
              return internalErrorList;
            }
          }
        }
        return internalErrorList;
      }
      JArray constraintArray = new();
      constraintArray.Add(ConfigToken.GetConstraintObject(constraintsIfTElementType1));
      constraintArray.Add(ConfigToken.GetConstraintObject(constraintsIfTElementType2));
      return new Constraint<JArray>(InnerMethod, new JProperty("ApplyConstraintsToCollection", constraintArray));
    }

    /// <summary>
    /// Ensures all items in the target JArray are of type
    /// <typeparam name="TElementType1"/>, or <typeparam name="TElementType2"/>,
    /// or <typeparam name="TElementType3"/>
    /// and applies all constraints on the type to which the element corresponds.
    /// WARNING: Casts will be attempted IN ORDER. For example, <see cref="ApplyConstraintsToJArray{string, int}"/>
    /// will NEVER treat the passed token as an int!
    /// </summary>
    /// <typeparam name="TElementType1">First type to check against the token value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType2">First type to check against the token value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType3">First type to check against the token value
    /// in the returned function.</typeparam>
    /// <param name="constraintsIfTElementType1">Constraints to execute if cast to
    /// <typeparam name="TElementType1"/> is successful.</param>
    /// <param name="constraintsIfTElementType2">Constraints to execute if cast to
    /// <typeparam name="TElementType2"/> is successful.</param>
    /// <param name="constraintsIfTElementType3">Constraints to execute if cast to
    /// <typeparam name="TElementType3"/> is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints.
    /// Can be used in the constructor of a ConfigToken.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType1, TElementType2, TElementType3>(Constraint<TElementType1>[] constraintsIfT1 = null, Constraint<TElementType2>[] constraintsIfT2 = null, Constraint<TElementType3>[] constraintsIfT3 = null)
    {
      List<Error> InnerMethod(JArray inputArray, string inputName)
      {
        List<Error> internalErrorList = new();
        foreach (JToken token in inputArray)
        {
          try
          {
            internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraintsIfT1));
          }
          catch
          {
            try
            {
              internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraintsIfT2));
            }
            catch
            {
              try
              {
                internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraintsIfT3));
              }
              catch
              {
                internalErrorList.Add(new Error($"Token {token} in collection {inputName} is an incorrect type. Expected one of: {typeof(TElementType1).Name}, {typeof(TElementType2).Name}, {typeof(TElementType3).Name}"));
                return internalErrorList;
              }
            }
          }
        }
        return internalErrorList;
      }
      JArray constraintArray = new();
      constraintArray.Add(ConfigToken.GetConstraintObject(constraintsIfT1));
      constraintArray.Add(ConfigToken.GetConstraintObject(constraintsIfT2));
      constraintArray.Add(ConfigToken.GetConstraintObject(constraintsIfT3));
      return new Constraint<JArray>(InnerMethod, new JProperty("ApplyConstraintsToCollection", constraintArray));
    }

    #endregion

    #region JObject Constraints

    /// <summary>
    /// Allows nested Json property checking.
    /// </summary>
    /// <param name="inputSchema">Schema object to apply to the Json.</param>
    /// <returns>Function ensuring the passed JObject contains all tokens in requiredTokens and all validation functions are passed.</returns>
    public static Constraint<JObject> ApplySchema(Schema inputSchema)
    {
      List<Error> InnerMethod(JObject inputJson, string inputName)
      {
        List<Error> internalErrorList = new();
        internalErrorList.AddRange(inputSchema.Validate(inputJson, new JObjectTranslator(), inputName, "inner json"));
        return internalErrorList;
      }
      return new Constraint<JObject>(InnerMethod, new JProperty("ApplySchema", inputSchema.ToString()));
    }

    #endregion
  }
}
