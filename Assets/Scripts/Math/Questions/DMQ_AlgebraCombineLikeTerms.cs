using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Algebra combining like terms: e.g. 2x + 3y + 5x = 7x + 3y
    /// </summary>
    public class DMQ_AlgebraCombineLikeTerms : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -10;
        private const int MAX_VAR1 = 10;
        private const int MIN_VAR2 = -10;
        private const int MAX_VAR2 = 10;
        private const int MIN_VAR3 = -10;
        private const int MAX_VAR3 = 10;


        public override string[] GetContent()
        {
            if (_rnd.Next(0, 2) == 0)
            {
                return GetContentWith2Terms();
            }
            else
            {
                return GetContentWith3Terms();
            }
        }

        private string[] GetContentWith3Terms()
        {
            string[] content = new string[6];

            // create the coefficients
            int var1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 1);
            int var2 = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2 + 1, 1);
            int var3 = GetNonZeroRandomNumber(MIN_VAR3, MAX_VAR3 + 1, 1);

            // decide what letters to use (at least two will always be identical since we always have 3 terms and only 2 letters to choose from)
            bool isX1 = (_rnd.Next(0, 2) == 0);
            bool isX2 = (_rnd.Next(0, 2) == 0);
            bool isX3 = (_rnd.Next(0, 2) == 0);

            // create the question
            content[0] = WrapMathString(FirstTermAsString(var1, isX1) + TermAsString(var2, isX2) + TermAsString(var3, isX3));

            // create the correct answer
            content[1] = CreateAnswerString(var1, var2, var3, isX1, isX2, isX3);

            // create the decoy answers (perturb one of the coefficients)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    // perturb one of the original coefficients (but must not be zero)
                    int newvar1 = var1;
                    int newvar2 = var2;
                    int newvar3 = var3;

                    switch (_rnd.Next(0, 3))
                    {
                        case 0:
                            newvar1 = PerturbNumber(newvar1, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 1:
                            newvar2 = PerturbNumber(newvar2, -5, 5, MIN_VAR2, MAX_VAR2);
                            break;

                        case 2:
                            newvar3 = PerturbNumber(newvar3, -5, 5, MIN_VAR3, MAX_VAR3);
                            break;
                    }

                    pertAnswer = CreateAnswerString(newvar1, newvar2, newvar3, isX1, isX2, isX3);

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Combining Like Terms";

            return content;
        }

        private string[] GetContentWith2Terms()
        {
            string[] content = new string[6];

            // create the coefficients
            int var1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 1);
            int var2 = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2 + 1, 1);

            // decide what letter to use
            bool isX = (_rnd.Next(0, 2) == 0);

            // create the question
            content[0] = WrapMathString(FirstTermAsString(var1, isX) + TermAsString(var2, isX));

            // create the correct answer
            content[1] = CreateAnswerString(var1, var2, 0, isX, isX, false);

            // create the decoy answers (perturb one of the coefficients)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    // perturb one of the original coefficients (but must not be zero)
                    int newvar1 = var1;
                    int newvar2 = var2;

                    switch (_rnd.Next(0, 2))
                    {
                        case 0:
                            newvar1 = PerturbNumber(newvar1, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 1:
                            newvar2 = PerturbNumber(newvar2, -5, 5, MIN_VAR2, MAX_VAR2);
                            break;
                    }

                    pertAnswer = CreateAnswerString(newvar1, newvar2, 0, isX, isX, false);

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Combining Like Terms";
            
            return content;
        }

        private string CreateAnswerString(int var1, int var2, int var3, bool isX1, bool isX2, bool isX3)
        {
            int xCoefficient = 0;
            int yCoefficient = 0;

            if (isX1)
            {
                xCoefficient += var1;
            }
            else
            {
                yCoefficient += var1;
            }

            if (isX2)
            {
                xCoefficient += var2;
            }
            else
            {
                yCoefficient += var2;
            }

            if (isX3)
            {
                xCoefficient += var3;
            }
            else
            {
                yCoefficient += var3;
            }

            if (xCoefficient == 0 && yCoefficient == 0)
            {
                return WrapMathString(FirstTermAsString(1, true));
            }
            if (xCoefficient == 0)
            {
                return WrapMathString(FirstTermAsString(yCoefficient, false));
            }
            else if (yCoefficient == 0)
            {
                return WrapMathString(FirstTermAsString(xCoefficient, true));
            }
            else
            {
                return WrapMathString(FirstTermAsString(xCoefficient, true) + TermAsString(yCoefficient, false));
            }
        }

        private string TermAsString(int coefficient, bool isX)
        {
            if (coefficient == 1)
            {
                return (" + " + (isX ? "x" : "y"));
            }
            else if (coefficient == -1)
            {
                return (" - " + (isX ? "x" : "y"));
            }
            else
            {
                return (coefficient < 0 ? " - " : " + ") + System.Math.Abs(coefficient).ToString() + (isX ? " * x" : " * y");
            }
        }

        private string FirstTermAsString(int coefficient, bool isX)
        {
            if (coefficient == 1)
            {
                return (isX ? "x" : "y");
            }
            else if (coefficient == -1)
            {
                return (isX ? "-x" : "-y");
            }
            else
            {
                return coefficient.ToString() + (isX ? " * x" : " * y");
            }
        }
    }
}
