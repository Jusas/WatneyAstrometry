using System;
using FluentAssertions;
using WatneyAstrometry.Core.MathUtils;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class MathUtilTests
    {
        [Fact]
        public void Should_solve_least_squares()
        {
            // Example equation from: https://phys.libretexts.org/Bookshelves/Astronomy__Cosmology/Book%3A_Celestial_Mechanics_(Tatum)/01%3A_Numerical_Methods/1.08%3A_Simultaneous_Linear_Equations_N__n
            // Inputs
            // 7a − 6b + 8c −15 = 0
            // 3a + 5b − 2c −27 = 0
            // 2a − 2b + 7c −20 = 0
            // 4a + 2b − 5c −2 = 0
            // 9a − 8b + 7c −5 = 0

            // Expected solution
            // a=2.474 b=5.397 c=3.723

            var solution = Equations.SolveLeastSquares(new (double a1, double a2, double a3, double b)[]
            {
                (7, -6, 8, -15),
                (3, 5, -2, -27),
                (2, -2, 7, -20),
                (4, 2, -5, -2),
                (9, -8, 7, -5)
            });

            Math.Round(solution.x1, 3).Should().Be(2.474);
            Math.Round(solution.x2, 3).Should().Be(5.397);
            Math.Round(solution.x3, 3).Should().Be(3.723);

        }
        
        

    }
}