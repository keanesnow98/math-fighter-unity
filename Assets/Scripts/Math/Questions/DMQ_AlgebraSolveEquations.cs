using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must solve simple equations: e.g. "x + 2 = 5" answer "x = 3";
    /// </summary>
    public class DMQ_AlgebraSolveEquations : DynamicMathQuestion
    {
        private QuestionAnswerIntegerTemplate[] _qaTemplates = new QuestionAnswerIntegerTemplate[]
            {
                new QuestionAnswerIntegerTemplate(
                    "x + VAR1 = VAR2",
                    0,  // here we specify which var is the answer (i.e. value of x) - in this case VAR0
                    new string[]
                    {
                        "1 RND 20",
                        "1 RND 20",
                        "VAR0 + VAR1",
                    },
                    "1.0"),    // custom info: text height modifier

                new QuestionAnswerIntegerTemplate(
                    "VAR1 + x = VAR2",
                    0,
                    new string[]
                    {
                        "1 RND 20",
                        "1 RND 20",
                        "VAR1 + VAR0",
                    },
                    "1.0"),

                new QuestionAnswerIntegerTemplate(
                    "x - VAR1 = VAR2",
                    0,
                    new string[]
                    {
                        "1 RND 20",
                        "1 RND 20",
                        "VAR0 - VAR1",
                    },
                    "1.0"),

                new QuestionAnswerIntegerTemplate(
                    "VAR1 - x = VAR2",
                    0,
                    new string[]
                    {
                        "1 RND 20",
                        "1 RND 20",
                        "VAR1 - VAR0",
                    },
                    "1.0"),

                new QuestionAnswerIntegerTemplate(
                    "VAR1 * x = VAR2",
                    0,
                    new string[]
                    {
                        "1 RND 12",
                        "1 RND 12",
                        "VAR1 * VAR0",
                    },
                    "1.0"),

                // identical copy of previous so chances of a multiplication equation are raised to level with the other types
                new QuestionAnswerIntegerTemplate(
                    "VAR1 * x = VAR2",
                    0,
                    new string[]
                    {
                        "1 RND 12",
                        "1 RND 12",
                        "VAR1 * VAR0",
                    },
                    "1.0"),

                new QuestionAnswerIntegerTemplate(
                    "VAR2 / x = VAR1",
                    0,
                    new string[]
                    {
                        "1 RND 12",
                        "1 RND 12",
                        "VAR0 * VAR1",
                    },
                    "0.8"),

                new QuestionAnswerIntegerTemplate(
                    "x / VAR1 = VAR0",
                    2,
                    new string[]
                    {
                        "1 RND 12",
                        "1 RND 12",
                        "VAR0 * VAR1",
                    },
                    "0.8"),
            };


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // choose and evaluate a template
            //QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(5, 7)];
            QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(0, _qaTemplates.Length)];
            template.EvaluateTemplate();

            // create the question
            string[] questionParts = template.Question.Split('=');
            content[0] = "Solve#@height{" + template.CustomInfo + "}#" + WrapMathString(questionParts[0]) + "#=" + questionParts[1];

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
            content[5] = "This is the hint for Solving Equations";

            return content;
        }
    }
}
