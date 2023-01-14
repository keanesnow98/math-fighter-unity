using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Algebra distributive law: a(bx + cy) = px + qy
    /// </summary>
    public class DMQ_AlgebraDitributiveLaw : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 12;
        private const int MIN_VAR2 = 1;
        private const int MAX_VAR2 = 12;
        private const int MIN_VAR3 = 1;
        private const int MAX_VAR3 = 12;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int var1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int var2 = _rnd.Next(MIN_VAR2, MAX_VAR2 + 1);
            int var3 = _rnd.Next(MIN_VAR3, MAX_VAR3 + 1);

            // workout the coefficients for the answer
            int answerCoefficient1 = var1 * var2;
            int answerCoefficient2 = var1 * var3;

            // decide whether or not to include a second letter variable
            bool includeY = (_rnd.Next(0, 3) == 0);
            
            // create the question
            content[0] = WrapMathString(var1.ToString() + " * ( " + var2.ToString() + " * x + " + var3.ToString() + (includeY ? " * y )" : ""));

            // create the correct answer
            content[1] = WrapMathString(answerCoefficient1.ToString() + " * x + " + answerCoefficient2.ToString() + (includeY ? " * y" : ""));

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    // perturb one of the original coefficients
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

                    int newAnswerCoefficient1 = newvar1 * newvar2;
                    int newAnswerCoefficient2 = newvar1 * newvar3;

                    pertAnswer = WrapMathString(newAnswerCoefficient1.ToString() + " * x + " + newAnswerCoefficient2.ToString() + (includeY ? " * y" : ""));

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for addition";

            return content;
        }
    }
}
