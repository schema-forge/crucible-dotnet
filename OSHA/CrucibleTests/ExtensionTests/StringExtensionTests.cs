using System;
using System.Collections.Generic;
using System.Linq;
using schemaforge.Crucible;
using schemaforge.Crucible.Extensions;
using Xunit;

namespace Extensions
{
    [Trait("Crucible", "")]
    public class StringExtensionTests
    {
        [Theory]
        [InlineData(" ")]
        [InlineData("")]
        [InlineData(null)]
        public void StringIsNullOrEmpty(string value)
        {
            Assert.True(value.IsNullOrEmpty());
        }

        [Theory]
        [InlineData(" Not Empty ")]
        [InlineData("notthiseither")]
        public void StringIsNotNullOrEmpty(string value)
        {
            Assert.False(value.IsNullOrEmpty());
        }

        [Theory]
        [InlineData(null,"notnull")]
        [InlineData("notnull",null)]
        public void AllIndexesOfVoid(string value1, string value2)
        {
            Assert.Throws<ArgumentException>(() => value1.AllIndexesOf(value2));
        }

        [Theory]
        [InlineData("Babadook","oo",new int[] { 5 })]
        [InlineData("It's in the blood","i",new int[] { 5 })]
        [InlineData("Oh it's in the bloooood","oo",new int[] { 17, 19 })]
        [InlineData("Against all the evils that OSHA can conjure","I send unto them",new int[] { })]
        public void AllIndexesOfString(string value1, string value2, int[] expectedLocations)
        {
            Assert.Equal(value1.AllIndexesOf(value2), expectedLocations.ToList());
        }

        [Theory]
        [InlineData("only you.", 'o',new int[] { 0, 6 })]
        [InlineData("Rip and tear, until it is done.", ';', new int[] { })]
        public void AllIndexesOfChar(string value1, char value2, int[] expectedLocations)
        {
            Assert.Equal(value1.AllIndexesOf(value2), expectedLocations.ToList());
        }

        [Theory]
        [InlineData("I SET FIRE",'I',2)]
        [InlineData("TO THE RAIN",'B',0)]
        [InlineData("WATCHED IT BURN AS I\nTOUCHED YOUR FACE",'\n',1)]
        public void CountOfChar(string searchedValue, char charToFind, int expectedNumber)
        {
            Assert.Equal(searchedValue.CountOfChar(charToFind), expectedNumber);
        }

        [Fact]
        public void CountOfVoid()
        {
            string searchedString = null;
            Assert.Throws<ArgumentException>(() => searchedString.CountOfChar('c'));
        }
    }
}
