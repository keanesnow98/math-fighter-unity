using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Players workout how much they would save on an item that is for sale with N% off
    /// </summary>
    public class DMQ_StoreSale : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 9;
        private const int MIN_VAR2 = 1;
        private const int MAX_VAR2 = 20;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars and answer
            float discount = (float)(_rnd.Next(MIN_VAR1, MAX_VAR1) * 10);
            float price = (float)(_rnd.Next(MIN_VAR2, MAX_VAR2) * 10);
            float answer = price * (discount / 100.0f);
            
            // create the question
            content[0] = "With " + discount + "% discount off of a $" + price + " Widget, how much will you save?";

            // fill in the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -30.0f to
            // +30.0f in steps of 10, but not including zero obviously) - And also must not go negative.

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            for (int i = 2; i < 5; i++)
            {
                float pertubation;
                string rep;

                do
                {
                    pertubation = (float)_rnd.Next(-3, 3) * 10.0f;
                    rep = (price * ((discount + pertubation) / 100.0f)).ToString();
                } while (discount + pertubation < 0 || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "1 Dollar is 100 Cents.  A Quarter is 25 Cents.  A Dime is 10 Cents.  And a Nickel is 5 Cents";

            return content;
        }
    }
}
