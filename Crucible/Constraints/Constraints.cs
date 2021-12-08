using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SchemaForge.Crucible.Extensions;
using SchemaForge.Crucible.Utilities;

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Indicates the type of the constraint; Standard constraints apply to values
  /// that have been cast to the corresponding TValueType of Constraint whereas
  /// Format constraints are applied to those values cast to strings instead.
  /// </summary>
  public enum ConstraintType
  {
    /// <summary>
    /// Indicates that this constraint applies to casted values.
    /// </summary>
    Standard,
    /// <summary>
    /// Indicates that this constraint applies to the string version of the <see cref="Field"/> value.
    /// </summary>
    Format
  }

  /// <summary>
  /// An object that represents a rule that a <see cref="Field"/> value must follow. When
  /// passed to a <see cref="Field"/>, the passed Function
  /// will be executed on the value corresponding with the <see cref="Field"/>.
  /// </summary>
  public abstract class Constraint
  {
    /// <summary>
    /// JProperty representation of this constraint. Will be used when saving a <see cref="Schema"/> to Json.
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
    public List<SchemaError> Errors { get; protected set; }

    /// <summary>
    /// Gives the type of the constraint; <see cref="ConstraintType.Format"/> constraints are applied to the original <see cref="Field"/>
    /// value cast to string while <see cref="ConstraintType.Standard"/> constraints are applied after casting to the required type.
    /// </summary>
    public ConstraintType ConstraintType { get; protected set; } = ConstraintType.Standard;
  }

  /// <summary>
  /// An object that represents a rule that a <see cref="Field"/> value must follow. When
  /// passed to a <see cref="Field"/>, the <see cref="Function"/>
  /// will be executed on the value corresponding with the <see cref="Field"/>.
  /// </summary>
  /// <typeparam name="TValueType">Type of the constraint; must match up with the <see cref="Field"/> to which the Constraint is being passed.</typeparam>
  public class Constraint<TValueType> : Constraint
  {
    /// <summary>
    /// Function that will be applied by the constraint if this is a <see cref="ConstraintType.Standard"/> constraint.
    /// </summary>
    public Func<TValueType, string, List<SchemaError>> Function { get; protected set; }

    /// <summary>
    /// Function that will be applied by the constraint if this is a <see cref="ConstraintType.Format"/> constraint.
    /// </summary>
    public Func<string, string, List<SchemaError>> FormatFunction { get; protected set; }

    /// <summary>
    /// Constraint objects represent a rule that is applied to a <see cref="Field"/>; <paramref name="inputFunction"/> is the validation function that will be executed on the <see cref="Field"/>'s value while the <paramref name="inputProperty"/> is the representation of the constraint as a <see cref="JProperty"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Throws <see cref="ArgumentNullException"/> if <paramref name="inputProperty"/> name or value is null or empty.</exception>
    /// <param name="inputFunction">Function to execute from this <see cref="Constraint"/>. The TValueType in the function is the value being tested; the string is the name of the <see cref="Field"/> in the object being tested.</param>
    /// <param name="inputProperty">JProperty representation of this <see cref="Constraint"/>. Neither name nor value can be null or whitespace.</param>
    /// <param name="constraintErrors">Errors generated while creating this <see cref="Constraint"/>.</param>
    public Constraint(Func<TValueType, string, List<SchemaError>> inputFunction, JProperty inputProperty = null, List<SchemaError> constraintErrors = null)
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
      Errors = constraintErrors.Exists() ? constraintErrors : new List<SchemaError>();
    }

    /// <summary>
    /// Use this overload only if you intend to create a non-Standard constraint type.
    /// Format constraints are applied to the input data before it is cast to another
    /// format; this is especially useful for potentially destructive casts such as
    /// DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Throws <see cref="ArgumentNullException"/> if <paramref name="inputProperty"/>
    /// name or value is null or empty or if attempting to pass <see cref="ConstraintType.Standard"/> to this overload.</exception>
    /// <param name="inputFunction">Function to execute from this constraint.
    /// The first string argument is the input data value cast to string; the
    /// second string argument is the name of the <see cref="Field"/> in the object being tested.</param>
    /// <param name="constraintType">Type of this constraint. Currently, only <see cref="ConstraintType.Format"/> is supported here.</param>
    /// <param name="inputProperty">JProperty representation of this constraint. Neither name nor value can be null or whitespace.</param>
    /// <param name="constraintErrors">Errors generated while creating this constraint.</param>
    public Constraint(Func<string, string, List<SchemaError>> inputFunction, ConstraintType constraintType, JProperty inputProperty = null, List<SchemaError> constraintErrors = null)
    {
      if (constraintType == ConstraintType.Format)
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
        Errors = constraintErrors.Exists() ? constraintErrors : new List<SchemaError>();
      }
      else if (constraintType == ConstraintType.Standard)
      {
        throw new ArgumentException("To construction Standard constraints, use the constructor that does not pass a constraint type.");
      }
    }

    /// <summary>
    /// Returns the <see cref="Function"/> of this <see cref="Constraint"/> as an object.
    /// </summary>
    /// <returns>The <see cref="Function"/> boxed in an object.</returns>
    public override object GetFunction() => Function;
  }

  /// <summary>
  /// Contains the definitions of all prepackaged Constraint-generating functions.
  /// </summary>
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
      List<SchemaError> InnerMethod(TValueType inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputValue.CompareTo(lowerBound) < 0)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputValue} is less than enforced lower bound {lowerBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod,new JProperty(nameof(ConstrainValueLowerBound), lowerBound));
    }

    /// <summary>
    /// Constrains comparable values with only an upper bound.
    /// </summary>
    /// <typeparam name="TValueType"><see cref="IComparable{TValueType}"/> and <see cref="IFormattable"/> type to check.</typeparam>
    /// <param name="upperBound"><typeparamref name="TValueType"/> used as the lower bound in the returned function, inclusive.</param>
    /// <returns>Function checking to ensure that the value of the passed <typeparamref name="TValueType"/> is greater than the provided lower bound.</returns>
    public static Constraint<TValueType> ConstrainValueUpperBound<TValueType>(TValueType upperBound) where TValueType : IComparable, IComparable<TValueType>, IFormattable
    {
      List<SchemaError> InnerMethod(TValueType inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputValue.CompareTo(upperBound) > 0)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputValue} is greater than enforced upper bound {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, new JProperty(nameof(ConstrainValueUpperBound), upperBound));
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
      List<SchemaError> InnerMethod(TValueType inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputValue.CompareTo(lowerBound) < 0 || inputValue.CompareTo(upperBound) > 0)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputValue} is invalid. Value must be greater than or equal to {lowerBound} and less than or equal to {upperBound}"));
          return internalErrorList;
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, new JProperty(nameof(ConstrainValue), lowerBound + ", " + upperBound));
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
      List<SchemaError> InnerMethod(TValueType inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        foreach ((TValueType, TValueType) domain in domains)
        {
          if (inputValue.CompareTo(domain.Item1) > 0 && inputValue.CompareTo(domain.Item2) < 0)
          {
            return internalErrorList; // Return empty error list if a match is found.
          }
        }
        internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputValue} is invalid. Value must fall within one of the following domains, inclusive: {string.Join(" ", domains.Select(x => x.ToString()))}"));
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, new JProperty(nameof(ConstrainValue), JArray.FromObject(domains.Select(x => "(" + x.Item1 + ", " + x.Item2 + ")"))));
    }

    /// <summary>
    /// Constrains the number of digits a number has after the decimal.
    /// </summary>
    /// <param name="upperBound">Maximum number of digits after the decimal.</param>
    /// <returns>A new <see cref="Constraint"/> containing a method to constrain decimal digits.</returns>
    public static Constraint<TValueType> ConstrainDigits<TValueType>(int upperBound) where TValueType : struct,
          IComparable,
          IComparable<TValueType>,
          IConvertible,
          IEquatable<TValueType>,
          IFormattable
    {
      List<SchemaError> InnerMethod(string inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if(inputValue.Contains('.'))
        {
          string[] splitDouble = inputValue.Split('.');
          if(splitDouble[1].Length > upperBound)
          {
            internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputValue} is invalid. Value can have no more than {upperBound} digits after the decimal."));
          }
        }
        return internalErrorList;
      }
      return new Constraint<TValueType>(InnerMethod, ConstraintType.Format, new JProperty(nameof(ConstrainDigits), upperBound));
    }

    #endregion

    #region String Constraints

    /// <summary>
    /// Ensure that the passed <typeparamref name="T"/> matches at least one of the values provided in <paramref name="acceptableValues"/>.
    /// </summary>
    /// <param name="acceptableValues">List of values used to build the returned function.</param>
    /// <returns>Function checking to ensure that the value of the passed item is one of <paramref name="acceptableValues"/>.</returns>
    public static Constraint<T> AllowValues<T>(params T[] acceptableValues)
    {
      // TODO: Optimize collection for searching?
      // Switch case to reroute types to appropriate overloads?
      List<SchemaError> InnerMethod(T inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (!acceptableValues.Contains(inputValue)) //Returns false if inputValue is not in provided list
        {
          internalErrorList.Add(new SchemaError($"Input {inputName} with value {inputValue} is not valid. Valid values: {string.Join(", ", acceptableValues)}")); // Tell the user what's wrong and how to fix it.
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod,new JProperty(nameof(AllowValues),JArray.FromObject(acceptableValues)));
    }
    /// <summary>
    /// Ensure that the passed <typeparamref name="T"/> does not match any of the values provided in <paramref name="forbiddenValues"/>.
    /// </summary>
    /// <param name="forbiddenValues">List of values used to build the returned function.</param>
    /// <returns>Function checking to ensure that the value of the passed item is not any of <paramref name="forbiddenValues"/>.</returns>
    public static Constraint<T> ForbidValues<T>(params T[] forbiddenValues)
    {
      // TODO: Optimize collection for searching?
      // Switch case to reroute types to appropriate overloads?
      List<SchemaError> InnerMethod(T inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (forbiddenValues.Contains(inputValue)) //Returns false if inputValue is not in provided list
        {
          internalErrorList.Add(new SchemaError($"Input {inputName} with value {inputValue} is invalid. Value must not be any of: {string.Join(", ", forbiddenValues)}"));
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty(nameof(ForbidValues), JArray.FromObject(forbiddenValues)));
    }
    /// <summary>
    /// Ensures that the <see cref="Field"/> value is an exact match to at least one of the passed <paramref name="patterns"/>.
    /// </summary>
    /// <param name="patterns">Valid Regex pattern(s) used in the returned function.</param>
    /// <returns>Function checking to ensure that the string value isan exact match to at least one of the passed <paramref name="patterns"/>.</returns>
    public static Constraint<string> ConstrainStringWithRegexExact(params Regex[] patterns)
    {
      List<SchemaError> InnerMethod(string inputString, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        foreach (Regex pattern in patterns)
        {
          if(pattern.Match(inputString).Length == inputString.Length)
          {
            return internalErrorList;  // Return empty error list if a match is found.
          }
        }
        if (patterns.Length == 1)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} is not an exact match to pattern {patterns[0]}"));
        }
        else
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} is not an exact match to any pattern: {string.Join<Regex>(" ", patterns)}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty(nameof(ConstrainStringWithRegexExact),JArray.FromObject(patterns)));
    }

    /// <summary>
    /// Constrains length of a string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <returns>Function that ensures the length of a string is at least <paramref name="lowerBound"/>.</returns>
    public static Constraint<string> ConstrainStringLengthLowerBound(int lowerBound)
    {
      List<SchemaError> InnerMethod(string inputString, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputString.Length < lowerBound)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} must have a length of at least {lowerBound}. Actual length: {inputString.Length}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod,new JProperty(nameof(ConstrainStringLengthLowerBound), lowerBound));
    }

    /// <summary>
    /// Constrains length of a string value.
    /// </summary>
    /// <param name="lowerBound">Minimum length of passed string.</param>
    /// <param name="upperBound">Maximum length of passed string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if <paramref name="lowerBound"/> is greater than <paramref name="upperBound"/>.</exception>
    /// <returns>Function that ensures the length of a string is at least <paramref name="lowerBound"/> and at most <paramref name="upperBound"/>.</returns>
    public static Constraint<string> ConstrainStringLength(int lowerBound, int upperBound)
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"{nameof(ConstrainStringLength)} lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: {upperBound}");
      }
      List<SchemaError> InnerMethod(string inputString, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputString.Length < lowerBound || inputString.Length > upperBound)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} must have a length of at least {lowerBound} and at most {upperBound}. Actual length: {inputString.Length}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty(nameof(ConstrainStringLength), lowerBound + ", " + upperBound));
    }

    /// <summary>
    /// Constrains length of a string value.
    /// </summary>
    /// <param name="upperBound">Maximum length of passed string.</param>
    /// <returns>Function that ensures the length of a string is less than or equal to <paramref name="upperBound"/>.</returns>
    public static Constraint<string> ConstrainStringLengthUpperBound(int upperBound)
    {
      List<SchemaError> InnerMethod(string inputString, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        if (inputString.Length > upperBound)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} must have a length no longer than {upperBound}. Actual length: {inputString.Length}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty(nameof(ConstrainStringLengthUpperBound), upperBound));
    }

    /// <summary>
    /// Ensures that the <see cref="Field"/>'s value does not contain any of <paramref name="forbiddenSubstrings"/>.
    /// </summary>
    /// <param name="forbiddenSubstrings">Strings that cannot occur in the input string.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if no strings are passed.</exception>
    /// <returns>Function that ensures the input string does not contain any of <paramref name="forbiddenSubstrings"/>.</returns>
    public static Constraint<string> ForbidSubstrings(params string[] forbiddenSubstrings)
    {
      if (forbiddenSubstrings.Length == 0)
      {
        throw new ArgumentException($"{nameof(ForbidSubstrings)} must have at least one parameter.");
      }
      List<SchemaError> InnerMethod(string inputString, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        bool containsForbidden = false;
        foreach (string forbiddenSubstring in forbiddenSubstrings)
        {
          if (inputString.IndexOf(forbiddenSubstring) != -1)
          {
            containsForbidden = true;
            break;
          }
        }
        if (containsForbidden)
        {
          internalErrorList.Add(new SchemaError($"Field {inputName} with value {inputString} contains at least one of a forbidden substring: {string.Join(" ", forbiddenSubstrings)}"));
        }
        return internalErrorList;
      }
      return new Constraint<string>(InnerMethod, new JProperty(nameof(ForbidSubstrings),JArray.FromObject(forbiddenSubstrings.ToArray())));
    }

    #endregion

    #region JArray Constraints

    /// <summary>
    /// Constrains the number of items in an <see cref="IEnumerable"/> object with a lower bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target <see cref="IEnumerable"/>.</param>
    /// <returns>Constraint ensuring an enumerable has at least <paramref name="lowerBound"/> items.</returns>
    public static Constraint<T> ConstrainCollectionCountLowerBound<T>(int lowerBound) where T: IEnumerable
    {
      List<SchemaError> InnerMethod(T inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        int count = inputArray.Count();
        if (count < lowerBound)
        {
          internalErrorList.Add(new SchemaError($"Collection {inputName} contains {count} values, but must contain at least {lowerBound} values."));
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty(nameof(ConstrainCollectionCountLowerBound), lowerBound));
    }

    /// <summary>
    /// Constrains the number of items in an <see cref="IEnumerable"/> with a lower bound and upper bound.
    /// </summary>
    /// <param name="lowerBound">Minimum number of items in the target <see cref="IEnumerable"/>.</param>
    /// <param name="upperBound">Maximum number of items in the target <see cref="IEnumerable"/>.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if <paramref name="lowerBound"/> is greater than <paramref name="upperBound"/>.</exception>
    /// <returns>Function ensuring a JArray has at least <paramref name="lowerBound"/> and at most <paramref name="upperBound"/> items.</returns>
    public static Constraint<T> ConstrainCollectionCount<T>(int lowerBound, int upperBound) where T: IEnumerable
    {
      if (lowerBound > upperBound)
      {
        throw new ArgumentException($"{nameof(ConstrainCollectionCount)} lowerBound must be less than or equal to upperBound. Passed lowerBound: {lowerBound} Passed upperBound: " + upperBound);
      }
      List<SchemaError> InnerMethod(T inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        int count = inputArray.Count();
        if (count < lowerBound || count > upperBound)
        {
          internalErrorList.Add(new SchemaError($"Collection {inputName} contains {count} values, but must contain between {lowerBound} and {upperBound} values."));
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty(nameof(ConstrainCollectionCount), lowerBound + ", " + upperBound));
    }

    /// <summary>
    /// Constrains the number of items in an <see cref="IEnumerable"/> object with an upper bound.
    /// </summary>
    /// <param name="upperBound">Minimum number of items in the target <see cref="IEnumerable"/>.</param>
    /// <returns>Constraint ensuring an enumerable has at least <paramref name="upperBound"/> items.</returns>
    public static Constraint<T> ConstrainCollectionCountUpperBound<T>(int upperBound) where T : IEnumerable
    {
      List<SchemaError> InnerMethod(T inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        int count = inputArray.Count();
        if (count > upperBound)
        {
          internalErrorList.Add(new SchemaError($"Collection {inputName} contains {count} values, but must contain fewer than {upperBound} values."));
        }
        return internalErrorList;
      }
      return new Constraint<T>(InnerMethod, new JProperty(nameof(ConstrainCollectionCountUpperBound), upperBound));
    }

    /// <summary>
    /// Encapsulates the process of typechecking and applying all passed constraints.
    /// </summary>
    /// <typeparam name="TValueType">Expected type of the <see cref="Field"/> value being evaluated.</typeparam>
    /// <param name="inputToken">Input <see cref="JToken"/>.</param>
    /// <param name="fieldName">Name of the input <see cref="JToken"/>.</param>
    /// <param name="constraints">Constraints to apply to the input <see cref="Field"/> value.</param>
    /// <returns>List{Error} generated by applying all of the constraints.</returns>
    private static List<SchemaError> ApplyConstraintsHelper<TValueType>(JToken inputToken, string fieldName, Constraint<TValueType>[] constraints)
    {
      List<SchemaError> internalErrorList = new List<SchemaError>();
      try
      {
        if (constraints.Exists())
        {
          TValueType castValue = inputToken.Value<TValueType>();
          foreach (Constraint<TValueType> constraint in constraints)
          {
            internalErrorList.AddRange(constraint.Function(castValue, fieldName));
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
    /// Ensures all items in the target <see cref="JArray"/> are of type <typeparamref name="TElementType"/> and pass all provided constraints.
    /// </summary>
    /// <typeparam name="TElementType">Type of all items in the target <see cref="JArray"/>.</typeparam>
    /// <param name="constraints">List of functions to run on all items in the <see cref="JArray"/> individually.</param>
    /// <returns>Function ensuring that all items in the target <see cref="JArray"/> are of type <typeparamref name="TElementType"/> and pass all provided constraints.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType>(params Constraint<TElementType>[] constraints)
    {
      List<SchemaError> InnerMethod(JArray inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        foreach(JToken token in inputArray)
        {
          try
          {
            internalErrorList.AddRange(ApplyConstraintsHelper(token, "in collection " + inputName, constraints));
          }
          catch
          {
            internalErrorList.Add(new SchemaError($"Value {token} in array {inputName} is an incorrect type. Expected value type: {typeof(TElementType).Name}"));
          }
        }
        return internalErrorList;
      }
      JArray constraintArray = new JArray();
      constraintArray.Add(Field.GetConstraintObject(constraints));
      return new Constraint<JArray>(InnerMethod, new JProperty(nameof(ApplyConstraintsToJArray), constraintArray));
    }

    /// <summary>
    /// Ensures all items in the target <see cref="JArray"/> are of type
    /// <typeparamref name="TElementType1"/> or <typeparamref name="TElementType2"/>
    /// and applies all constraints on the type to which the element corresponds.
    /// WARNING: Casts will be attempted IN ORDER. For example, ApplyConstraintsToJArray{string, int}
    /// will NEVER treat the passed value as an int!
    /// </summary>
    /// <typeparam name="TElementType1">First type to check against the <see cref="Field"/>'s value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType2">Second type to check against the <see cref="Field"/>'s value
    /// in the returned function.</typeparam>
    /// <param name="constraintsIfTElementType1">Constraints to execute if cast to
    /// <typeparamref name="TElementType1"/> is successful.</param>
    /// <param name="constraintsIfTElementType2">Constraints to execute if cast to
    /// <typeparamref name="TElementType2"/> is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints.
    /// Can be used in the constructor of a <see cref="Field"/>.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType1,TElementType2>(Constraint<TElementType1>[] constraintsIfTElementType1 = null, Constraint<TElementType2>[] constraintsIfTElementType2 = null)
    {
      List<SchemaError> InnerMethod(JArray inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
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
              internalErrorList.Add(new SchemaError($"Value {token} in collection {inputName} is an incorrect type. Expected one of: {typeof(TElementType1).Name}, {typeof(TElementType2).Name}"));
            }
          }
        }
        return internalErrorList;
      }
      JArray constraintArray = new JArray();
      constraintArray.Add(Field.GetConstraintObject(constraintsIfTElementType1));
      constraintArray.Add(Field.GetConstraintObject(constraintsIfTElementType2));
      return new Constraint<JArray>(InnerMethod, new JProperty(nameof(ApplyConstraintsToJArray), constraintArray));
    }

    /// <summary>
    /// Ensures all items in the target <see cref="JArray"/> are of type
    /// <typeparamref name="TElementType1"/>, or <typeparamref name="TElementType2"/>,
    /// or <typeparamref name="TElementType3"/>
    /// and applies all constraints on the type to which the element corresponds.
    /// WARNING: Casts will be attempted IN ORDER. For example, ApplyConstraintsToJArray{string, int}
    /// will NEVER treat the passed value as an int!
    /// </summary>
    /// <typeparam name="TElementType1">First type to check against the value value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType2">Second type to check against the value value
    /// in the returned function.</typeparam>
    /// <typeparam name="TElementType3">Third type to check against the value value
    /// in the returned function.</typeparam>
    /// <param name="constraintsIfT1">Constraints to execute if cast to
    /// <typeparamref name="TElementType1"/> is successful.</param>
    /// <param name="constraintsIfT2">Constraints to execute if cast to
    /// <typeparamref name="TElementType2"/> is successful.</param>
    /// <param name="constraintsIfT3">Constraints to execute if cast to
    /// <typeparamref name="TElementType3"/> is successful.</param>
    /// <returns>Composite function of the type cast and all passed constraints.
    /// Can be used in the constructor of a <see cref="Field"/>.</returns>
    public static Constraint<JArray> ApplyConstraintsToJArray<TElementType1, TElementType2, TElementType3>(Constraint<TElementType1>[] constraintsIfT1 = null, Constraint<TElementType2>[] constraintsIfT2 = null, Constraint<TElementType3>[] constraintsIfT3 = null)
    {
      List<SchemaError> InnerMethod(JArray inputArray, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
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
                internalErrorList.Add(new SchemaError($"Value {token} in collection {inputName} is an incorrect type. Expected one of: {typeof(TElementType1).Name}, {typeof(TElementType2).Name}, {typeof(TElementType3).Name}"));
              }
            }
          }
        }
        return internalErrorList;
      }
      JArray constraintArray = new JArray();
      constraintArray.Add(Field.GetConstraintObject(constraintsIfT1));
      constraintArray.Add(Field.GetConstraintObject(constraintsIfT2));
      constraintArray.Add(Field.GetConstraintObject(constraintsIfT3));
      return new Constraint<JArray>(InnerMethod, new JProperty(nameof(ApplyConstraintsToJArray), constraintArray));
    }

    #endregion

    #region JObject Constraints

    /// <summary>
    /// Applies a <see cref="Schema"/> to the value of this <see cref="Field"/> with the
    /// <see cref="Schema.Validate{TCollectionType}(TCollectionType, ISchemaTranslator{TCollectionType}, string, bool, bool)"/> method.
    /// </summary>
    /// <param name="inputSchema"><see cref="Schema"/> object to apply to the designated object.</param>
    /// <returns>Function that adds all the <see cref="List{Error}"/> generated by using the <see cref="Schema"/> to validate the passed <see cref="JObject"/></returns>
    public static Constraint<JObject> ApplySchema(Schema inputSchema)
    {
      List<SchemaError> InnerMethod(JObject inputJson, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        internalErrorList.AddRange(inputSchema.Validate(inputJson, new JObjectTranslator(), $"inner object {inputName}"));
        return internalErrorList;
      }
      return new Constraint<JObject>(InnerMethod, new JProperty(nameof(ApplySchema), inputSchema.ToString()));
    }

    /// <summary>
    /// Applies a <see cref="Schema"/> to the value of this <see cref="Field"/> with the
    /// <see cref="Schema.Validate{TCollectionType}(TCollectionType, ISchemaTranslator{TCollectionType}, string, bool, bool)"/> method. 
    /// This overload will apply a different <see cref="Schema"/> to sub-objects based on 
    /// the value of the the specified <paramref name="typeField"/>.
    /// </summary>
    /// <param name="typeField">Name of the <see cref="Field"/> that indicates the <see cref="Schema"/> that should be used for this sub-object.</param>
    /// <param name="typeMap">Dictionary mapping type names to the <see cref="Schema"/> that should be used for each type.</param>
    /// <returns>Function that adds all the <see cref="List{Error}"/> generated by using the <see cref="Schema"/> to validate the passed <see cref="JObject"/></returns>
    public static Constraint<JObject> ApplySchema(string typeField, Dictionary<string, Schema> typeMap)
    {
      List<SchemaError> InnerMethod(JObject inputJson, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        string type = inputJson[typeField].ToString();
        if(typeMap.ContainsKey(type))
        {
          internalErrorList.AddRange(typeMap[type].Validate(inputJson, new JObjectTranslator(), $"inner object {inputName}"));
        }
        else
        {
          internalErrorList.Add(new SchemaError($"Type {type} is not a valid type for field {typeField}"));
        }
        return internalErrorList;
      }
      return new Constraint<JObject>(InnerMethod, new JProperty(nameof(ApplySchema), typeMap.ToString()));
    }

    #endregion

    #region Datetime Constraints

    /// <summary>
    /// Ensures a <see cref="DateTime"/> value follows at least one of the passed
    /// Custom Format Specifier <paramref name="formats"/>.
    /// Including this <see cref="Constraint"/> will update the <see cref="DateTime"/>
    /// parser in <see cref="Conversions"/>, allowing the parser to recognize
    /// <see cref="DateTime"/>s in the provided formats.
    /// </summary>
    /// <param name="formats">Formats in the <see cref="DateTime"/> Custom Format
    /// Specifier format; e.g., "yyyy-MM-dd", "ddd MMMM, yyyy"</param>
    /// <returns>A function ensuring that the <see cref="Field"/>'s value is in one of the provided
    /// Custom Format Specifier <paramref name="formats"/>.</returns>
    public static Constraint<DateTime> ConstrainDateTimeFormat(params string[] formats)
    {
      foreach(string format in formats)
      {
        Conversions.RegisterDateTimeFormat(format);
      }
      List<SchemaError> InnerMethod(string inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        foreach(string format in formats)
        {
          if(DateTime.TryParseExact(inputValue, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
          {
            return internalErrorList;
          }
        }
        
        internalErrorList.Add(new SchemaError($"Input {inputName} with value {inputValue} is not in a valid DateTime format. Valid DateTime formats: {formats.Join(", ")}"));

        return internalErrorList;
      }
      return new Constraint<DateTime>(InnerMethod, ConstraintType.Format, new JProperty(nameof(ConstrainDateTimeFormat), JArray.FromObject(formats)));
    }

    #endregion

    #region Metaconstraints

    /// <summary>
    /// A logic constraint that will check the passed value against all
    /// <paramref name="constraints"/> and return the <see cref="List{T}"/> of <see cref="SchemaError"/>
    /// of the first constraint that does not generate fatal errors, or the
    /// <see cref="List{T}"/> of <see cref="SchemaError"/> of all constraints combined if none pass.
    /// </summary>
    /// <typeparam name="TValueType">Value type that will be checked with
    /// constraints applying to said value type.</typeparam>
    /// <param name="constraints">Constraints to check; if at least one passes,
    /// the value will be considered valid.</param>
    /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/>
    /// if fewer than two constraints are passed.</exception>
    /// <returns>A function that returns the <see cref="List{T}"/> of <see cref="SchemaError"/> resulting
    /// from checking whether or not the passed <typeparamref name="TValueType"/> passes
    /// at least one of <paramref name="constraints"/></returns>
    public static Constraint<TValueType> MatchAnyConstraint<TValueType>(params Constraint<TValueType>[] constraints)
    {
      if(constraints.Length < 2)
      {
        throw new ArgumentException($"{nameof(MatchAnyConstraint)} requires at least 2 constraint arguments.");
      }
      List<SchemaError> InnerFunction(TValueType inputValue, string inputName)
      {
        List<SchemaError> internalErrorList = new List<SchemaError>();
        List<SchemaError>[] constraintResults = new List<SchemaError>[constraints.Length];
        for (int i = constraints.Length; i-- > 0;)
        {
          List<SchemaError> constraintErrorList = constraints[i].Function(inputValue, inputName);
          if (constraintErrorList.AnyFatal())
          {
            constraintResults[i] = constraintErrorList;
          }
          else
          {
            if (constraintErrorList.Count > 0)
            {
              internalErrorList.Add(new SchemaError($"{inputName} passed at least one constraint in Any clause, but generated non-fatal errors.", Severity.Info));
              internalErrorList.AddRange(constraintErrorList);
              internalErrorList.Add(new SchemaError($"{inputName} Any clause output complete.", Severity.Info));
            }
            return internalErrorList;
          }
        }
        internalErrorList.Add(new SchemaError($"{inputName} Any clause generated fatal errors. At least one set of errors must be avoided.", Severity.Info));
        for(int i=constraintResults.Length;i-- > 0;)
        {
          internalErrorList.AddRange(constraintResults[i]);
        }
        internalErrorList.Add(new SchemaError($"{inputName} Any clause output complete.", Severity.Info));
        return internalErrorList;
      }
      JObject constraintObject = new JObject();
      foreach (Constraint<TValueType> constraint in constraints)
      {
        constraintObject.Add(constraint.Property);
      }
      return new Constraint<TValueType>(InnerFunction, new JProperty(nameof(MatchAnyConstraint), constraintObject));
    }

    #endregion
  }
}
