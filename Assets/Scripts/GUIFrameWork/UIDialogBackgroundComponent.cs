using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Materials;

namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// On registration this component hides it's owner and replaces it with 9 static sprites
    /// that form a dialog background.  The original object still exists so that any necessary
    /// interaction with the composite dialog background can be done.
    /// </summary>
    [TorqueXmlSchemaType]
    public class UIDialogBackgroundComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            SceneObject.Visible = false;

            _sprites = new T2DStaticSprite[9];
            T2DStaticSprite bgSprite;
            float x = SceneObject.Position.X;
            float y = SceneObject.Position.Y;
            float w = SceneObject.Size.X;
            float h = SceneObject.Size.Y;
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            // top left corner
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Top__Left_cornerMaterial");
            bgSprite.Position = new Vector2(x - w2 + (bgSprite.Size.X * 0.5f), y - h2 + (bgSprite.Size.Y * 0.5f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[0] = bgSprite;

            // top right corner
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Top__Right_CornerMaterial");
            bgSprite.Position = new Vector2(x + w2 - (bgSprite.Size.X * 0.5f), y - h2 + (bgSprite.Size.Y * 0.5f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[1] = bgSprite;

            // bottom left corner
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Bottom__left_cornerMaterial");
            bgSprite.Position = new Vector2(x - w2 + (bgSprite.Size.X * 0.5f), y + h2 - (bgSprite.Size.Y * 0.5f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[2] = bgSprite;

            // bottom right corner
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Bottom__Right_CornerMaterial");
            bgSprite.Position = new Vector2(x + w2 - (bgSprite.Size.X * 0.5f), y + h2 - (bgSprite.Size.Y * 0.5f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[3] = bgSprite;

            // left side
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Left_EdgeMaterial");
            bgSprite.Position = new Vector2(x - w2 + (bgSprite.Size.X * 0.5f), y);
            bgSprite.Size = new Vector2(bgSprite.Size.X, h - (bgSprite.Size.Y * 2.0f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[4] = bgSprite;

            // right side
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Right_EdgeMaterial");
            bgSprite.Position = new Vector2(x + w2 - (bgSprite.Size.X * 0.5f), y);
            bgSprite.Size = new Vector2(bgSprite.Size.X, h - (bgSprite.Size.Y * 2.0f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[5] = bgSprite;

            // top side
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Center__Top_EdgeMaterial");
            bgSprite.Position = new Vector2(x, y - h2 + (bgSprite.Size.Y * 0.5f));
            bgSprite.Size = new Vector2(w - (bgSprite.Size.X * 2.0f), bgSprite.Size.Y);
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[6] = bgSprite;

            // bottom side
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Center__Bottom_EdgeMaterial");
            bgSprite.Position = new Vector2(x, y + h2 - (bgSprite.Size.Y * 0.5f));
            bgSprite.Size = new Vector2(w - (bgSprite.Size.X * 2.0f), bgSprite.Size.Y);
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[7] = bgSprite;

            // center
            bgSprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("dialogbackgroundsprite");
            bgSprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("ui_Center_PieceMaterial");
            bgSprite.Position = new Vector2(x, y);
            bgSprite.Size = new Vector2(w - (bgSprite.Size.X * 2.0f), h - (bgSprite.Size.Y * 2.0f));
            bgSprite.Layer = SceneObject.Layer;
            TorqueObjectDatabase.Instance.Register(bgSprite);
            _sprites[8] = bgSprite;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DStaticSprite[] _sprites;

        #endregion
    }
}
