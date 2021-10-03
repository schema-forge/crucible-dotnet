using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Xunit;

namespace Extensions
{
  [Trait("Crucible", "")]
  public class StringExtensionTests
  {
    /// <summary>
    /// Tests to ensure IsNullOrEmpty() catches whitespace, empty string, and null.
    /// </summary>
    /// <param name="value">Value to check for emptiness.</param>
    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData(null)]
    public void StringIsNullOrEmpty(string value) => Assert.True(value.IsNullOrEmpty());

    /// <summary>
    /// Tests to ensure IsNullOrEmpty() will not give false positives on things that start and end with spaces or regular strings.
    /// </summary>
    /// <param name="value">Value to check for emptiness.</param>
    [Theory]
    [InlineData(" Not Empty ")]
    [InlineData("notthiseither")]
    public void StringIsNotNullOrEmpty(string value) => Assert.False(value.IsNullOrEmpty());

    /// <summary>
    /// Tests to ensure that <see cref="StringExtensions.AllIndexesOf(string, char)"/> will throw an exception if one of the arguments is null.
    /// </summary>
    /// <param name="value1">Value that the <see cref="StringExtensions.AllIndexesOf(string, char)"/> method will be called on.</param>
    /// <param name="value2">Char that will be passed to <see cref="StringExtensions.AllIndexesOf(string, char)"/></param>
    [Theory]
    [InlineData(null, "notnull")]
    [InlineData("notnull", null)]
    public void AllIndexesOfVoid(string value1, string value2) => Assert.Throws<ArgumentException>(() => value1.AllIndexesOf(value2));

    /// <summary>
    /// Tests to ensure that <see cref="StringExtensions.AllIndexesOf(string, char)"/> will count the expected number of instances of strings, ensuring that there are no off-by-one errors or issues with repetitive structure.
    /// </summary>
    /// <param name="value1">Value that the <see cref="StringExtensions.AllIndexesOf(string, char)"/> method will be called on.</param>
    /// <param name="value2">Substring that will be passed to <see cref="StringExtensions.AllIndexesOf(string, char)"/></param>
    /// <param name="expectedLocations">Expected indexes to be returned.</param>
    [Theory]
    [InlineData("Babadook", "oo", new int[] { 5 })]
    [InlineData("It's in the blood", "i", new int[] { 5 })]
    [InlineData("Oh it's in the bloooood", "oo", new int[] { 17, 19 })] // When searching a string, the full match will be excluded from the next search step, so there are only two locations returned.
    [InlineData("Against all the evils that OSHA can conjure", "I send unto them", new int[] { })]
    public void AllIndexesOfString(string value1, string value2, int[] expectedLocations) => Assert.Equal(value1.AllIndexesOf(value2), expectedLocations.ToList());

    /// <summary>
    /// Tests to ensure that <see cref="StringExtensions.AllIndexesOf(string, char)"/> will count the expected number of instances of characters, ensuring that there are no off-by-one errors.
    /// </summary>
    /// <param name="value1">Value that the <see cref="StringExtensions.AllIndexesOf(string, char)"/> method will be called on.</param>
    /// <param name="value2">Character that will be passed to <see cref="StringExtensions.AllIndexesOf(string, char)"/></param>
    /// <param name="expectedLocations">Expected indexes to return.</param>
    [Theory]
    [InlineData("only you.", 'o', new int[] { 0, 6 })]
    [InlineData("Rip and tear, until it is done.", ';', new int[] { })]
    public void AllIndexesOfChar(string value1, char value2, int[] expectedLocations) => Assert.Equal(value1.AllIndexesOf(value2), expectedLocations.ToList());

    /// <summary>
    /// Tests to ensure that <see cref="StringExtensions.CountOfChar(string, char)"/> counts characters correctly at the end, beginning, and middle of a string.
    /// </summary>
    /// <param name="searchedValue">String that the <see cref="StringExtensions.CountOfChar(string, char)"/> method will be called on.</param>
    /// <param name="charToFind">Char that will be passed to <see cref="StringExtensions.CountOfChar(string, char)"/></param>
    /// <param name="expectedNumber">Number of times the given character actually occurs in the given string.</param>
    [Theory]
    [InlineData("AND", 'D', 1)]
    [InlineData("I SET FIRE", 'I', 2)]
    [InlineData("TO THE RAIN", 'B', 0)]
    [InlineData("WATCHED IT BURN AS I\nTOUCHED YOUR FACE", '\n', 1)]
    public void CountOfChar(string searchedValue, char charToFind, int expectedNumber) => Assert.Equal(expectedNumber, searchedValue.CountOfChar(charToFind));

    /// <summary>
    /// Ensures that <see cref="StringExtensions.CountOfChar(string, char)"/> throws an exception on a null string.
    /// </summary>
    [Fact]
    public void CountOfVoid()
    {
      string searchedString = null;
      Assert.Throws<ArgumentException>(() => searchedString.CountOfChar('c'));
    }
  }
}
