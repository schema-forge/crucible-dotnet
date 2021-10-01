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
  class SchemaDateTime
  {
    public DateTime CastValue { get; private set; }
    public string StringRepresentation { get; private set; }
    public SchemaDateTime(DateTime dateTime, string stringRepresentation)
    {
      CastValue = dateTime;
      StringRepresentation = stringRepresentation;
    }
  }

  public enum ConstraintType
  {
    Standard,
    Format
  }

  public abstract class Constraint
  {
    /// <summary>
    /// JProperty representation of this constraint. Will be used when saving a Schema to Json.
    /// </summary>
    public JProperty Property { get; protected set; } = null;

    /// <summary>
    /// Used solely to retrieve the function as an object when deserializing.
    /// </summary>
    /// <returns>Function boxed in an object.</returns>
    public abstract object GetFunction();

    /// <summary>
    /// List of errors generated during creation of the constraint.
    /// </summary>
    public List<Error> Errors { get; protected set; }

    /// <summary>
    /// Gives the type of the constraint; Format constraints are applied to the original token
    /// value cast to string while Standard constraints are applied post-cast.
    /// </summary>
    public ConstraintType ConstraintType { get; protected set; } = ConstraintType.Standard;
  }

  public class Constraint<TValueType> : Constraint
  {
    /// <summary>
    /// Function that will be applied by the constraint if this is a Standard constraint.
    /// </summary>
    public Func<TValueType, string, List<Error>> Function { get; protected set; }

    /// <summary>
    /// Function that will be applied by the constraint if this is a Format constraint.
    /// </summary>
    public Func<string, string, List<Error>> FormatFunction { get; protected set; }

    /// <summary>
    /// Constraint objects represent a rule that is applied to a token; the Function property is the validation function that will be executed on the token's value while the Property is the representation of the constraint as a JProperty.
    /// </summary>
    /// <param name="inputFunction">Function to execute from this constraint. The TValueType in the function is the value being tested; the string is the name of the token in the object being tested.</param>
    /// <param name="inputProperty">JProperty representation of this constraint. Neither name nor value can be null or whitespace.</param>
    /// <param name="constraintErrors">Errors generated while creating this constraint.</param>
    public Constraint(Func<TValueType, string, List<Error>> inputFunction, JProperty inputProperty = null, List<Error> constraintErrors = null)
    {
      BuildConstraint(inputFunction, inputProperty, constraintErrors);
    }

    /// <summary>
    /// Use this overload only if you intend to create a non-Standard constraint type.
    /// Format constraints are applied to the input data before it is cast to another
    /// format; this is especially useful for potentially destructive casts such as
    /// DateTime.
    /// </summary>
    /// <param name="inputFunction">Function to execute from this constraint. The first string argument is the input data value cast to string; the second string argument is the name of the token in the object being tested.</param>
    /// <param name="constraintType">Type of this constraint. Currently, only <see cref="ConstraintType.Format"/> is supported here.</param>
    /// <param name="inputProperty">JProperty representation of this constraint. Neither name nor value can be null or whitespace.</param>
    /// <param name="constraintErrors">Errors generated while creating this constraint.</param>
    public Constraint(Func<string, string, List<Error>> inputFunction, ConstraintType constraintType, JProperty inputProperty = null, List<Error> constraintErrors = null)
    {
      if(constraintType == ConstraintType.Format)
      {
        ConstraintType = ConstraintType.Format;
        if (inputProperty.Exists())
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
        }
        FormatFunction = inputFunction;
        Errors = constraintErrors.Exists() ? constraintErrors : new List<Error>();
      }
      else if (constraintType == ConstraintType.Standard)
      {
        throw new ArgumentException("To construction Standard constraints, use the constructor that does not pass a constraint type.");
      }
    }

    private void BuildConstraint(Func<TValueType, string, List<Error>> inputFunction, JProperty inputProperty, List<Error> constraintErrors)
    {
      if (inputProperty.Exists())
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
      }
      Function = inputFunction;
      Errors = constraintErrors.Exists() ? constraintErrors : new List<Error>();
    }

    public override object GetFunction() => Function;
  }

  public static class Constraints
  {
    #region IComparable Constraints

    /// <summary>
    /// Constrains comparable values with only a lower bound.
    /// </summary>
    /// <typeparam name="TValueType"><see cref="IComparable{TValueType}"/> and <see cref="IFormattable"/> type to check.</typeparam>
    /// <param name="lowerBound"><typeparamref name="TValueType"/> used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed <typeparamref name="TValueType"/> is greater than the provided lower bound.</returns>
    public static Constraint<TValueType> ConstrainValueLowerBound<TValueType>(TValueType lowerBound) where TValueType : IComparable, IComparable<TValueType>, IFormattable
    {
      List<Error> InnerMethod(TValueType inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputValue.CompareTo(lowerBound) < 0)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is less than enforced lower bound {lowerBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod,new JProperty("ConstrainValueLowerBound",lowerBound));
    }

    /// <summary>
    /// Constrains comparable values with only an upper bound.
    /// </summary>
    /// <typeparam name="TValueType"><see cref="IComparable{TValueType}"/> and <see cref="IFormattable"/> type to check.</typeparam>
    /// <param name="upperBound"><typeparamref name="TValueType"/> used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed <typeparamref name="TValueType"/> is greater than the provided lower bound.</returns>
    public static Constraint<TValueType> ConstrainValueUpperBound<TValueType>(TValueType upperBound) where TValueType : IComparable, IComparable<TValueType>, IFormattable
    {
      List<Error> InnerMethod(TValueType inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputValue.CompareTo(upperBound) > 0)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is greater than enforced upper bound {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, new JProperty("ConstrainValueUpperBound", upperBound));
    }

    /// <summary>
    /// Constrains comparable values with a lower bound and an upper bound.
    /// </summary>
    /// <typeparam name="TValueType"><see cref="IComparable{TValueType}"/> and <see cref="IFormattable"/> type to check.</typeparam>
    /// <param name="lowerBound"><typeparamref name="TValueType"/> used as the lower bound in the returned function, inclusive.</param>
    /// <param name="upperBound"><typeparamref name="TValueType"/> used as the upper bound in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/> if upperBound is greater than lowerBound.</exception>
    /// <returns>Function checking to ensure that the value of the passed <typeparamref name="TValueType"/> is greater than the provided lower bound.</returns>
    public static Constraint<TValueType> ConstrainValue<TValueType>(TValueType lowerBound, TValueType upperBound) where TValueType : IComparable, IComparable<TValueType>, IFormattable
    {
      if (lowerBound.CompareTo(upperBound) > 0)
      {
        throw new ArgumentException($"ConstrainValue lower bound must be less than or equal to upper bound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<Error> InnerMethod(TValueType inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        if (inputValue.CompareTo(lowerBound) < 0 || inputValue.CompareTo(upperBound) > 0)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputValue} is invalid. Value must be greater than or equal to {lowerBound} and less than or equal to {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, new JProperty("ConstrainValue", lowerBound + ", " + upperBound));
    }

    /// <summary>
    /// Constrains comparable values using any number of provided domains as tuples in format (lowerBound, upperBound)
    /// </summary>
    /// <typeparam name="TValueType"><see cref="IComparable{TValueType}"/> and <see cref="IFormattable"/> type to check.</typeparam>
    /// <param name="domains">(<typeparamref name="TValueType"/>, <typeparamref name="TValueType"/>) tuples in format (lowerBound, upperBound) used as possible domains in the returned function, inclusive.</param>
    /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/> if the first item of any passed tuple is greater than the second item.</exception>
    /// <returns>Function checking to ensure that the value of the passed <typeparamref name="TValueType"/> is within at least one of the provided domains.</returns>
    public static Constraint<TValueType> ConstrainValue<TValueType>(params (TValueType, TValueType)[] domains) where TValueType : IComparable, IComparable<TValueType>, IFormattable
    {
      foreach ((TValueType, TValueType) domain in domains)
      {
        if (domain.Item1.CompareTo(domain.Item2) > 0)
        {
          throw new ArgumentException($"Domain {domain} is invalid: Item 1 (lower bound) must be less than Item 2 (upper bound)");
        }
      }
      List<Error> InnerMethod(TValueType inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        bool matchesAtLeastOne = false;
        foreach ((TValueType, TValueType) domain in domains)
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
      return new Constraint<TValueType>(InnerMethod, new JProperty("ConstrainValue", JArray.FromObject(domains.Select(x => "(" + x.Item1 + ", " + x.Item2 + ")"))));
    }

    /// <summary>
    /// Constrains the number of digits a double has after the decimal.
    /// </summary>
    /// <param name="upperBound">Maximum number of digits after the decimal.</param>
    /// <returns>A new <see cref="Constraint{double}"/> containing a method to constrain decimal digits.</returns>
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
      // TODO: Optimize collection for searching?
      // Switch case to reroute types to appropriate overloads?
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
    /// <param name="forbiddenSubstrings">Characters that cannot occur in the input string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if no chars are passed.</exception>
    /// <returns>Function that ensures the input string does not contain any of the passed characters.</returns>
    public static Constraint<string> ForbidSubstrings(params string[] forbiddenSubstrings)
    {
      if (forbiddenSubstrings.Length == 0)
      {
        throw new ArgumentException("ForbidSubstrings must have at least one parameter.");
      }
      List<Error> InnerMethod(string inputString, string inputName)
      {
        List<Error> internalErrorList = new();
        bool containsForbidden = false;
        foreach (string forbiddenCharacter in forbiddenSubstrings)
        {
          if (inputString.IndexOf(forbiddenCharacter) != -1)
          {
            containsForbidden = true;
          }
        }
        if (containsForbidden)
        {
          internalErrorList.Add(new Error($"Token {inputName} with value {inputString} contains at least one of a forbidden substring: {string.Join(" ", forbiddenSubstrings)}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty("ForbidSubstrings",JArray.FromObject(forbiddenSubstrings.ToArray())));
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
          try
          {
            internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraints));
          }
          catch
          {
            internalErrorList.Add(new Error($"Value {token} in array {inputName} is an incorrect type. Expected value type: {typeof(TElementType).Name}"));
            return internalErrorList;
          }
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

    #region Datetime Constraints

    /// <summary>
    /// Ensures a DateTime value follows at least one of the passed formats.
    /// </summary>
    /// <param name="formats">Formats in the DateTime Custom Format Specifier format; e.g., "yyyy-MM-dd", "ddd MMMM, yyyy"</param>
    /// <returns></returns>
    public static Constraint<DateTime> ConstrainDateTimeFormat(params string[] formats)
    {
      List<Error> InnerMethod(string inputValue, string inputName)
      {
        List<Error> internalErrorList = new();

        bool atLeastOneSuccess = false;

        foreach(string format in formats)
        {
          if(DateTime.TryParseExact(inputValue, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
          {
            atLeastOneSuccess = true;
            break;
          }
        }
        
        if(!atLeastOneSuccess)
        {
          internalErrorList.Add(new Error($"Input {inputName} with value {inputValue} is not in a valid DateTime format. Valid DateTime formats: {formats.Join(", ")}"));
        }

        return internalErrorList;
      }
      return new Constraint<DateTime>(InnerMethod, ConstraintType.Format, new JProperty("ConstrainDateTimeFormat", JArray.FromObject(formats)));
    }

    #endregion

    #region Metaconstraints

    /// <summary>
    /// A logic constraint that will check the passed value against all
    /// <paramref name="constraints"/> and return the <see cref="List{T}"/> of <see cref="Error"/>
    /// of the first constraint that does not generate fatal errors, or the
    /// <see cref="List{T}"/> of <see cref="Error"/> of all constraints combined if none pass.
    /// </summary>
    /// <typeparam name="TValueType">Value type that will be checked with
    /// constraints applying to said value type.</typeparam>
    /// <param name="constraints">Constraints to check; if at least one passes,
    /// the value will be considered valid.</param>
    /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/>
    /// if fewer than two constraints are passed.</exception>
    /// <returns>A function that returns the <see cref="List{T}"/> of <see cref="Error"/> resulting
    /// from checking whether or not the passed <typeparamref name="TValueType"/> passes
    /// at least one of <paramref name="constraints"/></returns>
    public static Constraint<TValueType> MatchAnyConstraint<TValueType>(params Constraint<TValueType>[] constraints)
    {
      if(constraints.Length < 2)
      {
        throw new ArgumentException("MatchAnyConstraint requires at least 2 constraint arguments.");
      }
      List<Error> InnerFunction(TValueType inputValue, string inputName)
      {
        List<Error> internalErrorList = new();
        List<Error>[] constraintResults = new List<Error>[constraints.Length];
        for (int i = constraints.Length; i-- > 0;)
        {
          List<Error> constraintErrorList = constraints[i].Function(inputValue, inputName);
          if (constraintErrorList.AnyFatal())
          {
            constraintResults[i] = constraintErrorList;
          }
          else
          {
            if (constraintErrorList.Count > 0)
            {
              internalErrorList.Add(new Error($"{inputName} passed at least one constraint in Any clause, but generated non-fatal errors.", Severity.Info));
              internalErrorList.AddRange(constraintErrorList);
              internalErrorList.Add(new Error($"{inputName} Any clause output complete.", Severity.Info));
            }
            return internalErrorList;
          }
        }
        internalErrorList.Add(new Error($"{inputName} Any clause generated fatal errors. At least one set of errors must be avoided.", Severity.Info));
        foreach (List<Error> errorList in constraintResults)
        {
          internalErrorList.AddRange(errorList);
        }
        internalErrorList.Add(new Error($"{inputName} Any clause output complete.", Severity.Info));
        return internalErrorList;
      }
      JObject constraintObject = new();
      foreach (Constraint<TValueType> constraint in constraints)
      {
        constraintObject.Add(constraint.Property);
      }
      return new Constraint<TValueType>(InnerFunction, new JProperty("Any", constraintObject));
    }

    #endregion
  }
}
