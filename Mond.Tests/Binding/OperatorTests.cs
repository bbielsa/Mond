﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public class OperatorTests
    {
        [MondOperatorModule]
        class MyOperators
        {
            private static Random _rng = new Random();

            [MondOperator("<..>")]
            public static MondValue InclusiveRange(double begin, double end)
            {
                return CreateGenerator(begin, end, true);
            }

            [MondOperator("<...>")]
            public static MondValue ExclusiveRange(double begin, double end)
            {
                return CreateGenerator(begin, end, false);
            }

            [MondOperator("%%")]
            public static double RandomNumber(MondValue range)
            {
                if(range.Type == MondValueType.Number)
                    return _rng.Next(0, (int)range);

                return _rng.Next((int)range["begin"], (int)range["end"] + (range["inclusive"] ? 1 : 0));
            }

            private static MondValue CreateGenerator(double begin, double end, bool inclusive)
            {
                var actualBegin = Math.Min(begin, end);
                var actualEnd = Math.Max(begin, end);
                var range = MondValue.FromEnumerable(Generate(actualBegin, actualEnd, inclusive));
                range["begin"] = begin;
                range["end"] = end;
                range["inclusive"] = inclusive;

                return range;
            }

            private static IEnumerable<MondValue> Generate(double begin, double end, bool inclusive)
            {
                var i = begin;
                for(; i < end; ++i)
                    yield return i;

                if(inclusive)
                    yield return ++i;
            }
        }

        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState();
            MondOperatorModuleBinder.Bind<MyOperators>(_state);
        }

        [Test]
        public void Unary()
        {
            var result = _state.Run("return %% 5;");

            Assert.True(NumberBetween(result, 0, 5));
        }

        [Test]
        public void Binary()
        {
            var result = _state.Run(@"
                return {
                    a: 0 <..> 5,
                    b: 6 <...> 10
                };");

            var a = result["a"];
            var b = result["b"];

            Assert.True(a["inclusive"] == true && a["begin"] == 0 && a["end"] == 5);
            Assert.True(b["inclusive"] == false && b["begin"] == 6 && b["end"] == 10);
        }
        
        [Test]
        public void Mixed()
        {
            var result = _state.Run(@"
                return {
                    a: %%(0 <..> 5),
                    b: %%(6 <...> 10)
                };");

            Assert.True(NumberBetween(result["a"], 0, 5));
            Assert.True(NumberBetween(result["b"], 6, 9));
        }

        private static bool NumberBetween(double n, double lo, double hi)
        {
            return n >= lo && n <= hi;
        }
    }
}
