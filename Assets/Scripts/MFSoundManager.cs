using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;

#if !XBOX
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
#endif

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;

using GarageGames.Torque.GameUtil;
using GarageGames.Torque.XNA;
using MathFreak.AsyncTaskFramework;



namespace MathFreak
{
    /// <summary>
    /// Loads/saves audio settings.  Will play/stop music tracks on request.  And will play sfx.
    /// 
    /// NOTE: when sfx are requested to play it is only a request - some sfx have limitations applied
    /// such as a minimum time between playing the sfx and playing it again (these are defined in the sfx's
    /// info data).
    /// </summary>
    public class MFSoundManager
    {
        protected const string TASKLIST_SOUNDMANAGER = "soundmanager";

        public const string FEUI_MUSIC = "feui_music";
        public const string CAVEMAN_MUSIC = "cat2_music";
        public const string TEACHER_MUSIC = "cat1_music";
        public const string ROBOT_MUSIC = "cat3_music";
        public const string ATOMICKID_MUSIC = "cat4_music";
        public const string EINSTEIN_MUSIC = "cat5_music";
        public const string MATHLORD_MUSIC = "cat6_music";

        private static MFSoundManager _instance;
        private float _dt;
        private float _totalElapsedTime;

        private AudioCategory _soundCat;
        private AudioCategory _musicCat;

        public enum EnumSFX {
            ButtonHiglight = 0,
            MenuFalloff,
            MenuFlyon,
            ButtonPressed,
            DialogShown,
            DialogHidden,
            SelectionArrowMoved,
            VsScreen,
            TeleportIn,
            TeleportOut,
            StartButton,
            WrongBuzzard,
            QuestionAppears,
            Perfect,
            YouWin,
            YouLose,
            Freeze,
            Hint,
            Correct,
            KO,
            CavemanAttack,
            TeacherAttack,
            RobotAttack,
            AtomicKidAttack,
            EinsteinAttack,
            CavemanSuper,
            TeacherSuper,
            RobotSuper,
            AtomicKidSuper,
            EinsteinSuper,
            GetReady,
            CavemanLose,
            TeacherLose,
            RobotLose,
            AtomicKidLose,
            EinsteinLose,
            Lightening1,
            Lightening2,
            Lightening3,
            CavemanTaunt,
            TeacherTaunt,
            RobotTaunt,
            AtomicKidTaunt,
            EinsteinTaunt,
            TimerPart1,
            TimerPart2,
            TimerPart3,
            BossAttack,
            BossSuperAttack,
            BossLose,
            BossTimeRunningOut,
            BossTaunt,
            BossLaughter,
            NewChallenger,
            Silence,
        };

        private SfxInfo[] _sfxInfo = new SfxInfo[(int)EnumSFX.Silence];
        private List<SfxInfo> _sfxRequests = new List<SfxInfo>();

        private bool _playingMusic;
        private string _currentMusicTrack;

        private int _soundVolume;
        private int _musicVolume;

        public int SoundVolume
        {
            get { return _soundVolume; }

            set
            {
                _soundVolume = value;

                if (_soundVolume < 0)
                {
                    _soundVolume = 0;
                }
                else if (_soundVolume > 100)
                {
                    _soundVolume = 100;
                }

#if XBOX
                // xbox needs a volume boost
                _soundCat.SetVolume(7.0f * (float)_soundVolume / 100.0f);
#else
                _soundCat.SetVolume((float)_soundVolume / 100.0f);
#endif
            }
        }

        public int MusicVolume
        {
            get { return _musicVolume; }

            set
            {
                _musicVolume = value;

                if (_musicVolume < 0)
                {
                    _musicVolume = 0;
                }
                else if (_musicVolume > 100)
                {
                    _musicVolume = 100;
                }

#if XBOX
                // xbox needs a volume boost
                _musicCat.SetVolume(7.0f * (float)_musicVolume / 100.0f);
#else
                _musicCat.SetVolume((float)_musicVolume / 100.0f);
#endif
            }
        }

        public static MFSoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MFSoundManager();
                }

                return _instance;
            }
        }

        protected MFSoundManager()
        {
        }

        public void Init()
        {
            SoundManager.Instance.RegisterSoundGroup("Music", @"data\audio\music.xwb", @"data\audio\Sound Bank.xsb", true);
            SoundManager.Instance.RegisterSoundGroup("SFX", @"data\audio\sfx.xwb", @"data\audio\Sound Bank.xsb", false);
            _soundCat = TorqueEngineComponent.Instance.SFXDevice.GetCategory("Default");
            _musicCat = TorqueEngineComponent.Instance.SFXDevice.GetCategory("Music");

            // register info for sfx
            _sfxInfo[(int)EnumSFX.ButtonHiglight] = new SfxInfo("button_highlight", 0.0f);
            _sfxInfo[(int)EnumSFX.MenuFalloff] = new SfxInfo("buttons_fall_off", 0.0f);
            _sfxInfo[(int)EnumSFX.MenuFlyon] = new SfxInfo("buttons_fly_on", 0.0f);
            _sfxInfo[(int)EnumSFX.ButtonPressed] = new SfxInfo("press_button", 0.0f);
            _sfxInfo[(int)EnumSFX.DialogShown] = new SfxInfo("dialog_shown", 0.0f);
            _sfxInfo[(int)EnumSFX.DialogHidden] = new SfxInfo("dialog_hidden", 0.0f);
            _sfxInfo[(int)EnumSFX.SelectionArrowMoved] = new SfxInfo("char_select", 0.0f);
            _sfxInfo[(int)EnumSFX.VsScreen] = new SfxInfo("vs_screen", 0.0f);
            _sfxInfo[(int)EnumSFX.TeleportIn] = new SfxInfo("teleport_in", 0.0f);
            _sfxInfo[(int)EnumSFX.TeleportOut] = new SfxInfo("teleport_out", 0.0f);
            _sfxInfo[(int)EnumSFX.StartButton] = new SfxInfo("start_button", 0.0f);
            _sfxInfo[(int)EnumSFX.WrongBuzzard] = new SfxInfo("wrong_buzzard", 0.0f);
            _sfxInfo[(int)EnumSFX.QuestionAppears] = new SfxInfo("question_appears", 0.0f);
            _sfxInfo[(int)EnumSFX.Perfect] = new SfxInfo("perfect_voc_lo", 0.0f);
            _sfxInfo[(int)EnumSFX.YouWin] = new SfxInfo("you_win", 0.0f);
            _sfxInfo[(int)EnumSFX.YouLose] = new SfxInfo("you_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.Freeze] = new SfxInfo("character_freeze", 0.0f);
            _sfxInfo[(int)EnumSFX.Hint] = new SfxInfo("hint", 0.5f);
            _sfxInfo[(int)EnumSFX.Correct] = new SfxInfo("correct_stab", 0.0f);
            _sfxInfo[(int)EnumSFX.KO] = new SfxInfo("KO", 0.0f);
            _sfxInfo[(int)EnumSFX.CavemanAttack] = new SfxInfo("caveman_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.TeacherAttack] = new SfxInfo("woman_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.RobotAttack] = new SfxInfo("robot_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.AtomicKidAttack] = new SfxInfo("atomic kid_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.EinsteinAttack] = new SfxInfo("einstein_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.CavemanSuper] = new SfxInfo("caveman_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.TeacherSuper] = new SfxInfo("woman_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.RobotSuper] = new SfxInfo("robot_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.AtomicKidSuper] = new SfxInfo("atomic_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.EinsteinSuper] = new SfxInfo("einstein_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.GetReady] = new SfxInfo("get_ready", 0.0f);
            _sfxInfo[(int)EnumSFX.CavemanLose] = new SfxInfo("caveman_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.TeacherLose] = new SfxInfo("woman_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.RobotLose] = new SfxInfo("robot_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.AtomicKidLose] = new SfxInfo("atomic_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.EinsteinLose] = new SfxInfo("einstein_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.Lightening1] = new SfxInfo("attack_int_02", 0.0f);
            _sfxInfo[(int)EnumSFX.Lightening2] = new SfxInfo("attack_int_03", 0.0f);
            _sfxInfo[(int)EnumSFX.Lightening3] = new SfxInfo("attack_int_04", 0.0f);
            _sfxInfo[(int)EnumSFX.CavemanTaunt] = new SfxInfo("caveman_taunt", 0.0f);
            _sfxInfo[(int)EnumSFX.TeacherTaunt] = new SfxInfo("teacher_taunt", 0.0f);
            _sfxInfo[(int)EnumSFX.RobotTaunt] = new SfxInfo("robot_taunt", 0.0f);
            _sfxInfo[(int)EnumSFX.AtomicKidTaunt] = new SfxInfo("atomic_taunt", 0.0f);
            _sfxInfo[(int)EnumSFX.EinsteinTaunt] = new SfxInfo("einstein_taunt", 0.0f);
            _sfxInfo[(int)EnumSFX.TimerPart1] = new SfxInfo("timer_01", 0.0f);
            _sfxInfo[(int)EnumSFX.TimerPart2] = new SfxInfo("timer_02", 0.0f);
            _sfxInfo[(int)EnumSFX.TimerPart3] = new SfxInfo("timer_03", 0.0f);
            _sfxInfo[(int)EnumSFX.BossAttack] = new SfxInfo("lord_attack", 0.0f);
            _sfxInfo[(int)EnumSFX.BossSuperAttack] = new SfxInfo("lord_attack_max", 0.0f);
            _sfxInfo[(int)EnumSFX.BossLose] = new SfxInfo("lord_lose", 0.0f);
            _sfxInfo[(int)EnumSFX.BossTimeRunningOut] = new SfxInfo("lord_time", 0.0f);
            _sfxInfo[(int)EnumSFX.BossTaunt] = new SfxInfo("lord_weakling", 0.0f);
            _sfxInfo[(int)EnumSFX.BossLaughter] = new SfxInfo("lord_laughter", 0.0f);
            _sfxInfo[(int)EnumSFX.NewChallenger] = new SfxInfo("duntada", 0.0f);

            // create an async task list that we can use to run any task we need to do - e.g. fading in/out music volume or such like.
            AsyncTaskManager.Instance.NewTaskList(TASKLIST_SOUNDMANAGER);
        }

        protected void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_SOUNDMANAGER, startImmediately);
        }

        public void LoadSettings()
        {
#if !XBOX
            // attempt to load sound volume info from disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("audiooptions.mfs", FileMode.Open, FileAccess.Read, FileShare.None);
                SoundVolume = (int)formatter.Deserialize(stream);
                MusicVolume = (int)formatter.Deserialize(stream);

                Debug.WriteLine("Successfully loaded audio settings");
            }
            catch (Exception e)
            {
                // couldn't load from disc so set the volumes to hardcoded values
                Debug.WriteLine("Setting audio volumes using hardcoded value  - message:" + e.Message);
                //                Debug.WriteLine("inner message: " + e.InnerException.Message);
                SoundVolume = 100;
                MusicVolume = 100;
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
                String filename = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + "audiooptions.mfs");

                file = File.Open(filename, FileMode.Open);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                SaveData data = (SaveData)serializer.Deserialize(file);
                SoundVolume = data.SoundVolume;
                MusicVolume = data.MusicVolume;
                Debug.WriteLine("Successfully loaded audio settings");
            }
            catch (Exception e)
            {
                // couldn't load from disc so set the volumes using hardcoded values
                Debug.WriteLine("Setting audio volumes using hardcoded value  - message:" + e.Message);
                //Debug.WriteLine("inner message: " + e.InnerException.Message);
                SoundVolume = 100;
                MusicVolume = 100;
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

        public void SaveSettings()
        {
#if !XBOX
            // save the settings to disc
            Stream stream = null;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("audiooptions.mfs", FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, SoundVolume);
                formatter.Serialize(stream, MusicVolume);
                Debug.WriteLine("Successfully saved audio settings");
            }
            catch (Exception e)
            {
                // failed to save the options to disc - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save settings! - message: " + e.Message);
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
                String filename = Path.Combine(container.Path, Game.Instance.XBOXGamerFileString + "audiooptions.mfs");
                
                file = File.Open(filename, FileMode.Create);

                XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                SaveData data;
                data.SoundVolume = SoundVolume;
                data.MusicVolume = MusicVolume;
                serializer.Serialize(file, data);
                Debug.WriteLine("Successfully saved audio settings");
            }
            catch (Exception e)
            {
                // failed to save the audio options to disc - can display a message - for now we just let it fail silently
                Debug.WriteLine("Failed to save audio settings! - message: " + e.Message);
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

        public void PlayMusic(string trackName)
        {
            // if playing a track but it's a different one then stop that track
            if (_playingMusic && trackName != _currentMusicTrack)
            {
                Debug.WriteLine("Going to play a new music track");
                StopMusic();
            }

            // if not playing anything then play the requested track
            if (!_playingMusic)
            {
                Debug.WriteLine("Starting playing a new music track");
                _playingMusic = true;
                _currentMusicTrack = trackName;
                SoundManager.Instance.PlaySound("Music", trackName);
            }

            // else just ignore this request - we are already playing the requested track
        }

        public void StopMusic()
        {
            if (_playingMusic)
            {
                Debug.WriteLine("Stopping Music");
                _playingMusic = false;
                _currentMusicTrack = null;
                TorqueEngineComponent.Instance.SFXDevice.GetCategory("Music").Stop(AudioStopOptions.Immediate);
            }
        }

        /// <summary>
        /// The sfx will play the next time the soundmanager ticks - if the parameters allow it
        /// (e.g. some sfx will have minimum time delay between playing instances of the sfx so
        /// as to avoid sounding bad)
        /// </summary>
        /// <param name="sfx"></param>
        public void PlaySFX(EnumSFX sfx)
        {
            if (sfx == EnumSFX.Silence) return;

            _sfxInfo[(int)sfx].Requested = true;
            _sfxRequests.Add(_sfxInfo[(int)sfx]);
        }

        /// <summary>
        /// Will play the sfx immediately and without heed to any constraint parameters that would
        /// be in effect if using PlaySFX()
        /// </summary>
        /// <param name="sfx"></param>
        public void PlaySFXImmediately(EnumSFX sfx)
        {
            if (sfx == EnumSFX.Silence) return;

            SfxInfo sfxToPlay = _sfxInfo[(int)sfx];

            if (sfxToPlay.Cue != null && sfxToPlay.Cue.IsPaused)
            {
                sfxToPlay.Cue.Stop(AudioStopOptions.Immediate);
            }

            sfxToPlay.TimeOfPrevPlay = _totalElapsedTime;
            sfxToPlay.Cue = SoundManager.Instance.PlaySound("SFX", sfxToPlay.Name);
        }

        /// <summary>
        /// Pauses the named sfx (the most recent instance of it that is)
        /// </summary>
        /// <param name="sfx"></param>
        public void PauseSFX(EnumSFX sfx)
        {
            if (sfx == EnumSFX.Silence) return;

            SfxInfo sfxToPause = _sfxInfo[(int)sfx];

            if (sfxToPause.Cue != null && sfxToPause.Cue.IsPlaying)
            {
                sfxToPause.Cue.Pause();
            }
        }

        /// <summary>
        /// Resumes a paused sfx
        /// </summary>
        /// <param name="sfx"></param>
        public void ResumeSFX(EnumSFX sfx)
        {
            if (sfx == EnumSFX.Silence) return;

            SfxInfo sfxToResume = _sfxInfo[(int)sfx];

            if (sfxToResume.Cue != null && sfxToResume.Cue.IsPaused)
            {
                sfxToResume.Cue.Resume();
            }
        }

        public void StopSFX(EnumSFX sfx)
        {
            if (sfx == EnumSFX.Silence) return;

            StopSFX(_sfxInfo[(int)sfx]);
        }

        private void StopSFX(SfxInfo sfx)
        {
            if (sfx.Cue != null && sfx.Cue.IsPlaying)
            {
                sfx.Cue.Stop(AudioStopOptions.Immediate);
                sfx.Cue = null;
            }

            sfx.Requested = false;
        }

        public void StopAllSFX()
        {
            foreach (SfxInfo sfx in _sfxInfo)
            {
                StopSFX(sfx);
            }
        }

        public void Tick(float dt)
        {
            _totalElapsedTime += dt;
            _dt = dt;

            // tick any async tasks
            AsyncTaskManager.Instance.Tick(TASKLIST_SOUNDMANAGER);

            // process any sfx requests
            foreach (SfxInfo sfx in _sfxRequests)
            {
                if (sfx.Requested)
                {
                    sfx.Requested = false;

                    if (_totalElapsedTime - sfx.TimeOfPrevPlay >= sfx.MinTimeBetweenPlaying)
                    {
                        if (sfx.Cue != null && sfx.Cue.IsPaused)
                        {
                            StopSFX(sfx);
                        }

                        sfx.TimeOfPrevPlay = _totalElapsedTime;
                        sfx.Cue = SoundManager.Instance.PlaySound("SFX", sfx.Name);
                    }
                }
            }

            _sfxRequests.Clear();
        }



        /// <summary>
        /// Each sfx can have some information associated with it about how/when to play it
        /// </summary>
        private class SfxInfo
        {
            public string Name;

            /// <summary>
            /// The minimum time that must elapse (in seconds) before this sfx can be played again.
            /// </summary>
            public float MinTimeBetweenPlaying;

            /// <summary>
            /// Set to true if this sfx is being requested to play
            /// </summary>
            public bool Requested = false;

            public float TimeOfPrevPlay;
            public Cue Cue;


            public SfxInfo(string name, float minTimeBetweenPlaying)
            {
                Name = name;
                MinTimeBetweenPlaying = minTimeBetweenPlaying;
            }
        }



        [Serializable]
        public struct SaveData
        {
            public int SoundVolume;
            public int MusicVolume;
        }
    }
}
