using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SchemaForge.Crucible;

using static SchemaForge.Crucible.Constraints;

namespace SchemaForge.Crucible
{
  public class ShippingAndReceiving
  {
    /*

    CONSRUCTION ZONE AHEAD
    DO NOT ENTER
    !DANGER!
    -----------------------------------------

    */
    /*
    
    "FieldName":
    {
	    "Type":"Integer"
	    "ConstrainValues":["(
    }

    */
    //private static readonly Dictionary<JToken, Func<JToken, Constraint>> StringToConstraint = new()
    //{
    //  { "ConstrainValues", (JToken args) => GetConstrainValues(args) }
    //};

    //private static Constraint GetConstrainValues(JToken args)
    //{
    //  string argsString = args.ToString().Replace(" ", "");

    //}

    /*
    
    -----------------------------------------
    END CONSTRUCTION ZONE
    
    */

    private static List<int> GetIntArgsFromJToken(JToken args)
    {
      JTokenType[] acceptableTypes = { JTokenType.String, JTokenType.Integer, JTokenType.Float };
      if (!acceptableTypes.Contains(args.Type))
      {
        throw new ArgumentException($"JToken {args} of type {args.Type} was passed to method that only accepts String, Integer, and Float.");
      }
      string[] argString = args.ToString().Replace(" ", "").Split(',');
      if (argString.Length > 2)
      {
        throw new ArgumentException("Comma-separated list must have no more than two values.");
      }
      else
      {
        List<int> argsInt = new();
        foreach (string stringArg in argString)
        {
          if (stringArg.Contains('.') && !EndsInZeroHelper(stringArg))
          {
            throw new ArgumentException($"Error encountered while parsing value {stringArg}: Value is not parseable to int without loss of information.");
          }
          if (int.TryParse(stringArg, out int intArg))
          {
            argsInt.Add(intArg);
          }
          else
          {
            throw new ArgumentException("Error encountered while parsing value " + stringArg + ": Value is not a valid int.");
          }
        }
        return argsInt;
      }
    }

    private static List<double> GetDoubleArgsFromJToken(JToken args)
    {
      JTokenType[] acceptableTypes = { JTokenType.String, JTokenType.Integer, JTokenType.Float };
      if (!acceptableTypes.Contains(args.Type))
      {
        throw new ArgumentException($"JToken {args} of type {args.Type} was passed to method that only accepts String, Integer, and Float.");
      }
      string[] argString = args.ToString().Replace(" ", "").Split(',');
      if (argString.Length > 2)
      {
        throw new ArgumentException("Comma-separated list must have no more than two values.");
      }
      else
      {
        List<double> argsDouble = new();
        foreach (string stringArg in argString)
        {
          if (double.TryParse(stringArg, out double doubleArg))
          {
            argsDouble.Add(doubleArg);
          }
          else
          {
            throw new ArgumentException($"Error encountered while parsing value {stringArg}: Value is not a valid double.");
          }
        }
        return argsDouble;
      }
    }

    private static readonly Dictionary<string, string> InternalTypeMap = new()
    {
      { "Byte", "Integer" },
      { "SByte", "Integer" },
      { "Int16", "Integer" },
      { "Int32", "Integer" },
      { "Int64", "Integer" },
      { "UInt16", "Integer" },
      { "UInt32", "Integer" },
      { "UInt64", "Integer" },
      { "Boolean", "Boolean" },
      { "String", "String" },
      { "DateTime", "Date" },
      { "Date", "Date" },
      { "TimeSpan", "TimeSpan" },
      { "Decimal", "Decimal" },
      { "Double", "Decimal" },
      { "JArray", "Array" },
      { "JObject", "Json" }
    };

    //private static Dictionary<string, Func<Constraint[], ConfigToken>> InternalDeserializeType = new()
    //{
    //  { "Integer", GetConstraintsForType<long> },
    //  { "String", GetConstraintsForType<string> },
    //  { "Decimal", GetConstraintsForType<double> },
    //  { "Array", GetConstraintsForType<JArray> },
    //  { "JObject", GetConstraintsForType<JObject> }
    //};

    /// <summary>
    /// Converts a type and array of constraints to a ConstraintContainer with
    /// constraints of the specified type.
    /// </summary>
    /// <param name="typeString">One of the supported types as strings, which
    /// can be retrieved with <see cref="ShippingAndReceiving.GetSupportedTypes()"/></param>
    /// <param name="constraints">Constraints to pass along after the type check.</param>
    /// <returns>GetConstraintsForType with a type</returns>
    //public static ConfigToken DeserializeType(string typeString, Constraint[] constraints) => InternalDeserializeType[typeString](constraints);

    /// <summary>
    /// Converts a type from the C# type to the SchemaForge equivalent. See
    /// <see cref="ShippingAndReceiving.GetSupportedTypes()"/> for the list of types.
    /// </summary>
    /// <param name="typeString">C# type to convert, without the namespace designation.</param>
    /// <returns></returns>
    public static string TypeMap(string typeString) => InternalTypeMap[typeString];

    /// <summary>
    /// Returns all convertible C# types as a list of string.
    /// </summary>
    /// <returns>List of keys in InternalTypeMap. To add to this list, use
    /// <see cref="ShippingAndReceiving.AddSupportedType"/></returns>
    public static List<string> GetSupportedTypes() => InternalTypeMap.Keys.ToList();

    /// <summary>
    /// Used as part of the type deserializer. When adding a new supported type,
    /// only use the method name and type argument. Do not attempt to pass constraints.
    /// </summary>
    /// <typeparam name="T">Type to align with a string key.</typeparam>
    /// <param name="constraints">Irrelevant. Populated only when deserializing a Json file to a Schema.</param>
    /// <returns>ConstraintContainer containing constraints of that particular type.</returns>
    //public static ConstraintContainer GetConstraintsForType<T>(Constraint[] constraints)
    //{
    //  List<Constraint<T>> constraintList = new();
    //  foreach(Constraint constraint in constraints)
    //  {
    //    constraintList.Add(new Constraint<T>((Func<T, string, List<Error>>)constraint.GetFunction(), constraint.Property));
    //  }
    //  return ApplyConstraints<T>(constraintList.ToArray());
    //}

    /// <summary>
    /// Adds a new supported type for serializing and deserializing.
    /// For typeDeserializer, pass <see cref="ShippingAndReceiving.GetConstraintsForType{T}"/>
    /// with only the type parameter provided. It should line up with the provided
    /// csTypeName.
    /// </summary>
    /// <param name="csTypeName">C# type name without namespace, such as UInt64 or String</param>
    /// <param name="schemaForgeTypeName">Equivalent name to serialize the C# type name to.</param>
    /// <param name="typeDeserializer"><see cref="ShippingAndReceiving.GetConstraintsForType{T}"/>
    /// with a type parameter equivalent to the C# type name being provided.</param>
    //public static void AddSupportedType(string csTypeName, string schemaForgeTypeName, Func<Constraint[], ConstraintContainer> typeDeserializer)
    //{
    //  InternalTypeMap.Add(csTypeName, schemaForgeTypeName);
    //  InternalDeserializeType.Add(schemaForgeTypeName, typeDeserializer);
    //}

    /// <summary>
    /// Accepts a JProperty. The Name of the JProperty will be used to look up
    /// a value in <see cref="StringToConstraint"/> and the Value of the
    /// JProperty will be passed as the argument to the corresponding
    /// function. To retrieve a list of supported constraint names,
    /// use <see cref="ShippingAndReceiving.GetSupportedConstraints()"/>
    /// </summary>
    /// <typeparam name="T">Type of the returned constraint.</typeparam>
    /// <param name="inputProperty">JProperty to deserialize into a Constraint.</param>
    /// <returns>Constraint of the specified type using the name and arguments provided in the JProperty.</returns>
    //public static Constraint<T> JPropertyToConstraint<T>(JProperty inputProperty) => (Constraint<T>)StringToConstraint[inputProperty.Name](inputProperty.Value);

    /// <summary>
    /// Gets a list of strings that can be converted to constraints when passed
    /// as the name of a JProperty to <see cref="ShippingAndReceiving.JPropertyToConstraint{T}(JProperty)"/>
    /// </summary>
    /// <returns>List of all valid constraint names that can be deserialized.</returns>
    //public static List<string> GetSupportedConstraints() => StringToConstraint.Keys.Select(x => x.ToString()).ToList();

    /// <summary>
    /// Returns true if the string representation of a number is something like "3.0" or "3." and false otherwise.
    /// </summary>
    /// <param name="inputString">String to check.</param>
    /// <returns>Bool indicating if the decimal places are all zero.</returns>
    private static bool EndsInZeroHelper(string inputString) => inputString.Replace("0", "")[^1] == '.';

  }
}
