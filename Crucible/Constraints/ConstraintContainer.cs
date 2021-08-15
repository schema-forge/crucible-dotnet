using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SchemaForge.Crucible.Extensions;

namespace SchemaForge.Crucible
{
  //public sealed class ConstraintContainer<TCastType>
  //{
  //  /// <summary>
  //  /// Contains a representation of this ConstraintContainer. Methods like ApplyConstraints that can take more than one type parameter can return more than one JObject representation of a type and its constraints; therefore, we take a JArray instead of a single JObject.
  //  /// </summary>
  //  public JArray JsonConstraints { get; private set; }
  //  /// <summary>
  //  /// The function that applies all the constraints in the container. This function should be composed from all the constraints passed to the method that returns a ConstraintContainer. See ApplyConstraints and its overloads for an example.
  //  /// </summary>
  //  public Func<TCastType, string, List<Error>> ApplyConstraints { get; private set; }

  //  /// <summary>
  //  /// Builds a ConstraintContainer, which will be invoked by a ConfigToken to apply all the rules contained therein.
  //  /// </summary>
  //  /// <exception cref="ArgumentException">Thrown if an element in inputArray is not a JObject or an element of inputArray does not contain Type.</exception>
  //  /// <param name="inputFunction">Function to execute when this ConstraintContainer is invoked.</param>
  //  /// <param name="inputArray">Array of JObject representations of constraint sets. Each value must be a JObject and must contain Type at the very least.</param>
  //  public ConstraintContainer(Func<TCastType, string, List<Error>> inputFunction, JArray inputArray)
  //  {
  //    foreach (JToken value in inputArray)
  //    {
  //      if (value.Type != JTokenType.Object)
  //      {
  //        throw new ArgumentException($"Token {value} is not a JObject.");
  //      }
  //      else if (!value.Contains("Type"))
  //      {
  //        throw new ArgumentException($"Constraint representation is missing required token Type: {value}");
  //      }
  //    }
  //    JsonConstraints = inputArray;
  //    ApplyConstraints = inputFunction;
  //  }

  //  public override string ToString() => JsonConstraints.Count > 1 ? JsonConstraints.ToString() : JsonConstraints[0].ToString();

  //  public JToken ToJToken() => JsonConstraints.Count > 1 ? JsonConstraints : JsonConstraints[0];
  //}
}
