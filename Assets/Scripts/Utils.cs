using Microsoft.Xna.Framework;

using GarageGames.Torque.Materials;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using MathFreak.GamePlay;
using GarageGames.Torque.T2D;



namespace MathFreak
{
    /// <summary>
    /// Facilitates creating a list of material references in a component in the scene editor
    /// </summary>
    public class XMLRenderMaterial
    {
        public RenderMaterial Material
        {
            get { return _material; }
            set { _material = value; }
        }

        RenderMaterial _material;
    }

    /// <summary>
    /// Facilitates creating a list of animation references in a component in the scene editor
    /// </summary>
    public class XMLAnimationData
    {
        public T2DAnimationData Animation
        {
            get { return _animation; }
            set { _animation = value; }
        }

        T2DAnimationData _animation;
    }

    public class Util
    {
        public static T StringToEnum<T>(string enumString)
        {
            return (T)Enum.Parse(typeof(T), enumString, false);
        }

        /// <summary>
        /// Finds the greatest common divisor (GCD)
        /// </summary>
        public static int FindGCD(int value1, int value2)
        {
            while (value1 > 0 && value2 > 0)
            {
                if (value1 > value2)
                {
                    value1 -= value2;
                }
                else
                {
                    value2 -= value1;
                }
            }

            return (value1 > 0 ? value1 : value2);
        }

        public static float PointToAngle(float x, float y)
        {
            float angle = 0.0f;

            if (x == 0.0f)
            {
                if (y < 0.0f)
                {
                    angle = 180.0f;
                }
                else if (y > 0.0f)
                {
                    angle = 0.0f;
                }
                else   // y == 0.0f
                {
                    angle = 0.0f;
                }
            }
            else if (y == 0.0f)
            {
                if (x < 0.0f)
                {
                    angle = 270.0f;
                }
                else // x > 0.0f
                {
                    angle = 90.0f;
                }
            }
            else
            {
                angle = MathHelper.ToDegrees((float)System.Math.Atan(x / y));

                if (y < 0.0f)
                {
                    angle += 180.0f;
                }
                else if (x < 0.0f)
                {
                    angle += 360.0f;
                }
            }

            return angle;
        }

        public static float SmallestDiffBetweenAngles(float angle1, float angle2)
        {
            float diff1;
            float diff2;

            if (angle1 > angle2)
            {
                diff1 = angle1 - angle2;
                diff2 = (360.0f - angle1) + angle2;
                return diff1 < diff2 ? -diff1 : diff2;
            }
            else
            {
                diff1 = angle2 - angle1;
                diff2 = (360.0f - angle2) + angle1;
                return diff1 < diff2 ? diff1 : -diff2;
            }
        }

        //private static int _renderTargetCount = 0;

        public static RenderTarget2D CreateRenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, int mipmaplevels, SurfaceFormat surfaceFormat)
        {
            //_renderTargetCount++;

            return new RenderTarget2D(graphicsDevice, width, height, mipmaplevels, SurfaceFormat.Color/*surfaceFormat*/);
        }
    }
}
