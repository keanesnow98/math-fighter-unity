using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Net;
using System.IO;
using Microsoft.Xna.Framework.Storage;
using System.Xml.Serialization;
using System.Diagnostics;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;



namespace MathFreak.Highscores
{
    /// <summary>
    /// Handles common highscore functionality, such as storing the data, load/save,
    /// and displaying the data.  Specific classes will derive from this to handle Local and P2P
    /// highscores.
    /// </summary>
    public class HighscoreData
    {
        private const int VERSION = 1001;
        private const int MAX_HIGHSCORES = 1000;   // we need to limit this to something that p2p can handle comfortably
        private const int TRIM_ALLOWANCE = 50; // allow to go over by this many when adding new scores so we aren't constantly resizing the list during p2p score sharing

        protected List<SinglePlayerScoreData> _singlePlayerScores;
        protected List<MultiplayerScoreData> _multiplayerScores;

        protected bool _hasBeenUpdated;

        public List<SinglePlayerScoreData> SinglePlayerScores
        {
            get { return _singlePlayerScores; }
        }

        public List<MultiplayerScoreData> MultiplayerScores
        {
            get { return _multiplayerScores; }
        }

        private static HighscoreData _instance;

        public static HighscoreData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HighscoreData();
                }

                return _instance;
            }
        }


        private HighscoreData()
        {
        }

        public void Load()
        {
            // attempt to load scores from disc
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String filename = Path.Combine(container.Path, "highscores.mfs");

                file = File.Open(filename, FileMode.Open);

                // deserialize
                PlayerScoreData data = new PlayerScoreData();
                XmlSerializer serializer = new XmlSerializer(typeof(PlayerScoreData));
                data = (PlayerScoreData)serializer.Deserialize(file);

                if (data.Version != VERSION)
                {
                    throw new InvalidDataException("highscores save file version does not match expected version number (" + data.Version + "vs" + VERSION + ")");
                }

                // convert to list form
                _singlePlayerScores = new List<SinglePlayerScoreData>(data.SinglePlayerScores);
                _multiplayerScores = new List<MultiplayerScoreData>(data.MultiplayerScores);

                // trim?
                if (_singlePlayerScores.Count > MAX_HIGHSCORES + TRIM_ALLOWANCE)
                {
                    _singlePlayerScores.RemoveRange(MAX_HIGHSCORES, _singlePlayerScores.Count - MAX_HIGHSCORES);
                }

                if (_multiplayerScores.Count > MAX_HIGHSCORES + TRIM_ALLOWANCE)
                {
                    _multiplayerScores.RemoveRange(MAX_HIGHSCORES, _multiplayerScores.Count - MAX_HIGHSCORES);
                }

                // get additional data
                HighscoresP2P.Instance.NextScoreIndexToUpload = data.NextScoreIndexToUpload;

                Debug.WriteLine("Successfully loaded highscores");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load highscores! - message: " + e.Message);

                // couldn't load from disc so create empty score data
                _singlePlayerScores = new List<SinglePlayerScoreData>(1000);
                _multiplayerScores = new List<MultiplayerScoreData>(1000);

                //// TESTING
                //_singlePlayerScores = new List<SinglePlayerScoreData>(10000);
                //_multiplayerScores = new List<MultiplayerScoreData>(10000);

                ////for (int i = 0; i < 10000; i++)
                ////{
                ////    _singlePlayerScores.Add(new SinglePlayerScoreData("Gamer" + i, i, Game.Instance.Rnd.Next(3)));
                ////    _multiplayerScores.Add(new MultiplayerScoreData("Gamer" + i, i, (uint)i));
                ////    _hasBeenUpdated = true;
                ////}

                //for (int i = 100000; i >= 0; i--)
                //{
                //    _singlePlayerScores.Add(new SinglePlayerScoreData("Gamer" + i, i, Game.Instance.Rnd.Next(3)));
                //    _multiplayerScores.Add(new MultiplayerScoreData("Gamer" + i, i, (uint)i));
                //    _hasBeenUpdated = true;
                //}
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }

                if (container != null)
                {
                    container.Dispose();
                }
            }
        }

        public void Save()
        {
            Save(false);
        }

        public void Save(bool forceSave)
        {
            // only save when there is something new to save or we are being command to save regardless
            if (!forceSave && !_hasBeenUpdated) return;

            _hasBeenUpdated = false;    // flag as having saved the data - if it turns out we can't save the data then we'll assume there is an issue with that and not try again until there is more new highscore data to save

            // save the highscores to disc
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String filename = Path.Combine(container.Path, "highscores.mfs");

                file = File.Open(filename, FileMode.Create);

                // convert to array form
                PlayerScoreData data = new PlayerScoreData();
                data.SinglePlayerScores = _singlePlayerScores.ToArray();
                data.MultiplayerScores = _multiplayerScores.ToArray();

                // add additional data
                data.NextScoreIndexToUpload = HighscoresP2P.Instance.NextScoreIndexToUpload;

                // serialize
                XmlSerializer serializer = new XmlSerializer(typeof(PlayerScoreData));
                serializer.Serialize(file, data);
                Debug.WriteLine("Successfully saved highscores");
            }
            catch (Exception e)
            {
                // failed to save the highscores - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save highscores! - message: " + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }

                if (container != null)
                {
                    container.Dispose();
                }
            }
        }

        public SinglePlayerScoreData GetSinglePlayerHighscore(string gamerTag)
        {
            int count = _singlePlayerScores.Count;

            for (int i = 0; i < count; i++)
            {
                if (_singlePlayerScores[i].GamerTag == gamerTag) return _singlePlayerScores[i];
            }

            return null;
        }

        public MultiplayerScoreData GetMultiplayerHighscore(string gamerTag)
        {
            int count = _multiplayerScores.Count;

            for (int i = 0; i < count; i++)
            {
                if (_multiplayerScores[i].GamerTag == gamerTag) return _multiplayerScores[i];
            }

            return null;
        }

        public void AddSinglePlayerHighscore(SinglePlayerScoreData data)
        {
            bool hasRemovedOldScore = false;
            bool hasAddedNewScore = false;

            for (int i = 0; i < _singlePlayerScores.Count && (!hasRemovedOldScore || !hasAddedNewScore); i++)
            {
                SinglePlayerScoreData score = _singlePlayerScores[i];

                if (!hasRemovedOldScore && score.GamerTag == data.GamerTag)
                {
                    // only remove the score if we have already added a better one
                    if (hasAddedNewScore)
                    {
                        hasRemovedOldScore = true;
                        _singlePlayerScores.RemoveAt(i);
                        i--;
                        continue;
                    }
                    // else the current score is actually better than the new one we've been given
                    // so don't change anything - we're done here
                    else
                    {
                        // check if the grade badge is better than we already have
                        if (data.GradeBadge > score.GradeBadge)
                        {
                            score.GradeBadge = data.GradeBadge;
                            _hasBeenUpdated = true;
                        }

                        // we're done here...
                        hasAddedNewScore = true;
                        break;
                    }
                }                

                if (!hasAddedNewScore)
                {
                    if (data.Rating > score.Rating)
                    {
                        hasAddedNewScore = true;
                        _singlePlayerScores.Insert(i, data);
                        _hasBeenUpdated = true;
                        continue;
                    }
                }
            }

            // if we didn't add the new score yet then add it to the end of the list
            if (!hasAddedNewScore)
            {
                _singlePlayerScores.Add(data);
                _hasBeenUpdated = true;
            }

            // trim the list if it's grown too big
            if (_singlePlayerScores.Count > MAX_HIGHSCORES + TRIM_ALLOWANCE)
            {
                _singlePlayerScores.RemoveRange(MAX_HIGHSCORES, _singlePlayerScores.Count - MAX_HIGHSCORES);
            }
        }

        public void AddMultiplayerHighscore(MultiplayerScoreData data)
        {
            bool hasRemovedOldScore = false;
            bool hasAddedNewScore = false;

            MultiplayerScoreData oldScore = GetMultiplayerHighscore(data.GamerTag);

            // if the data provided is not more recent that the stuff we already have then we're done already
            if (oldScore != null && oldScore.MatchesPlayed >= data.MatchesPlayed) return;

            // else we need to integrate the new score - add the new one and remove the old one
            for (int i = 0; i < _multiplayerScores.Count && (!hasRemovedOldScore || !hasAddedNewScore); i++)
            {
                MultiplayerScoreData score = _multiplayerScores[i];

                if (!hasAddedNewScore)
                {
                    if (data.Rating > score.Rating)
                    {
                        hasAddedNewScore = true;
                        _multiplayerScores.Insert(i, data);
                        _hasBeenUpdated = true;
                        continue;
                    }
                }

                if (!hasRemovedOldScore && score.GamerTag == data.GamerTag)
                {
                    hasRemovedOldScore = true;
                    _multiplayerScores.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            // if we didn't add the new score yet then add it to the end of the list
            if (!hasAddedNewScore)
            {
                _multiplayerScores.Add(data);
                _hasBeenUpdated = true;
            }

            // trim the list if it's grown too big
            if (_multiplayerScores.Count > MAX_HIGHSCORES + TRIM_ALLOWANCE)
            {
                _multiplayerScores.RemoveRange(MAX_HIGHSCORES, _multiplayerScores.Count - MAX_HIGHSCORES);
            }
        }

        public void IntegrateNewSinglePlayerScores(SinglePlayerScoreData[] newSinglePlayerScores, int start, int count)
        {
            // NOTE: count may go beyond the end of the array so we check for end of array condition
            int length = newSinglePlayerScores.Length;

            for (int i = start; i < start + count && i < length; i++)
            {
                AddSinglePlayerHighscore(newSinglePlayerScores[i]);
            }
        }

        public void IntegrateNewMultiplayerScores(MultiplayerScoreData[] newMultiplayerScores, int start, int count)
        {
            // NOTE: count may go beyond the end of the array so we check for end of array condition
            int length = newMultiplayerScores.Length;

            for (int i = start; i < start + count && i < length; i++)
            {
                AddMultiplayerHighscore(newMultiplayerScores[i]);
            }
        }

        public void UpdateGradeBadgeSprite(T2DStaticSprite sprite, string gamerTag)
        {
            int gradeBadge = 0;
            
            SinglePlayerScoreData score = GetSinglePlayerHighscore(gamerTag);

            if (score != null)
            {
                gradeBadge = score.GradeBadge;
            }

            if (gradeBadge != 0)
            {
                sprite.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("badgesMaterial");
                sprite.MaterialRegionIndex = gradeBadge - 1;    // material cells are zero indexed, but badges start at grade 1
                sprite.Visible = true;
            }
            else
            {
                // no grade badge so hide the sprite
                sprite.Visible = false;
            }
        }



        [Serializable]
        public class SinglePlayerScoreData
        {
            public string GamerTag;
            public int Rating;
            public int GradeBadge;


            public SinglePlayerScoreData() : this("???", 0, 0)
            {
            }

            public SinglePlayerScoreData(string gamerTag, int rating) : this(gamerTag, rating, 0)
            {
            }

            public SinglePlayerScoreData(string gamerTag, int rating, int gradeBadge)
            {
                GamerTag = gamerTag;
                Rating = rating;
                GradeBadge = gradeBadge;
            }

            public static void Write(PacketWriter writer, SinglePlayerScoreData data)
            {
                writer.Write(data.GamerTag);
                writer.Write(data.Rating);
                writer.Write(data.GradeBadge);
            }

            public static SinglePlayerScoreData Read(PacketReader reader)
            {
                SinglePlayerScoreData data = new SinglePlayerScoreData();

                data.GamerTag = reader.ReadString();
                data.Rating = reader.ReadInt32();
                data.GradeBadge = reader.ReadInt32();

                return data;
            }
        }

        
        
        [Serializable]
        public class MultiplayerScoreData
        {
            public string GamerTag;
            public int Rating;
            public uint MatchesPlayed;


            public MultiplayerScoreData() { }

            public MultiplayerScoreData(string gamerTag, int rating, uint matchesPlayed)
            {
                GamerTag = gamerTag;
                Rating = rating;
                MatchesPlayed = matchesPlayed;
            }

            public static void Write(PacketWriter writer, MultiplayerScoreData data)
            {
                writer.Write(data.GamerTag);
                writer.Write(data.Rating);
                writer.Write(data.MatchesPlayed);
            }

            public static MultiplayerScoreData Read(PacketReader reader)
            {
                MultiplayerScoreData data = new MultiplayerScoreData();

                data.GamerTag = reader.ReadString();
                data.Rating = reader.ReadInt32();
                data.MatchesPlayed = reader.ReadUInt32();

                return data;
            }
        }



        /// <summary>
        /// Xbox seems to require a single root object when
        /// serializing xml.  So this is our root object.
        /// Plus it's also sometimes useful to have a single
        /// data instance that can be passed around instead
        /// of passing pair of arrays.
        /// </summary>
        [Serializable]
        public class PlayerScoreData
        {
            // file load/save versioning incase patches ever needed later
            public int Version = VERSION;

            // used by the P2P uploading to record the last score data it sent so it can pick
            // up where it left off later (e.g. if the player quits after a few minutes and there
            // are thousands of scores to upload only the first few K will have been uploaded -
            // but saving this index means we can pickup where we left off the next time the
            // player runs the game)
            public int NextScoreIndexToUpload;

            // the meat of itz
            public SinglePlayerScoreData[] SinglePlayerScores;
            public MultiplayerScoreData[] MultiplayerScores;

            // These two counts are optional - some code will use it if the arrays might be
            // longer than the actual data, but at the moment it is just the P2P uploading
            // that needs to do this.  (NOTE: there is some 'mini' P2P in the settings lobby
            // when players exchange scores there)
            [XmlIgnore]
            public int SpCount;

            [XmlIgnore]
            public int MpCount;


            public PlayerScoreData Clone()
            {
                PlayerScoreData newData = new PlayerScoreData();

                newData.SpCount = SpCount;
                newData.MpCount = MpCount;
                newData.SinglePlayerScores = SinglePlayerScores.Clone() as SinglePlayerScoreData[];
                newData.MultiplayerScores = MultiplayerScores.Clone() as MultiplayerScoreData[];

                return newData;
            }
        }
    }
}
