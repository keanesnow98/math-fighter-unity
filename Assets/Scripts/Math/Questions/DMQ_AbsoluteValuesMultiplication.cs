using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must find the value of an multiplication problem that includes one or more terms
    /// of the kind |x|.  There will be two terms and there can be four cases: "|x * y|",
    /// "|x| * y", "x * |y|", and "|x| * |y|".
    /// </summary>
    public class DMQ_AbsoluteValuesMultiplication : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -12;
        private const int MAX_VAR1 = 13;
        private const int MIN_VAR2 = -12;
        private const int MAX_VAR2 = 13;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int var1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1, _rnd.Next(1, MAX_VAR1));
            int var2 = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2, _rnd.Next(1, MAX_VAR2));
            
            // decide which problem type to use the vars in
            bool prefixNeg1 = false;
            bool prefixNeg2 = false;
            string question = null;
            int answer = 0;

            // NOTE: the '|' symbol is represented by the ` symbol due to a font creation goof (my fault not ahmed's as it happens)
            switch (_rnd.Next(0, 4))
            {
                case 0: // |x * y|
                    prefixNeg1 = (_rnd.Next(2) == 0);
                    question = (prefixNeg1 ? "-" : "") + "`" + var1 + " x " + var2 + "`";
                    answer = prefixNeg1 ? -System.Math.Abs(var1 * var2) : System.Math.Abs(var1 * var2);
                    break;

                case 1: // |x| 8 y
                    prefixNeg1 = (_rnd.Next(2) == 0);
                    question = (prefixNeg1 ? "-" : "") + "`" + var1 + "` x " + var2;
                    answer = prefixNeg1 ? -System.Math.Abs(var1) * var2 : System.Math.Abs(var1) * var2;
                    break;

                case 2: // x * |y|
                    prefixNeg2 = (_rnd.Next(2) == 0);
                    question = var1.ToString() + " x " + (prefixNeg2 ? "-" : "") + "`" + var2 + "`";
                    answer = prefixNeg2 ? var1 * (-System.Math.Abs(var2)) : var1 * System.Math.Abs(var2);
                    break;

                case 3: // |x| * |y|
                    prefixNeg1 = (_rnd.Next(2) == 0);
                    prefixNeg2 = (_rnd.Next(2) == 0);
                    question = (prefixNeg1 ? "-" : "") + "`" + var1 + "` x " + (prefixNeg2 ? "-" : "") + "`" + var2 + "`";
                    answer = (prefixNeg1 ? (-System.Math.Abs(var1)) : System.Math.Abs(var1)) * (prefixNeg2 ? (-System.Math.Abs(var2)) : System.Math.Abs(var2));
                    break;
            }

            content[0] = question + " = ?";
            content[1] = answer.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();
            alreadyUsed.Add(answer, 1);

            content[2] = (-answer).ToString();
            alreadyUsed.Add(-answer, 2);

            for (int i = 3; i < 5; i++)
            {
                int pertubation;
                int pertResult;

                do
                {
                    pertubation = _rnd.Next(-5, 5);
                    pertResult = answer + pertubation;

                    if (_rnd.Next(2) == 0)
                    {
                        pertResult = -pertResult;
                    }

                } while (alreadyUsed.ContainsKey(pertResult));

                content[i] = pertResult.ToString();
                alreadyUsed.Add(pertResult, i);
            }

            // provide a hint text
            content[5] = "This is the hint for absolute value multiplication";

            return content;
        }
    }
}
