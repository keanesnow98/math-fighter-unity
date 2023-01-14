using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Evaluate a number raised to an exponent
    /// </summary>
    public class DMQ_SimpleExponents : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -10;
        private const int MAX_VAR1 = 10;
        private const int MIN_VAR2 = 0;
        private const int MAX_VAR2 = 4;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // get the number, get the exponent, and workout the answer
            int number = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 2);
            int exponent = _rnd.Next(MIN_VAR2, MAX_VAR2 + 1);
            int answer = (int)System.Math.Pow(number, exponent);

            // create the question
            if (number > 0)
            {
                content[0] = WrapMathString(number.ToString() + " ^ " + exponent.ToString()) + "#= ?";
            }
            else
            {
                content[0] = WrapMathString("( " + number.ToString() + " ) ^ " + exponent.ToString()) + "#= ?";
            }

            // create the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();

            for (int i = 2; i < 5; i++)
            {
                int pertubation;

                do
                {
                    pertubation = _rnd.Next(-5, 5);

                } while (pertubation == 0 || alreadyUsed.ContainsKey(pertubation));

                content[i] = (answer + pertubation).ToString();
                alreadyUsed.Add(pertubation, i);
            }

            // provide a hint text
            content[5] = "This is the hint for simple exponents";

            return content;
        }
    }
}
