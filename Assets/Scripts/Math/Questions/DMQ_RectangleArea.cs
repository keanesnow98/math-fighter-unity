using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout the area of the rectangle
    /// </summary>
    public class DMQ_RectangleArea : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 12;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the side lengths and workout the answer
            int length1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int length2 = _rnd.Next(0, 3) == 0 ? length1 : _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int answer = length1 * length2;

            // create the question
            if (length1 == length2)
            {
                content[0] = "A square with sides of " + length1 + " inches has an area of?";
            }
            else
            {
                content[0] = "A " + length1 + " inch by " + length2 + " inch rectangle has an area of?";
            }

            // fill in the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the answer by some amount)

            // ...create the pertubations - should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 0);

            for (int i = 2; i < 5; i++)
            {
                int pertubation;
                string rep = content[1];

                do
                {
                    pertubation = _rnd.Next(-10, 11);
                    rep = (answer + pertubation).ToString();

                } while (answer + pertubation < 1 || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Rectangle Area";

            return content;
        }
    }
}
