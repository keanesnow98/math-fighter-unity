using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 5.
    /// </summary>
    public class QuestionCategory_5 : QuestionCategory
    {
        private const string CATNAME = "Grade 5";

        public QuestionCategory_5()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_5(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 7;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Simplifying Fractions", new DMQ_FractionsSimplify()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Least Common Multiple", new DMQ_FractionsCommonDenominator()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Improper Fractions", new DMQ_FractionsImproper()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Fractions Addition", new DMQ_FractionsAddition()));
        }

        protected override void CreateAndAddLevel_5()
        {
            AddLevel(new Level("Fractions Subtraction", new DMQ_FractionsSubtraction()));
        }

        protected override void CreateAndAddLevel_6()
        {
            AddLevel(new Level("Fractions Multiplication", new DMQ_FractionsMultiplication()));
        }

        protected override void CreateAndAddLevel_7()
        {
            AddLevel(new Level("Fractions Division", new DMQ_FractionsDivision()));
        }
    }
}
