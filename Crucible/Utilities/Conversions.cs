using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaForge.Crucible.Utilities
{
  /*
  
  TODO:

  Note for future me; AllowDateTimeFormat() constraint that would just add possible formats and not take away the ones that .NET parser already understands?
  
  */
  /// <summary>
  /// Holds classes and utility methods for special conversions.
  /// </summary>
  public class Conversions
  {

    internal static readonly Dictionary<string, string> JsonTypeMap = new Dictionary<string, string>()
    {
      { "Byte", "Number" },
      { "SByte", "Number" },
      { "Single", "Number" },
      { "Double", "Number" },
      { "Decimal", "Number" },
      { "Int16", "Number" },
      { "UInt16", "Number" },
      { "Int32", "Number" },
      { "UInt32", "Number" },
      { "Int64", "Number" },
      { "UInt64", "Number" },
      { "JObject", "Object" },
      { "Boolean", "Boolean" },
      { "JArray", "Array" },
      { "DateTime", "String" },
      { "String", "String" },
      { "Char", "String" }
    };

    internal static string GetEquivalentJsonType(string cSharpType) => $"Json " + (JsonTypeMap.ContainsKey(cSharpType) ? JsonTypeMap[cSharpType] : cSharpType.Contains("[]") ? "array" : "null");

    /// <summary>
    /// Holds the <see cref="DateTime"/> formats in <see cref="DateTime"/> Custom Format Specifier format; e.g., "yyyy-MM-dd", "ddd MMMM, yyyy"
    /// </summary>
    private static HashSet<string> DateTimeFormats = new HashSet<string>();

    /// <summary>
    /// Checks whether or not <see cref="TryConvertDateTime(string, out DateTime)"/> recognizes the passed format.
    /// </summary>
    /// <param name="format">Format to search for in the current list of known formats.</param>
    /// <returns>Bool indicating if the parser recognizes the passed format.</returns>
    public static bool CheckDateTimeConversion(string format) => DateTimeFormats.Contains(format);

    /// <summary>
    /// Adds a new <see cref="DateTime"/> format for <see cref="TryConvertDateTime(string, out DateTime)"/> to use.
    /// </summary>
    /// <param name="format">A format in <see cref="DateTime"/> Custom Format Specifier format; e.g., "yyyy-MM-dd", "ddd MMMM, yyyy"</param>
    public static void RegisterDateTimeFormat(string format) => DateTimeFormats.Add(format);

    /// <summary>
    /// Removes a <see cref="DateTime"/> format from the list of known formats.
    /// </summary>
    /// <param name="format">Format to search for in the current list and remove.</param>
    public static void DeregisterDateTimeFormat(string format) => DateTimeFormats.Remove(format);

    /// <summary>
    /// Attempts to convert the input string to <see cref="DateTime"/>; first, it
    /// attempts to use <see cref="DateTime.TryParse(string?, out DateTime)"/>.
    /// If this fails, it will try each <see cref="DateTimeFormats"/>.
    /// To add a new format, use <see cref="RegisterDateTimeFormat(string)"/>
    /// </summary>
    /// <param name="inputString">String to parse into <see cref="DateTime"/></param>
    /// <param name="outputDateTime">Parsed <see cref="DateTime"/></param>
    /// <returns>Bool indicating whether or not the conversion was successful.</returns>
    public static bool TryConvertDateTime(string inputString, out DateTime outputDateTime)
    {
      if(DateTime.TryParse(inputString, out outputDateTime))
      {
        return true;
      }
      foreach(string format in DateTimeFormats)
      {
        if (DateTime.TryParseExact(inputString, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out outputDateTime))
        {
          return true;
        }
      }
      outputDateTime = default;
      return false;
    }
  }
}
