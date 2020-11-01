﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
namespace CS_Classes
{
    public class Sieve
    {
        public List<BigInteger> GetPrimeNumbers(int count)
        {
            var output = new List<BigInteger>();
            for (BigInteger n = 2; output.Count < count; n++)
            {
                if (output.All(x => n % x != 0)) output.Add(n);
            }
            return output;
        }
    }
}
