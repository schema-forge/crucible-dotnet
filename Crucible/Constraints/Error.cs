using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using schemaforge.Crucible.Extensions;

namespace schemaforge.Crucible
{
  public enum Severity
  {
    Warning,
    Fatal,
    NullOrEmpty,
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

    public override string ToString() => ErrorMessage;
  }
}
