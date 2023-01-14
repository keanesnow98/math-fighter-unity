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
using MathFreak.AsyncTaskFramework;
using MathFreak.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.Net;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the multiplayer gameplay stuff that is common to the multiplayer gameplay
    /// modes (local and remote).
    /// 
    /// NOTE: *all* game modes are 'multiplayer' now - singleplayer mode includes an AI player.
    /// 
    /// NOTE: this gamestate should not be registered with the gamestate manager; it is the shared
    /// base class that the specific multiplayer gamestates will derive from, but not a gamestate
    /// that should be used directly.
    /// </summary>
    public abstract class GameStateGameplayMultiplayer : GameStateGameplay
    {
        protected int _prevScoringPlayer;    // the player who scored the last correct answer


        protected override void DoUnload()
        {
            base.DoUnload();

            //// return the session to the 'lobby' state
            //if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            //{
            //    //NetworkSessionManager.Instance.Session.EndGame();
            //    NetworkSessionManager.Instance.Session.ResetReady();
            //}
        }

        protected override void InitializeGamePlay()
        {
            base.InitializeGamePlay();

            _prevScoringPlayer = -1;
        }

        protected override bool GameEndedConditionMet()
        {
            return (_health[0] == 0 || _health[1] == 0);
        }

        protected override void DoGameEnded()
        {
            Assert.Fatal(GameEndedConditionMet(), "Multiplayer mode: DoGameEnded() called before game-end condition was met");

            // move to 'win' gameplay state - in multiplayer it is always the case that one player wins - losing is implicit and not presented onscreen
            _state = EnumGameplayState.GameWon;

            // a short pause first though
            AsyncTaskManager.Instance.AddTask(AsyncTask_Wait(0.5f), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
        }

        protected override void ProcessAnswerCorrect()
        {
            // if the scoring player is not the same as for the last question then we reset the multiplier
            if (_activePlayer != _prevScoringPlayer)
            {
                ResetMultiplier();
            }

            base.ProcessAnswerCorrect();

            _prevScoringPlayer = _activePlayer;
        }

        protected virtual string GetGameOverMessage()
        {
//#if XBOX
            if (_health[1] == 0)
            {
                return Game.Instance.ActiveGameplaySettings.Players[0].GamerRef.Gamertag + " WINS!";
            }
            else
            {
                return Game.Instance.ActiveGameplaySettings.Players[1].GamerRef.Gamertag + " WINS!";
            }
//#else
//            return "XXXX Wins!";
//#endif
        }

        protected virtual Game.EnumMathFreakFont GetGameOverMessageFont()
        {
            return Game.EnumMathFreakFont.PlayerXXXXWins;
        }

        protected virtual void PlayWinLoseSFX()
        {
            // no sfx to play
        }

        protected virtual void PlayPerfectSFX()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Perfect);
        }

        protected override void ProcessGameWon()
        {
            if (_isExiting) return;

            _isExiting = true;

            base.ProcessGameWon();

            int winner;

            // do the tutor's win animation
            if (_health[1] == 0)
            {
                winner = 0;
                AsyncTaskManager.Instance.AddTask(TutorManager.Instance.PlayAnim(0, Game.Instance.ActiveGameplaySettings.Players[0].Character, TutorManager.Tutor.EnumAnim.Win, _tutorSprites[0]), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
                //AsyncTaskManager.Instance.AddTask(TutorManager.Instance.PlayAnim(Game.Instance.ActiveGameplaySettings.Players[1].Character, TutorManager.Tutor.EnumAnim.Lose, _tutorSprites[1]), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
            }
            else
            {
                winner = 1;
                //AsyncTaskManager.Instance.AddTask(TutorManager.Instance.PlayAnim(Game.Instance.ActiveGameplaySettings.Players[0].Character, TutorManager.Tutor.EnumAnim.Lose, _tutorSprites[0]), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
                AsyncTaskManager.Instance.AddTask(TutorManager.Instance.PlayAnim(1, Game.Instance.ActiveGameplaySettings.Players[1].Character, TutorManager.Tutor.EnumAnim.Win, _tutorSprites[1]), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
            }

            // move to the outro gameplay state after displaying the win message
            _state = EnumGameplayState.Outro;

            if (_health[winner] == Game.Instance.ActiveGameplaySettings.EnergyBar)
            {
                AsyncTaskManager.Instance.AddTask(AsyncTask_ShowPerfectWinMessage(winner, GetGameOverMessage().ToUpper(), GetGameOverMessageFont()), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
            }
            else
            {
                AsyncTaskManager.Instance.AddTask(AsyncTask_ShowWinMessage(winner, GetGameOverMessage().ToUpper(), GetGameOverMessageFont()), TASKLIST_GAMEPLAY_ASYNC_EVENTS, true);
            }
        }

        protected override void UpdateMultiplier()
        {
            // if the scoring player is not the same as for the last question then we reset the multiplier
            if (_activePlayer != _prevScoringPlayer)
            {
                ResetMultiplier();
            }

            // and ALWAYS do the normal update too so that the new player get's their multiplier
            base.UpdateMultiplier();
        }

        protected override void  ResetMultiplierAfterWrongAnswer()
        {
            // only reset the multiplier if the active player is the one who has
            // the multipier going at the moment.
            if (_activePlayer == _prevScoringPlayer)
            {
                base.ResetMultiplierAfterWrongAnswer();
            }
        }

        protected override void DoEndOfSequenceFX(int sequenceCount, int sequenceScore)
        {
            if (sequenceCount > 2)
            {
                AddAsyncTask(AsyncTask_ShowEndOfSequenceMessage(sequenceCount, _prevScoringPlayer), true);
                AddAsyncTask(AsyncTask_ShowEndOfSequenceStats(sequenceCount, sequenceScore, _prevScoringPlayer), true);
            }
        }

        public override void Tick(float dt)
        {
            ProcessSessionEvents(); // session events are processed before any other stuff - they will be able to add waitlist entries etc - plus some events will render any further game processing irrelevant (like if the session has ended)
            GetNextNetworkMessage();    // next get any pending network message (after the session stuff as session might have shutdown and then there would be no messages to get anyway)
            base.Tick(dt);        
        }

        protected virtual void GetNextNetworkMessage()
        {
            // no messages to get
        }

        protected virtual void ProcessSessionEvents()
        {
            // if the session isn't active then don't process the events as we already shutdown the session
            if (!NetworkSessionManager.Instance.IsActiveSession)
            {
                //Debug.WriteLine("GameStateGameplayMultiplayer::ProcessSessionEvents() - session is no longer valid - ignoring events");
                return;
            }

            if (_isExiting) return;

            // pull the next event from the queue and process it
            if (NetworkSessionManager.Instance.EventCount == 0) return;

            SessionEventInfo eventInfo = NetworkSessionManager.Instance.GetNextEvent();

            switch (eventInfo.Type)
            {
                case SessionEventInfo.EnumSessionEvent.GamerLeft:
                    OnGamerLeft(eventInfo);
                    break;

                case SessionEventInfo.EnumSessionEvent.GamerJoined:
                    OnGamerJoined(eventInfo);
                    break;

                case SessionEventInfo.EnumSessionEvent.SessionEnded:
                    OnSessionEnded(eventInfo);
                    break;
            }
        }

        protected virtual void OnGamerLeft(SessionEventInfo eventInfo)
        {
            Debug.WriteLine("Gamer has left the session: " + (eventInfo.Args as GamerLeftEventArgs).Gamer.Gamertag);
        }

        protected virtual void OnGamerJoined(SessionEventInfo eventInfo)
        {
            Debug.WriteLine("Gamer has joined the session: " + (eventInfo.Args as GamerJoinedEventArgs).Gamer.Gamertag);
        }
        
        protected virtual void OnSessionEnded(SessionEventInfo eventInfo)
        {
            _isExiting = true;  // we *will* be exiting
            Debug.WriteLine("Network session has ended: " + (eventInfo.Args as NetworkSessionEndedEventArgs).EndReason);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowWinMessage(int player, string winMessage, Game.EnumMathFreakFont font)
        {
            // block non-async gameplay state processing
            RegisterWaiting("showwinmessage");

            // bonus on winning? - depends on how much health the player has left
            if (player == 0)
            {
                AddAsyncTask(AsyncTask_UpdateScore((int)(Game.Instance.ActiveGameplaySettings.ScoreMultiplier * (float)_health[player])), true);
            }

            // sfx
            PlayWinLoseSFX();

            // show message - <<GAMERTAG>> WINS!
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.CharacterHeight = 110;
            messageText.Font = font;

            messageText.TextValue = winMessage;

            // pause for a few seconds
            float elapsed = 0.0f;

            while (elapsed < 4.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // finished
            UnRegisterWaiting("showwinmessage");
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowPerfectWinMessage(int player, string winMessage, Game.EnumMathFreakFont font)
        {
            // block non-async gameplay state processing
            RegisterWaiting("showperfectwinmessage");

            // bonus on winning? - depends on how much health the player has left
            if (player == 0)
            {
                AddAsyncTask(AsyncTask_UpdateScore((int)(Game.Instance.ActiveGameplaySettings.ScoreMultiplier * (float)_health[player])), true);
            }

            // show message - <<GAMERTAG>> WINS!
            PlayWinLoseSFX();

            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.CharacterHeight = 110;
            messageText.Font = font;
            messageText.TextValue = winMessage;

            // pause for a few seconds
            float elapsed = 0.0f;

            while (elapsed < 4.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // show PERFECT message

            // ...bonus for perfect?
            if (player == 0)
            {
                AddAsyncTask(AsyncTask_UpdateScore((int)(Game.Instance.ActiveGameplaySettings.ScoreMultiplier * (float)Game.Instance.ActiveGameplaySettings.EnergyBar)), true);
            }

            PlayPerfectSFX();

            messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.CharacterHeight = 110;
            messageText.Font = font;
            messageText.TextValue = "PERFECT!";

            // pause for a few seconds
            elapsed = 0.0f;

            while (elapsed < 2.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // finished
            UnRegisterWaiting("showperfectwinmessage");
        }
    }
}
