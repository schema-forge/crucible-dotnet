﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SchemaForge.Crucible.Extensions;
using Newtonsoft.Json.Linq;

namespace SchemaForge.Crucible
{
  public abstract class ConfigToken
  {
    /// <summary>
    /// Name of the token; corresponds to a token present in the JObject that will be validated.
    /// </summary>
    public string TokenName { get; protected set; }

    /// <summary>
    /// String that will be added to the ErrorList of a Schema object as ErrorSeverity.Info when a validation error occurs for this token.
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    /// Contains all the errors generated by validation functions.
    /// </summary>
    public List<Error> ErrorList { get; protected set; } = new();

    /// <summary>
    /// Indicates whether or not DefaultValue is populated. Necessary because DefaultValue is generically typed and cannot be in the abstract class.
    /// </summary>
    public bool ContainsDefaultValue { get; protected set; } = false;

    /// <summary>
    /// If false, null or empty token values are fatal errors. If true, they are warnings instead.
    /// </summary>
    public bool AllowNull { get; protected set; } = false;

    /// <summary>
    /// Represents the constraints on the token as a set of Json objects, one per possible type.
    /// </summary>
    public JArray JsonConstraint { get; protected set; } = new();

    /// <summary>
    /// Indicates whether or not this token is required.
    /// </summary>
    public bool Required { get; protected set; }

    /// <summary>
    /// Converts a ConfigToken into a JProperty of format "<see cref="TokenName"/>":{ "Constraints":<see cref="JsonConstraint"/>, "Description":"<see cref="Description"/>" }
    /// </summary>
    /// <returns>JProperty of format "<see cref="TokenName"/>":{ "Constraints":<see cref="JsonConstraint"/>, "Description":"<see cref="Description"/>"</returns>
    public JProperty ToJProperty() => new(TokenName, new JObject() { { "Constraints", JsonConstraint.Count > 1 ? JsonConstraint : JsonConstraint[0] }, { "Description", Description } });

    /// <summary>
    /// Returns a new <see cref="ConfigToken"/> with an additional type and new constraints added. Used primarily during deserialization.
    /// </summary>
    /// <typeparam name="TNewType">New possible type to add to the ConfigToken.</typeparam>
    /// <param name="newConstraints">Constraints to apply if cast to the new type is successful.</param>
    /// <returns>New <see cref="ConfigToken"/> with all pre-existing types, plus <typeparamref name="TNewType"/></returns>
    public abstract ConfigToken AddNewType<TNewType>(Constraint<TNewType>[] newConstraints);

    /// <summary>
    /// Extracts a token from the given <paramref name="collection"/> using the 
    /// <see cref="ISchemaTranslator{TCollectionType, TValueType}.TryCastToken{TCastType}(TCollectionType, string)"/>
    /// method, with <see cref="TokenName"/> as the passed string. All non-abstract
    /// ConfigTokens have a set of type parameters. The method above will attempt casts
    /// in the order those type parameters are provided; the first successful cast will
    /// result in the corresponding ConstraintsIfTypeN array being applied to the token value.
    /// </summary>
    /// <typeparam name="TCollectionType">Collection from which the token will be
    /// extracted.</typeparam>
    /// <typeparam name="TValueType">The type of elements in the collection.
    /// For example, for a Newtonsoft JObject, this would be JToken.</typeparam>
    /// <param name="collection">Collection to extract a value from.</param>
    /// <param name="translator">Translator object to use when interacting with
    /// the specified collection.</param>
    /// <returns>Bool indicating whether or not any fatal errors were
    /// raised by the constraint functions.</returns>
    public virtual bool Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator)
    {
      if (translator.TokenIsNullOrEmpty(collection, TokenName))
      {
        ErrorList.Add(new Error($"Value of token {TokenName} is null or empty.", AllowNull?Severity.Warning:Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }

    /// <summary>
    /// Inserts this token's DefaultValue into <paramref name="collection"/> using the relevant method from <paramref name="translator"/>.
    /// </summary>
    /// <typeparam name="TCollectionType">Type of collection to perform the operation on.</typeparam>
    /// <param name="collection">Collection to insert the value into.</param>
    /// <param name="translator">Translator used to interpret <typeparamref name="TCollectionType"/></param>
    /// <returns>A new <typeparamref name="TCollectionType"/> <paramref name="collection"/> with the value added.</returns>
    public abstract TCollectionType InsertDefaultValue<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator);

    #region Overrides

    public override string ToString() => TokenName;

    /// <summary>
    /// All equality operators compare the TokenName of two ConfigTokens to determine equality.
    /// </summary>
    /// <param name="obj">The other object to compare.</param>
    /// <returns>Bool indicating if two ConfigTokens have the same name.</returns>
    public override bool Equals(object obj) => this.Equals(obj as ConfigToken);

    /// <summary>
    /// All equality operators compare the TokenName of two ConfigTokens to determine equality.
    /// </summary>
    /// <param name="other">The other token to compare.</param>
    /// <returns>Bool indicating if two ConfigTokens have the same name.</returns>
    public bool Equals(ConfigToken other) => other.Exists() && this.TokenName == other.TokenName;

    /// <summary>
    /// Sets the HashCode of a ConfigToken to the HashCode of its TokenName.
    /// </summary>
    /// <returns>HashCode of TokenName string.</returns>
    public override int GetHashCode() => TokenName.GetHashCode();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets JObject representation of the type and all other constraints.
    /// </summary>
    /// <typeparam name="TValueType">Type of all constraints.</typeparam>
    /// <param name="constraints">Array of constraints that have been applied by <see cref="ApplyConstraints{TValueType}"/>.</param>
    /// <returns>JObject representation of the constraints applied to the token.</returns>
    public static JObject GetConstraintObject<TValueType>(Constraint<TValueType>[] constraints)
    {
      JObject constraintObject = new();
      try
      {
        if (typeof(TValueType).ToString().Contains("Nullable"))
        {
          constraintObject.Add("Type", ShippingAndReceiving.TypeMap(typeof(TValueType).GenericTypeArguments[0].Name));
        }
        else
        {
          constraintObject.Add("Type", ShippingAndReceiving.TypeMap(typeof(TValueType).Name));
        }
      }
      catch
      {
        throw new ArgumentException($"Attempted to pass unsupported type {typeof(TValueType).Name}\nSupported types: {ShippingAndReceiving.GetSupportedTypes().Join(", ")}\nIf needed, use AddSupportedType to add a new type to the recognized types. This will enable SchemaForge to recognize this token type for your project.");
      }
      if (constraints.Exists())
      {
        foreach (Constraint<TValueType> constraint in constraints)
        {
          if(constraint.Property.Exists())
          {
            constraintObject.Add(constraint.Property);
          }
        }
      }
      return constraintObject;
    }

    protected void BuildConfigTokenCore(string inputName, string inputHelpString, bool required, bool allowNull)
    {
      if (inputName.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputName));
      }
      Required = required;
      TokenName = inputName;
      Description = inputHelpString;
      AllowNull = allowNull;
    }

    /// <summary>
    /// Takes the castResult from a call to an ISchemaTranslator's
    /// TryCastValue function, then applies constraints if the cast
    /// succeeded or returns false if it did not.
    /// </summary>
    /// <typeparam name="TValueType">Type that the collection member was cast to.</typeparam>
    /// <param name="castResult">Token value cast to TValueType from TryCastToken.</param>
    /// <param name="constraints">Constraints to apply.</param>
    protected void InternalValidate<TValueType>(TValueType castResult, List<Constraint<TValueType>> constraints)
    {
      if(constraints.Exists())
      {
        foreach (Constraint<TValueType> constraint in constraints)
        {
          ErrorList.AddRange(constraint.Function(castResult, TokenName));
        }
      }
    }

    /// <summary>
    /// Takes the castResult from a call to an ISchemaTranslator's
    /// TryCastValue function to string, then applies format constraints.
    /// </summary>
    /// <typeparam name="TValueType">Type that the collection member was originally cast to.</typeparam>
    /// <param name="castResult">Tuple that indicates whether the cast succeeded and contains the cast result.</param>
    /// <param name="constraints">Constraints to apply if cast succeeded.</param>
    protected void InternalValidateFormat<TValueType>(string castResult, List<Constraint<TValueType>> constraints)
    {
      if (constraints.Exists())
      {
        foreach (Constraint<TValueType> constraint in constraints)
        {
          ErrorList.AddRange(constraint.FormatFunction(castResult, TokenName));
        }
      }
    }

    #endregion
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
  /// All passed types must be unique.
  /// WARNING: Casts will be attempted IN ORDER. For example,
  /// ConfigToken{string, int} will NEVER treat the passed token as an int!
  /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
  /// </summary>
  /// <typeparam name="Type1">The 1st possible value type of this token.</typeparam>
  public class ConfigToken<Type1> : ConfigToken
  {
    /// <summary>
    /// If DefaultValue is set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type1>> FormatConstraintsIfType1 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Constraint<Type1>[] constraintsIfType1 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, required, allowNull);
      BuildConstraints(constraintsIfType1);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputDescription is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null)
    {
      Type[] typeArray = GetType().GetGenericArguments();
      if (typeArray.Distinct().Count() != typeArray.Length)
      {
        throw new ArgumentException($"ConfigToken for {TokenName} contains duplicate Type arguments: {string.Join(", ", typeArray.Select(x => x.Name))}");
      }
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      ConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type1>>();
      FormatConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type1>>();
    }

    #endregion

    /// <summary>
    /// Returns a new <see cref="ConfigToken"/> with an additional type and new constraints added. Used primarily during deserialization.
    /// </summary>
    /// <exception cref="ArgumentException">If this <see cref="ConfigToken"/> already has <typeparamref name="TNewType"/></exception>
    /// <typeparam name="TNewType">New possible type to add to the ConfigToken.</typeparam>
    /// <param name="newConstraints">Constraints to apply if cast to the new type is successful.</param>
    /// <returns>New <see cref="ConfigToken{Type1,TNewType}"/></returns>
    public override ConfigToken AddNewType<TNewType>(Constraint<TNewType>[] newConstraints = null)
    {
      return GetType().GetGenericArguments().Contains(typeof(TNewType))
        ? throw new ArgumentException($"ConfigToken already has type {typeof(TNewType).Name}.")
        : DefaultValue.Exists()
        ? new ConfigToken<Type1, TNewType>(TokenName, Description, DefaultValue, ConstraintsIfType1.ToArray(), newConstraints, AllowNull)
        : new ConfigToken<Type1, TNewType>(TokenName, Description, ConstraintsIfType1.ToArray(), newConstraints, Required, AllowNull);
    }

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed collection item.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator)
    {
      base.Validate(collection, translator);
      if (translator.TryCastToken(collection, TokenName, out Type1 newValue1))
      {
        InternalValidate(newValue1, ConstraintsIfType1);
        if (FormatConstraintsIfType1.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType1);
        }
      }
      else
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
  /// All passed types must be unique.
  /// WARNING: Casts will be attempted IN ORDER. For example,
  /// ConfigToken{string, int} will NEVER treat the passed token as an int!
  /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
  /// </summary>
  /// <typeparam name="Type1">The 1st possible value type of this token.</typeparam>
  /// <typeparam name="Type2">The 2nd possible value type of this token.</typeparam>
  public class ConfigToken<Type1, Type2> : ConfigToken
  {
    /// <summary>
    /// If DefaultValue is set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type1>> FormatConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type2>> ConstraintsIfType2 { get; protected set; } = new();
    public List<Constraint<Type2>> FormatConstraintsIfType2 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, required, allowNull);
      BuildConstraints(constraintsIfType1, constraintsIfType2);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputDescription is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1, constraintsIfType2);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null)
    {
      Type[] typeArray = GetType().GetGenericArguments();
      if (typeArray.Distinct().Count() != typeArray.Length)
      {
        throw new ArgumentException($"ConfigToken for {TokenName} contains duplicate Type arguments: {string.Join(", ", typeArray.Select(x => x.Name))}");
      }
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType2));
      ConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type1>>();
      FormatConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type1>>();
      ConstraintsIfType2 = constraintsIfType2.Exists() ? constraintsIfType2.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type2>>();
      FormatConstraintsIfType2 = constraintsIfType2.Exists() ? constraintsIfType2.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type2>>();
    }

    #endregion

    /// <summary>
    /// Returns a new <see cref="ConfigToken"/> with an additional type and new constraints added. Used primarily during deserialization.
    /// </summary>
    /// <exception cref="ArgumentException">If this <see cref="ConfigToken"/> already has <typeparamref name="TNewType"/></exception>
    /// <typeparam name="TNewType">New possible type to add to the ConfigToken.</typeparam>
    /// <param name="newConstraints">Constraints to apply if cast to the new type is successful.</param>
    /// <returns>New <see cref="ConfigToken{Type1, Type2,TNewType}"/></returns>
    public override ConfigToken AddNewType<TNewType>(Constraint<TNewType>[] newConstraints = null)
    {
      return GetType().GetGenericArguments().Contains(typeof(TNewType))
        ? throw new ArgumentException($"ConfigToken already has type {typeof(TNewType).Name}.")
        : DefaultValue.Exists()
        ? new ConfigToken<Type1, Type2, TNewType>(TokenName, Description, DefaultValue, ConstraintsIfType1.ToArray(), ConstraintsIfType2.ToArray(), newConstraints, AllowNull)
        : new ConfigToken<Type1, Type2, TNewType>(TokenName, Description, ConstraintsIfType1.ToArray(), ConstraintsIfType2.ToArray(), newConstraints, Required, AllowNull);
    }

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed collection item.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator)
    {
      base.Validate(collection, translator);
      if (translator.TryCastToken(collection, TokenName, out Type1 newValue1))
      {
        InternalValidate(newValue1, ConstraintsIfType1);
        if (FormatConstraintsIfType1.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType1);
        }
      }
      else if (translator.TryCastToken(collection, TokenName, out Type2 newValue2))
      {
        InternalValidate(newValue2, ConstraintsIfType2);
        if (FormatConstraintsIfType2.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType2);
        }
      }
      else
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}, {typeof(Type2).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
  /// All passed types must be unique.
  /// WARNING: Casts will be attempted IN ORDER. For example,
  /// ConfigToken{string, int} will NEVER treat the passed token as an int!
  /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
  /// </summary>
  /// <typeparam name="Type1">The 1st possible value type of this token.</typeparam>
  /// <typeparam name="Type2">The 2nd possible value type of this token.</typeparam>
  /// <typeparam name="Type3">The 3rd possible value type of this token.</typeparam>
  public class ConfigToken<Type1, Type2, Type3> : ConfigToken
  {
    /// <summary>
    /// If DefaultValue is set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type1>> FormatConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type2>> ConstraintsIfType2 { get; protected set; } = new();
    public List<Constraint<Type2>> FormatConstraintsIfType2 { get; protected set; } = new();
    public List<Constraint<Type3>> ConstraintsIfType3 { get; protected set; } = new();
    public List<Constraint<Type3>> FormatConstraintsIfType3 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="constraintsIfType3">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type3"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, required, allowNull);
      BuildConstraints(constraintsIfType1, constraintsIfType2, constraintsIfType3);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input collection to a Schema object.
    /// All passed types must be unique.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputDescription is null, whitespace, or empty.</exception>
    /// <exception cref="ArgumentException">If all Type arguments are not unique.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputDescription">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="constraintsIfType3">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type3"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputDescription, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputDescription, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1, constraintsIfType2, constraintsIfType3);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null)
    {
      Type[] typeArray = GetType().GetGenericArguments();
      if (typeArray.Distinct().Count() != typeArray.Length)
      {
        throw new ArgumentException($"ConfigToken for {TokenName} contains duplicate Type arguments: {string.Join(", ", typeArray.Select(x => x.Name))}");
      }
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType2));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType3));
      ConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type1>>();
      FormatConstraintsIfType1 = constraintsIfType1.Exists() ? constraintsIfType1.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type1>>();
      ConstraintsIfType2 = constraintsIfType2.Exists() ? constraintsIfType2.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type2>>();
      FormatConstraintsIfType2 = constraintsIfType2.Exists() ? constraintsIfType2.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type2>>();
      ConstraintsIfType3 = constraintsIfType3.Exists() ? constraintsIfType3.Where(x => x.ConstraintType == ConstraintType.Standard).ToList() : new List<Constraint<Type3>>();
      FormatConstraintsIfType3 = constraintsIfType3.Exists() ? constraintsIfType3.Where(x => x.ConstraintType == ConstraintType.Format).ToList() : new List<Constraint<Type3>>();
    }

    #endregion

    /// <summary>
    /// Returns a new <see cref="ConfigToken"/> with an additional type and new constraints added. Used primarily during deserialization.
    /// </summary>
    /// <exception cref="ArgumentException">If this <see cref="ConfigToken"/> already has <typeparamref name="TNewType"/></exception>
    /// <typeparam name="TNewType">New possible type to add to the ConfigToken.</typeparam>
    /// <param name="newConstraints">Constraints to apply if cast to the new type is successful.</param>
    /// <returns>New <see cref="ConfigToken{Type1, Type2, Type3,TNewType}"/></returns>
    public override ConfigToken AddNewType<TNewType>(Constraint<TNewType>[] newConstraints = null)
    {
      throw new ArgumentException($"A ConfigToken cannot have more than three types.");
    }

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed collection item.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator)
    {
      base.Validate(collection, translator);
      if (translator.TryCastToken(collection, TokenName, out Type1 newValue1))
      {
        InternalValidate(newValue1, ConstraintsIfType1);
        if (FormatConstraintsIfType1.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType1);
        }
      }
      else if (translator.TryCastToken(collection, TokenName, out Type2 newValue2))
      {
        InternalValidate(newValue2, ConstraintsIfType2);
        if (FormatConstraintsIfType2.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType2);
        }
      }
      else if (translator.TryCastToken(collection, TokenName, out Type3 newValue3))
      {
        InternalValidate(newValue3, ConstraintsIfType3);
        if (FormatConstraintsIfType3.Count > 0)
        {
          translator.TryCastToken(collection, TokenName, out string stringValue);
          InternalValidateFormat(stringValue, FormatConstraintsIfType3);
        }
      }
      else
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}, {typeof(Type2).Name}, {typeof(Type3).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType>(TCollectionType collection, ISchemaTranslator<TCollectionType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }
}
