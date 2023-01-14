using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must find the least common multiplier of two numbers (i.e. the same as lowest common denominator of two fractions)
    /// </summary>
    public class DMQ_FractionsCommonDenominator : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 2;
        private const int MAX_VAR1 = 12;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the denominators
            int denominator1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int denominator2 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);

            // get the answer
            int answer = FindLowestCommonDenominator(denominator1, denominator2);

            // create the question
            content[0] = "Find the lowest common multiple of " + denominator1.ToString() + " and " + denominator2.ToString();

            // fill in the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the answer denominator by some amount in the range -5.0f to +5.0f, but not including zero obviously, and also should not result in a value less than 2)

            // ...create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                float pertubation;
                string rep;

                do
                {
                    pertubation = _rnd.Next(-5, 5);
                    rep = (answer + pertubation).ToString();

                } while (answer + pertubation < 2 || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Least Common Multiple";

            return content;
        }
    }
}
