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
using GarageGames.Torque.MathUtil;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates.Dialogs;
using Microsoft.Xna.Framework.Graphics;
using MathFreak.vfx;
using MathFreak.Text;
using MathFreak.Highscores;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate is the base class for the character selection screen - this screen also acts as
    /// the Lobby in multiplayer games (the screen where the players both agree they are ready to play).
    /// </summary>
    [TorqueXmlSchemaType]
    public abstract class GameStateCharacterSelectionLobby : GameState
    {
        protected bool _isExiting;

        private enum EnumTeleportAnimState { Finished, Lightening, Effect };

        protected const String TASKLIST_CHARACTERSELECTION_ASYNC_EVENTS = "characterselectionasyncevents";
        private List<String> _waitList;
        protected float _dt;

        public const int PLAYER_1 = 0;
        public const int PLAYER_2 = 1;

        protected float[] ANGLES = new float[5] { 0.0f, 72.0f, 144.0f, 216.0f, 288.0f };

        protected TutorManager.EnumTutor _player1_hilightedCharacter;
        protected TutorManager.EnumTutor _player2_hilightedCharacter;
        protected float _player1_displayedSelectorAngle;
        protected float _player2_displayedSelectorAngle;
        protected float _player1_targetSelectorAngle;
        protected float _player2_targetSelectorAngle;
        protected TutorManager.EnumTutor _player1_selectedCharacter;
        protected TutorManager.EnumTutor _player2_selectedCharacter;
        protected TutorManager.EnumTutor[] _teleporterRequest = new TutorManager.EnumTutor[2];

        protected PlayerCharacterSelector _player1_characterSelector;
        protected PlayerCharacterSelector _player2_characterSelector;

        private Vector2 _arrow1Pos;
        private Vector2 _arrow2Pos;

        private RenderMaterial _arrow1Texture;
        private RenderMaterial _arrowBothTexture;
        private RenderMaterial _p1Texture;
        private RenderMaterial _p2Texture;
        private RenderMaterial _p1p2Texture;

        // special stuff for handling boss selection (which requires a combo of button presses)
        protected Buttons[] _comboToMatch = new Buttons[] { Buttons.DPadUp, Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight, Buttons.DPadLeft, Buttons.DPadRight, Buttons.Y, Buttons.X, Buttons.Y, Buttons.X };
        private List<Buttons> _comboEntered;
        private bool _buttonWasDown;
        private Buttons _buttonThatWasDown;

        protected bool _canUseSelectorWheel;

        protected bool CanUseSelectorWheel
        {
            get { return _canUseSelectorWheel && !_isExiting; }
        }


        public override void  Init()
        {
            base.Init();

            _waitList = new List<String>();
            AsyncTaskManager.Instance.NewTaskList(TASKLIST_CHARACTERSELECTION_ASYNC_EVENTS);
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _isExiting = false;
            _canUseSelectorWheel = false;
            FEUIbackground.PlayMusic();
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            FEUIbackground.Instance.Unload();

            // note: loading the scene *after* transitioning the other screen off and unloaded it so we don't overlap with the other scene            
            Game.Instance.LoadScene(@"data\levels\CharacterSelectionLobby.txscene");
            Game.Instance.LoadScene(@"data\levels\CharacterSelectionLobby_Extra.txscene");

            _arrow1Pos = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_arrow1").Position;
            _arrow2Pos = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_arrow2").Position;

            InitializeScene();
            SetupCharacterSelectors();
            InitializeSelections();

            //GUIManager.Instance.ActivateGUI();

            InitBossSelection();

            _canUseSelectorWheel = true;
        }

        // derived classes can do any extra scene intialization here
        protected virtual void InitializeScene()
        {
            TutorManager.Instance.CacheAnims();

            // cache the textures we will need to swap around (there is a 'quit' dialog so we need to make sure we have the textures cached due to TX silliness)
            _arrow1Texture = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Selecttion_screen_green_P1Material");
            _arrowBothTexture = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Selecttion_screen_violet_P1_P2Material");
            _p1Texture = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("_1PMaterial");
            _p2Texture = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("_2PMaterial");
            _p1p2Texture = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("_1P2PMaterial");

            // initially hide the teleport character sprites and overlay animations
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_character1").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_character2").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_overlayanim1").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_overlayanim2").Visible = false;

            // set up the teleporter requests and async tasks
            _teleporterRequest[0] = TutorManager.EnumTutor.None;
            _teleporterRequest[1] = TutorManager.EnumTutor.None;
            AddAsyncTask(AsyncTask_TeleportAnimation(TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_character1"), TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("selection_overlayanim1"), PLAYER_1), true);
            AddAsyncTask(AsyncTask_TeleportAnimation(TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_character2"), TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("selection_overlayanim2"), PLAYER_2), true);

            // fill in the gamertags
            UpdateGamerTag(PLAYER_1);
            UpdateGamerTag(PLAYER_2);

            // timeout counter - only visible in LIVE multiplayer
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_counter").Visible = false;
        }

        protected virtual void InitializeSelections()
        {
            _player1_selectedCharacter = TutorManager.EnumTutor.None;
            _player2_selectedCharacter = TutorManager.EnumTutor.None;
            _player1_hilightedCharacter = TutorManager.EnumTutor.None;
            _player2_hilightedCharacter = TutorManager.EnumTutor.None;

            MoveToCharacter(PLAYER_1, Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].Character);
            MoveToCharacter(PLAYER_2, Game.Instance.ActiveGameplaySettings.Players[PLAYER_2].Character);

            if (_player1_hilightedCharacter != TutorManager.EnumTutor.MathLord)
            {
                _player1_displayedSelectorAngle = ANGLES[(int)_player1_hilightedCharacter];
            }

            if (_player2_hilightedCharacter != TutorManager.EnumTutor.MathLord)
            {
                _player2_displayedSelectorAngle = ANGLES[(int)_player2_hilightedCharacter];
            }

            UpdateSelectionDisplay();

            SetSelectorAngles(_player1_targetSelectorAngle, _player2_targetSelectorAngle);
        }

        protected virtual void SetupCharacterSelectors()
        {
            _player1_characterSelector = Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].GetCharacterSelector(PLAYER_1);
            _player2_characterSelector = Game.Instance.ActiveGameplaySettings.Players[PLAYER_2].GetCharacterSelector(PLAYER_2);
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();
            DoUnload();
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();
            DoUnload();
        }

        protected virtual void DoUnload()
        {
            AsyncTaskManager.Instance.KillAllTasks(TASKLIST_CHARACTERSELECTION_ASYNC_EVENTS, true);
            _waitList.Clear();
            Game.Instance.UnloadScene(@"data\levels\CharacterSelectionLobby_Extra.txscene");
            Game.Instance.UnloadScene(@"data\levels\CharacterSelectionLobby.txscene");
        }

        public override bool CanAcceptInvite()
        {
            return !_isExiting;
        }

        public override void OnInviteAccepted()
        {
            base.OnInviteAccepted();
            _isExiting = true;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            ProcessBossSelection();

            UpdateSelectorAngles();

            // tick the async events list
            _dt = dt;

            AsyncTaskManager.Instance.Tick(TASKLIST_CHARACTERSELECTION_ASYNC_EVENTS);

            // if there's anything that wants us to wait until it completes then just return
            if (_waitList.Count > 0) return;

            ProcessTick(dt);
        }

        protected virtual void InitBossSelection()
        {
            _comboEntered = new List<Buttons>(_comboToMatch.Length);
            _buttonWasDown = false;

            // button/s may be down when entering the screen so need to discount that 'press' initially
            GamePadState gps = GamePad.GetState(Game.Instance.XNAPlayerIndex);

            for (Buttons buttonID = Buttons.DPadUp; buttonID <= Buttons.Y; buttonID = (Buttons)((int)buttonID * 2)) // buttons enum goes up as multiples of two so we're using that to loop through them quickly (note: Enum.GetValues() is not available on xbox)
            {
                if (gps.IsButtonDown(buttonID))
                {
                    _buttonWasDown = true;
                    _buttonThatWasDown = buttonID;
                    Debug.WriteLine("Button detected already pressed: " + buttonID);
                    break;  // done now - we only search for the first pressed button
                }
            }
        }

        protected virtual void ProcessBossSelection()
        {
            // already entered enough button presses?
            if (_comboEntered.Count >= _comboToMatch.Length) return;

            // else get another one if there is one
            GamePadState gps = GamePad.GetState(Game.Instance.XNAPlayerIndex);

            bool buttonsAreDown = false;

            for (Buttons buttonID = Buttons.DPadUp; buttonID <= Buttons.Y; buttonID = (Buttons)((int)buttonID * 2))
            {
                // button was pressed
                if (gps.IsButtonDown(buttonID))
                {
                    buttonsAreDown = true;

                    // button is newly pressed this gametick
                    if (!_buttonWasDown || buttonID != _buttonThatWasDown)
                    {
                        _buttonThatWasDown = buttonID;
                        _comboEntered.Add(buttonID);
                        Debug.WriteLine("Entered: " + buttonID);
                        break;  // done now - we only search for the first newly pressed button we come across and ignore the rest
                    }
                }
            }

            _buttonWasDown = buttonsAreDown;

            // if got enough button presses then check for a match
            if (_comboEntered.Count == _comboToMatch.Length)
            {
                for (int i = 0; i < _comboToMatch.Length; i++)
                {
                    if (_comboEntered[i] != _comboToMatch[i]) return;   // failed to match
                }

                // matched!
                Debug.WriteLine("combo matched!");

                _player1_hilightedCharacter = TutorManager.EnumTutor.MathLord;
                OnAction_Pressed_A(PLAYER_1);
            }
        }

        protected void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_CHARACTERSELECTION_ASYNC_EVENTS, startImmediately);
        }

        // Derived classes should override this instead of Tick()
        public virtual void ProcessTick(float dt)
        {
            if (CanUseSelectorWheel)
            {
                _player1_characterSelector.Tick(this, dt);
                _player2_characterSelector.Tick(this, dt);
            }
        }

        protected void RegisterWaiting(String id)
        {
            // we don't have any tasks that cause the character selection screen to wait, which can have multiple instances going on at the same time
            Assert.Fatal(!_waitList.Contains(id), "Duplicate task added to character selection screen wait list");

            _waitList.Add(id);
        }

        protected bool IsWaiting(String id)
        {
            return _waitList.Contains(id);
        }

        protected void UnRegisterWaiting(String id)
        {
            _waitList.Remove(id);
        }

        public PlayerLocalCharacterSelectorInputComponent GetNextAvailableInputComponent()
        {
            // return the first available input component
            PlayerLocalCharacterSelectorInputComponent inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_inputcomponent1").Components.FindComponent<PlayerLocalCharacterSelectorInputComponent>();
            if (inputComponent.IsAvailable) return inputComponent;

            inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_inputcomponent2").Components.FindComponent<PlayerLocalCharacterSelectorInputComponent>();
            if (inputComponent.IsAvailable) return inputComponent;

            Assert.Fatal(false, "No available input components for character selection!!!");
            return null;
        }

        protected virtual void QuitSelectionScreen(int player)
        {
            Debug.WriteLine("quit selection screen?");
            AddAsyncTask(AsyncTask_ShowQuitDialog(), true);
        }

        public virtual void OnAction_Pressed_A(int player)
        {
            if (player == PLAYER_1)
            {
                SelectCharacter(player, _player1_hilightedCharacter);
            }
            else
            {
                SelectCharacter(player, _player2_hilightedCharacter);
            }
        }

        public virtual void OnAction_Pressed_B(int player)
        {
            QuitSelectionScreen(player);
        }

        protected virtual bool CheckReadyToPlay()
        {
            if (AllPlayersReady())
            {
                AddAsyncTask(AsyncTask_GotoVsScreen(), true);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool AllPlayersReady()
        {
            return (_player1_selectedCharacter != TutorManager.EnumTutor.None && _player2_selectedCharacter != TutorManager.EnumTutor.None);
        }

        public virtual void MoveToNextCharacter(int player)
        {
            if (player == PLAYER_1)
            {
                MoveToCharacter(PLAYER_1, TutorManager.GetTutorAfter(_player1_hilightedCharacter));
            }
            else
            {
                MoveToCharacter(PLAYER_2, TutorManager.GetTutorAfter(_player2_hilightedCharacter));
            }
        }

        public virtual void MoveToPrevCharacter(int player)
        {
            if (player == PLAYER_1)
            {
                MoveToCharacter(PLAYER_1, TutorManager.GetTutorBefore(_player1_hilightedCharacter));
            }
            else
            {
                MoveToCharacter(PLAYER_2, TutorManager.GetTutorBefore(_player2_hilightedCharacter));
            }
        }

        public virtual void MoveToCharacterAtAngle(int player, float angle)
        {
            const float RANGE = 30.0f;

            // based on the angle we will decide which character to highlight (i.e. 5 ranges to test)
            for (int i = 0; i < 5; i++)
            {
                float characterAngle = ANGLES[i];

                if (i == 0) // angle is zero so need to handle wrapping issues
                {
                    if (angle >= 360.0f - RANGE || angle <= RANGE)
                    {
                        MoveToCharacter(player, (TutorManager.EnumTutor)0);
                        break;
                    }
                }
                else
                {
                    if (angle >= characterAngle - RANGE && angle <= characterAngle + RANGE)
                    {
                        MoveToCharacter(player, (TutorManager.EnumTutor)i);
                        break;
                    }
                }
            }
        }

        public virtual void MoveToCharacter(int player, TutorManager.EnumTutor character)
        {
            if (character == TutorManager.EnumTutor.None)
            {
                character = TutorManager.EnumTutor.Caveman;
            }

            if (player == PLAYER_1)
            {
                if (_player1_hilightedCharacter != character)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.SelectionArrowMoved);
                }

                _player1_hilightedCharacter = character;

            }
            else
            {
                if (_player2_hilightedCharacter != character)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.SelectionArrowMoved);
                }

                _player2_hilightedCharacter = character;
            }

            UpdateSelectionDisplay();
        }

        /// <summary>
        /// Selects the currently highlighted character (i.e a highlighting a character does not select it - selection has to be explicitly requested)
        /// </summary>
        protected virtual void SelectCharacter(int player, TutorManager.EnumTutor character)
        {
            if (player == PLAYER_1)
            {
                if (character != _player1_selectedCharacter)
                {
                    if (OnSelectionChanged(player, character))
                    {
                        DoSelectionChange(player, character);
                    }
                }
            }
            else
            {
                if (character != _player2_selectedCharacter)
                {
                    if (OnSelectionChanged(player, character))
                    {
                        DoSelectionChange(player, character);
                    }
                }
            }
        }

        protected void DoSelectionChange(int player, TutorManager.EnumTutor character)
        {
            if (player == PLAYER_1)
            {
                _teleporterRequest[PLAYER_1] = character;

                _player1_selectedCharacter = character;
                Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].Character = character;
            }
            else
            {
                _teleporterRequest[PLAYER_2] = character;

                _player2_selectedCharacter = character;
                Game.Instance.ActiveGameplaySettings.Players[PLAYER_2].Character = character;
            }

            if (character == TutorManager.EnumTutor.None)
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.TeleportOut);
            }
            else
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.TeleportIn);
            }

            if (character == TutorManager.EnumTutor.MathLord)
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.BossLaughter);
            }

            UpdateSelectionDisplay();
            CheckReadyToPlay();
        }

        // should return true if the selection change is okay to go ahead with,
        // else should return false.
        protected virtual bool OnSelectionChanged(int player, TutorManager.EnumTutor character)
        {
            return true;
        }

        protected virtual void UpdateSelectionDisplay()
        {
            // update target for the target position to move the selector to
            if (_player1_hilightedCharacter != TutorManager.EnumTutor.None && _player1_hilightedCharacter != TutorManager.EnumTutor.MathLord)
            {
                _player1_targetSelectorAngle = ANGLES[(int)_player1_hilightedCharacter];
            }


            if (_player2_hilightedCharacter != TutorManager.EnumTutor.None && _player2_hilightedCharacter != TutorManager.EnumTutor.MathLord)
            {
                _player2_targetSelectorAngle = ANGLES[(int)_player2_hilightedCharacter];
            }

            UpdateABPrompts();
        }

        protected virtual void UpdateABPrompts()
        {
        }

        public void UpdateGamerTag(int player)
        {
            if (player == PLAYER_1)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_gamertag1").Components.FindComponent<MathMultiTextComponent>().TextValue = Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].GamerTag;
            }
            else
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("selection_gamertag2").Components.FindComponent<MathMultiTextComponent>().TextValue = Game.Instance.ActiveGameplaySettings.Players[PLAYER_2].GamerTag;
            }
        }

        private void UpdateSelectorAngles()
        {
            const float ANGLE_INC = 5.0f;

            float diff1 = Util.SmallestDiffBetweenAngles(_player1_displayedSelectorAngle, _player1_targetSelectorAngle);
            float diff2 = Util.SmallestDiffBetweenAngles(_player2_displayedSelectorAngle, _player2_targetSelectorAngle);

            if (diff1 < 0.1f)
            {
                if (diff1 < -ANGLE_INC)
                {
                    _player1_displayedSelectorAngle -= ANGLE_INC;
                }
                else
                {
                    _player1_displayedSelectorAngle = _player1_targetSelectorAngle;
                }

                _player1_displayedSelectorAngle = _player1_displayedSelectorAngle % 360.0f;
            }
            else if (diff1 > 0.1f)
            {
                if (diff1 > ANGLE_INC)
                {
                    _player1_displayedSelectorAngle += ANGLE_INC;
                }
                else
                {
                    _player1_displayedSelectorAngle = _player1_targetSelectorAngle;
                }

                _player1_displayedSelectorAngle = _player1_displayedSelectorAngle % 360.0f;
            }
            else
            {
                _player1_displayedSelectorAngle = _player1_targetSelectorAngle;
            }


            if (diff2 < 0.1f)
            {
                if (diff2 < -ANGLE_INC)
                {
                    _player2_displayedSelectorAngle -= ANGLE_INC;
                }
                else
                {
                    _player2_displayedSelectorAngle = _player2_targetSelectorAngle;
                }

                _player2_displayedSelectorAngle = _player2_displayedSelectorAngle % 360.0f;
            }
            else if (diff2 > 0.1f)
            {
                if (diff2 > ANGLE_INC)
                {
                    _player2_displayedSelectorAngle += ANGLE_INC;
                }
                else
                {
                    _player2_displayedSelectorAngle = _player2_targetSelectorAngle;
                }

                _player2_displayedSelectorAngle = _player2_displayedSelectorAngle % 360.0f;
            }
            else
            {
                _player2_displayedSelectorAngle = _player2_targetSelectorAngle;
            }

            SetSelectorAngles(_player1_displayedSelectorAngle, _player2_displayedSelectorAngle);
        }

        private void SetSelectorAngles(float angle1, float angle2)
        {
            T2DStaticSprite arrow1 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_arrow1");
            T2DStaticSprite arrow2 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_arrow2");
            T2DStaticSprite text1 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_p1text");
            T2DStaticSprite text2 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_p2text");

            // position arrows
            arrow1.Rotation = angle1;
            Rotation2D rotation = new Rotation2D(MathHelper.ToRadians(arrow1.Rotation));
            Vector2 rotationOffset = new Vector2(0.0f, 102.5f);
            Vector2 rotatedOffset = rotation.Rotate(rotationOffset);
            arrow1.Position = _arrow1Pos + (rotationOffset - rotatedOffset);

            arrow2.Rotation = angle2;
            rotation = new Rotation2D(MathHelper.ToRadians(arrow2.Rotation));
            rotationOffset = new Vector2(0.0f, 102.5f);
            rotatedOffset = rotation.Rotate(rotationOffset);
            arrow2.Position = _arrow2Pos + (rotationOffset - rotatedOffset);

            // update arrow sprites and text sprites (if both players' arrows are at the same position)
            float angleDiff = Util.SmallestDiffBetweenAngles(arrow1.Rotation, arrow2.Rotation);

            if (angleDiff < 0.1f && angleDiff > -0.1)
            {
                arrow1.Material = _arrowBothTexture;
                text1.Material = _p1p2Texture;
                arrow2.Visible = false;

                Texture2D texture = ((_p1p2Texture as SimpleMaterial).Texture.Instance as Texture2D);
                text1.Size = new Vector2(texture.Width, texture.Height);
            }
            else
            {
                arrow1.Material = _arrow1Texture;
                text1.Material = _p1Texture;
                arrow2.Visible = true;

                Texture2D texture = ((_p1Texture as SimpleMaterial).Texture.Instance as Texture2D);
                text1.Size = new Vector2(texture.Width, texture.Height);
            }
        }

        protected virtual void GotoVsScreen()
        {
        }

        // Will pause the selection screen processing (animations will run, but no user input accepted)
        // for a moment and then push the Vs Screen gamestate
        protected IEnumerator<AsyncTaskStatus> AsyncTask_GotoVsScreen()
        {
            if (_isExiting) yield break;

            _isExiting = true;  // both players have selected so we *will* be exiting the gamestate

            // block non-async gameplay state processing
            RegisterWaiting("gotovsscreen");

            // pause for a moment so players can see what the selections are
            float elapsedTime = 0.0f;

            while (elapsedTime < 1.5f)
            {
                elapsedTime += _dt;
                yield return null;
            }

            // push the next gamestate
            Debug.WriteLine("Character selection screen: moving to next game state");
            GotoVsScreen();

            // note: we don't unregister the wait request because nothing else should happen until this screen unloads now anyway (except for any async stuff still running obviously)
            //UnRegisterWaiting("gotovsscreen");
        }

        protected virtual void OnQuitting()
        {
        }

        // Shows the 'quit' dialog and waits for a response - if the player says 'yes' then we will quit
        // to the previous game state, else we just kill the dialog and return to character selection.
        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowQuitDialog()
        {
            if (_isExiting) yield break;

            //// block non-async gameplay state processing
            //RegisterWaiting("quitselectionscreen");

            // show the dialog
            _canUseSelectorWheel = false;
            DialogQuitCharacterSelection.ResetResponse();
            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_QUITCHARACTERSELECTION, null);

            // wait for a response from the user
            while (!DialogQuitCharacterSelection.ResponseRecieved) yield return null;

            yield return null;  // allow an extra tick so the dialog is *definitely* finished unloading itself before we unload ourselves too - or things could get messed up.

            if (_isExiting) yield break;

            // if the player said 'yes' then quit the selection screen - but don't unregister the waiting
            // request because nothing else should be happening with this screen processing wise.
            if (DialogQuitCharacterSelection.Response)
            {
                _isExiting = true;

                Debug.WriteLine("quitting sc!");
                OnQuitting();
                DoQuitting();
            }
            // player cancelled the dialog - go back to business as usual
            else
            {
                Debug.WriteLine("NOT quitting sc!");
                _canUseSelectorWheel = true;
                //UnRegisterWaiting("quitselectionscreen");
            }
        }

        protected virtual void DoQuitting()
        {
            GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
        }

        // This async task will run for the duration that the selection screen is displayed.
        // It will poll for teleport in/out requests and process the teleporting visual representation
        // as approprite - if a request is made in the middle of a teleport in/out then that is fine;
        // the visual representation will simply switch to the opposite direction.
        protected IEnumerator<AsyncTaskStatus> AsyncTask_TeleportAnimation(T2DStaticSprite tutorSprite, T2DAnimatedSprite overlayAnim, int player)
        {
            // set up some variables (starts with no tutor in the tube)
            const float LIGHTENING_DURATION = 0.05f; // amount of time lightening is playing before the teleport effect kicks in
            const float EFFECT_DURATION = 1.0f;
            const float TOTAL_DURATION = LIGHTENING_DURATION + EFFECT_DURATION;
            const float TELEPORTOFFSET_FPS = 30.0f; // the teleporter 'effect' can run a bit too fast (full fps), so this constant determines the frame rate we really want it to animate at

            TeleportEffectComponent effect = tutorSprite.Components.FindComponent<TeleportEffectComponent>();
            float animTime = 0.0f;
            float lighteningTime = 0.0f;
            float teleportOffsetTime = 0.0f;
            bool playForwards = false;
            EnumTeleportAnimState animState = EnumTeleportAnimState.Finished;
            TutorManager.EnumTutor currentTutor = TutorManager.EnumTutor.None;
            TutorManager.EnumTutor nextTutor = TutorManager.EnumTutor.None;

            overlayAnim.AnimationData.Material.IsTranslucent = true;
            (overlayAnim.AnimationData.Material as SimpleMaterial).IsColorBlended = true;
            overlayAnim.Visible = true;
            overlayAnim.VisibilityLevel = 0.0f;
            tutorSprite.Visible = false;

            // this will just loop inifinitely, polling for tutor selection changes and processing the current animation state
            while (true)
            {
                // if the player made a selection different from our current tutor then record their
                // selection as the next tutor to display and set playback direction
                if (_teleporterRequest[player] != TutorManager.EnumTutor.None)
                {
                    nextTutor = _teleporterRequest[player];
                    _teleporterRequest[player] = TutorManager.EnumTutor.None;

                    // ...if there is a current tutor then switch to reverse playback
                    if (currentTutor != TutorManager.EnumTutor.None)
                    {
                        playForwards = false;
                    }
                    // else setup the current tutor (this happens only the first time the teleport is filled)
                    else
                    {
                        currentTutor = nextTutor;
                    }
                }

                // process the current animation state
                switch (animState)
                {
                    case EnumTeleportAnimState.Finished:
                        // if there is a tutor waiting to get selected then
                        if (nextTutor != TutorManager.EnumTutor.None)
                        {
                            // ...if the next tutor is the same as the current tutor then the tutor is arriving
                            if (nextTutor == currentTutor)
                            {
                                playForwards = true;
                                nextTutor = TutorManager.EnumTutor.None;
                                SimpleMaterial sm = TutorManager.Instance.GetTeleportPic(player, currentTutor);
                                effect.BaseTexture = TutorManager.Instance.GetTeleportPic(player, currentTutor);
                                animState = EnumTeleportAnimState.Lightening;
                                lighteningTime = 0.0f;
                            }
                            // ...else they are waiting for the current tutor to leave
                            else
                            {
                                playForwards = false;
                                animState = EnumTeleportAnimState.Effect;
                                animTime = 1.0f;
                                lighteningTime = 1.0f;
                                tutorSprite.Visible = true;
                            }
                        }
                        break;

                    case EnumTeleportAnimState.Lightening:
                        // if playing forwards
                        if (playForwards)
                        {
                            // ...set the animation playback direction
                            overlayAnim.PlayBackwards = false;

                            // ...update the timer
                            lighteningTime += _dt;

                            // ...update lightening alpha
                            overlayAnim.VisibilityLevel = 1.0f;

                            // ...if enough time has elapsed then goto the effect state
                            if (lighteningTime >= LIGHTENING_DURATION)
                            {
                                animState = EnumTeleportAnimState.Effect;
                                animTime = 0.0f;
                                tutorSprite.Visible = true;
                            }
                        }
                        // else playing backwards
                        else
                        {
                            // ...set the animation playback direction
                            overlayAnim.PlayBackwards = true;

                            // ...update the timer
                            lighteningTime -= _dt;

                            // ...update lightening alpha
                            overlayAnim.VisibilityLevel = 1.0f;

                            // ...if timer has reached zero then goto the finished state
                            if (lighteningTime <= 0.0f)
                            {
                                overlayAnim.VisibilityLevel = 0.0f;
                                animState = EnumTeleportAnimState.Finished;
                                currentTutor = nextTutor;
                            }
                        }
                        break;

                    case EnumTeleportAnimState.Effect:
                        // if playing forwards
                        if (playForwards)
                        {
                            // ...set lightening anim direction
                            overlayAnim.PlayBackwards = false;

                            // ...update the timers
                            lighteningTime += _dt;
                            animTime += _dt;
                            teleportOffsetTime += _dt;

                            // ...update lightening alpha
                            overlayAnim.VisibilityLevel = 1.0f - 2.0f * (animTime / EFFECT_DURATION);

                            // ...update the effect params
                            effect.AnimTime = animTime / TOTAL_DURATION;    // note: we normalize this so the effect can assume a rangr of 0-1

                            if (teleportOffsetTime >= 1.0f / TELEPORTOFFSET_FPS)
                            {
                                teleportOffsetTime -= (1.0f / TELEPORTOFFSET_FPS);
                                effect.TeleportOffset = new Vector2((float)Game.Instance.Rnd.NextDouble(), (float)Game.Instance.Rnd.NextDouble());
                            }

                            // ...if enough time has elapsed then goto the finished state
                            if (animTime >= EFFECT_DURATION)
                            {
                                overlayAnim.VisibilityLevel = 0.0f;
                                effect.AnimTime = 1.0f;
                                animState = EnumTeleportAnimState.Finished;
                                nextTutor = TutorManager.EnumTutor.None;
                            }
                        }
                        // else playing backwards
                        else
                        {
                            // ...set lightening anim direction
                            overlayAnim.PlayBackwards = true;

                            // ...update the timers
                            lighteningTime -= _dt;
                            animTime -= _dt;
                            teleportOffsetTime -= _dt;

                            // ...update lightening alpha
                            overlayAnim.VisibilityLevel = 1.0f - 2.0f * (animTime / EFFECT_DURATION);

                            // ...update the effect params
                            effect.AnimTime = animTime / TOTAL_DURATION;    // note: we normalize this so the effect can assume a rangr of 0-1

                            if (teleportOffsetTime <= 0.0f)
                            {
                                teleportOffsetTime = 1.0f / TELEPORTOFFSET_FPS + teleportOffsetTime;
                                effect.TeleportOffset = new Vector2((float)Game.Instance.Rnd.NextDouble(), (float)Game.Instance.Rnd.NextDouble());
                            }

                            // ...if timer has reached zero then goto the lightening state
                            if (animTime <= 0.0f)
                            {
                                effect.AnimTime = 0.0f;
                                animState = EnumTeleportAnimState.Lightening;
                                tutorSprite.Visible = false;
                            }
                        }
                        break;
                }

                yield return null;
            }
        }
    }
}
