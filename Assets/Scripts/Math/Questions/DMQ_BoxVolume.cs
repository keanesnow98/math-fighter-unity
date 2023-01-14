using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout the volume of the box
    /// </summary>
    public class DMQ_BoxVolume : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 12;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the side lengths and workout the answer
            int length1 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int length2 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int length3 = _rnd.Next(MIN_VAR1, MAX_VAR1 + 1);
            int answer = length1 * length2 * length3;

            // create the question
            content[0] = "A box of height " + length1 + ", width " + length2 + " , and height " + length3 + " has a volume of?";

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
            content[5] = "This is the hint for Box Volume";

            return content;
        }
    }
}
