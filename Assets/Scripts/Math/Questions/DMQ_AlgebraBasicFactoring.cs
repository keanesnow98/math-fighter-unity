using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using System.Diagnostics;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Algebra basic factoring: 4x^3 + 2x^2 == 2x^2(2x + 1)
    /// </summary>
    public class DMQ_AlgebraBasicFactoring : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 6;
        private const int MIN_VAR2 = 0;
        private const int MAX_VAR2 = 2;
        private const int MIN_VAR3 = 1;
        private const int MAX_VAR3 = 6;
        private const int MIN_VAR4 = 1;
        private const int MAX_VAR4 = 2;
        private const int MIN_VAR5 = 1;
        private const int MAX_VAR5 = 6;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // NOTE: basically this is the reverse of the distributive law so we will create
            // the answer first and then workout what the question is.  That way we garrauntee
            // that the question will be solvable with nice integer coefficients.

            // create the vars
            int coefficient1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int exponent1 = coefficient1 > 1 ? _rnd.Next(MIN_VAR2, MAX_VAR2 + 1) : _rnd.Next(1, MAX_VAR2 + 1);  // making sure we have something other than "1" outside the brackets - so if coefficient is 1 then we make sure there is at least "x" to put outside the brackets
            int coefficient2 = RandomPrime(MIN_VAR3, MAX_VAR3 + 1);   // must be a prime
            int exponent2 = _rnd.Next(MIN_VAR4, (3 - exponent1) + 1);  // we want the answer exponent to max at 3 so we make sure the second exponent will add up to 3 or less when we add it to the first exponent
            int coefficient3 = GetRandomUniquePrime(MIN_VAR5, MAX_VAR5 + 1, coefficient2);   // must be a prime and not the same as coefficient 2 unless value is 1 (this is for making sure the 'factoring' will work properly when we create the question)
            int exponent3 = 0;  // always zero or would not make proper sense when we create the question

            // workout the coefficients for the question
            int questionCoefficient1 = coefficient1 * coefficient2;
            int questionCoefficient2 = coefficient1 * coefficient3;
            int questionExponent1 = exponent1 + exponent2;
            int questionExponent2 = exponent1 + exponent3;

            // create the question
            content[0] = WrapMathString(TermAsString(questionCoefficient1, questionExponent1) + " + " + TermAsString(questionCoefficient2, questionExponent2));

            // create the correct answer
            content[1] = WrapMathString(TermAsString(coefficient1, exponent1) + " * ( " + TermAsString(coefficient2, exponent2) + " + " + TermAsString(coefficient3, exponent3) + " )", 0.9f);

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
                    int newCoefficient3 = coefficient3;
                    int newExponent1 = exponent1;
                    int newExponent2 = exponent2;
                    int newExponent3 = exponent3;

                    switch (_rnd.Next(0, 5))
                    {
                        case 0:
                            newCoefficient1 = PerturbNumber(newCoefficient1, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 1:
                            newExponent1 = PerturbNumber(newExponent1, -2, 2, MIN_VAR2, MAX_VAR2);
                            break;

                        case 2:
                            newCoefficient2 = PerturbNumber(newCoefficient2, -5, 5, MIN_VAR3, MAX_VAR3);
                            break;

                        case 3:
                            newExponent2 = PerturbNumber(newExponent2, -2, 2, MIN_VAR4, MAX_VAR4);
                            break;

                        case 4:
                            newCoefficient3 = PerturbNumber(newCoefficient3, -2, 2, MIN_VAR5, MAX_VAR5);
                            break;
                    }

                    pertAnswer = WrapMathString(TermAsString(newCoefficient1, newExponent1) + " * ( " + TermAsString(newCoefficient2, newExponent2) + " + " + TermAsString(newCoefficient3, newExponent3) + " )", 0.9f);

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Basic Factoring";

            return content;
        }

        private string TermAsString(int coefficient, int exponent)
        {
            if (coefficient == 0)
            {
                return " 0";
            }

            if (exponent == 0)
            {
                return " " + coefficient;
            }
            else if (exponent == 1)
            {
                if (coefficient == 1)
                {
                    return " x";
                }
                else
                {
                    return " " + coefficient + " * x";
                }
            }
            else
            {
                if (coefficient == 1)
                {
                    return " x ^ " + exponent;
                }
                else
                {
                    return " " + coefficient + " * x ^ " + exponent;
                }
            }
        }

        // special method to get a random primer number between min/max but not including exception
        // .... except if exception is '1' :)  (a rather idiosynchratic requirement of part of the code,
        // but better as a separate method than 'inline' for readability).
        private int GetRandomUniquePrime(int min, int max, int exception)
        {
            if (exception == 1)
            {
                return RandomPrime(min, max);
            }
            else
            {
                int ret;

                do
                {
                    ret = RandomPrime(min, max);

                } while (ret == exception);

                return ret;
            }
        }
    }
}
