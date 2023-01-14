using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout the slope perpendicular to the line between two points
    /// </summary>
    public class DMQ_FindPerpendicularSlope : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -10;
        private const int MAX_VAR1 = 10;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars and workout the answer
            int p1x = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int p1y = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int p2x = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int p2y = _rnd.Next(MIN_VAR1, MAX_VAR1);

            // ...avoid divide by zero (infinite slope)
            if (p1y == p2y)
            {
                p2y = p1y + 1;
            }

            float answer = -((float)p2x - (float)p1x) / ((float)p2y - (float)p1y);

            // create the question
            content[0] = "Find the slope perpendicular to the line (" + p1x + "," + p1y + ") to (" + p2x + "," + p2y + ")";

            // fill in the answer
            content[1] = GetStringWithSignificantFigures(answer, 3);

            // create the decoy answers (perturb the correct answer by some amount in the range -5.0f to +5.0f, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();

            for (int i = 2; i < 5; i++)
            {
                float pertubation;
                string rep;

                do
                {
                    pertubation = ((float)_rnd.NextDouble() * 10.0f) - 5.0f;
                    rep = GetStringWithSignificantFigures(answer + pertubation, 3);

                } while (pertubation == 0.0f || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Find Perpendicular Slope";

            return content;
        }
    }
}
