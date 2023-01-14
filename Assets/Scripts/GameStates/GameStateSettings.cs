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



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate is the base class for gamestates dealing with the game settings.
    /// </summary>
    [TorqueXmlSchemaType]
    public abstract class GameStateSettings : GameState
    {
        protected bool _isExiting; // this is an important flag - there are multiple ways to exit this gamestate and they can be triggered independently of one another, so in order to stop exiting-based 'race' conditions this flag is checked before any 'exiting' of the gamestate is commenced (the flag will of course be set to true once an 'exiting' process has commenced so that no other 'exiting' process will be allowed to start)

        protected static GameStateSettings _activeInstance;

        private List<T2DStaticSprite> _readyText = new List<T2DStaticSprite>(2);
        protected List<bool> _readyStatus = new List<bool>(2);

        protected const String TASKLIST_SETTINGSLOBBY_ASYNC_EVENTS = "settingslobbyasyncevents";
        private List<String> _waitList;

        protected bool _isShowingQuitDialog;

        protected bool _showChatIcon = false;
        protected bool _showKickPrompt = false;


        public override void Init()
        {
            base.Init();

            _waitList = new List<String>();
            AsyncTaskManager.Instance.NewTaskList(TASKLIST_SETTINGSLOBBY_ASYNC_EVENTS);
        }

        protected void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_SETTINGSLOBBY_ASYNC_EVENTS, startImmediately);
        }

        protected void RegisterWaiting(String id)
        {
            // we don't have any tasks that cause the settings lobby to wait, which can have multiple instances going on at the same time
            Assert.Fatal(!_waitList.Contains(id), "Duplicate task added to settings lobby screen wait list");

            _waitList.Add(id);
        }

        protected void UnRegisterWaiting(String id)
        {
            _waitList.Remove(id);
        }

        protected bool IsWaiting(String id)
        {
            return _waitList.Contains(id);
        }

        public override void PreTransitionOn(string paramString)
        {
            _activeInstance = this;
            _isExiting = false;
            _isShowingQuitDialog = false;

            // clear lists so game doesn't automatically proceed to the selection screen
            _readyStatus.Clear();
            _readyText.Clear();

            FEUIbackground.PlayMusic();

            base.PreTransitionOn(paramString);
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            FEUIbackground.Instance.Unload();

            // note: loading the scene *after* transitioning the other screen off and unloaded it so we don't overlap with the other scene
            Game.Instance.LoadScene(@"data\levels\Settings.txscene");
            Game.Instance.LoadScene(@"data\levels\Settings_Extra.txscene");

            InitializeSettings();
            InitializeScene();

            //HighscoreData.Instance.Save();

            GUIManager.Instance.SetDefault(GetDefaultFocusedGUIComponent());
            GUIManager.Instance.ActivateGUI();
        }

        protected virtual GUIComponent GetDefaultFocusedGUIComponent()
        {
            throw new NotImplementedException();
        }

        protected virtual void InitializeScene()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("settings_kickplayers").Visible = false;
            UpdatePlayers();
            OnUpdatedMathGradeSettings();
        }

        protected virtual void InitializeSettings()
        {
            UpdateSettingsDisplays();
        }

        protected virtual List<Player> GetPlayerList()
        {
            throw new NotImplementedException();
        }

        // Called to update the player info displayed in this screen.  Derived classes will call this
        // when they have added or removed players and the list needs updating.
        protected void UpdatePlayers()
        {
            List<Player> players = GetPlayerList();

            // clear everything and hide all displays
            _readyStatus.Clear();
            _readyText.Clear();

            for (int i = 0; i < 2; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_ready" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamertag" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamerpic" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gradebadge" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_chat" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamerpicbg" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("settings_playerinput" + i).Components.FindComponent<ReadyToStartComponent>().ClearInputMap();
            }

            // fill the lists, show info, and hook local players up to an input component
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                // ready status
                _readyStatus.Add(false);
                _readyText.Add(TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_ready" + i));

                // gamertag
                T2DSceneObject gamertag = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamertag" + i);
                gamertag.Visible = true;
                gamertag.Components.FindComponent<MathMultiTextComponent>().TextValue = player.GamerTag;

                // grade badge
                T2DStaticSprite gradebadge = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gradebadge" + i);
                HighscoreData.Instance.UpdateGradeBadgeSprite(gradebadge, player.GamerTag);

                // chat icon
                if (_showChatIcon)
                {
                    T2DStaticSprite chatIcon = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_chat" + i);
                    UpdateChatIcon(chatIcon, player.GamerTag);
                }

                // kick prompt
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_kickplayers").Visible = _showKickPrompt;

                // gamer pic
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamerpicbg" + i).Visible = true;
                T2DStaticSprite gamerpic = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_gamerpic" + i);
                gamerpic.Visible = true;
                SimpleMaterial gamerPicMaterial = new SimpleMaterial();
                gamerpic.Material = gamerPicMaterial;

                try
                {
                    gamerPicMaterial.SetTexture(player.GamerPic);
                }
                catch (Exception e)
                {
                    Assert.Warn(false, "WARNING: Gamerpic for " + player.GamerTag + " could not be retrieved\n" + e.Message);
                    gamerPicMaterial.SetTexture(null);
                }

                // input component
                if (player is PlayerLocal)
                {
                    ReadyToStartComponent inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("settings_playerinput" + i).Components.FindComponent<ReadyToStartComponent>();
                    inputComponent.SetupInputMap(this, player as PlayerLocal);
                }
            }
        }

        protected virtual void UpdateChatIcon(T2DStaticSprite icon, string gamertag)
        {
        }

        protected void UpdateChatIcons()
        {
            if (!_showChatIcon) return;

            List<Player> players = GetPlayerList();

            for (int i = 0; i < players.Count; i++)
            {
                T2DStaticSprite chatIcon = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("settings_chat" + i);
                UpdateChatIcon(chatIcon, players[i].GamerTag);
            }
        }

        // Called to allow derived classes to update the ready statuses of players - this is polymorphic
        // because different types of player report 'ready' differently.  That is, players in a multiplayer
        // game will be in a network session (even in local multiplayer) and the session can tell us about
        // ready statuses, whereas in single player there is no network session, so we rely on direct input
        // from the player to tell us they are ready.
        protected virtual void UpdateReadyStatuses()
        {
            throw new NotImplementedException();
        }

        public virtual void ToggleLocalPlayerReady(PlayerLocal player)
        {
            player.IsReady = !player.IsReady;
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
            AsyncTaskManager.Instance.KillAllTasks(TASKLIST_SETTINGSLOBBY_ASYNC_EVENTS, true);
            _waitList.Clear();
            Game.Instance.UnloadScene(@"data\levels\Settings_Extra.txscene");
            Game.Instance.UnloadScene(@"data\levels\Settings.txscene");
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

            AsyncTaskManager.Instance.Tick(TASKLIST_SETTINGSLOBBY_ASYNC_EVENTS);

            // if there's anything that wants us to wait until it completes then just return
            if (_waitList.Count > 0) return;

            ProcessTick(dt);
        }

        protected virtual void ProcessTick(float dt)
        {
            if (_isExiting) return;

            UpdateChatIcons();

            // update the 'ready' indicators
            UpdateReadyStatuses();
            int readyCount = 0;

            for (int i = 0; i < _readyStatus.Count; i++)
            {
                if (_readyStatus[i])
                {
                    _readyText[i].Visible = true;
                    readyCount++;
                }
                else
                {
                    _readyText[i].Visible = false;
                }
            }

            // if all players are ready then it's time to go select the fighters
            if (readyCount == _readyStatus.Count && readyCount >= MinPlayersRequired())
            {
                OnAction_Continue();
            }
        }

        protected virtual int MinPlayersRequired()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnAction_Back()
        {
            AddAsyncTask(AsyncTask_ShowQuitDialog(), true);
        }

        protected virtual void OnQuitting()
        {
        }

        protected virtual bool OnAction_Continue()
        {
            if (!HasSelectedOneOrMoreGrades()) return false;    // have to have some grades selected or can't play the game

            if (!_isExiting)
            {
                // make sure a location has been picked
                Game.Instance.ActiveGameplaySettings.UpdateLocationToUse();

                _isExiting = true;
                GotoSelectionScreen();
                return true;
            }

            return false;
        }

        protected bool HasSelectedOneOrMoreGrades()
        {
            for (int i = 0; i < 14; i++)
            {
                if (Game.Instance.ActiveGameplaySettings.MathGradeIsEnabled(i)) return true;
            }

            return false;
        }

        protected virtual void GotoSelectionScreen()
        {
            GUIManager.Instance.SetFocus(null);
            GUIManager.Instance.DeactivateGUI();
        }

        protected virtual void OnAction_AllowChallengersToggle()
        {
        }

        protected virtual void OnAction_MathGradeIncrease()
        {
            // increase the math grade setting
            Game.Instance.ActiveGameplaySettings.IncreaseMathGradeSetting();

            // update the game settings for the math grades
            OnUpdatedMathGradeSettings();

            // update displays and misc
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_MathGradeDecrease()
        {
            // decrease the math grade setting
            Game.Instance.ActiveGameplaySettings.DecreaseMathGradeSetting();

            // update the game settings for the math grades
            OnUpdatedMathGradeSettings();

            // update displays and misc
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected void OnAction_GradeCheckboxToggled(int grade)
        {
            Game.Instance.ActiveGameplaySettings.EnableMathGrade(grade, TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("checkboxsettings_grade" + grade).Components.FindComponent<MFGradeCheckboxButton>().IsChecked);

            if (Game.Instance.ActiveGameplaySettings.MathGradesSetting == GamePlaySettings.EnumMathGradesSetting.Custom)
            {
                Game.Instance.CustomMathGradeSelections[0][grade] = Game.Instance.ActiveGameplaySettings.MathGradeIsEnabled(grade);
            }

            OnSettingsChanged();
        }

        protected void OnAction_UpdateNavigation(int grade)
        {
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_mathgrade").Components.FindComponent<GUIComponent>().South = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("checkboxsettings_grade" + grade);
        }

        protected void OnUpdatedMathGradeSettings()
        {
            switch (Game.Instance.ActiveGameplaySettings.MathGradesSetting)
            {
                case GamePlaySettings.EnumMathGradesSetting.Easy:
                    Game.Instance.ActiveGameplaySettings.EnableMathGrades(new bool[14] { false, true, true, true, true, false, false, false, false, false, false, false, false, false });
                    EnableGradeCheckboxes(false);
                    break;

                case GamePlaySettings.EnumMathGradesSetting.Medium:
                    Game.Instance.ActiveGameplaySettings.EnableMathGrades(new bool[14] { false, false, false, false, false, true, true, true, true, false, false, false, false, false });
                    EnableGradeCheckboxes(false);
                    break;

                case GamePlaySettings.EnumMathGradesSetting.Hard:
                    Game.Instance.ActiveGameplaySettings.EnableMathGrades(new bool[14] { false, false, false, false, false, false, false, false, false, true, true, true, true, false });
                    EnableGradeCheckboxes(false);
                    break;

                case GamePlaySettings.EnumMathGradesSetting.All:
                    Game.Instance.ActiveGameplaySettings.EnableMathGrades(new bool[14] { false, true, true, true, true, true, true, true, true, true, true, true, true, true });
                    EnableGradeCheckboxes(false);
                    break;

                case GamePlaySettings.EnumMathGradesSetting.Custom:
                    Game.Instance.ActiveGameplaySettings.EnableMathGrades(GetCustomSettings());
                    EnableGradeCheckboxes(true);
                    break;

                default:
                    Assert.Fatal(false, "UpdateMathGradeSettings() - invalid math grade setting: " + Game.Instance.ActiveGameplaySettings.MathGradesSetting);
                    break;
            }

            for (int i = 1; i < 14; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("checkboxsettings_grade" + i).Components.FindComponent<MFGradeCheckboxButton>().IsChecked = Game.Instance.ActiveGameplaySettings.MathGradeIsEnabled(i);
            }
        }

        protected virtual bool[] GetCustomSettings()
        {
            return Game.Instance.CustomMathGradeSelections[0];
        }

        protected void EnableGradeCheckboxes(bool enable)
        {
            for (int i = 1; i < 14; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("checkboxsettings_grade" + i).Components.FindComponent<MFGradeCheckboxButton>().Enabled = enable;
            }

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("settings_xprompt").Visible = enable;
        }

        protected virtual void OnAction_EnergyBarIncrease()
        {
            Game.Instance.ActiveGameplaySettings.EnergyBar += 10;
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_EnergyBarDecrease()
        {
            Game.Instance.ActiveGameplaySettings.EnergyBar -= 10;
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_DifficultyLevelIncrease()
        {
            Game.Instance.ActiveGameplaySettings.DifficultyLevel++;
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_DifficultyLevelDecrease()
        {
            Game.Instance.ActiveGameplaySettings.DifficultyLevel--;
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_LocationIncrease()
        {
            Game.Instance.ActiveGameplaySettings.IncreaseLocation();
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected virtual void OnAction_LocationDecrease()
        {
            Game.Instance.ActiveGameplaySettings.DecreaseLocation();
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        // if a derived class needs to do something generic on *any* settings change then they
        // can override this method.
        protected virtual void OnSettingsChanged()
        {
        }

        protected void UpdateSettingsDisplays()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_acceptchallengers").Components.FindComponent<MFTextOptionButton>().TextLabel = "Challengers: " + (Game.Instance.ActiveGameplaySettings.AllowChallengers ? "Yes" : "No");
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_energybar").Components.FindComponent<MFTextOptionButton>().TextLabel = "Energy: " + Game.Instance.ActiveGameplaySettings.EnergyBar;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_mathgrade").Components.FindComponent<MFTextOptionButton>().TextLabel = "Grades: " + Game.Instance.ActiveGameplaySettings.MathGradesSetting;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_location").Components.FindComponent<MFTextOptionButton>().TextLabel = "Location: " + TutorManager.Instance.GetLocationName(0, Game.Instance.ActiveGameplaySettings.Location);

            switch (Game.Instance.ActiveGameplaySettings.DifficultyLevel)
            {
                case 0:
                    TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_difficultylevel").Components.FindComponent<MFTextOptionButton>().TextLabel = "Difficulty: Easy";
                    break;

                case 1:
                    TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_difficultylevel").Components.FindComponent<MFTextOptionButton>().TextLabel = "Difficulty: Medium";
                    break;

                case 2:
                    TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonsettings_difficultylevel").Components.FindComponent<MFTextOptionButton>().TextLabel = "Difficulty: Hard";
                    break;

                default:
                    Assert.Fatal(false, "Unrecognized difficulty level setting");
                    break;
            }
        }

        protected virtual void ShowCustomQuitDialog()
        {
            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_QUITSETTINGSLOBBY, null);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowQuitDialog()
        {
            if (_isExiting) yield break;

            //// block non-async gameplay state processing
            //RegisterWaiting("quitsettingslobby");
            _isShowingQuitDialog = true;

            DialogQuitSettingsLobby.ResetResponse();
            ShowCustomQuitDialog();

            // wait for a response from the user
            while (!DialogQuitSettingsLobby.ResponseRecieved) yield return null;

            yield return null;  // allow an extra tick so the dialog is *definitely* finished unloading itself before we unload ourselves too - or things could get messed up.

            if (_isExiting) yield break;

            _isShowingQuitDialog = false;

            // process the response
            switch (DialogQuitSettingsLobby.Response)
            {
                case DialogQuitSettingsLobby.EnumResponse.Cancelled:
                case DialogQuitSettingsLobby.EnumResponse.None: // will be none if the user hit escape/B-button to close the dialog
                    Debug.WriteLine("NOT quitting settings lobby!");
                    UpdatePlayers();
                    //UnRegisterWaiting("quitsettingslobby");
                    break;

                case DialogQuitSettingsLobby.EnumResponse.ReturnToMenu:
                    if (!_isExiting)
                    {
                        _isExiting = true;
                        Debug.WriteLine("quitting settings lobby!");
                        OnQuitting();
                        GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
                    }
                    break;

                case DialogQuitSettingsLobby.EnumResponse.ReturnToChampionship:
                    if (!_isExiting)
                    {
                        _isExiting = true;
                        Debug.WriteLine("returning to the championship game we left earlier!");
                        OnQuitting();
                        GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_SINGLEPLAYER, null);
                    }
                    break;
            }
        }



        public static GUIActionDelegate Back { get { return _back; } }
        //public static GUIActionDelegate Continue { get { return _continue; } }
        public static GUIActionDelegate AllowChallengersToggle { get { return _allowChallengersToggle; } }
        public static GUIActionDelegate MathGradeIncrease { get { return _mathGradeIncrease; } }
        public static GUIActionDelegate MathGradeDecrease { get { return _mathGradeDecrease; } }
        public static GUIActionDelegate EnergyBarIncrease { get { return _energyBarIncrease; } }
        public static GUIActionDelegate EnergyBarDecrease { get { return _energyBarDecrease; } }
        public static GUIActionDelegate DifficultyLevelIncrease { get { return _difficultyLevelIncrease; } }
        public static GUIActionDelegate DifficultyLevelDecrease { get { return _difficultyLevelDecrease; } }
        public static GUIActionDelegate LocationIncrease { get { return _locationIncrease; } }
        public static GUIActionDelegate LocationDecrease { get { return _locationDecrease; } }
        public static GUIActionDelegate GradeCheckboxToggle { get { return _gradeCheckboxToggle; } }
        public static GUIActionDelegate UpdateNavigation { get { return _updateNavigation; } }

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_Back();
        });

        //private static GUIActionDelegate _continue = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        //{
        //    _activeInstance.OnAction_Continue();
        //});

        private static GUIActionDelegate _allowChallengersToggle = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_AllowChallengersToggle();
        });

        private static GUIActionDelegate _mathGradeIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_MathGradeIncrease();
        });

        private static GUIActionDelegate _mathGradeDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_MathGradeDecrease();
        });

        private static GUIActionDelegate _energyBarIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_EnergyBarIncrease();
        });

        private static GUIActionDelegate _energyBarDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_EnergyBarDecrease();
        });

        private static GUIActionDelegate _difficultyLevelIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_DifficultyLevelIncrease();
        });

        private static GUIActionDelegate _difficultyLevelDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_DifficultyLevelDecrease();
        });

        private static GUIActionDelegate _locationIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_LocationIncrease();
        });

        private static GUIActionDelegate _locationDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_LocationDecrease();
        });

        private static GUIActionDelegate _gradeCheckboxToggle = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_GradeCheckboxToggled(int.Parse(paramString));
        });

        private static GUIActionDelegate _updateNavigation = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_UpdateNavigation(int.Parse(paramString));
        });
    }
}
