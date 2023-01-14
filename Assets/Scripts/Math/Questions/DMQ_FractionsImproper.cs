using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player has to convert the mixed number (x + y / z) to an improper fraction (a / b)
    /// </summary>
    public class DMQ_FractionsImproper : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 12;
        private const int MIN_VAR2 = 2;
        private const int MAX_VAR2 = 12;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars for the question
            int denominator = _rnd.Next(MIN_VAR2, MAX_VAR2 + 1);
            int numerator = denominator - _rnd.Next(1, denominator);  // must be less than the denominator, but greater than zero
            int wholeNumber = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);

            // workout the numerator for the answer
            int answerNumerator = (wholeNumber * denominator) + numerator;
            
            // create the question
            content[0] = WrapMathString(wholeNumber.ToString() + " + " + numerator.ToString() + " / " + denominator.ToString(), 0.8f) + "#= ?";

            // fill in the correct answer
            content[1] = WrapMathString(answerNumerator.ToString() + " / " + denominator.ToString(), 0.8f);

            // create the decoy answers (perturb the answer numerator by some amount in the range -5.0f to +5.0f, but not including zero obviously and can't result in a negative or zero numerator)

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
                    rep = WrapMathString((answerNumerator + pertubation).ToString() + " / " + denominator.ToString(), 0.8f); ;

                } while (answerNumerator + pertubation < 1 || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for mixed number to fraction";

            return content;
        }
    }
}
