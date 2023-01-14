using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must determine the value of x for limits such as: lim x->4, x + 4 = 8
    /// </summary>
    public class DMQ_Limits : DynamicMathQuestion
    {
        // hand coded templates for the pemdas expressions so that they are well controlled
        // with regard to the demands being placed on the player.
        private QuestionAnswerIntegerTemplate[] _qaTemplates = new QuestionAnswerIntegerTemplate[]
            {
                // lim x->a, bx = c
                new QuestionAnswerIntegerTemplate(
                    "VAR0#VAR1 * x",
                    2,
                    new string[]
                    {
                        "1 RND 12", // a
                        "2 RND 12", // b
                        "VAR1 * VAR0", // c
                    },
                    "1.0"),

                // lim x->a, bx = c (identical to previous one so as to increase chances of this template being used)
                new QuestionAnswerIntegerTemplate(
                    "VAR0#VAR1 * x",
                    2,
                    new string[]
                    {
                        "1 RND 12", // a
                        "2 RND 12", // b
                        "VAR1 * VAR0", // c
                    },
                    "1.0"),

                // lim x->a, b + x = c
                new QuestionAnswerIntegerTemplate(
                    "VAR0#VAR1 + x",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 20", // b
                        "VAR1 + VAR0", // c
                    },
                    "1.0"),

                // lim x->a, b - x = c
                new QuestionAnswerIntegerTemplate(
                    "VAR0#VAR1 - x",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 20", // b
                        "VAR1 - VAR0", // c
                    },
                    "1.0"),

                // lim x->a, x - b = c
                new QuestionAnswerIntegerTemplate(
                    "VAR0#x - VAR1",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 20", // b
                        "VAR0 - VAR1", // c
                    },
                    "1.0"),

                // lim x->a, x + b = c
                new QuestionAnswerIntegerTemplate(
                    "VAR0#x + VAR1",
                    2,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 20", // b
                        "VAR0 + VAR1", // c
                    },
                    "1.0"),
            };


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // choose and evaluate a template
            QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(0, _qaTemplates.Length)];
            template.EvaluateTemplate();

            // create the question
            string[] questionParts = template.Question.Split('#');
            content[0] = "@math_limit{x," + questionParts[0] +",-0.3}#" + WrapMathString(questionParts[1]) + "# = ?";

            // fill in the answer
            content[1] = template.Answer;

            // create the decoy answers (perturb the answer by some amount)

            // ...create the pertubations - should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 0);

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
            content[5] = "This is the hint for Limits";

            return content;
        }
    }
}
