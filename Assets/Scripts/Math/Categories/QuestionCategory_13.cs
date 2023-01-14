using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 13.
    /// </summary>
    public class QuestionCategory_13 : QuestionCategory
    {
        private const string CATNAME = "Grade 13";

        public QuestionCategory_13()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_13(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 2;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Basic Derivatives", new DMQ_Derivatives()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Basic Limits", new DMQ_Limits()));
        }
    }
}
