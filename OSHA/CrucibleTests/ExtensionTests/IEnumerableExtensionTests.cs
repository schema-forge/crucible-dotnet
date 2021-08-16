using System;
using System.Collections.Generic;
using System.Linq;
using SchemaForge.Crucible;
using SchemaForge.Crucible.Extensions;
using Xunit;

namespace Extensions
{
  public class IEnumerableExtensionTests
  {
    private class DecoyObject
    {
      private string SneakyString { get; }
      public DecoyObject(string inputString)
      {
        SneakyString = inputString + inputString.Reverse().Join("");
      }

      public override string ToString()
      {
        return SneakyString;
      }
    }
    [Fact]
    public void IEnumerableIntArrayCharJoinTest()
    {
      int[] testArray = { 6, 6, 4, 7, 5 };
      string expected = "6,6,4,7,5";
      Assert.Equal(expected,testArray.Join(','));
    }

    [Fact]
    public void IEnumerableIntArrayStringJoinTest()
    {
      int[] testArray = { 6, 6, 4, 7, 5 };
      string expected = "6, 6, 4, 7, 5";
      Assert.Equal(expected, testArray.Join(", "));
    }

    [Fact]
    public void IEnumerableIntHashSetCharJoinTest()
    {
      HashSet<int> testArray = new() { 3, 9, 7, 1, 5 };
      string expected = "3,9,7,1,5";
      Assert.Equal(expected, testArray.Join(','));
    }

    [Fact]
    public void IEnumerableIntHashSetStringJoinTest()
    {
      HashSet<int> testArray = new() { 3, 9, 7, 1, 5 };
      string expected = "3, 9, 7, 1, 5";
      Assert.Equal(expected, testArray.Join(", "));
    }

    [Fact]
    public void DecoyObjectArrayTest()
    {
      DecoyObject[] testArray = { new DecoyObject("Our lives are not our own."), new DecoyObject("We are bound to others,"), new DecoyObject("past and present.") };
      string expected = "Our lives are not our own..nwo ruo ton era sevil ruO We are bound to others,,srehto ot dnuob era eW past and present..tneserp dna tsap";
      Assert.Equal(expected, testArray.Join(' '));
    }

    [Fact]
    public void AnyFatal_IEnumerable_ReturnsTrue()
    {
      HashSet<Error> errorSet = new() { new Error("Isildur") };
      Assert.True(errorSet.AnyFatal());
    }

    [Fact]
    public void AnyFatal_IEnumerable_ReturnsFalse()
    {
      HashSet<Error> errorSet = new() { new Error("Boromir",Severity.Warning) };
      Assert.False(errorSet.AnyFatal());
    }

    [Fact]
    public void AnyFatal_IList_ReturnsFalse()
    {
      Error[] errorList = { new Error("Boromir", Severity.Warning) };
      Assert.False(errorList.AnyFatal());
    }
  }
}
