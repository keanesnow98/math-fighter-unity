using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using GarageGames.Torque.Core;
using System.IO;
using System.Diagnostics;

#if !XBOX
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using System.Xml.Serialization;
using MathFreak.Highscores;
using Microsoft.Xna.Framework.Net;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// Encapsulates the settings for a game - includes the players that will be in the game
    /// </summary>
    public class GamePlaySettings
    {
        private QuestionDealer _dealer;
        protected List<Player> _players;
        protected float _gradeMultipier;

        private bool _allowChallengers;
        private bool[] _mathGrades;
        private int _energyBar;
        private int _difficultyLevel;

        private bool _hasReplayed;  // true if player in championship mode has replayed a match they lost

        public enum EnumMathGradesSetting { Easy, Medium, Hard, All, Custom };
        private const int MATHGRADESETTINGS_COUNT = 5;
        private EnumMathGradesSetting _mathGradesSetting;

        private TutorManager.EnumTutor _location;   // this one is the one shown in the settings (can be None)
        private TutorManager.EnumTutor _locationToUse;  // this one is the actual one to use (cannot be None)

        private HighscoreData.SinglePlayerScoreData[] _spscoreData = new HighscoreData.SinglePlayerScoreData[2];

        public QuestionDealer Dealer
        {
            get { return _dealer; }
            set { _dealer = value; }
        }
        
        public List<Player> Players
        {
            get { return _players; }
        }

        public bool AllowChallengers
        {
            get { return _allowChallengers; }
            set { _allowChallengers = value; }
        }

        public bool HasReplayed
        {
            get { return _hasReplayed; }
            set { _hasReplayed = value; }
        }

        public int EnergyBar
        {
            get { return _energyBar; }

            set
            {
                _energyBar = value;

                if (_energyBar < 50)
                {
                    _energyBar = 50;
                }
                else if (_energyBar > 2000)
                {
                    _energyBar = 2000;
                }
            }
        }

        public int DifficultyLevel
        {
            get { return _difficultyLevel; }

            set
            {
                _difficultyLevel = value;

                if (_difficultyLevel < 0)
                {
                    _difficultyLevel = 0;
                }
                else if (_difficultyLevel > 2)
                {
                    _difficultyLevel = 2;
                }
            }
        }

        public TutorManager.EnumTutor Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public TutorManager.EnumTutor LocationToUse
        {
            get { return _locationToUse; }
            set { Assert.Fatal(value != TutorManager.EnumTutor.None, "LocationToUse should not be None"); _locationToUse = value; }
        }

        public EnumMathGradesSetting MathGradesSetting
        {
            get { return _mathGradesSetting; }
            set { _mathGradesSetting = value; }
        }

        public int PlayerScore
        {
            get { return _spscoreData[0].Rating; }
        }

        public float ScoreMultiplier
        {
            get { return _gradeMultipier * (float)(_difficultyLevel + 1) * 10.0f; } // so minimum multiplier is 10 and with minimum damage of 10 that makes the base score 100
        }

        
        public GamePlaySettings()
        {
            InitToDefaults();
        }

        public GamePlaySettings(GamePlaySettings settings)
        {
            if (settings != null)
            {
                _players = new List<Player>(settings.Players);
                AllowChallengers = settings.AllowChallengers;
                Dealer = settings.Dealer;
                DifficultyLevel = settings.DifficultyLevel;
                EnergyBar = settings.EnergyBar;
                _mathGrades = settings._mathGrades;
                Location = settings.Location;
                _locationToUse = settings._locationToUse;
                MathGradesSetting = settings.MathGradesSetting;
                _spscoreData[0] = settings._spscoreData[0];
                _spscoreData[1] = settings._spscoreData[1];
            }
            else
            {
                InitToDefaults();
            }
        }

        private void InitToDefaults()
        {
            _players = new List<Player>(2);
            _allowChallengers = true;
            _mathGrades = new bool[14];
            _energyBar = 100;
            _difficultyLevel = 1;
            _mathGradesSetting = EnumMathGradesSetting.Easy;
            _spscoreData[0] = new HighscoreData.SinglePlayerScoreData();
            _spscoreData[1] = new HighscoreData.SinglePlayerScoreData();
            _location = TutorManager.EnumTutor.None;
            _locationToUse = TutorManager.EnumTutor.Caveman;
        }

        public static NetworkSessionProperties GetInitialSessionCreationProperties()
        {
            NetworkSessionProperties properties = new NetworkSessionProperties();
            properties[(int)NetworkSessionManager.EnumSessionProperty.SessionType] = (int)NetworkSessionManager.EnumSessionType.Game;
            properties[(int)NetworkSessionManager.EnumSessionProperty.Grades] = Game.Instance.ActiveGameplaySettings.GetGradesAsInt();

            return properties;
        }

        // pattern of properties to match - null will match any value
        public static NetworkSessionProperties GetSessionPropertiesPattern(/* TODO: add parameters for the pattern */)
        {
            NetworkSessionProperties properties = new NetworkSessionProperties();
            properties[(int)NetworkSessionManager.EnumSessionProperty.SessionType] = (int)NetworkSessionManager.EnumSessionType.Game;

            return properties;
        }

        // returns a bit pattern representing the grades selected
        public int GetGradesAsInt()
        {
            int grades = 0;

            for (int grade = 1; grade < 14; grade++)
            {
                if (MathGradeIsEnabled(grade))
                {
                    grades += (1 << grade);
                }
            }

            return grades;
        }

        // checks to see if the specified grade is enabled in the packed grade integer (rather
        // than an internally stored list of grades.
        public static bool MathGradeIsEnabled(int grades, int grade)
        {
            return ((1 << grade) & grades) != 0;
        }

        public bool MathGradeIsEnabled(int grade)
        {
            return _mathGrades[grade];
        }

        public void EnableMathGrade(int grade, bool enabled)
        {
            _mathGrades[grade] = enabled;
        }

        public void EnableMathGrades(bool[] enabled)
        {
            int count = enabled.Length;

            Assert.Fatal(count == 14, "EnableMathGrades() - invalid count: " + count);

            for (int i = 0; i < count; i++)
            {
                _mathGrades[i] = enabled[i];
            }
        }

        public void EnableMathGrades(GamePlaySettings settings)
        {
            EnableMathGrades(settings._mathGrades);
        }

        public bool[] GetMathGrades()
        {
            return _mathGrades.Clone() as bool[];
        }

        public void CreateNewDealer()
        {
            _dealer = new QuestionDealer(_mathGrades);
        }

        public virtual void AddPlayer(Player player)
        {
            if (!_players.Contains(player))
            {
                Assert.Fatal(player.IsValid, "Trying to add a player to gamesettings, but the player is not valid");
                Assert.Fatal(_players.Count <= 2, "Trying to add a player to gamesettings, but all the player slots are already occupied");

                if (player.IsValid && _players.Count <= 2)
                {
                    _spscoreData[_players.Count].GamerTag = player.GamerTag;
                    _players.Add(player);
                }
            }
        }

        public virtual void RemovePlayer(Player player)
        {
            _players.Remove(player);
        }

        public virtual void RemoveAllPlayers()
        {
            _players.Clear();
        }

        public void Save(string filename)
        {
#if !XBOX
            // save the settings to disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, _allowChallengers);
                formatter.Serialize(stream, _difficultyLevel);
                formatter.Serialize(stream, _energyBar);
                formatter.Serialize(stream, _mathGradesSetting);
                formatter.Serialize(stream, _location);
                Debug.WriteLine("Successfully saved gameplay settings");
            }
            catch (Exception e)
            {
                // failed to save the options to disc - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save gameplay settings! - message: " + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
            }
            finally
            {
                if (null != stream)
                {
                    stream.Close();
                }
            }
#else
            // save the audio settings to the selected storage device
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String fullfilepath = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + filename);
                
                file = File.Open(fullfilepath, FileMode.Create);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveSettingsData));
                SaveSettingsData data;
                data.AllowChallengers = AllowChallengers;
                data.DifficultyLevel = DifficultyLevel;
                data.EnergyBar = EnergyBar;
                data.MathGradesSetting = MathGradesSetting;
                data.Location = Location;
                serializer.Serialize(file, data);
                Debug.WriteLine("Successfully saved gameplay settings");
            }
            catch (Exception e)
            {
                // failed to save the audio options to disc - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save gameplay settings! - message: " + e.Message);
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
#endif
        }

        public bool Load(string filename)
        {
#if !XBOX
            // attempt to load sound volume info from disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
                _allowChallengers = (bool)formatter.Deserialize(stream);
                _difficultyLevel = (int)formatter.Deserialize(stream);
                _energyBar = (int)formatter.Deserialize(stream);
                _mathGradesSetting = (EnumMathGradesSetting)formatter.Deserialize(stream);
                _location = (TutorManager.EnumTutor)formatter.Deserialize(stream);            
                Debug.WriteLine("Successfully loaded gameplay settings");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load gameplay settings  - message:" + e.Message);
                //                Debug.WriteLine("inner message: " + e.InnerException.Message);
                return false;
            }
            finally
            {
                if (null != stream)
                {
                    stream.Close();
                }
            }
#else
            // attempt to load sound volume info from the currently selected storage device
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String fullfilepath = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + filename);

                file = File.Open(fullfilepath, FileMode.Open, FileAccess.Read);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveSettingsData));
                SaveSettingsData data = (SaveSettingsData)serializer.Deserialize(file);
                AllowChallengers = data.AllowChallengers;
                DifficultyLevel = data.DifficultyLevel;
                EnergyBar = data.EnergyBar;
                MathGradesSetting = data.MathGradesSetting;
                Location = data.Location;
                Debug.WriteLine("Successfully loaded gameplay settings");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load gameplay settings  - message:" + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
                return false;
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
#endif

            return true;
        }

        public static void SaveMathGradeSelections(bool[][] mathgrades, string filename)
        {
#if !XBOX
            // save the settings to disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

                formatter.Serialize(stream, mathgrades.Length);

                for (int i = 0; i < mathgrades.Length; i++ )
                {
                    formatter.Serialize(stream, mathgrades[i].Length);

                    for (int j = 0; j < mathgrades[i].Length; j++)
                    {
                        formatter.Serialize(stream, mathgrades[i][j]);
                    }
                }

                Debug.WriteLine("Successfully saved custom grade selections");
            }
            catch (Exception e)
            {
                // failed to save the options to disc - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save custom grade selections! - message: " + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
            }
            finally
            {
                if (null != stream)
                {
                    stream.Close();
                }
            }
#else
            // save the audio settings to the selected storage device
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String fullfilepath = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + filename);

                file = File.Open(fullfilepath, FileMode.Create);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveSelectionData));
                SaveSelectionData data;
                data.MathGradeSelections = mathgrades;
                serializer.Serialize(file, data);
                Debug.WriteLine("Successfully saved custom grade selections");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to save custom grade selections! - message: " + e.Message);
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
#endif
        }

        public static bool[][] LoadMathGradeSelections(string filename)
        {
            bool[][] ret = null;

#if !XBOX
            // attempt to load sound volume info from disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);

                int numCustomSelections = (int)formatter.Deserialize(stream);
                ret = new bool[numCustomSelections][];

                for (int i = 0; i < numCustomSelections; i++ )
                {
                    int numGrades = (int)formatter.Deserialize(stream);
                    ret[i] = new bool[numGrades];

                    for (int j = 0; j < numGrades; j++)
                    {
                        ret[i][j] = (bool)formatter.Deserialize(stream);
                    }
                }

                Debug.WriteLine("Successfully loaded math grade selections");
            }
            catch (Exception e)
            {
                // couldn't load from disc so set the volumes to hardcoded values
                Debug.WriteLine("Failed to load math grade selections  - message:" + e.Message);
                //                Debug.WriteLine("inner message: " + e.InnerException.Message);
                ret = null;
            }
            finally
            {
                if (null != stream)
                {
                    stream.Close();
                }
            }
#else
            // attempt to load sound volume info from the currently selected storage device
            StorageContainer container = null;
            FileStream file = null;

            try
            {
                container = Game.Instance.XBOXStorageDevice.OpenContainer(Game.Instance.XBOXDataContainer);

                // Add the container path to our file name.
                String fullfilepath = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + filename);

                file = File.Open(fullfilepath, FileMode.Open, FileAccess.Read);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveSelectionData));
                SaveSelectionData data = (SaveSelectionData)serializer.Deserialize(file);
                ret = data.MathGradeSelections;
                Debug.WriteLine("Successfully loaded custom grade selections");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load custom grade selections  - message:" + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
                ret = null;
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
#endif

            return ret;
        }

        public void IncreaseMathGradeSetting()
        {
            int newGradeSetting = ((int)_mathGradesSetting) + 1;

            if (newGradeSetting >= MATHGRADESETTINGS_COUNT)
            {
                newGradeSetting = MATHGRADESETTINGS_COUNT - 1;
            }

            _mathGradesSetting = (EnumMathGradesSetting)newGradeSetting;
        }

        public void DecreaseMathGradeSetting()
        {
            int newGradeSetting = ((int)_mathGradesSetting) - 1;

            if (newGradeSetting < 0)
            {
                newGradeSetting = 0;
            }

            _mathGradesSetting = (EnumMathGradesSetting)newGradeSetting;
        }

        public void IncreaseLocation()
        {
            int newLocation = ((int)_location) + 1;

            if (newLocation >= TutorManager.TUTOR_COUNT)
            {
                newLocation = TutorManager.TUTOR_COUNT - 1;
            }

            _location = (TutorManager.EnumTutor)newLocation;
        }

        public void DecreaseLocation()
        {
            int newLocation = ((int)_location) - 1;

            if (newLocation < -1)
            {
                newLocation = -1;
            }

            _location = (TutorManager.EnumTutor)newLocation;
        }

        public virtual void ResetScoreData()
        {
            _spscoreData[0].Rating = 0;
            _spscoreData[1].Rating = 0;
            _spscoreData[0].GradeBadge = 0;
            _spscoreData[1].GradeBadge = 0;
        }

        public virtual void AccumulateScoreData(int[] health)
        {
            _spscoreData[0].Rating += CalculateRating(health[0]);
            _spscoreData[1].Rating += CalculateRating(health[1]);
        }

        // Uses the data stored in here in GameplaySettings to modify the score data.
        public virtual void AccumulateScoreData(int playerScore, int[] health)
        {
//            _spscoreData[0].Rating += CalculateRating(playerScore, health[0]);
            _spscoreData[0].Rating += playerScore;
            _spscoreData[1].Rating += CalculateRating(health[1]);
        }

        public int GetCurrentScore(int player)
        {
            return _spscoreData[player].Rating;
        }

        public void StoreScoreData()
        {
            if (!(_players[0] is PlayerLocalAI))
            {
                StoreScoreData(0);
            }

            if (!(_players[1] is PlayerLocalAI))
            {
                StoreScoreData(1);
            }
        }

        protected virtual void StoreScoreData(int player)
        {
            // find the lowest grade that was being played and award a grade badge for winning the championship at that grade or above.
            // but only if they haven't replayed a match they lost.  you gotta earn that badge! :)
            if (!_hasReplayed)
            {
                for (int i = 0; i < _mathGrades.Length; i++)
                {
                    if (_mathGrades[i])
                    {
                        _spscoreData[player].GradeBadge = i;
                        break;
                    }
                }

                Debug.WriteLine("Awarded grade badge: " + _spscoreData[player].GradeBadge);
            }

            HighscoreData.Instance.AddSinglePlayerHighscore(_spscoreData[player]);
        }

        // score is percentage health left multiplied by average grade, the difficulty level, and
        // finally by a scale value that makes the score satisfyingly big :)
        // NOTE: +1 on the difficulty level as it sis zero based.
        protected int CalculateRating(int health)
        {
            return (int)(((float)health / (float)_energyBar) * AvgGrade() * (float)(_difficultyLevel + 1) * 1000000.0f);
        }

        //protected int CalculateRating(int playerScore, int health)
        //{
        //    int score = playerScore + (int)(ScoreMultiplier * health);

        //    // perfect?
        //    if (health == _energyBar)
        //    {
        //        score += (int)(ScoreMultiplier * (float)_energyBar);
        //    }

        //    return score;
        //}

        protected float AvgGrade()
        {
            float totalGrades = 0.0f;
            int numGrades = 0;
            int count = _mathGrades.Length;

            for (int i = 0; i < count; i++)
            {
                if (_mathGrades[i])
                {
                    totalGrades += (float)i;
                    numGrades++;
                }
            }

            return totalGrades / (float)numGrades;
        }

        public void OnGameStarted()
        {
            _gradeMultipier = AvgGrade() * 0.5f + 0.5f; // (minimum multiplier of 1)
        }

        public void UpdateLocationToUse()
        {
            // random?
            if (_location == TutorManager.EnumTutor.None)
            {
                _locationToUse = TutorManager.Instance.GetRandomTutor();
            }
            // else just use the specified location
            else
            {
                _locationToUse = _location;
            }
        }



        [Serializable]
        public struct SaveSettingsData
        {
            public bool AllowChallengers;
            public int EnergyBar;
            public int DifficultyLevel;
            public EnumMathGradesSetting MathGradesSetting;
            public TutorManager.EnumTutor Location;
        }



        [Serializable]
        public struct SaveSelectionData
        {
            public bool[][] MathGradeSelections;
        }
    }
}
