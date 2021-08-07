using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaForge.Crucible.SchemaCore
{
  public class MetaSchema
  {
    private Dictionary<string, Schema> TypeToSchema { get; } = new()
    {
      {
        "Integer",
        new Schema(//new ConfigToken[]
                   //{
                   //  new ConfigToken("Type","Expected type of token value.", ApplyConstraints(AllowValues("Integer"))),
                   //  new ConfigToken("Domains", "Constrains integer values to a certain domain. Arguments must be one of: int; \"int, int\"; [\"[null, int]\",\"[int, null]\", \"[int, int]\" ...]", false,
                   //        ApplyConstraints(
                   //          new Constraint<JArray>[]
                   //          { ApplyConstraintsToCollection<int,JArray>
                   //            (constraintsIfTElementType2: new Constraint<JArray>[] {
                   //              ApplyConstraintsToCollection<int, JArray>(), ConstrainCollectionCount<JArray>(2, 2)
                   //            }
                   //            )
                   //          }
                   //        )
                   // ),
                   //  new ConfigToken("RestrictDecimalDigits", "Constrains number of digits after the decimal. Must one of: int, \"int, int\"", false,
                   //        ApplyConstraints<int, string>(
                   //          constraintsIfType2: new Constraint<string>[] { ConstrainStringWithRegexExact(new Regex("\\d+, *\\d+"), new Regex("\\d+")) }
                   //        )
                   //  ),
                   //}
      )
      },
      {
        "String",
        new Schema()
      }
    };
  }
}
