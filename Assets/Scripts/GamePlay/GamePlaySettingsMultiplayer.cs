using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework.Net;
using MathFreak.Highscores;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// Extends the basic gameplay settings to provide some extra stuff relevant to multiplayer.
    /// In particular it stores a list of players that are in the session.  This list is independent
    /// of the list of players actually playing a given match as MP LIVE can have people waiting to
    /// play.
    /// </summary>
    public class GamePlaySettingsMultiplayerLIVE : GamePlaySettings
    {
        private List<Player> _allPlayers;

        public List<Player> AllPlayers
        {
            get { return _allPlayers; }
        }

        private HighscoreData.MultiplayerScoreData[] _mpscoreData = new HighscoreData.MultiplayerScoreData[2];


        public GamePlaySettingsMultiplayerLIVE()
        {
            _allPlayers = new List<Player>(2);
            _mpscoreData[0] = new HighscoreData.MultiplayerScoreData();
            _mpscoreData[1] = new HighscoreData.MultiplayerScoreData();
        }

        public GamePlaySettingsMultiplayerLIVE(GamePlaySettingsMultiplayerLIVE settings)
            : base(settings)
        {
            _allPlayers = new List<Player>(2);
            _mpscoreData[0] = new HighscoreData.MultiplayerScoreData();
            _mpscoreData[1] = new HighscoreData.MultiplayerScoreData();
        }

        public override void AddPlayer(Player player)
        {
            // only add valid players
            if (!player.IsValid) return;

            // add the player to our big list
            _allPlayers.Add(player);

            // DON'T call the base class - we have separate methods to select players from the big list
            // for playing in the actual matches.
        }

        public override void RemovePlayer(Player player)
        {
            // remove from our big list
            bool result = _allPlayers.Remove(player);
            Assert.Fatal(result, "Failed to remove named player from the list of players we currently hold: " + player.GamerTag);

            // DON'T call the base class - we update the match players list via some specialist multiplayer
            // update stuff.
        }

        public override void RemoveAllPlayers()
        {
            base.RemoveAllPlayers();
            AllPlayers.Clear();
        }

        // will get the first two players from the list of all players and make them the players that will
        // be playing in a match when it is created.
        public void UpdateMatchPlayers()
        {
            _players.Clear();
            _players.Add(_allPlayers[0]);
            _players.Add(_allPlayers[1]);
            _mpscoreData[0].GamerTag = _players[0].GamerTag;
            _mpscoreData[1].GamerTag = _players[1].GamerTag;
            ResetScoreData();
        }

        // called to update the player queue after a match so that have a 'winner stays on' approach, where
        // if there are players waiting then the next one in the queue will play the winner.
        //
        // NOTE: the first two players in the queue are the ones who will play.  The winner
        // will not change their position in the queue (only the other two players will swap)
        public void UpdatePlayerQueueAfterMatch(Player winner, Player loser)
        {
            //// if there are less than 3 players in the session then no need to update the queue
            //if (_allPlayers.Count < 3) return;

            //// else leave the winner where they are and swap the other two
            //// ...temporarily remove the winner so there are only the other two remaining
            //int winnerIndx = _allPlayers.IndexOf(winner);
            //_allPlayers.RemoveAt(winnerIndx);

            //// ...swap the remaining two
            //Player p0 = _allPlayers[0];
            //Player p1 = _allPlayers[1];
            //_allPlayers[0] = p1;
            //_allPlayers[1] = p0;

            //// ...put the winner back at their old position
            //_allPlayers.Insert(winnerIndx, winner);
        }

        public void RemovePlayerByGamerTag(string gamerTag)
        {
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                if (_allPlayers[i].GamerTag == gamerTag)
                {
                    _allPlayers.RemoveAt(i);
                }
            }
        }

        public bool PlayerIsQueued(string gamerTag)
        {
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                if (_allPlayers[i].GamerTag == gamerTag)
                {
                    return true;
                }
            }

            return false;
        }

        public override void ResetScoreData()
        {
            _mpscoreData[0].Rating = 0;
            _mpscoreData[1].Rating = 0;
        }

        public override void AccumulateScoreData(int[] health)
        {
            _mpscoreData[0].MatchesPlayed++;
            _mpscoreData[0].Rating = (int)((float)_mpscoreData[0].Rating * (((float)_mpscoreData[0].MatchesPlayed - 1.0f) / (float)_mpscoreData[0].MatchesPlayed) + (float)CalculateRating(health[0]) * (1.0f / (float)_mpscoreData[0].MatchesPlayed));
            _mpscoreData[1].MatchesPlayed++;
            _mpscoreData[1].Rating = (int)((float)_mpscoreData[1].Rating * (((float)_mpscoreData[1].MatchesPlayed - 1.0f) / (float)_mpscoreData[1].MatchesPlayed) + (float)CalculateRating(health[1]) * (1.0f / (float)_mpscoreData[1].MatchesPlayed));
        }

        protected override void StoreScoreData(int player)
        {
            // we only update local player scores - we don't update scores for remote players; we
            // will get those scores via score sharing instead so that there is no corruption of
            // scores.
            if (_players[player] is PlayerLocal)
            {
                HighscoreData.Instance.AddMultiplayerHighscore(_mpscoreData[player]);
            }
        }
    }
}
