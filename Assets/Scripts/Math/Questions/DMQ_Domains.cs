using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates questions about Domains of the form: what is not in the domain of "a / x + b"
    /// i.e. what value of x will make the denominator zero and thus not a number
    /// </summary>
    public class DMQ_Domains : DynamicMathQuestion
    {
        private QuestionAnswerIntegerTemplate[] _qaTemplates = new QuestionAnswerIntegerTemplate[]
            {
                // a / (bx + c)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 / ( VAR1 * x + VAR3 )",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 6", // b
                        "-6 RND -1", // x
                        "-VAR1 * VAR2",  // c = -bx
                    },
                    "1.0"),

                // a / (bx - c)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 / ( VAR1 * x - VAR3 )",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 6", // b
                        "1 RND 6", // x
                        "VAR1 * VAR2",  // c = bx
                    },
                    "1.0"),

                // a / (b + cx)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 / ( VAR3 + VAR1 * x )",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 6", // c
                        "-6 RND 1", // x
                        "-VAR1 * VAR2",  // b = -cx
                    },
                    "1.0"),

                // a / (b - cx)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 / ( VAR3 - VAR1 * x )",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 6", // c
                        "1 RND 6", // x
                        "VAR1 * VAR2",  // b = -cx
                    },
                    "1.0"),
            };


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // choose and evaluate a template
            //QuestionAnswerIntegerTemplate template = _qaTemplates[3];
            QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(0, _qaTemplates.Length)];
            template.EvaluateTemplate();
            
            // create the question
            content[0] = "@height{0.8}#What is not in the Domain of#" + WrapMathString(template.Question);

            // fill in the answer
            content[1] = template.Answer;

            // create the decoy answers

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 1);

            int answerValue = int.Parse(template.Answer);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    pertAnswer = (answerValue + _rnd.Next(-10, 11)).ToString();

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Domains";

            return content;
        }
    }
}
