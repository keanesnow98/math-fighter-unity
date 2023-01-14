using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Evaluate the cube root of a number
    /// </summary>
    public class DMQ_Root3 : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 6;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // work out the answer first so we know it will be a whole number in the required range
            int answer = _rnd.Next(1, MAX_VAR1 + 1);

            // create the question
            content[0] = WrapMathString((answer * answer * answer).ToString() + " ROOT 3");

            // create the correct answer
            content[1] = answer.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();
            alreadyUsed.Add(answer, 1);

            for (int i = 2; i < 5; i++)
            {
                int pertAnswer;

                do
                {
                    pertAnswer = answer + _rnd.Next(-5, 5);

                } while (alreadyUsed.ContainsKey(pertAnswer) || pertAnswer < MIN_VAR1 || pertAnswer > MAX_VAR1);

                content[i] = (pertAnswer).ToString();
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for cube roots";

            return content;
        }
    }
}
