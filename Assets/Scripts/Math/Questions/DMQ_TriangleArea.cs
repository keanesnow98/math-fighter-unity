using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout the area of the triangle
    /// </summary>
    public class DMQ_TriangleArea : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 12;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the side lengths and workout the answer
            int length1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int length2 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            float answer = ((float)length1 * (float)length2 / 2.0f);

            // create the question
            content[0] = "A triangle with base " + length1 + " inches and height " + length2 + " inches has an area of?";

            // fill in the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the answer by some amount)

            // ...create the pertubations - should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 0);

            for (int i = 2; i < 5; i++)
            {
                float pertubation;
                string rep = content[1];

                do
                {
                    pertubation = (float)_rnd.Next(-20, 21) / 2.0f;
                    rep = (answer + pertubation).ToString();

                } while (answer + pertubation <= 0.0f || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Triangle Area";

            return content;
        }
    }
}
