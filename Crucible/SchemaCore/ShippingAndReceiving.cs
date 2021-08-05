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
  class ShippingAndReceiving
  {
    private static Dictionary<JToken, Func<JToken, Constraint>> StringToConstraint = new()
    {
    };

    private static Dictionary<string, string> InternalTypeMap = new()
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

    private static Dictionary<string, Func<Constraint[], ConstraintContainer>> InternalDeserializeType = new()
    {
      { "Integer", Blaaaaaargh<long> }
    };

    public static ConstraintContainer DeserializeType(string typeString, Constraint[] constraints) => InternalDeserializeType[typeString](constraints);

    public static string TypeMap(string typeString) => InternalTypeMap[typeString];

    public static ConstraintContainer Blaaaaaargh<T>(Constraint[] constraints) => ApplyConstraints<T>((Constraint<T>[])constraints);

    public static void AddSupportedType(string csTypeName, string schemaForgeTypeName, Func<Constraint[], ConstraintContainer> typeDeserializer)
    {
      InternalTypeMap.Add(csTypeName, schemaForgeTypeName);
      InternalDeserializeType.Add(schemaForgeTypeName, typeDeserializer);
    }

    public static Constraint<T> JPropertyToConstraint<T>(JProperty inputProperty) => (Constraint<T>)StringToConstraint[inputProperty.Name](inputProperty.Value);

    private static List<int> GetIntArgsFromJToken(JToken args)
    {
      JTokenType[] acceptableTypes = { JTokenType.String, JTokenType.Integer, JTokenType.Float };
      if (!acceptableTypes.Contains(args.Type))
      {

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

    /// <summary>
    /// Returns true if the string representation of a number is something like "3.0" or "3." and false otherwise.
    /// </summary>
    /// <param name="inputString">String to check.</param>
    /// <returns>Bool indicating if the decimal places are all zero.</returns>
    private static bool EndsInZeroHelper(string inputString) => inputString.Replace("0", "")[^1] == '.';

  }
}
