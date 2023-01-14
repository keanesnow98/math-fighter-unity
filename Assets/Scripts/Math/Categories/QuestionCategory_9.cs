using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 9.
    /// </summary>
    public class QuestionCategory_9 : QuestionCategory
    {
        private const string CATNAME = "Grade 9";

        public QuestionCategory_9()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_9(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 4;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Truncation", new DMQ_TruncatingNumbers()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Square Root", new DMQ_Root2()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Cube Root", new DMQ_Root3()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Fourth Root", new DMQ_Root4()));
        }
    }
}
