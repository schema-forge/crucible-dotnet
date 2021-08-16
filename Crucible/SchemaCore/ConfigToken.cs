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
    public string HelpString { get; protected set; }

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
    /// Converts a ConfigToken into a JProperty of format "<see cref="TokenName"/>":{ "Constraints":<see cref="JsonConstraint"/>, "Description":"<see cref="HelpString"/>" }
    /// </summary>
    /// <returns>JProperty of format "<see cref="TokenName"/>":{ "Constraints":<see cref="JsonConstraint"/>, "Description":"<see cref="HelpString"/>"</returns>
    public JProperty ToJProperty() => new(TokenName, new JObject() { { "Constraints", JsonConstraint.Count > 1 ? JsonConstraint : JsonConstraint[0] }, { "Description", HelpString } });

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
    public virtual bool Validate<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator)
    {
      if (translator.TokenIsNullOrEmpty(collection, TokenName))
      {
        ErrorList.Add(new Error($"Value of token {TokenName} is null or empty.", AllowNull?Severity.Warning:Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }

    public abstract TCollectionType InsertDefaultValue<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator);

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
          constraintObject.Add(constraint.Property);
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
      if (inputHelpString.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputHelpString));
      }
      Required = required;
      TokenName = inputName;
      HelpString = inputHelpString;
      AllowNull = allowNull;
    }

    protected bool InternalValidate<TValueType>((bool, TValueType) castResult, List<Constraint<TValueType>> constraints)
    {
      if (!castResult.Item1)
      {
        return false;
      }
      else
      {
        foreach (Constraint<TValueType> constraint in constraints)
        {
          ErrorList.AddRange(constraint.Function(castResult.Item2, TokenName));
        }
        return true;
      }
    }

    #endregion
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
  /// WARNING: Casts will be attempted IN ORDER. For example,
  /// ConfigToken{string, int} will NEVER treat the passed token as an int!
  /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
  /// </summary>
  /// <typeparam name="Type1">The 1st possible value type of this token.</typeparam>
  public class ConfigToken<Type1> : ConfigToken
  {
    /// <summary>
    /// If set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Constraint<Type1>[] constraintsIfType1 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, required, allowNull);
      BuildConstraints(constraintsIfType1);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null)
    {
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      ConstraintsIfType1 = constraintsIfType1.ToList();
    }

    #endregion

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed JToken.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator)
    {
      base.Validate(collection, translator);
      if (!InternalValidate(translator.TryCastToken<Type1>(collection, TokenName), ConstraintsIfType1))
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
  /// WARNING: Casts will be attempted IN ORDER. For example,
  /// ConfigToken{string, int} will NEVER treat the passed token as an int!
  /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
  /// </summary>
  /// <typeparam name="Type1">The 1st possible value type of this token.</typeparam>
  /// <typeparam name="Type2">The 2nd possible value type of this token.</typeparam>
  public class ConfigToken<Type1, Type2> : ConfigToken
  {
    /// <summary>
    /// If set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type2>> ConstraintsIfType2 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, required, allowNull);
      BuildConstraints(constraintsIfType1, constraintsIfType2);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1, constraintsIfType2);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null)
    {
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType2));
      ConstraintsIfType1 = constraintsIfType1.ToList();
      ConstraintsIfType2 = constraintsIfType2.ToList();
    }

    #endregion

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed JToken.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator)
    {
      base.Validate(collection, translator);
      if (!InternalValidate(translator.TryCastToken<Type1>(collection, TokenName), ConstraintsIfType1)
        && !InternalValidate(translator.TryCastToken<Type2>(collection, TokenName), ConstraintsIfType2))
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}, {typeof(Type2).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }

  /// <summary>
  /// A ConfigToken represents a value that is expected to exist in a collection processed by a Schema object.
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
    /// If set and the token is optional, then if the user does not include this token in their configuration file, the default value will be inserted with TokenName as the property name.
    /// </summary>
    public Type1 DefaultValue { get; protected set; }
    public List<Constraint<Type1>> ConstraintsIfType1 { get; protected set; } = new();
    public List<Constraint<Type2>> ConstraintsIfType2 { get; protected set; } = new();
    public List<Constraint<Type3>> ConstraintsIfType3 { get; protected set; } = new();

    #region Constructors

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="constraintsIfType3">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type3"/>.</param>
    /// <param name="required">If true, not detecting this token when applying a Schema is a <see cref="Severity.Fatal"/>
    /// If false, not detecting this token when applying a schema raises no error.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null, bool required = true, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, required, allowNull);
      BuildConstraints(constraintsIfType1, constraintsIfType2, constraintsIfType3);
    }

    /// <summary>
    /// A ConfigToken represents a token that is expected to exist in the input JObject to a Schema object.
    /// WARNING: Casts will be attempted IN ORDER. For example,
    /// ConfigToken{string, int} will NEVER treat the passed token as an int!
    /// Casts will stop at the first valid attempt and apply the relevant constraints as defined in the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">If inputName or inputHelpString is null, whitespace, or empty.</exception>
    /// <param name="inputName">Name of the token. This will be used to search the user config when validating.</param>
    /// <param name="inputHelpString">String that will be shown to the user in the event of a validation error.</param>
    /// <param name="inputDefaultValue"><typeparamref name="Type1"/> that will be inserted into the user config if an optional token is not provided.
    /// If provided, assumes this token is not required.</param>
    /// <param name="constraintsIfType1">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type1"/>.</param>
    /// <param name="constraintsIfType2">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type2"/>.</param>
    /// <param name="constraintsIfType3">Constraints that will be applied to the token's value if it can be cast to <typeparamref name="Type3"/>.</param>
    /// <param name="allowNull">If false, detecting a null or empty value is a <see cref="Severity.Fatal"/>
    /// If true, detecting a null or empty value is a <see cref="Severity.Warning"/></param>
    public ConfigToken(string inputName, string inputHelpString, Type1 inputDefaultValue, Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null, bool allowNull = false)
    {
      BuildConfigTokenCore(inputName, inputHelpString, false, allowNull);
      DefaultValue = inputDefaultValue;
      BuildConstraints(constraintsIfType1, constraintsIfType2, constraintsIfType3);
    }

    protected void BuildConstraints(Constraint<Type1>[] constraintsIfType1 = null, Constraint<Type2>[] constraintsIfType2 = null, Constraint<Type3>[] constraintsIfType3 = null)
    {
      JsonConstraint.Add(GetConstraintObject(constraintsIfType1));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType2));
      JsonConstraint.Add(GetConstraintObject(constraintsIfType3));
      ConstraintsIfType1 = constraintsIfType1.ToList();
      ConstraintsIfType2 = constraintsIfType2.ToList();
      ConstraintsIfType3 = constraintsIfType3.ToList();
    }

    #endregion

    /// <summary>
    /// Executes the ConfigToken's ValidationFunction on the passed JToken.
    /// </summary>
    /// <param name="tokenValue">Token value to validate.</param>
    /// <returns>Bool indicating whether any fatal errors were found during validation.</returns>
    public override bool Validate<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator)
    {
      base.Validate(collection, translator);
      if (!InternalValidate(translator.TryCastToken<Type1>(collection, TokenName), ConstraintsIfType1)
        && !InternalValidate(translator.TryCastToken<Type2>(collection, TokenName), ConstraintsIfType2)
        && !InternalValidate(translator.TryCastToken<Type3>(collection, TokenName), ConstraintsIfType3))
      {
        ErrorList.Add(new Error($"Token {TokenName} with value {translator.CollectionValueToString(collection, TokenName)} is an incorrect type. Expected one of: {typeof(Type1).Name}, {typeof(Type2).Name}, {typeof(Type3).Name}", Severity.Fatal));
      }
      return !ErrorList.AnyFatal();
    }
    public override TCollectionType InsertDefaultValue<TCollectionType, TValueType>(TCollectionType collection, ISchemaTranslator<TCollectionType, TValueType> translator) => translator.InsertToken(collection, TokenName, DefaultValue);
  }
}
