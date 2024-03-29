﻿using System;
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
    /// Indicates something that is not an error, but should be provided for the end user's edification.
    /// </summary>
    Info,
    /// <summary>
    /// Indicates an error that is not severe enough to mark the object invalid.
    /// </summary>
    Warning,
    /// <summary>
    /// Indicates an error that should invalidate the object being evaluated by the Schema.
    /// </summary>
    Fatal,
    /// <summary>
    /// Indicates that this is debugging info.
    /// </summary>
    Trace
  }
  /// <summary>
  /// In SchemaForge, a <see cref="SchemaError"/> is an object generated when using a <see cref="Schema"/> to validate another object.
  /// When converted to a string, <see cref="SchemaError"/>s show their severity and error message.
  /// </summary>
  public class SchemaError
  {
    /// <summary>
    /// Message detailing the error.
    /// </summary>
    public string ErrorMessage { get; }
    /// <summary>
    /// <see cref="Severity"/> of the error.
    /// </summary>
    public Severity ErrorSeverity { get; }

    /// <summary>
    /// Builds an <see cref="SchemaError"/> with the <see cref="ErrorMessage"/>
    /// set to <paramref name="inputMessage"/> and the <see cref="Severity"/>
    /// set to <paramref name="inputSeverity"/> if provided.
    /// </summary>
    /// <param name="inputMessage">The message to be displayed when this error is converted to string. Will display as "[<paramref name="inputSeverity"/>] <paramref name="inputMessage"/>"</param>
    /// <param name="inputSeverity">Severity of this error. Defaults to Fatal.</param>
    public SchemaError(string inputMessage, Severity inputSeverity = Severity.Fatal)
    {
      if(inputMessage.IsNullOrEmpty())
      {
        throw new ArgumentNullException(nameof(inputMessage),$"{nameof(inputMessage)} of Error cannot be null or whitespace.");
      }
      ErrorMessage = inputMessage;
      ErrorSeverity = inputSeverity;
    }

    /// <summary>
    /// Converts the error's severity and message to string format.
    /// </summary>
    /// <returns>String in the format "[<see cref="ErrorSeverity"/>] <see cref="ErrorMessage"/>"</returns>
    public override string ToString() => $"[{ErrorSeverity}] {ErrorMessage}";
  }
}
