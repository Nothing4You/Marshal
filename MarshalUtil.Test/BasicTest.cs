using System;
using System.Collections.Generic;
using Xunit;

namespace MarshalUtil.Test
{
    public class BasicTest
    {
        [Fact]
        public void ZeroDecode()
        {
            object value = new MarshalStream(@"~\x00\x00\x00\x00\x08").GetValue();

            Assert.NotNull(value);
            Assert.True(value is int);
            Assert.Equal(0, (int)value);
        }

        [Fact]
        public void Tuple1OneDecode()
        {
            object value = new MarshalStream(@"~\x00\x00\x00\x00\x25\x09").GetValue();

            Assert.NotNull(value);
            Assert.True(value is List<object>);
            Assert.Equal(1, ((List<object>)value).Count);
            Assert.True(((List<object>)value)[0] is int);
            Assert.Equal(1, ((List<object>)value)[0]);
        }

        [Fact]
        public void WrongInputLength()
        {
            MarshalStream ms = new MarshalStream(@"~\x00\x00\x00\x00\x25");

            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => ms.GetValue());
            Assert.StartsWith("Index is higher than dat!", ex.Message);
        }
    }
}
