using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Sim;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Util;
using GarageGames.Torque.GameUtil;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// The base component that all GUI components should ultimately derive from
    /// </summary>
    [TorqueXmlSchemaType]
    public class GUIComponent : TorqueComponent
    {
        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "1")]
        public virtual bool Enabled
        {
            get { return _enabled && SceneObject.Visible; }
            set { _enabled = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool IsNavigable
        {
            get { return _isNavigable; }
            set { _isNavigable = value; }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; }
        }

        public T2DSceneObject North
        {
            get { return _north; }
            set { _north = value; }
        }

        public T2DSceneObject South
        {
            get { return _south; }
            set { _south = value; }
        }

        public T2DSceneObject East
        {
            get { return _east; }
            set { _east = value; }
        }

        public T2DSceneObject West
        {
            get { return _west; }
            set { _west = value; }
        }

        /// <summary>
        /// Warning: this can go into an infinite loop if the gui component chain loops and all the
        /// components are disabled.  To prevent this situation it is advised that the caller does not
        /// query this property if the component is disabled.
        /// </summary>
        public T2DSceneObject NextNorth
        {
            get
            {
                // find the first enabled component to the north (if there is one)
                if (_north == null) return null;

                GUIComponent comp = _north.Components.FindComponent<GUIComponent>();

                if (comp.Enabled)
                {
                    return _north;
                }
                else
                {
                    return comp.NextNorth;
                }
            }
        }

        /// <summary>
        /// Warning: this can go into an infinite loop if the gui component chain loops and all the
        /// components are disabled.  To prevent this situation it is advised that the caller does not
        /// query this property if the component is disabled.
        /// </summary>
        public T2DSceneObject NextSouth
        {
            get
            {
                // find the first enabled component to the north (if there is one)
                if (_south == null) return null;

                GUIComponent comp = _south.Components.FindComponent<GUIComponent>();

                if (comp.Enabled)
                {
                    return _south;
                }
                else
                {
                    return comp.NextSouth;
                }
            }
        }

        /// <summary>
        /// Warning: this can go into an infinite loop if the gui component chain loops and all the
        /// components are disabled.  To prevent this situation it is advised that the caller does not
        /// query this property if the component is disabled.
        /// </summary>
        public T2DSceneObject NextEast
        {
            get
            {
                // find the first enabled component to the north (if there is one)
                if (_east == null) return null;

                GUIComponent comp = _east.Components.FindComponent<GUIComponent>();

                if (comp.Enabled)
                {
                    return _east;
                }
                else
                {
                    return comp.NextEast;
                }
            }
        }

        /// <summary>
        /// Warning: this can go into an infinite loop if the gui component chain loops and all the
        /// components are disabled.  To prevent this situation it is advised that the caller does not
        /// query this property if the component is disabled.
        /// </summary>
        public T2DSceneObject NextWest
        {
            get
            {
                // find the first enabled component to the north (if there is one)
                if (_west == null) return null;

                GUIComponent comp = _west.Components.FindComponent<GUIComponent>();

                if (comp.Enabled)
                {
                    return _west;
                }
                else
                {
                    return comp.NextWest;
                }
            }
        }

        public T2DSceneObject SceneObject
        {
            get { return _sceneObject; }
        }

        #endregion

        //======================================================
        #region Public Methods

        public virtual void OnClicked() { }
        public virtual void OnRightClicked() { }
        public virtual void OnMoveLeft() { }
        public virtual void OnMoveRight() { }
        public virtual void OnMoveUp() { }
        public virtual void OnMoveDown() { }
        public virtual int OnProcessButtons(ReadOnlyArray<MoveButton> buttons, int prevButton) { return -1; }    // atm this is only called for non-navigable components

        public virtual void OnGainedFocus() { }

        public virtual void OnLostFocus() { }

        public virtual bool HitTest(float x, float y)
        {
            //Debug.WriteLine("checking hit: " + x + "/" + y + " vs: " + _sceneObject.Position + " size: " + _sceneObject.Size);

            float w2 = _sceneObject.Size.X * 0.5f;
            float h2 = _sceneObject.Size.Y * 0.5f;

            // simple rectangle check - implement collision polygon or other hittest modes later if required
            if (x > _sceneObject.Position.X - w2 &&
                x < _sceneObject.Position.X + w2 &&
                y > _sceneObject.Position.Y - h2 &&
                y < _sceneObject.Position.Y + h2)
            {
                //Debug.WriteLine("HIT");
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            _sceneObject = owner as T2DSceneObject;

            if (_isNavigable)
            {
                GUIManager.Instance.AddNavigableGUIComponent(this);
            }
            else
            {
                GUIManager.Instance.AddNonNavigableGUIComponent(this);
            }

            if (_isDefault)
            {
                GUIManager.Instance.SetDefault(this);
            }

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();

            if (_isNavigable)
            {
                GUIManager.Instance.RemoveNavigableGUIComponent(this);
            }
            else
            {
                GUIManager.Instance.RemoveNonNavigableGUIComponent(this);
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DSceneObject _sceneObject;
        private bool _enabled = true;
        private bool _isNavigable;
        private bool _isDefault;
        private T2DSceneObject _north;
        private T2DSceneObject _south;
        private T2DSceneObject _east;
        private T2DSceneObject _west;

        #endregion
    }
}
