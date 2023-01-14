using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates simple pattern recognition problems in the form: 1, 2, 3, 4... what's next?
    /// </summary>
    public class DMQ_PatternRecognition : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 5;
        private const int MIN_VAR2 = 1;
        private const int MAX_VAR2 = 20;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int step = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int start = _rnd.Next(MIN_VAR2, MAX_VAR2);
            
            // create the question
            content[0] = start.ToString() + " " + (start + step).ToString() + " " + (start + step * 2).ToString() + " " + (start + step * 3).ToString() + "... what's next?";

            // create the correct answer
            int answer = start + step * 4;
            content[1] = answer.ToString();

            // create the decoy answers
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();

            for (int i = 2; i < 5; i++)
            {
                int pertubation;

                do
                {
                    pertubation = _rnd.Next(-(step - 1), MAX_VAR1);

                } while (pertubation == 0 || alreadyUsed.ContainsKey(pertubation));

                content[i] = (answer + pertubation).ToString();
                alreadyUsed.Add(pertubation, i);
            }

            // provide a hint text
            content[5] = "This is the hint for pattern recognition";

            return content;
        }
    }
}
