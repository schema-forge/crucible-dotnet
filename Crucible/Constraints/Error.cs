using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  public enum Severity
  {
    Warning,
    Fatal,
    Info,
    Trace
  }
  public class Error
  {
    public string ErrorMessage { get; }
    public Severity ErrorSeverity { get; }

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
