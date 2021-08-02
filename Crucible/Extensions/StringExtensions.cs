using System;
using System.Collections.Generic;
using System.Text;

namespace schemaforge.Crucible.Extensions
{
  public static class StringExtensions
  {
    /// <summary>
    /// Executes string.IsNullOrEmpty() and string.IsNullOrWhiteSpace().
    /// </summary>
    /// <param name="str">String to check for emptiness.</param>
    public static bool IsNullOrEmpty(this string str)
    {
      return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Gets a list of all the indices of the given string or char.
    /// </summary>
    /// <param name="str">String to search.</param>
    /// <param name="value">String to search for.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if str or value is null, empty, or whitespace.</exception>
    /// <returns>List of all indices of the search value within the target string.</returns>
    public static List<int> AllIndexesOf(this string str, string value)
    {
      if (str.IsNullOrEmpty())
      {
        throw new ArgumentException("The string to search must not be empty.");
      }
      if (value.IsNullOrEmpty())
      {
        throw new ArgumentException("The search string must not be empty.");
      }
      List<int> indexes = new();
      int loc = 0;
      while (true)
      {
        loc = str.IndexOf(value, loc);
        if (loc == -1)
        {
          break;
        }
        else
        {
          indexes.Add(loc);
          loc += value.Length;
        }
      }
      return indexes;
    }
    /// <summary>
    /// Gets a list of all the indices of the given string or char.
    /// </summary>
    /// <param name="str">String to search.</param>
    /// <param name="value">Char to search for.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if str is null, empty, or whitespace.</exception>
    /// <returns>List of all indices of the search value within the target string.</returns>
    public static List<int> AllIndexesOf(this string str, char value)
    {
      if (str.IsNullOrEmpty())
      {
        throw new ArgumentException("The string to search must not be empty.");
      }
      List<int> indexes = new();
      int loc = 0;
      while (true)
      {
        loc = str.IndexOf(value, loc);
        if (loc == -1)
        {
          break;
        }
        else
        {
          indexes.Add(loc);
          loc++;
        }
      }
      return indexes;
    }


    /// <summary>
    /// Counts the instances of a character in a string.
    /// </summary>
    /// <param name="input">Input string to search.</param>
    /// <param name="target">Char to search for.</param>
    /// <exception cref="ArgumentException">Throws ArgumentException if input is null, empty, or whitespace.</exception>
    /// <returns>Number of times char occurs in input.</returns>
    public static int CountOfChar(this string input, char target)
    {
      if (input.IsNullOrEmpty())
      {
        throw new ArgumentException("String to search must not be null or empty.");
      }
      char[] charArray = input.ToCharArray();
      int length = charArray.Length;
      int count = 0;
      for (int i = length - 1; i >= 0; i--)
      {
        if (charArray[i] == target)
        {
          count++;
        }
      }
      return count;
    }
  }
}
