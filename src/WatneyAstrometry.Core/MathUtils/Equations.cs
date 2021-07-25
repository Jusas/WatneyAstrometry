using System.Collections.Generic;
using System.Linq;

namespace WatneyAstrometry.Core.MathUtils
{
    public static class Equations
    {
        /// <summary>
        /// Solving least squares, for solving the plate constants.
        /// </summary>
        /// <param name="equationsOfCondition"></param>
        /// <returns></returns>
        public static (double x1, double x2, double x3) SolveLeastSquares(
            IList<(double a1, double a2, double a3, double b)> equationsOfCondition)
        {
            
            //var coefficients = equationsOfCondition.Select(e => e.a1 + e.a2 + e.a3 + e.b).ToArray();
            //var checksumA1 = equationsOfCondition.Select((e, idx) => (e, idx)).Sum(e => e.e.a1 * coefficients[e.idx]);
            //var checksumA2 = equationsOfCondition.Select((e, idx) => (e, idx)).Sum(e => e.e.a2 * coefficients[e.idx]);
            //var checksumA3 = equationsOfCondition.Select((e, idx) => (e, idx)).Sum(e => e.e.a3 * coefficients[e.idx]);

            // See: https://phys.libretexts.org/Bookshelves/Astronomy__Cosmology/Book%3A_Celestial_Mechanics_(Tatum)/01%3A_Numerical_Methods/1.08%3A_Simultaneous_Linear_Equations_N__n

            var A11 = equationsOfCondition.Sum(e => e.a1 * e.a1);
            var A12 = equationsOfCondition.Sum(e => e.a1 * e.a2);
            var A13 = equationsOfCondition.Sum(e => e.a1 * e.a3);
            var B1 = equationsOfCondition.Sum(e => e.a1 * e.b);
            var A22 = equationsOfCondition.Sum(e => e.a2 * e.a2);
            var A23 = equationsOfCondition.Sum(e => e.a2 * e.a3);
            var B2 = equationsOfCondition.Sum(e => e.a2 * e.b);
            var A33 = equationsOfCondition.Sum(e => e.a3 * e.a3);
            var B3 = equationsOfCondition.Sum(e => e.a3 * e.b);


            // YA: A11 * x + A12 * y + A13 * z + B1 = 0
            // YB: A12 * x + A22 * y + A23 * z + B2 = 0
            // YC: A13 * x + A23 * y + A33 * z + B3 = 0

            // See: https://www.cliffsnotes.com/study-guides/algebra/algebra-ii/linear-equations-in-three-variables/linear-equations-solutions-using-determinants-with-three-variables

            // D = A11   A12   A13
            //     A12   A22   A23
            //     A13   A23   A33


            var denominator = A11 * (A22*A33 - A23*A23) - A12 * (A12*A33 - A13*A23) + A13 * (A12*A23 - A13*A22);

            // Dx = -B1  A12   A13
            //      -B2  A22   A23
            //      -B3  A23   A33

            var dx = (-B1) * (A22*A33 - A23*A23) - (-B2) * (A12*A33 - A13*A23) + (-B3) * (A12*A23 - A13*A22);

            // Dy = A11  -B1   A13
            //      A12  -B2   A23
            //      A13  -B3   A33

            var dy = A11 * ((-B2)*A33 - A23*(-B3)) - A12 * ((-B1)*A33 - A13*(-B3)) + A13 * ((-B1)*A23 - A13*(-B2));

            // Dz = A11  A12   -B1
            //      A12  A22   -B2
            //      A13  A23   -B3

            var dz = A11 * (A22*(-B3) - (-B2)*A23) - A12 * (A12*(-B3) - (-B1)*A23) + A13 * (A12*(-B2) - (-B1)*A22);

            var x1 = dx / denominator;
            var x2 = dy / denominator;
            var x3 = dz / denominator;

            return (x1, x2, x3);

            

        }

    }
}