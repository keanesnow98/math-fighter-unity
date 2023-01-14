using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.GameUtil;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
//using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
//using GarageGames.Torque.GameUtil;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;

using MathFreak.GameStateFramework;
using MathFreak.GUIFrameWork;
using System.Threading;
using MathFreak.GameStates.Transitions;
using MathFreak.GamePlay;
using System.Diagnostics;
using MathFreak.Text;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates.Dialogs;
using MathFreak.Highscores;
using Microsoft.Xna.Framework.GamerServices;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the specifics of displaying the multiplayer highscores
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateHighscoresMultiplayer : GameStateHighscoresBase
    {
        private HighscoreData.MultiplayerScoreData[] _scores;
        private HighscoreData.MultiplayerScoreData[] _scoresToDisplay;


        public override void PreTransitionOn(string paramString)
        {
            _activeInstance = this;

            base.PreTransitionOn(paramString);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
            _scores = null;
            _scoresToDisplay = null;
        }

        protected override void InitializeData()
        {
            _scores = HighscoreData.Instance.MultiplayerScores.ToArray();

            //// convert data to an array and bubble sort it
            //int count = HighscoreData.Instance.MultiplayerScores.Count;
            //_scores = new HighscoreData.MultiplayerScoreData[count];

            //int indx = 0;

            //foreach (KeyValuePair<string, HighscoreData.MultiplayerScoreData> score in HighscoreData.Instance.MultiplayerScores)
            //{
            //    _scores[indx] = score.Value;
            //    indx++;
            //}

            //if (count > 1)
            //{
            //    for (int i = 1; i < count - 1; i++)
            //    {
            //        for (int j = 0; j < count - i; j++)
            //        {
            //            if (_scores[j].Rating < _scores[j + 1].Rating)
            //            {
            //                HighscoreData.MultiplayerScoreData temp = _scores[j + 1];
            //                _scores[j + 1] = _scores[j];
            //                _scores[j] = temp;
            //            }
            //        }
            //    }
            //}
        }

        protected override string GetSceneFileName()
        {
            return "HighscoresMultiplayer";
        }

        protected override void InitializeScene()
        {
            T2DStaticSprite shadow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("highscoremp_shadow");
            SimpleMaterial material = shadow.Material as SimpleMaterial;
            material.IsColorBlended = true;
            material.IsTranslucent = true;
            shadow.VisibilityLevel = 0.5f;
        }

        protected override void OnViewModeChanged()
        {
            switch (_viewMode)
            {
                case EnumViewMode.Friends:
#if XBOX
                    FriendCollection friends = Game.Instance.PrimaryGamer.GetFriends();
                    List<HighscoreData.MultiplayerScoreData> friendsScores = new List<HighscoreData.MultiplayerScoreData>();
                    int count = _scores.Length;

                    for (int i = 0; i < count && friendsScores.Count < friends.Count; i++)
                    {
                        if (Game.Instance.Gamertag == _scores[i].GamerTag)
                        {
                            friendsScores.Add(_scores[i]);
                            continue;
                        }

                        foreach (FriendGamer friend in friends)
                        {
                            if (friend.Gamertag == _scores[i].GamerTag)
                            {
                                friendsScores.Add(_scores[i]);
                                break;
                            }
                        }
                    }

                    _scoresToDisplay = friendsScores.ToArray();
#else
                    _scoresToDisplay = _scores;
#endif
                    break;

                case EnumViewMode.MyScore:
                    _scoresToDisplay = _scores;
                    break;

                case EnumViewMode.Overall:
                    _scoresToDisplay = _scores;
                    break;
            }

            base.OnViewModeChanged();
        }

        protected override void UpdateHighscoreDisplay()
        {
            base.UpdateHighscoreDisplay();

            // hide all the objects - we might not need them all
            for (int i = 0; i < PAGESIZE; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_position" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_gamertag" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_rating" + i).Visible = false;
            }

            // now setup the objects with the data and make visible (might be all or might be only some)
            int count = _scoresToDisplay.Length;

            for (int i = 0; i < PAGESIZE; i++)
            {
                int indx = _startPos + i;
                if (indx >= count) break;

                T2DSceneObject obj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_position" + i);
                obj.Components.FindComponent<MathMultiTextComponent>().TextValue = (indx + 1).ToString();
                obj.Visible = true;
                
                obj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_gamertag" + i);
                obj.Components.FindComponent<MathMultiTextComponent>().TextValue = _scoresToDisplay[indx].GamerTag;
                obj.Visible = true;

                obj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscoremp_rating" + i);
                obj.Components.FindComponent<MathMultiTextComponent>().TextValue = _scoresToDisplay[indx].Rating.ToString();
                obj.Visible = true;
            }
        }

        protected override int GetScoreCount()
        {
            return _scoresToDisplay.Length;
        }

        protected override int GetMyScorePos()
        {
            int count = _scoresToDisplay.Length;

            for (int i = 0; i < count; i++)
            {
                if (_scoresToDisplay[i].GamerTag == Game.Instance.Gamertag)
                {
                    return i;
                }
            }

            return base.GetMyScorePos();
        }
    }
}
