using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must simplify the fraction
    /// </summary>
    public class DMQ_FractionsSimplify : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 2;
        private const int MAX_VAR1 = 7;
        private const int MIN_VAR2 = 1;
        private const int MAX_VAR2 = 7;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the fraction
            int multiplier = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1); // applying a multiplier garrauntees that the fraction will require simplifying
            int answerDenominator = _rnd.Next(MIN_VAR2, MAX_VAR2 + 1);
            int answerNumerator = answerDenominator - _rnd.Next(1, answerDenominator);  // must be less than the denominator, but greater than zero

            // create the question
            int questionDenominator = multiplier * answerDenominator;
            int questionNumerator = multiplier * answerNumerator;
            content[0] = "Simplify#" + WrapMathString(questionNumerator.ToString() + " / " + questionDenominator.ToString(), 0.8f);

            // workout the answer (so far we've just come up with some random
            // numerator/denominator, but there is no garrauntee that we can't simplify
            // further than those initial numbers - so we simplify properly to be sure the anwer
            // is the most simplified it can be)
            answerNumerator = questionNumerator;
            answerDenominator = questionDenominator;
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
            content[5] = "This is the hint for Fractions Simplification";

            return content;
        }
    }
}
