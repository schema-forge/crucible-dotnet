using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using SchemaForge.Crucible.Extensions;
using SchemaForge.Crucible.Utilities;

namespace SchemaForge.Crucible
{
  /// <summary>
  /// Interprets any .NET object for a <see cref="Schema"/>
  /// object to validate; the object must have a property for all
  /// <see cref="Field"/>s contained in <see cref="Schema.Fields"/>. 
  /// Aligning property types with the types of each <see cref="Field"/>
  /// is recommended, though not required.
  /// </summary>
  public class ClassTranslator : ISchemaTranslator<object>
  {
    /// <inheritdoc/>
    public bool CollectionContains(object collection, string valueName) => collection.GetType().GetProperties().Select(x => x.Name).Contains(valueName);
    /// <inheritdoc/>
    public string CollectionValueToString(object collection, string valueName)
    {
      PropertyInfo valueProperty = collection.GetType().GetProperty(valueName);
      return valueProperty.GetValue(collection).ToString();
    }
    /// <inheritdoc/>
    public List<string> GetCollectionKeys(object collection) => collection.GetType().GetProperties().Select(x => x.Name).ToList();
    /// <inheritdoc/>
    public string GetEquivalentType(string cSharpType) => Conversions.GetEquivalentJsonType(cSharpType);

    /// <summary>
    /// Inserts <paramref name="newValue"/> into <paramref name="collection"/>
    /// with string designation <paramref name="valueName"/> and returns a
    /// new instance of the class object with the value inserted.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="collection"/>'s
    /// base type does not contain a property named <paramref name="valueName"/>
    /// or if it does contain a property of that name, but the type is not
    /// compatible with <typeparamref name="TDefaultValueType"/></exception>
    /// <typeparam name="TDefaultValueType">Type of the value to be inserted.</typeparam>
    /// <param name="collection">The collection from which to extract the value.</param>
    /// <param name="valueName">The string designator for the value to be extracted.</param>
    /// <param name="newValue">The new value to be inserted.</param>
    /// <returns>New object with value inserted.</returns>
    public object InsertFieldValue<TDefaultValueType>(object collection, string valueName, TDefaultValueType newValue)
    {
      PropertyInfo valueProperty = collection.GetType().GetProperty(valueName);
      try
      {
        if(valueProperty.PropertyType == typeof(TDefaultValueType))
        {
          valueProperty.SetValue(collection, newValue);
        }
        else
        {
          valueProperty.SetValue(collection, Convert.ChangeType(newValue, valueProperty.PropertyType));
        }
        return collection;
      }
      catch
      {
        throw new ArgumentException($"Attempted to set property {valueName} of type {valueProperty.GetType().FullName} on class type {collection.GetType().FullName} to value with type {typeof(TDefaultValueType).FullName}. There is no existing conversion from {valueProperty.GetType().FullName} to {typeof(TDefaultValueType).FullName}.");
      }
    }
    /// <inheritdoc/>
    public bool FieldValueIsNullOrEmpty(object collection, string valueName)
    {
      PropertyInfo valueProperty = collection.GetType().GetProperty(valueName);
      Type valueType = valueProperty.PropertyType;
      object value = valueProperty.GetValue(collection);
      return value != null
        && (valueType.IsAssignableFrom(typeof(JToken)) ? ((JToken)value).IsNullOrEmpty() : value.ToString().IsNullOrEmpty());
    }
    /// <inheritdoc/>
    public bool TryCastValue<TCastType>(object collection, string valueName, out TCastType outputValue)
    {
      PropertyInfo valueProperty = collection.GetType().GetProperty(valueName);
      object value = valueProperty.GetValue(collection);
      Type valueType = valueProperty.PropertyType;

      return InnerTryCastValue(value, valueType, out outputValue);
    }

    /// <summary>
    /// Performs the bulk of TryCastValue's work. Included as a separate method
    /// to allow calling itself when dealing with IEnumerables that need their constituent values casted to something else.
    /// </summary>
    /// <typeparam name="TCastType">Type to which <paramref name="value"/> will be cast.</typeparam>
    /// <param name="value">Value to be converted.</param>
    /// <param name="valueType">Original type of <paramref name="value"/>.</param>
    /// <param name="outputValue"><paramref name="value"/> as <typeparamref name="TCastType"/></param>
    /// <returns>Bool indicating if cast was successful.</returns>
    private bool InnerTryCastValue<TCastType>(object value, Type valueType, out TCastType outputValue)
    {
      Type castType = typeof(TCastType);
      if (castType == valueType)
      {
        outputValue = (TCastType)value;
        return true;
      }
      else if (castType == typeof(string))
      {
        outputValue = (TCastType)(object)value.ToString(); // This syntax is required because, even though we're in an
                                                           //   If statement that enforces the rule that TCastType must be DateTime, the compiler is silly.
        return true;
      }
      else if (castType == typeof(DateTime))
      {
        bool result = Conversions.TryConvertDateTime(value.ToString(), out DateTime outDateTime);
        outputValue = (TCastType)(object)outDateTime;
        return result;
      }
      else if (castType == typeof(JObject))
      {
        try
        {
          if (valueType == typeof(JObject))
          {
            outputValue = (TCastType)value;
            return true;
          }
          else
          {
            JObject Traverse(object localValue)
            {
              PropertyInfo[] properties = localValue.GetType().GetProperties();
              JObject returnObject = new JObject();
              List<Type> rawAddTypes = new List<Type>() { typeof(DateTime), typeof(string) };
              foreach (PropertyInfo property in properties)
              {
                if (property.PropertyType.IsValueType || property.PropertyType.IsAssignableFrom(typeof(JToken)) || rawAddTypes.Contains(property.PropertyType))
                {
                  returnObject.Add(property.Name, new JValue(property.GetValue(localValue)));
                }
                else
                {
                  object newLocalValue = property.GetValue(localValue);
                  returnObject.Add(property.Name, Traverse(newLocalValue));
                }
              }
              return returnObject;
            }
            outputValue = (TCastType)(object)Traverse(value);
            return true;
          }
        }
        catch
        {
          outputValue = default;
          return false;
        }
      }
      else if (typeof(IEnumerable).IsAssignableFrom(castType))
      {
        if(typeof(IEnumerable).IsAssignableFrom(valueType))
        {
          try
          {
            MethodInfo getEnumeratorMethod = valueType.GetMethod("GetEnumerator");
            object enumerator = getEnumeratorMethod.Invoke(value, null);
            Type enumeratorType = enumerator.GetType();
            MethodInfo moveNext = enumeratorType.GetMethod("MoveNext");
            PropertyInfo current = enumeratorType.GetProperty("Current");
            if (castType.IsGenericType)
            {
              Type[] genericArguments = castType.GetGenericArguments();
              object returnValue = Activator.CreateInstance(castType);
              MethodInfo add = returnValue.GetType().GetMethod("Add");
              
              MethodInfo tryCastType = this.GetType().GetMethod(nameof(InnerTryCastValue),BindingFlags.NonPublic | BindingFlags.Instance);
              MethodInfo castToInnerType = tryCastType.MakeGenericMethod(genericArguments[0]);
              while ((bool)moveNext.Invoke(enumerator, null))
              {
                object currentValue = current.GetValue(enumerator);
                Type currentValueType = currentValue.GetType();
                object innerConvertedValue = null;
                object[] args = new object[] { currentValue, currentValueType, innerConvertedValue };
                castToInnerType.Invoke(this, args);
                add.Invoke(returnValue, new object[] { args[2] });
              }
              outputValue = (TCastType)returnValue;
              return true;
            }
            else
            {
              List<object> returnValue = new List<object>();
              while ((bool)moveNext.Invoke(enumerator, null))
              {
                returnValue.Add(current.GetValue(enumerator));
              }
              if(castType == typeof(JArray))
              {
                outputValue = (TCastType)(object)JArray.FromObject(returnValue);
                return true;
              }
              else
              {
                outputValue = (TCastType)(object)returnValue;
                return true;
              }
            }
          }
          catch
          {
          }
        }
      }
      else
      {
        try
        {
          outputValue = (TCastType)Convert.ChangeType(value, typeof(TCastType));
          return true;
        }
        catch
        {
        }
      }
      outputValue = default;
      return false;
    }
  }
}
