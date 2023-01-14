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
    /// This gamestate is the base class for gamestates displaying highscores
    /// </summary>
    [TorqueXmlSchemaType]
    public /*abstract*/ class GameStateHighscoresBase : GameState   // NOTE: not abstract because then the action delegates would not show up in TXB
    {
        protected const int PAGESIZE = 10;

        protected bool _invalidatedDisplay;

        protected static GameStateHighscoresBase _activeInstance;

        protected enum EnumViewMode { Friends = 1, MyScore, Overall, };

        protected EnumViewMode _viewMode;
        protected int _startPos;
        protected int _highlightedPos;


        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            FEUIbackground.Instance.Unload();

            // note: loading the scene *after* transitioning the other screen off and unloaded it so we don't overlap with the other scene
            Game.Instance.LoadScene(@"data\levels\" + GetSceneFileName() + ".txscene");
            Game.Instance.LoadScene(@"data\levels\Highscores_Extra.txscene");

            InitializeScene();
            InitializeData();

            _viewMode = EnumViewMode.MyScore;
            OnViewModeChanged();

            _invalidatedDisplay = true;

            GUIManager.Instance.ActivateGUI();
        }

        protected virtual void InitializeData()
        {
        }

        protected virtual string GetSceneFileName()
        {
            return null;
        }

        protected virtual void InitializeScene()
        {
        }

        protected virtual void UpdateHighscoreDisplay()
        {
            int count = GetScoreCount();

            if (_highlightedPos >= count)
            {
                _highlightedPos = count - 1;
            }

            if (_highlightedPos != -1)
            {
                MFHighscoreHighlighter highlighter = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_highlighter" + (_highlightedPos - _startPos)).Components.FindComponent<MFHighscoreHighlighter>();
                GUIManager.Instance.SetFocus(highlighter);
            }

            _invalidatedDisplay = false;
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();
            Game.Instance.UnloadScene(@"data\levels\Highscores_Extra.txscene");
            Game.Instance.UnloadScene(@"data\levels\" + GetSceneFileName() + ".txscene");
        }

        protected virtual int GetScoreCount()
        {
            return 0;
        }

        // derived classes should override this to return the position of the current gamer
        protected virtual int GetMyScorePos()
        {
            return -1;
        }

        private void SetStartPos(int pos)
        {
            int count = GetScoreCount();

            // check for going too far down the list
            if (pos > count - PAGESIZE)
            {
                pos = count - PAGESIZE;
            }

            // check for going too far up the list
            if (pos < 0)
            {
                pos = 0;
            }

            // if position has changed then update
            if (pos != _startPos)
            {
                _startPos = pos;
                _invalidatedDisplay = true;
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_invalidatedDisplay)
            {
                UpdateHighscoreDisplay();
            }
        }

        protected virtual void OnAction_Back()
        {
            GameStateManager.Instance.Pop();
        }

        private void OnAction_ViewModeChangeUp()
        {
            int viewMode = (int)_viewMode + 1;

            if (viewMode > 3)
            {
                viewMode = 1;
            }

            _viewMode = (EnumViewMode)viewMode;

            OnViewModeChanged();
        }

        private void OnAction_ViewModeChangeDown()
        {
            int viewMode = (int)_viewMode - 1;

            if (viewMode < 1)
            {
                viewMode = 3;
            }

            _viewMode = (EnumViewMode)viewMode;

            OnViewModeChanged();
        }

        protected virtual void OnViewModeChanged()
        {
            _invalidatedDisplay = true;
            int myPos;

            switch (_viewMode)
            {
                case EnumViewMode.Friends:
                    myPos = GetMyScorePos();
                    SetStartPos(myPos - PAGESIZE / 2);
                    _highlightedPos = myPos;
                    TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_viewmode").Components.FindComponent<MathMultiTextComponent>().TextValue = "< Friends >";
                    break;

                case EnumViewMode.MyScore:
                    myPos = GetMyScorePos();
                    SetStartPos(myPos - PAGESIZE / 2);
                    _highlightedPos = myPos;
                    TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_viewmode").Components.FindComponent<MathMultiTextComponent>().TextValue = "< My Score >";
                    break;

                case EnumViewMode.Overall:
                    SetStartPos(0);
                    _highlightedPos = 0;
                    TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_viewmode").Components.FindComponent<MathMultiTextComponent>().TextValue = "< Overall >";
                    break;
            }
        }

        private void OnAction_Up()
        {
            int newHighlightedPos = _highlightedPos - 1;

            // scrolling up?
            if (newHighlightedPos < _startPos)
            {
                SetStartPos(_startPos - 1);
                newHighlightedPos = _startPos;
                _highlightedPos = newHighlightedPos;
            }

            // check if highlighter moved
            if (newHighlightedPos != _highlightedPos)
            {
                _highlightedPos = newHighlightedPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_Down()
        {
            int newHighlightedPos = _highlightedPos + 1;

            // scrolling down?  (note: will automatically trigger an update of the display if scrolling occurs)
            if (newHighlightedPos >= _startPos + PAGESIZE)
            {
                SetStartPos(_startPos + 1);
                _highlightedPos = _startPos + PAGESIZE - 1;
            }

            // check if highlighter moved
            if (newHighlightedPos != _highlightedPos)
            {
                _highlightedPos = newHighlightedPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_PageUp()
        {
            SetStartPos(_startPos - PAGESIZE);

            if (_startPos != _highlightedPos)
            {
                _highlightedPos = _startPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_PageDown()
        {
            SetStartPos(_startPos + PAGESIZE);

            if (_startPos + PAGESIZE - 1 != _highlightedPos)
            {
                _highlightedPos = _startPos + PAGESIZE - 1;
                _invalidatedDisplay = true;
            }
        }



        public static GUIActionDelegate Back { get { return _back; } }
        public static GUIActionDelegate ViewModeChangeUp { get { return _viewModeChangeUp; } }
        public static GUIActionDelegate ViewModeChangeDown { get { return _viewModeChangeDown; } }
        public static GUIActionDelegate PageUp { get { return _pageUp; } }
        public static GUIActionDelegate PageDown { get { return _pageDown; } }
        public static GUIActionDelegate Up { get { return _up; } }
        public static GUIActionDelegate Down { get { return _down; } }

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_Back();
        });

        private static GUIActionDelegate _viewModeChangeUp = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_ViewModeChangeUp();
        });

        private static GUIActionDelegate _viewModeChangeDown = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_ViewModeChangeDown();
        });

        private static GUIActionDelegate _pageUp = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_PageUp();
        });

        private static GUIActionDelegate _pageDown= new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_PageDown();
        });

        private static GUIActionDelegate _up = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_Up();
        });

        private static GUIActionDelegate _down = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _activeInstance.OnAction_Down();
        });
    }
}
