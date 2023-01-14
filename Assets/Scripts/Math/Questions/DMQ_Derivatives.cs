using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using System.Diagnostics;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Players must find the derivatives of polynomials containing three terms.
    /// </summary>
    public class DMQ_Derivatives : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -12;
        private const int MAX_VAR1 = 12;
        private const int MIN_VAR2 = -12;
        private const int MAX_VAR2 = 12;
        private const int MIN_VAR3 = -12;
        private const int MAX_VAR3 = 12;
        private const int MIN_VAR4 = -12;
        private const int MAX_VAR4 = 12;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int coefficient1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 1);
            int exponent1 = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2 + 1, 1);
            int coefficient2 = GetNonZeroRandomNumber(MIN_VAR3, MAX_VAR3 + 1, 1);
            int exponent2 = _rnd.Next(MIN_VAR4, MAX_VAR4 + 1);

            // create the question
            content[0] = "f(x) =#" + WrapMathString(FirstTermAsString(coefficient1, exponent1) + TermAsString(coefficient2, exponent2));

            // create the correct answer
            content[1] = "@height{0.8}#f'(x) =#" + WrapMathString(FirstTermAsString(coefficient1 * exponent1, exponent1 - 1) + TermAsString(coefficient2 * exponent2, exponent2 - 1));

            // create the decoy answers (perturb one of the variables in answer)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    // perturb one of the original coefficients or exponents
                    int newCoefficient1 = coefficient1;
                    int newCoefficient2 = coefficient2;
                    int newExponent1 = exponent1;
                    int newExponent2 = exponent2;

                    switch (_rnd.Next(0, 5))
                    {
                        case 0:
                            newCoefficient1 = PerturbNumber(newCoefficient1, -5, 5, MIN_VAR1, MAX_VAR1);
                            if (newCoefficient1 == 0) newCoefficient1 = 1;
                            break;

                        case 1:
                            newExponent1 = PerturbNumber(newExponent1, -5, 5, MIN_VAR2, MAX_VAR2);
                            if (newExponent1 == 0) newExponent1 = 1;
                            break;

                        case 2:
                            newCoefficient2 = PerturbNumber(newCoefficient2, -5, 5, MIN_VAR3, MAX_VAR3);
                            if (newCoefficient2 == 0) newCoefficient2 = 1;
                            break;

                        case 3:
                            newExponent2 = PerturbNumber(newExponent2, -5, 5, MIN_VAR4, MAX_VAR4);
                            break;
                    }

                    pertAnswer = "@height{0.8}#f'(x) =#" + WrapMathString(FirstTermAsString(newCoefficient1 * newExponent1, newExponent1 - 1) + TermAsString(newCoefficient2 * newExponent2, newExponent2 - 1));

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Basic Derivatives";

            return content;
        }

        private string FirstTermAsString(int coefficient, int exponent)
        {
            Assert.Fatal(coefficient != 0, "FirstTermAsString() - coefficient is zero");

            string TermNoNegs = TermAsStringNoNeg(coefficient, exponent);

            if (coefficient < 0)
            {
                return "-" + TermNoNegs;
            }
            else
            {
                return TermNoNegs;
            }
        }

        private string TermAsString(int coefficient, int exponent)
        {
            string TermNoNegs = TermAsStringNoNeg(coefficient, exponent);

            if (coefficient == 0)
            {
                return TermNoNegs;
            }
            else if (coefficient < 0)
            {
                return " - " + TermNoNegs;
            }
            else
            {
                return " + " + TermNoNegs;
            }
        }

        private string TermAsStringNoNeg(int coefficient, int exponent)
        {
            if (coefficient == 0) return " ";

            int absCoefficient = System.Math.Abs(coefficient);

            if (exponent == 0)
            {
                return absCoefficient.ToString();
            }
            else if (exponent == 1)
            {
                if (absCoefficient == 1)
                {
                    return "x";
                }
                else
                {
                    return absCoefficient.ToString() + " * x";
                }
            }
            else
            {
                if (absCoefficient == 1)
                {
                    return "x ^ " + exponent;
                }
                else
                {
                    return absCoefficient.ToString() + " * x ^ " + exponent;
                }
            }
        }
    }
}
