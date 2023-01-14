using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 11.
    /// </summary>
    public class QuestionCategory_11 : QuestionCategory
    {
        private const string CATNAME = "Grade 11";

        public QuestionCategory_11()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_11(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 3;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Evaluating Functions", new DMQ_EvaluateFunctions()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Quadrants", new DMQ_WhichQuadrant()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Domains", new DMQ_Domains()));
        }
    }
}
