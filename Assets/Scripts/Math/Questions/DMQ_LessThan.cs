using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates simple less-than problem in the form: Which answer is less than x
    /// </summary>
    public class DMQ_LessThan : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 50;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int x = _rnd.Next(MIN_VAR1, MAX_VAR1);
            
            // create the question
            content[0] = "Which answer is less than " + x;

            // create the correct answer
            int answer = x - _rnd.Next(1, x < 10 ? x : 10);
            content[1] = answer.ToString();

            // create the decoy answers
            int decoy = x + _rnd.Next(1, 10);
            content[2] = decoy.ToString();
            decoy += _rnd.Next(1, 10);
            content[3] = decoy.ToString();
            decoy += _rnd.Next(1, 10);
            content[4] = decoy.ToString();

            // provide a hint text
            content[5] = "This is the hint for less-than";

            return content;
        }
    }
}
