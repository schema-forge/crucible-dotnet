using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Indicates the severity of an error found while validating.
  /// </summary>
  public enum Severity
  {
    /// <summary>
    /// Indicates an error that is not severe enough to mark the object invalid, but may not be considered a best practice.
    /// </summary>
    Warning,
    /// <summary>
    /// Indicates an error that should invalidate the object being evaluated by the Schema.
    /// </summary>
    Fatal,
    /// <summary>
    /// Indicates something that is not an error, but should be provided for the end user's edification.
    /// </summary>
    Info,
    /// <summary>
    /// Indicates that this is debugging info.
    /// </summary>
    Trace
  }
  /// <summary>
  /// In SchemaForge, an Error is an object generated when using a Schema to validate another object.
  /// When converted to a string, Errors show their severity and error message.
  /// </summary>
  public class Error
  {
    /// <summary>
    /// Message detailing the nature of the error that was raised.
    /// </summary>
    public string ErrorMessage { get; }
    /// <summary>
    /// Severity of the error that was raised.
    /// </summary>
    public Severity ErrorSeverity { get; }

    /// <summary>
    /// In SchemaForge, an Error is an object generated when using a Schema to validate another object.
    /// When converted to a string, Errors show their severity and error message.
    /// </summary>
    /// <param name="inputMessage">The message to be displayed when this error is converted to string. Will display as [<paramref name="inputSeverity"/>] <paramref name="inputMessage"/></param>
    /// <param name="inputSeverity">Severity of this error. Defaults to Fatal.</param>
    public Error(string inputMessage, Severity inputSeverity = Severity.Fatal)
    {
      if(inputMessage.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputMessage),"inputMessage of Error cannot be null or whitespace.");
      }
      ErrorMessage = inputMessage;
      ErrorSeverity = inputSeverity;
    }

    public override string ToString() => $"[{ErrorSeverity}] {ErrorMessage}";
  }
}
