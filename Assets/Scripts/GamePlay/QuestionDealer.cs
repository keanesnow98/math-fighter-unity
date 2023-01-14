using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Categories;
using GarageGames.Torque.Core;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// Abstracts the dealing of questions - the gameplay gamestate doesn't need to know the category/level
    /// settings; it can just get the questions from the dealer.  The dealer will handle all the details.
    /// </summary>
    public class QuestionDealer
    {
        private const int MAX_REPETION_AVOIDANCE_ITERATION_COUNT = 10;
        private const int MAX_ALREADYASKED_LISTSIZE = 100;

        private List<QuestionCategory> _cats;
        private List<QuestionContent> _alreadyAsked;

        // create a dealer with all categories with all their levels included
        public QuestionDealer()
        {
            _cats = new List<QuestionCategory>();

            _cats.Add(new QuestionCategory_0());
            _cats.Add(new QuestionCategory_1());
            _cats.Add(new QuestionCategory_2());
            _cats.Add(new QuestionCategory_3());
            _cats.Add(new QuestionCategory_4());
            _cats.Add(new QuestionCategory_5());
            _cats.Add(new QuestionCategory_6());
            _cats.Add(new QuestionCategory_7());
            _cats.Add(new QuestionCategory_8());
            _cats.Add(new QuestionCategory_9());
            _cats.Add(new QuestionCategory_10());
            _cats.Add(new QuestionCategory_11());
            _cats.Add(new QuestionCategory_12());
            _cats.Add(new QuestionCategory_13());

            _alreadyAsked = new List<QuestionContent>(MAX_ALREADYASKED_LISTSIZE);
        }

        // create a dealer with just the specified category with all its levels included
        public QuestionDealer(int catNum)
        {
            _cats = new List<QuestionCategory>();

            switch (catNum)
            {
                case 0:
                    _cats.Add(new QuestionCategory_0());
                    break;

                case 1:
                    _cats.Add(new QuestionCategory_1());
                    break;

                case 2:
                    _cats.Add(new QuestionCategory_2());
                    break;

                case 3:
                    _cats.Add(new QuestionCategory_3());
                    break;

                case 4:
                    _cats.Add(new QuestionCategory_4());
                    break;

                case 5:
                    _cats.Add(new QuestionCategory_5());
                    break;

                case 6:
                    _cats.Add(new QuestionCategory_6());
                    break;

                case 7:
                    _cats.Add(new QuestionCategory_7());
                    break;

                case 8:
                    _cats.Add(new QuestionCategory_8());
                    break;

                case 9:
                    _cats.Add(new QuestionCategory_9());
                    break;

                case 10:
                    _cats.Add(new QuestionCategory_10());
                    break;

                case 11:
                    _cats.Add(new QuestionCategory_11());
                    break;

                case 12:
                    _cats.Add(new QuestionCategory_12());
                    break;

                case 13:
                    _cats.Add(new QuestionCategory_13());
                    break;

                default:
                    Assert.Fatal(false, "Category out of range: " + catNum);
                    _cats.Add(new QuestionCategory_1());
                    break;
            }

            _alreadyAsked = new List<QuestionContent>(MAX_ALREADYASKED_LISTSIZE);
        }

        // create a dealer with all the specified grades
        public QuestionDealer(bool[] grades)
        {
            int count = grades.Length;

            Assert.Fatal(count == 14, "QuestionDealer() - invalid grade count: " + count);

            _cats = new List<QuestionCategory>();
            _alreadyAsked = new List<QuestionContent>(MAX_ALREADYASKED_LISTSIZE);

            for (int i = 0; i < count; i++)
            {
                if (grades[i])
                {
                    AddGrade(i);
                }
            }
        }

        private void AddGrade(int grade)
        {
            switch (grade)
            {
                case 0:
                    _cats.Add(new QuestionCategory_0());
                    break;

                case 1:
                    _cats.Add(new QuestionCategory_1());
                    break;

                case 2:
                    _cats.Add(new QuestionCategory_2());
                    break;

                case 3:
                    _cats.Add(new QuestionCategory_3());
                    break;

                case 4:
                    _cats.Add(new QuestionCategory_4());
                    break;

                case 5:
                    _cats.Add(new QuestionCategory_5());
                    break;

                case 6:
                    _cats.Add(new QuestionCategory_6());
                    break;

                case 7:
                    _cats.Add(new QuestionCategory_7());
                    break;

                case 8:
                    _cats.Add(new QuestionCategory_8());
                    break;

                case 9:
                    _cats.Add(new QuestionCategory_9());
                    break;

                case 10:
                    _cats.Add(new QuestionCategory_10());
                    break;

                case 11:
                    _cats.Add(new QuestionCategory_11());
                    break;

                case 12:
                    _cats.Add(new QuestionCategory_12());
                    break;

                case 13:
                    _cats.Add(new QuestionCategory_13());
                    break;

                default:
                    Assert.Fatal(false, "Category out of range: " + grade);
                    _cats.Add(new QuestionCategory_1());
                    break;
            }
        }

        //// create a dealer with just the specified category with just the specified level included
        //public QuestionDealer(int catNum, int levelNum)
        //{
        //    _cats = new List<QuestionCategory>();

        //    switch (catNum)
        //    {
        //        case 0:
        //            _cats.Add(new QuestionCategory_0(levelNum));
        //            break;

        //        case 1:
        //            _cats.Add(new QuestionCategory_1(levelNum));
        //            break;

        //        case 2:
        //            _cats.Add(new QuestionCategory_2(levelNum));
        //            break;

        //        case 3:
        //            _cats.Add(new QuestionCategory_3(levelNum));
        //            break;

        //        case 4:
        //            _cats.Add(new QuestionCategory_4(levelNum));
        //            break;

        //        case 5:
        //            _cats.Add(new QuestionCategory_5(levelNum));
        //            break;

        //        default:
        //            Assert.Fatal(false, "Category out of range: " + catNum);
        //            _cats.Add(new QuestionCategory_1(levelNum));
        //            break;
        //    }

        //    _alreadyAsked = new List<QuestionContent>(MAX_ALREADYASKED_LISTSIZE);
        //}

        public QuestionContent GetQuestion()
        {
            Assert.Fatal(_cats.Count > 0, "Trying to get a question when there are no categories currently set");

            QuestionCategory cat;

            // if only one category then pick that one
            if (_cats.Count == 1)
            {
                cat = _cats[0];
            }
            // else pick a category at random
            else
            {
                cat = _cats[Game.Instance.Rnd.Next(0, _cats.Count)];
            }

            // get a question from the selected category (avoiding repeating an earlier question if possible)
            int iterationCounter = 0;
            QuestionContent content = cat.GetQuestion();

            while (AlreadyAsked(content) && iterationCounter < MAX_REPETION_AVOIDANCE_ITERATION_COUNT)
            {
                content = cat.GetQuestion();
                iterationCounter++;
            }

            // store the question so we can prevent repetition later
            // ...check if list is too big and empty it if it is
            if (_alreadyAsked.Count >= MAX_ALREADYASKED_LISTSIZE)
            {
                _alreadyAsked.Clear();
            }

            // ...store the question
            _alreadyAsked.Add(content);

            // and... we're done - return the question for use
            return content;
        }

        private bool AlreadyAsked(QuestionContent content)
        {
            foreach (QuestionContent prevContent in _alreadyAsked)
            {
                if (content.Question == prevContent.Question)
                {
                    return true;
                }
            }

            return false;
        }

        public TutorManager.EnumTutor GetTutor()
        {
            return _cats[0].GetQuestion().Tutor;
        }
    }
}
