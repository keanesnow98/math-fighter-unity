using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.GamerServices;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using MathFreak.GameStates;
using GarageGames.Torque.Util;

namespace MathFreak.GamePlay
{
    /// <summary>
    /// Handles the local AI player specific stuff for the player representation.
    /// </summary>
    public class PlayerLocalAI : Player
    {
        private enum EnumAIState { Idle, DecideWhenToAnswer, WaitingToAnswer };
        private EnumAIState _AIstate;
        private float _difficultyLevel;
        private float _AIDifficultyWrongAnswerMultiplier;
        private float[] _minAnswerTime;
        private float[] _maxAnswerTime;
        private float[] _avgAnswerTime;
        private float _timeToAnswerAt;
        private int _correctAnswer;
        private int _answerToGive;
        private float _answerTimeElapsed;
        private int _opponentHealth;
        private float _comboCounter;
        private bool _isDefeated;
        private bool _hasFoughtTheBoss;

        private const float DIFFICULTY_INC = 0.2f;  // enough increase that it increases by a bit less than whole difficulty level over the course of the championship (playing the 6th character will take it to a whole level increase)

        private int _playerIndex;
        private List<TutorManager.EnumTutor> _avaliableCharacters;  // for championship mode - this is how the computer will keep track of what players the player has yet to play

        public bool IsDefeated
        {
            get { return _isDefeated; }
        }

        public int PlayerIndex
        {
            get { return _playerIndex; }
        }

        public override bool  IsValid
        {
            get { return _playerIndex == -1; }
        }

        public override Texture2D GamerPic
        {
            get
            {
                return TutorManager.Instance.GetGamerPic(0, Character).Texture.Instance as Texture2D;
            }
        }

        public override string GamerTag
        {
            get
            {
                return TutorManager.Instance.GetGamerTag(0, Character);
            }
        }

        public PlayerLocalAI(int playerIndex, int difficultyLevel)
        {
            _avaliableCharacters = new List<TutorManager.EnumTutor>(5);

            _avaliableCharacters.Add(TutorManager.EnumTutor.Einstein);
            _avaliableCharacters.Add(TutorManager.EnumTutor.Caveman);
            _avaliableCharacters.Add(TutorManager.EnumTutor.SchoolTeacher);
            _avaliableCharacters.Add(TutorManager.EnumTutor.Robot);
            _avaliableCharacters.Add(TutorManager.EnumTutor.AtomicKid);

            _playerIndex = playerIndex;

            _difficultyLevel = (float)difficultyLevel - DIFFICULTY_INC; // note: because will increment the AI's difficulty level so the first time we set it slightly lower so when incremented it ends up at the right value

            _AIstate = EnumAIState.Idle;
        }

        private void SetDifficultyMultipliers()
        {
            Debug.WriteLine("AI is at difficulty level: " + _difficultyLevel);

            _AIDifficultyWrongAnswerMultiplier = (4.0f - _difficultyLevel);
        }

        public void IncreaseDifficulty()
        {
            _difficultyLevel += DIFFICULTY_INC;
            SetDifficultyMultipliers();
        }

        public void RemoveAvailableCharacter(TutorManager.EnumTutor characterToRemove)
        {
            _avaliableCharacters.Remove(characterToRemove);
        }

        public TutorManager.EnumTutor PickRandomAvailableCharacter()
        {
            return _avaliableCharacters[Game.Instance.Rnd.Next(_avaliableCharacters.Count)];
        }

        public TutorManager.EnumTutor PickBoss()
        {
            _hasFoughtTheBoss = true;
            return TutorManager.EnumTutor.MathLord;
        }

        public int AvailableCharacterCount()
        {
            return _avaliableCharacters.Count;
        }

        public List<TutorManager.EnumTutor> GetUnavailableCharacters()
        {
            List<TutorManager.EnumTutor> ret = new List<TutorManager.EnumTutor>(5);

            ret.Add(TutorManager.EnumTutor.Einstein);
            ret.Add(TutorManager.EnumTutor.Caveman);
            ret.Add(TutorManager.EnumTutor.SchoolTeacher);
            ret.Add(TutorManager.EnumTutor.Robot);
            ret.Add(TutorManager.EnumTutor.AtomicKid);

            foreach (TutorManager.EnumTutor character in _avaliableCharacters)
            {
                ret.Remove(character);
            }

            return ret;
        }

        public override PlayerCharacterSelector GetCharacterSelector(int playerNum)
        {
            return new PlayerLocalAICharacterSelector(playerNum, this);
        }

        public void Tick(float dt)
        {
            switch (_AIstate)
            {
                case EnumAIState.Idle:
                    ProcessIdle(dt);
                    break;

                case EnumAIState.DecideWhenToAnswer:
                    ProcessDecideWhenToAnswer(dt);
                    break;

                case EnumAIState.WaitingToAnswer:
                    ProcessWaitingToAnswer(dt);
                    break;

                default:
                    Assert.Fatal(false, "Unrecognized AI state: " + _AIstate);
                    break;
            }
        }

        private void ProcessIdle(float dt)
        {
            // possibly do a taunt (very infrequently though)
            TauntPressed  = (Game.Instance.Rnd.Next(100000) < 2);
        }

        private void ProcessDecideWhenToAnswer(float dt)
        {
            // reset the clock
            _answerTimeElapsed = 0.0f;

            // pick a linearly distributed random number and index into a logit function to get
            // an abstracted answer time value that is non-linearly distributed - logit it will
            // tend to return central values in preference to values at the extremes, thus making
            // our response times group around the center (which will become our desired averge time
            // when later we convert the abstract answer time into a concrete one).
            double rnd = Game.Instance.Rnd.NextDouble();
            float abstractAnswerTime = (float)System.Math.Log(rnd / (1 - rnd));

            // normalize the logit time to -1 to +1
            abstractAnswerTime /= 5.0f; // divide by five as most useful section of the logit function for us is the range -5 to +5

            if (abstractAnswerTime < -1.0f)
            {
                abstractAnswerTime = -1.0f;
            }
            else if (abstractAnswerTime > 1.0f)
            {
                abstractAnswerTime = 1.0f;
            }

            // convert the abstracted answer time into a concrete one
            // ...calculate the answer time parameters to use
            float minTime;
            float maxTime;
            float avgTime;

            CalculateAnswerTimeParams(out minTime, out maxTime, out avgTime);

            // ...calculate our conrete answer time
            if (abstractAnswerTime < 0.0f)
            {
                _timeToAnswerAt = minTime + (1.0f + abstractAnswerTime) * (avgTime - minTime);
            }
            else
            {
                _timeToAnswerAt = avgTime + abstractAnswerTime * (maxTime - avgTime);
            }

            // ...silliness check - make sure we are within the min/max
            if (_timeToAnswerAt < minTime)
            {
                _timeToAnswerAt = minTime;
            }
            else if (_timeToAnswerAt > maxTime)
            {
                _timeToAnswerAt = maxTime;
            }

            //_timeToAnswerAt = 20.0f;  // for testing time running out anims
            Debug.WriteLine("AI decided to answer at: " + _timeToAnswerAt + "seconds");

            // should we give the answer correct?
            if (Game.Instance.Rnd.Next(100) > (int)(_AIDifficultyWrongAnswerMultiplier * 5.0f))
            {
                Debug.WriteLine("AI decided to give correct answer");
                _answerToGive = _correctAnswer;
            }
            // otherwise just pick a wrong one at random
            else
            {
                Debug.WriteLine("AI decided to give wrong answer");

                do
                {
                    _answerToGive = Game.Instance.Rnd.Next(4);

                } while (_answerToGive == _correctAnswer);
            }

            // decide whether to use the super attack if the opportunity comes up
            if (_comboCounter > 2.0f)
            {
                // ...if using a super would KO the other player then use it!
                if (_opponentHealth - GameStateGameplay.CalcSuperDamage(_comboCounter) <= 0)
                {
                    SuperAttackPressed = true;
                }
                // ...else chance of deciding to use the super increases linearly as combo
                // count gets higher.  Going all the way up to 100% chance if we have the 10x
                // multiplier
                else if (Game.Instance.Rnd.Next(100) <= (int)_comboCounter * 10)
                {
                    SuperAttackPressed = true;
                }
                else
                {
                    SuperAttackPressed = false;
                }
            }

            // move to next state
            _AIstate = EnumAIState.WaitingToAnswer;
        }

        private void ProcessWaitingToAnswer(float dt)
        {
            // if the time has come to make our answer then do so and then move to the idle state
            _answerTimeElapsed += dt;

            if (_answerTimeElapsed >= _timeToAnswerAt)
            {
                Debug.WriteLine("AI answering");

                // answer
                OnButtonPressed(GameStateGameplay.MapAnswerWindowIndexToGamepadButton(_answerToGive));

                // move to idle state
                _AIstate = EnumAIState.Idle;
            }
            else
            {
                ProcessIdle(dt);
            }
        }

        public void OnQuestionStarted(int correctAnswer, float[] minAnswerTime, float[] maxAnswerTime, float[] avgAnswerTime, int opponentHealth, float comboCounter)
        {
            _correctAnswer = correctAnswer;
            _minAnswerTime = minAnswerTime;
            _maxAnswerTime = maxAnswerTime;
            _avgAnswerTime = avgAnswerTime;
            _opponentHealth = opponentHealth;
            _comboCounter = comboCounter;

            _AIstate = EnumAIState.DecideWhenToAnswer;
        }

        public void OnQuestionFinished()
        {
            _AIstate = EnumAIState.Idle;
        }

        public void OnLost()
        {
            if (_hasFoughtTheBoss)
            {
                _isDefeated = true;
            }
        }

        private void CalculateAnswerTimeParams(out float minTime, out float maxTime, out float avgTime)
        {
            // workout the upper and lower indexes into the answer time param arrays (difficulty level can be fractional, but array indexes are obviously integer)
            int lowerIndx = (int)System.Math.Floor(_difficultyLevel);
            int upperIndx = (int)System.Math.Ceiling(_difficultyLevel);
            float fractionalIndx = _difficultyLevel - lowerIndx;

            // silliness checks - make sure indexes are within min/max and make sense
            if (lowerIndx < 0)
            {
                Assert.Fatal(false, "Invalid min index in AI params calculatoin: " + lowerIndx + "/" + upperIndx);
                lowerIndx = 0;
            }
            else if (lowerIndx > 3)
            {
                Assert.Fatal(false, "Invalid min index in AI params calculatoin: " + lowerIndx + "/" + upperIndx);
                lowerIndx = 3;
            }

            if (upperIndx < 0)
            {
                Assert.Fatal(false, "Invalid max index in AI params calculatoin: " + lowerIndx + "/" + upperIndx);
                upperIndx = 0;
            }
            else if (upperIndx > 3)
            {
                if (_difficultyLevel > 3.1f)    // we can get rounding errors that take it over 3.0 so check that it's not just a simple rounding error before we assert
                {
                    Assert.Fatal(false, "Invalid max index in AI params calculatoin: " + lowerIndx + "/" + upperIndx);
                }

                upperIndx = 3;
            }

            if (upperIndx < lowerIndx)
            {
                Assert.Fatal(false, "Invalid min/max indexes in AI params calculatoin: " + lowerIndx + "/" + upperIndx);
                upperIndx = lowerIndx;
            }

            // linearly interpolate between lower and upper param values to get the values we will actually use
            minTime = MathHelper.Lerp(_minAnswerTime[lowerIndx], _minAnswerTime[upperIndx], fractionalIndx);
            maxTime = MathHelper.Lerp(_maxAnswerTime[lowerIndx], _maxAnswerTime[upperIndx], fractionalIndx);
            avgTime = MathHelper.Lerp(_avgAnswerTime[lowerIndx], _avgAnswerTime[upperIndx], fractionalIndx);
            //minTime = _minAnswerTime[lowerIndx] + (fractionalIndx * (_minAnswerTime[upperIndx] - _minAnswerTime[lowerIndx]));
            //maxTime = _maxAnswerTime[lowerIndx] + (fractionalIndx * (_maxAnswerTime[upperIndx] - _maxAnswerTime[lowerIndx]));
            //avgTime = _avgAnswerTime[lowerIndx] + (fractionalIndx * (_avgAnswerTime[upperIndx] - _avgAnswerTime[lowerIndx]));
        }
    }



    public class PlayerLocalAICharacterSelector : PlayerCharacterSelector
    {
        private PlayerLocalAI _AIplayer;
        private bool _doSelectCharacter = false;
        private float _thinkingDelay;   // how much 'thinking' time is left before the AI chooses it's character

        public PlayerLocalAICharacterSelector(int playerNum, PlayerLocalAI AIplayer)
            : base(playerNum)
        {
            _AIplayer = AIplayer;
        }

        public void EnableSelectingCharacter()
        {
            _doSelectCharacter = true;
            _thinkingDelay = 0.5f;
        }

        public override void Tick(GameStateCharacterSelectionLobby lobby, float dt)
        {
            if (!_doSelectCharacter) return;

            _thinkingDelay -= dt;

            if (_thinkingDelay > 0.0f) return;

            TutorManager.EnumTutor characterToSelect = TutorManager.EnumTutor.None;

            // time to play as the player's character?
            if (_AIplayer.AvailableCharacterCount() == 1)
            {
                characterToSelect = _AIplayer.PickRandomAvailableCharacter();
            }
            // else select some other character at random (any character except the one the player is using)
            else if (_AIplayer.AvailableCharacterCount() > 1)
            {
                do
                {
                    characterToSelect = _AIplayer.PickRandomAvailableCharacter();

                } while (characterToSelect == Game.Instance.ActiveGameplaySettings.Players[0].Character);
            }
            // else no characters are left apart from the boss
            else
            {
                characterToSelect = _AIplayer.PickBoss();
            }

            // TESTING
            //characterToSelect = _AIplayer.PickBoss();
            //characterToSelect = _AIplayer.PickRandomAvailableCharacter();
            //characterToSelect = TutorManager.EnumTutor.AtomicKid;
            // TESTING ENDS

            Debug.WriteLine("AI selected " + characterToSelect.ToString());

            // update the AI settings and select the character
            _AIplayer.RemoveAvailableCharacter(characterToSelect);
            lobby.MoveToCharacter(_playerNum, characterToSelect);
            lobby.OnAction_Pressed_A(_playerNum);   // select the character            
            lobby.UpdateGamerTag(GameStateCharacterSelectionLobby.PLAYER_2);

            _doSelectCharacter = false;
        }
    }
}

