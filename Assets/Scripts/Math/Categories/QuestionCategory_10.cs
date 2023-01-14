using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 10.
    /// </summary>
    public class QuestionCategory_10 : QuestionCategory
    {
        private const string CATNAME = "Grade 10";

        public QuestionCategory_10()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_10(int levelNum)
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
            AddLevel(new Level("Solving Equations", new DMQ_AlgebraSolveEquations()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Distributive Law", new DMQ_AlgebraDitributiveLaw()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Basic Factoring", new DMQ_AlgebraBasicFactoring()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Combining Like Terms", new DMQ_AlgebraCombineLikeTerms()));
        }
    }
}
