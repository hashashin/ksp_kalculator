// -------------------------------------------------------------------------------------------------
// math_helper.cs 0.1
//
// Kalculator plugin math helper.
// Copyright (C) 2014 Iván Atienza
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// 
// Email: mecagoenbush at gmail dot com
// Freenode: hashashin
//
// -------------------------------------------------------------------------------------------------

using System;

namespace kalculator
{

    internal static class MathHelper
    {
        // Secant 
        public static double Sec(double x)
        {
            return 1 / Math.Cos(x);
        }

        // Cosecant
        public static double Cosec(double x)
        {
            return 1 / Math.Sin(x);
        }

        // Cotangent 
        public static double Cotan(double x)
        {
            return 1 / Math.Tan(x);
        }

        // Hyperbolic Secant 
        public static double HSec(double x)
        {
            return 2 / (Math.Exp(x) + Math.Exp(-x));
        }

        // Hyperbolic Cosecant 
        public static double HCosec(double x)
        {
            return 2 / (Math.Exp(x) - Math.Exp(-x));
        }

        // Hyperbolic Cotangent 
        public static double HCotan(double x)
        {
            return (Math.Exp(x) + Math.Exp(-x)) / (Math.Exp(x) - Math.Exp(-x));
        }

        // Logarithm to base N 
        public static double LogN(double x, double n)
        {
            return Math.Log(x) / Math.Log(n);
        }
        
        // Factorial
        public static double fact(double x)
        {
            if (x == 0) return 1;
            else return x * fact(x - 1);
        }
    }
}