using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout the slope of the line between two points
    /// </summary>
    public class DMQ_FindSlope : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 0;
        private const int MAX_VAR1 = 5;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int p1x = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int p1y = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int p2x = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int p2y = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);

            // ...no zero gradients
            if (p1y == p2y)
            {
                p2y = p1y + 1;
            }

            // ...avoid divide by zero (infinite slope)
            if (p1x == p2x)
            {
                p2x = p1x + 1;
            }

            // create the question
            content[0] = "Find the slope of the line (" + p1x + "," + p1y + ") to (" + p2x + "," + p2y + ")";

            // fill in the answer
            int answerNumerator = p2y - p1y;
            int answerDenominator = p2x - p1x;
            //ReduceFractionToSimplestForm(ref answerNumerator, ref answerDenominator);

            bool negative = answerNumerator * answerDenominator < 0 ? true : false; // deciding whether or not to prepend a minus sign to the fraction
            answerDenominator = System.Math.Abs(answerDenominator);
            answerNumerator = System.Math.Abs(answerNumerator);

            if (answerNumerator % answerDenominator == 0)
            {
                content[1] = (negative ? "-" : "") + (answerNumerator / answerDenominator);
            }
            else
            {
                content[1] = (negative ? "-#" : "") + WrapMathString(answerNumerator + " / " + answerDenominator, 0.8f);
            }

            // create the decoy answers (perturb the correct answer by some amount in the range -5.0f to +5.0f, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                int newp1x = p1x;
                int newp1y = p1y;
                int newp2x = p2x;
                int newp2y = p2y;
                string rep;

                do
                {
                    // perturb one of the coordinates
                    switch (_rnd.Next(0, 4))
                    {
                        case 0:
                            newp1x = PerturbNumber(p1x, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 1:
                            newp1y = PerturbNumber(p1y, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 2:
                            newp2x = PerturbNumber(p2x, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;

                        case 3:
                            newp2y = PerturbNumber(p2y, -5, 5, MIN_VAR1, MAX_VAR1);
                            break;
                    }


                    // ...no zero gradients
                    if (newp1y == newp2y)
                    {
                        newp2y = newp1y + 1;
                    }

                    // avoid divide by zero (infinite slope)
                    if (newp1x == newp2x)
                    {
                        newp2x = newp1x + 1;
                    }

                    // generate the decoy answer string
                    answerNumerator = newp2y - newp1y;
                    answerDenominator = newp2x - newp1x;
                    //ReduceFractionToSimplestForm(ref answerNumerator, ref answerDenominator);

                    negative = answerNumerator * answerDenominator < 0 ? true : false; // deciding whether or not to prepend a minus sign to the fraction
                    answerDenominator = System.Math.Abs(answerDenominator);
                    answerNumerator = System.Math.Abs(answerNumerator);

                    if (answerNumerator % answerDenominator == 0)
                    {
                        rep = (negative ? "-" : "") + (answerNumerator / answerDenominator);
                    }
                    else
                    {
                        rep = (negative ? "-#" : "") + WrapMathString(answerNumerator + " / " + answerDenominator, 0.8f);
                    }

                } while (alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Find Slope";

            return content;
        }
    }
}
