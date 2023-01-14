using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 6.
    /// </summary>
    public class QuestionCategory_6 : QuestionCategory
    {
        private const string CATNAME = "Grade 6";

        public QuestionCategory_6()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_6(int levelNum)
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
            AddLevel(new Level("Power 2 Exponents", new DMQ_ExponentsPower2()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Power 3 Exponents", new DMQ_ExponentsPower3()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Power 4 Exponents", new DMQ_ExponentsPower4()));
        }
    }
}
