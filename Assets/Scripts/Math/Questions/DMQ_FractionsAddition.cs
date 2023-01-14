using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must add the two fractions (the result is also reduced to it's simplest form)
    /// </summary>
    public class DMQ_FractionsAddition : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 2;
        private const int MAX_VAR1 = 5;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the fractions
            int denominator1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int numerator1 = denominator1 - _rnd.Next(1, denominator1);  // must be less than the denominator, but greater than zero
            int denominator2 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int numerator2 = denominator2 - _rnd.Next(1, denominator2);  // must be less than the denominator, but greater than zero

            // create the question
            content[0] = WrapMathString(numerator1.ToString() + " / " + denominator1.ToString() + " + " + numerator2.ToString() + " / " + denominator2.ToString(), 0.8f) + "#= ?";

            // workout the answer
            // ...first find the lowest common denominator
            int commonDenominator = FindLowestCommonDenominator(denominator1, denominator2);

            // ...then modify the numerators based on the common denominator
            numerator1 *= commonDenominator / denominator1;
            numerator2 *= commonDenominator / denominator2;

            // ...add the numerators
            int combinedNumerator = numerator1 + numerator2;

            // ...finally get the resulting fraction - simplified if possible
            int answerNumerator = combinedNumerator;
            int answerDenominator = commonDenominator;
            ReduceFractionToSimplestForm(ref answerNumerator, ref answerDenominator);

            // fill in the correct answer
            if (answerNumerator % answerDenominator == 0)
            {
                content[1] = (answerNumerator / answerDenominator).ToString();
            }
            else
            {
                content[1] = WrapMathString(answerNumerator.ToString() + " / " + answerDenominator.ToString(), 0.8f);
            }

            // create the decoy answers (perturb the answer denominator by some amount - we will divide or multiply it by some amount, but also check the resulting denominator is not less than 2
            // the numerator will then be decided depending on the denominator

            // ...create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 0);

            for (int i = 2; i < 5; i++)
            {
                int perturbedNumerator;
                int perturbedDenominator;
                string rep = content[1];

                do
                {
                    if (_rnd.Next(2) == 0)
                    {
                        perturbedDenominator = answerDenominator * _rnd.Next(1, 4);
                    }
                    else
                    {
                        perturbedDenominator = answerDenominator / _rnd.Next(1, 4);
                    }

                    if (perturbedDenominator < 2) continue;

                    perturbedNumerator = perturbedDenominator - _rnd.Next(1, perturbedDenominator);  // must be less than the denominator, but greater than zero
                    ReduceFractionToSimplestForm(ref perturbedNumerator, ref perturbedDenominator);

                    //if (perturbedNumerator % perturbedDenominator == 0)
                    //{
                    //    content[1] = (perturbedNumerator / perturbedDenominator).ToString();
                    //}
                    //else
                    //{
                        rep = WrapMathString(perturbedNumerator.ToString() + " / " + perturbedDenominator.ToString(), 0.8f);
                    //}

                } while (perturbedDenominator < 2 || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Fractions Addition";

            return content;
        }
    }
}
