using System;
using System.Collections.Generic;
using System.Linq;
using schemaforge.Crucible;
using schemaforge.Crucible.Extensions;
using Xunit;

namespace Extensions
{
  public class ArrayExtensionTests
  {
    [Fact]
    public void CloneArrayEmptyTest()
    {
      int[] emptyArray = null;
      Assert.Throws<ArgumentNullException>(() => emptyArray.CloneArray());
    }

    [Fact]
    public void CloneArrayValueTypeTest()
    {
      int[] firstArray = { 3, 7, 5, 11 };
      int[] secondArray = firstArray.CloneArray();

      bool allMatch = true;
      for (int i = secondArray.Length - 1; i-- > 0;)
      {
        if (firstArray[i] != secondArray[i])
        {
          allMatch = false;
        }
      }
      Assert.True(allMatch);
    }

    [Fact]
    public void CloneArrayReferenceTypeTest()
    {
      string[] firstArray = { "All", "my", "friends", "are", "watching" };
      string[] secondArray = firstArray.CloneArray();

      bool allMatch = true;
      for (int i = secondArray.Length - 1; i-- > 0;)
      {
        if (firstArray[i] != secondArray[i])
        {
          allMatch = false;
        }
      }
      Assert.True(allMatch);
    }

    [Fact]
    public void CloneEmptyArrayTest()
    {
      string[] firstArray = { };
      string[] secondArray = firstArray.CloneArray();

      Assert.True(secondArray.Length == 0);
    }

    [Theory]
    [InlineData(new int[] { 3, 5, 7 }, new int[] { 7, 5, 3 })] // Odd number of items.
    [InlineData(new int[] { 3, 5, 8, 6 }, new int[] { 6, 8, 5, 3 })] // Even number of items.
    [InlineData(new int[] { }, new int[] { })] // Empty arrays.
    public void ReverseArrayTests(int[] originalArray, int[] expectedArray)
    {
      Assert.Equal(expectedArray, originalArray.Reverse());
    }
  }
}
