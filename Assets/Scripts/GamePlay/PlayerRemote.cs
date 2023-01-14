using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// Handles the xbox live player specific stuff for the player representation
    /// </summary>
    public class PlayerRemote : Player
    {
        private NetworkGamer _gamer;

//#if XBOX
        public override Gamer GamerRef
        {
            get { return _gamer; }
        }
//#endif

        public override Texture2D GamerPic
        {
            get
            {
                try
                {
                    return _gamer.GetProfile().GamerPicture;
                }
                catch
                {   
                    // it's possible to get a gamer privilege acception here
                    return null;
                }
            }
        }

        public override string GamerTag
        {
            get { return _gamer.Gamertag; }
        }

        public PlayerRemote(NetworkGamer gamer)
        {
            _gamer = gamer;
        }

        public override bool IsValid
        {
            get
            {
                return true;
            }
        }

        public override PlayerCharacterSelector GetCharacterSelector(int playerNum)
        {
            return new PlayerRemoteCharacterSelector(playerNum);
        }
    }



    public class PlayerRemoteCharacterSelector : PlayerCharacterSelector
    {
        public PlayerRemoteCharacterSelector(int playerNum)
            : base(playerNum)
        {
        }

        public override void Tick(MathFreak.GameStates.GameStateCharacterSelectionLobby lobby, float dt)
        {
            // does nothing - the selection lobby handles sending/recieving character selection updates
        }
    }
}
