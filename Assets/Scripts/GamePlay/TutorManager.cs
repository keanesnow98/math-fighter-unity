using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using MathFreak.AsyncTaskFramework;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Materials;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using MathFreak.vfx;

namespace MathFreak.GamePlay
{
    // Manages the tutors - mainly art stuff like gamerpics and animations.
    //
    // NOTE: the animation handling has been extended to support stuff beyond just using T2DAnimatedSprite,
    // so that we can use static image+shader or whatever is required.
    public class TutorManager
    {
        private static TutorManager _instance;

        public static TutorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TutorManager();
                }

                return _instance;
            }
        }

        private TutorManager()
        {
            // NOTE: two sets of tutor instances as we will need one for each player (root reason: we need to be able to kill async task on a character, but only for a given player - otherwise if both players have the same character it will kill the task regardless of the player and thus mess up the anims)
            _tutors[0, (int)EnumTutor.Einstein] = new EinsteinTutor();
            _tutors[0, (int)EnumTutor.Caveman] = new CavemanTutor();
            _tutors[0, (int)EnumTutor.SchoolTeacher] = new SchoolTeacherTutor();
            _tutors[0, (int)EnumTutor.Robot] = new RobotTutor();
            _tutors[0, (int)EnumTutor.AtomicKid] = new AtomicKidTutor();
            _tutors[0, (int)EnumTutor.MathLord] = new MathLordTutor();

            _tutors[1, (int)EnumTutor.Einstein] = new EinsteinTutor();
            _tutors[1, (int)EnumTutor.Caveman] = new CavemanTutor();
            _tutors[1, (int)EnumTutor.SchoolTeacher] = new SchoolTeacherTutor();
            _tutors[1, (int)EnumTutor.Robot] = new RobotTutor();
            _tutors[1, (int)EnumTutor.AtomicKid] = new AtomicKidTutor();
            _tutors[1, (int)EnumTutor.MathLord] = new MathLordTutor();

            RumbleShakeFX = new RumbleShake();
        }

        /// <summary>
        /// Because the tutors can play their animations on scenes that may be overlayed by other
        /// scenes it is important that they grab references to their animations before other
        /// scenes are overlayed.  Otherwise due to some lovely Torque X engine crappiness, they
        /// can end up playing animationdata and material instances that are owned by a scene that
        /// may be unloaded while they are using the material/animationdata.
        /// 
        /// This method should be called at the start of any scene using tutors so that tutors
        /// have the right animationdata and material references.
        /// </summary>
        public void CacheAnims()
        {
            for (int player = 0; player < 2; player++)
            {
                for (int i = 0; i < TUTOR_COUNT; i++)
                {
                    _tutors[player, i].CacheAnims();
                }
            }
        }

        /// <summary>
        /// Call this if and when we need to release cached anims - this is for when we quit
        /// the main gameplay screen; we need to release the anims that were loaded especially
        /// for the gameplay.  On non-gameplay screens it doesn't matter as any anims cached
        /// are shared ones anyway so will not cause exceptions to be thrown.
        /// </summary>
        public void ReleaseCachedAnims()
        {
            for (int player = 0; player < 2; player++)
            {
                for (int i = 0; i < TUTOR_COUNT; i++)
                {
                    _tutors[player, i].ReleaseCachedAnims();
                }
            }
        }
    
        public IEnumerator<AsyncTaskStatus> PlayAnim(int player, EnumTutor tutor, Tutor.EnumAnim anim, T2DSceneObject sprite)
        {
            IEnumerator<AsyncTaskStatus> task = _tutors[player, (int)tutor].PlayAnim(anim, sprite);
            _tutors[player, (int)tutor].OnNewAsyncAnimTask(task);
            return task;
        }

        public IEnumerator<AsyncTaskStatus> PlayNormalAttackAnim(int player, EnumTutor tutor, T2DAnimatedSprite tutorSprite, T2DAnimatedSprite damageSprite, TutorAttackAnimStatus status)
        {
            IEnumerator<AsyncTaskStatus> task = _tutors[player, (int)tutor].PlayNormalAttackAnim(tutorSprite, damageSprite, status);
            _tutors[player, (int)tutor].OnNewAsyncAnimTask(task);
            return task;
        }

        public IEnumerator<AsyncTaskStatus> PlaySuperAttackAnim(int player, EnumTutor tutor, T2DAnimatedSprite tutorSprite, T2DAnimatedSprite damageSprite, T2DAnimatedSprite[] ghostSprites, float power, string ghostTaskList, TutorAttackAnimStatus status)
        {
            IEnumerator<AsyncTaskStatus> task = _tutors[player, (int)tutor].PlaySuperAttackAnim(tutorSprite, damageSprite, ghostSprites, power, ghostTaskList, status);
            _tutors[player, (int)tutor].OnNewAsyncAnimTask(task);
            return task;
        }

        public static EnumTutor GetTutorAfter(EnumTutor tutor)
        {
            tutor++;

            if ((int)tutor >= TUTOR_COUNT - 1)  // NOTE: last tutor is the boss character and not available to select unless a special code is entered
            {
                tutor = (EnumTutor)0;
            }

            return tutor;
        }

        public static EnumTutor GetTutorBefore(EnumTutor tutor)
        {
            tutor--;

            if ((int)tutor < 0)
            {
                tutor = (EnumTutor)(TUTOR_COUNT - 2);   // NOTE: last tutor is the boss character and not selectable unless a special code is entered
            }

            return tutor;
        }

        public SimpleMaterial GetVsSprite(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetVsSprite(tutor);
        }

        public SimpleMaterial GetGamerPic(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetGamerPic();
        }

        public string GetGamerTag(int player, EnumTutor tutor)
        {
            if (tutor != EnumTutor.None)
            {
                return _tutors[player, (int)tutor].GetGamerTag();
            }
            else
            {
                return "???";
            }
        }

        public string GetLocationName(int player, EnumTutor tutor)
        {
            if (tutor != EnumTutor.None)
            {
                return _tutors[player, (int)tutor].GetLocationName();
            }
            else
            {
                return "???";
            }
        }

        public SimpleMaterial GetTeleportPic(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetTeleportPic();
        }

        public SimpleMaterial GetBG(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetBG();
        }

        public SimpleMaterial GetCupEmboss(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetCupEmboss();
        }

        public string GetAssetPath(int player, EnumTutor tutor)
        {
            return _tutors[player, (int)tutor].GetAssetPath();
        }

        public void PlayMusic(int player, EnumTutor tutor)
        {
            MFSoundManager.Instance.PlayMusic(_tutors[player, (int)tutor].GetMusicTrack());
        }

        public void PlayTauntSFX(int player, EnumTutor tutor)
        {
            MFSoundManager.Instance.PlaySFX(_tutors[player, (int)tutor].GetTauntSFX());
        }

        public void PlayTimeRunningOutSFX(int player, EnumTutor tutor)
        {
            MFSoundManager.Instance.PlaySFX(_tutors[player, (int)tutor].GetTimeRunningOutSFX());
        }

        public bool IsAnimationPlaying(int player, EnumTutor tutor, Tutor.EnumAnim anim, T2DAnimatedSprite sprite)
        {
            return _tutors[player, (int)tutor].IsAnimationPlaying(anim, sprite);
        }

        public T2DAnimationData GetAnimationData(int player, EnumTutor tutor, Tutor.EnumAnim anim)
        {
            return _tutors[player, (int)tutor].GetAnimationData(anim);
        }

        public EnumTutor GetRandomTutor()
        {
            return (TutorManager.EnumTutor)Game.Instance.Rnd.Next(0, TUTOR_COUNT);
        }

        ////////////////////////////////////////
        // Tutor Data
        ////////////////////////////////////////

        public enum EnumTutor { None = -1, Einstein = 0, Caveman = 1, SchoolTeacher = 2, Robot = 3, AtomicKid = 4, MathLord = 5 };    // yep - explicitly setting the enum values so we can use them as indexes into arrays - could use constant ints, but enums show up as named when debugging (and also in the scene editor), so more useful.
        public const int TUTOR_COUNT = 6;
        private Tutor[,] _tutors = new Tutor[2, TUTOR_COUNT];
        public RumbleShake RumbleShakeFX;



        /// <summary>
        /// Base class for tutors - can handle single shot animations automatically, but for sequences of
        /// animations derived classes should handle it themselves.
        /// </summary>
        public class Tutor
        {
            public enum EnumAnim { Idle = 0, Damaged = 1, Win = 2, Lose = 3, Impatient = 4, TimeRunningOut = 5, Selected = 6, Deselected = 7, SuperAttack = 8, VsScreen = 9, NormalAttack = 10, NormalDamage = 11, SuperDamage = 12, GamerPic = 13, Taunt = 14, Freeze = 15, Unfreeze = 16, BG = 17, CupEmboss = 18 };
            private const int ANIM_COUNT = 19;
            protected string[] _animNames = new string[ANIM_COUNT];
            protected Object[] _animData = new Object[ANIM_COUNT];
            protected string _musicTrack;
            protected MFSoundManager.EnumSFX _tauntSFX;
            protected MFSoundManager.EnumSFX _timeRunningOutSFX = MFSoundManager.EnumSFX.Silence;
            protected bool _flipNormalAttackDamageAnim;
            protected bool _flipSuperAttackDamageAnim;
            protected MFSoundManager.EnumSFX _normalAttackSFX;
            protected MFSoundManager.EnumSFX _superAttackSFX;
            protected MFSoundManager.EnumSFX _loseSFX;

            protected IEnumerator<AsyncTaskStatus> _asyncAnimTask;  // tracks currently active animation task if there is one so that we can kill early if we need to


            public virtual void CacheAnims()
            {
                for (int i = 0; i < ANIM_COUNT; i++)
                {
                    _animData[i] = TorqueObjectDatabase.Instance.FindObject<Object>(_animNames[i]);
                    //Assert.Warn(_animData[i] != null, "Could not find asset for: " + (EnumAnim)i + " (looking for: " + _animNames[i] + ")");
                }
            }

            public virtual void ReleaseCachedAnims()
            {
                for (int i = 0; i < ANIM_COUNT; i++)
                {
                    _animData[i] = null;
                }
            }

            public void OnNewAsyncAnimTask(IEnumerator<AsyncTaskStatus> task)
            {
                // if an async anim task is already running then kill it (actually it might have stopped by now, but no problem; if it's not in the tasklist then the kill call will be ignored anyway)
                if (_asyncAnimTask != null)
                {
                    AsyncTaskManager.Instance.KillTask(_asyncAnimTask);
                }

                // reference the new task
                _asyncAnimTask = task;
            }

            public bool IsAnimationPlaying(EnumAnim anim, T2DAnimatedSprite sprite)
            {
                return (sprite.IsAnimationPlaying && sprite.AnimationData == (_animData[(int)anim] as T2DAnimationData));
            }

            public T2DAnimationData GetAnimationData(EnumAnim anim)
            {
                return (_animData[(int)anim] as T2DAnimationData);
            }

            public IEnumerator<AsyncTaskStatus> PlayAnim(EnumAnim anim, T2DSceneObject sprite)
            {
                return AsyncTask_PlayAnim(anim, sprite);
            }

            protected virtual IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(EnumAnim anim, T2DSceneObject sprite)
            {
                switch (anim)
                {
                    case EnumAnim.Selected:
                        //Debug.WriteLine("sprite: " + sprite.Name);
                        //Debug.WriteLine("material: " + (_animData[(int)EnumAnim.Selected] as SimpleMaterial).TextureFilename);
                        //(sprite as T2DStaticSprite).Material = _animData[(int)EnumAnim.Selected] as SimpleMaterial;
                        //sprite.Visible = true;
                        sprite.Visible = true;
                        PlayAnim(_animData[(int)anim] as T2DAnimationData, sprite as T2DAnimatedSprite);
                        yield break;

                    case EnumAnim.Deselected:
                        sprite.Visible = false;
                        yield break;

                    case EnumAnim.Lose:
                        MFSoundManager.Instance.PlaySFX(_loseSFX);
                        PlayAnim(_animData[(int)anim] as T2DAnimationData, sprite as T2DAnimatedSprite);
                        yield break;

                    default:
                        PlayAnim(_animData[(int)anim] as T2DAnimationData, sprite as T2DAnimatedSprite);
                        yield break;
                }
            }

            protected bool PlayAnim(T2DAnimationData animData, T2DAnimatedSprite sprite)
            {
                return PlayAnim(animData, sprite, 0);
            }

            protected bool PlayAnim(T2DAnimationData animData, T2DAnimatedSprite sprite, int delay)
            {
                if (animData != null)
                {
                    //// different animations may be different sizes
                    //Texture2D texture = ((animData.Material as SimpleMaterial).Texture.Instance) as Texture2D;
                    //sprite.Size = new Vector2(animData.AnimationFramesList[0].Width * texture.Width, animData.AnimationFramesList[0].Height * texture.Height);

                    // play it
                    sprite.PlayAnimation(animData, delay);

                    // let the caller know it's playing
                    return true;
                }
                else
                {
                    return false;
                }
            }

            protected IEnumerator<AsyncTaskStatus> AsyncTask_PlayMultiPartAnim(T2DAnimatedSprite sprite, T2DAnimationData[] animData)
            {
                T2DAnimatedSprite animSprite = sprite as T2DAnimatedSprite;

                // play the first n-1 anims
                int i;

                for (i = 0; i < animData.Length - 1; i++)
                {
                    // play ith anim
                    PlayAnim(animData[i], animSprite);

                    // wait for ith anim to finish
                    while (!animSprite.IsAnimationPlaying) yield return null;
                    while (animSprite.IsAnimationPlaying) yield return null;
                }

                // play the nth anim
                PlayAnim(animData[i], animSprite);
            }

            public virtual IEnumerator<AsyncTaskStatus> PlayNormalAttackAnim(T2DAnimatedSprite tutorSprite, T2DAnimatedSprite damageSprite, TutorAttackAnimStatus status)
            {
                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Playing;

                // trigger sfx
                MFSoundManager.Instance.PlaySFX(_normalAttackSFX);

                // play the tutor's attack anim
                T2DAnimationData attackAnimData = _animData[(int)EnumAnim.NormalAttack] as T2DAnimationData;                

                if (attackAnimData == null)
                {
                    Assert.Fatal(false, "No animation data found for tutor's attack anim");
                    status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;
                    yield break;
                }

                int attackAnimFrame = -1;
                tutorSprite.OnFrameChange = new OnFrameChangeDelegate(delegate(int frame)
                {
                    attackAnimFrame = frame;
                });

                tutorSprite.PlayAnimation(attackAnimData, false);

                // at the critical frame in the attack anim we will launch the damage anim
                int criticalFrame = GetCriticalNormalAttackAnimFrame();
                while (attackAnimFrame < criticalFrame) yield return null;

                T2DAnimationData damageAnimData = _animData[(int)EnumAnim.NormalDamage] as T2DAnimationData;

                if (damageAnimData == null)
                {
                    Assert.Fatal(false, "No animation data found for tutor's damage dealing anim");
                    status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;
                    yield break;
                }

                int damageAnimFrame = -1;
                damageSprite.OnFrameChange = new OnFrameChangeDelegate(delegate(int frame)
                {
                    damageAnimFrame = frame;
                });

                damageSprite.Visible = true;
                damageSprite.Layer = tutorSprite.Layer - 1;
                damageSprite.FlipX = !tutorSprite.FlipX ? !_flipNormalAttackDamageAnim : false;   // if the tutor who is attacking is not flipped then the other one must be flipped - so we need to undo the flip (or not) depending on the value of our flipping property - make sense? :)
                damageSprite.PlayAnimation(damageAnimData, false);

                // when the damage dealing anim reaches the critical frame we will update the status to tell the caller that they
                // should play the opponent's damaged anim
                criticalFrame = GetCriticalNormalAttackDamageAnimFrame();
                while (damageAnimFrame < criticalFrame) yield return null;

                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.ReachedDamageDealingFrame;
                yield return null;

                // wait until both anims have finished and report completed
                while (tutorSprite.IsAnimationPlaying || damageSprite.IsAnimationPlaying) yield return null;

                damageSprite.Visible = false;
                damageSprite.FlipX = !tutorSprite.FlipX;    // damage sprite needs flipping to the opposite of the attacking tutor so that it is reset to what it was originally
                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;
            }

            protected virtual int GetCriticalNormalAttackAnimFrame()
            {
                return 0;
            }

            protected virtual int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            public virtual IEnumerator<AsyncTaskStatus> PlaySuperAttackAnim(T2DAnimatedSprite tutorSprite, T2DAnimatedSprite damageSprite, T2DAnimatedSprite[] ghostSprites, float power, string ghostTaskList, TutorAttackAnimStatus status)
            {
                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Playing;

                // trigger sfx
                MFSoundManager.Instance.PlaySFX(_superAttackSFX);

                // play the tutor's attack anim
                T2DAnimationData attackAnimData = _animData[(int)EnumAnim.SuperAttack] as T2DAnimationData;

                if (attackAnimData == null)
                {
                    Assert.Fatal(false, "No animation data found for tutor's super attack anim");
                    status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;
                    yield break;
                }

                int attackAnimFrame = -1;
                tutorSprite.OnFrameChange = new OnFrameChangeDelegate(delegate(int frame)
                {
                    attackAnimFrame = frame;
                });

                tutorSprite.PlayAnimation(attackAnimData, false);

                // trigger the ghost 'trail' fx if enough power level
                int numGhostsInTrail = 0;

                if ((int)power >= 5)
                {
                    numGhostsInTrail = (((int)power - 4) * GameStateGameplay.SUPERTRAILFX_GHOSTSPERSEGMENT) + GameStateGameplay.SUPERTRAILFX_GHOSTSPERSEGMENT;

                    (attackAnimData.Material as SimpleMaterial).IsTranslucent = true;
                    (attackAnimData.Material as SimpleMaterial).IsColorBlended = true;

                    for (int i = 0; i < numGhostsInTrail; i++)
                    {
                        ghostSprites[i].Visible = true;
                        ghostSprites[i].VisibilityLevel = ((0.5f / (float)(numGhostsInTrail + 1)) * (float)i) + 0.2f;
                        PlayAnim(attackAnimData, ghostSprites[i], numGhostsInTrail - i);
                    }
                }

                // at the critical frame in the attack anim we will launch the damage anim
                int criticalFrame = GetCriticalSuperAttackAnimFrame();
                while (attackAnimFrame < criticalFrame) yield return null;

                T2DAnimationData damageAnimData = _animData[(int)EnumAnim.SuperDamage] as T2DAnimationData;

                if (damageAnimData == null)
                {
                    Assert.Fatal(false, "No animation data found for tutor's super damage dealing anim");
                    status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;
                    yield break;
                }

                int damageAnimFrame = -1;
                damageSprite.OnFrameChange = new OnFrameChangeDelegate(delegate(int frame)
                {
                    damageAnimFrame = frame;
                });

                damageSprite.Visible = true;
                damageSprite.Layer = tutorSprite.Layer - 1;
                damageSprite.FlipX = !tutorSprite.FlipX ? !_flipSuperAttackDamageAnim : false;   // if the tutor who is attacking is not flipped then the other one must be flipped - so we need to undo the flip (or not) depending on the value of our flipping property - make sense? :)
                damageSprite.PlayAnimation(damageAnimData, false);

                // do the caveman's shake and rumble fx
                TutorManager.Instance.RumbleShakeFX.Start(0.3f, 0.0f, 15.0f, 5.0f, 5.0f, ghostTaskList);

                // when the damage dealing anim reaches the critical frame we will update the status to tell the caller that they
                // should play the oponent's damaged anim
                criticalFrame = GetCriticalSuperAttackDamageAnimFrame();
                while (damageAnimFrame < criticalFrame) yield return null;

                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.ReachedDamageDealingFrame;
                yield return null;

                // wait until the attack and damage anims have finished
                while (tutorSprite.IsAnimationPlaying || damageSprite.IsAnimationPlaying) yield return null;

                damageSprite.Visible = false;
                damageSprite.FlipX = !tutorSprite.FlipX;    // damage sprite needs flipping to the opposite of the attacking tutor so that it is reset to what it was originally
                status.AnimStatus = TutorAttackAnimStatus.EnumAnimStatus.Completed;

                // stop the rumble/shake stuff
                TutorManager.Instance.RumbleShakeFX.Stop();

                // wait until the ghost sprites are finished and hide them - hide them one by one as they were delayed by staggered amount
                // one by one in order so we don't need to repeated poll them all in a loop.
                for (int i = 0; i < numGhostsInTrail; i++)
                {
                    // wait for anim to stop playing
                    while (ghostSprites[i].IsAnimationPlaying) yield return null;

                    // hide the ghost
                    ghostSprites[i].Visible = false;
                }
            }

            protected virtual int GetCriticalSuperAttackAnimFrame()
            {
                return 0;
            }

            protected virtual int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public virtual SimpleMaterial GetVsSprite(EnumTutor tutor)
            {
                return _animData[(int)EnumAnim.VsScreen] as SimpleMaterial;
            }

            public virtual SimpleMaterial GetGamerPic()
            {
                return _animData[(int)EnumAnim.GamerPic] as SimpleMaterial;
            }

            public virtual string GetGamerTag()
            {
                throw new NotImplementedException();
            }

            public virtual string GetLocationName()
            {
                throw new NotImplementedException();
            }

            public virtual SimpleMaterial GetTeleportPic()
            {
                return _animData[(int)EnumAnim.Selected] as SimpleMaterial;
            }

            public virtual SimpleMaterial GetBG()
            {
                return _animData[(int)EnumAnim.BG] as SimpleMaterial;
            }

            public virtual SimpleMaterial GetCupEmboss()
            {
                return _animData[(int)EnumAnim.CupEmboss] as SimpleMaterial;
            }

            public virtual string GetAssetPath()
            {
                throw new NotImplementedException();
            }

            public string GetMusicTrack()
            {
                return _musicTrack;
            }

            public MFSoundManager.EnumSFX GetTauntSFX()
            {
                return _tauntSFX;
            }

            public MFSoundManager.EnumSFX GetTimeRunningOutSFX()
            {
                return _timeRunningOutSFX;
            }
        }



        private class CavemanTutor  : Tutor
        {
            public CavemanTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "anim_CaveMan_Idle";
                _animNames[(int)EnumAnim.Damaged] = "CaveMan_DamagedAnimation";
                _animNames[(int)EnumAnim.Win] = "anim_CaveMan_Dancing";
                _animNames[(int)EnumAnim.Lose] = "Cave_Man_LoseAnimation";
                _animNames[(int)EnumAnim.Impatient] = "anim_CaveMan_Impatient";
                _animNames[(int)EnumAnim.TimeRunningOut] = "anim_CaveMan_TimeRunningOut";
                _animNames[(int)EnumAnim.Selected] = "Cave_Man_2__Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "Caveman_SuperAttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "Cave_Man_2__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "CaveMan_AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "CaveMan_attackDamageAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "CaveMan_super_attackDamageAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "Cave_Man_2__Portrait_P2Material";
                _animNames[(int)EnumAnim.Taunt] = "CaveMan_TauntAnimation";
                _animNames[(int)EnumAnim.Freeze] = "CaveMan_FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "CaveMan_UnfreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__Prehistoric_outer_viewMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "Caveman_CupMaterial";

                _musicTrack = MFSoundManager.CAVEMAN_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.CavemanTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.CavemanAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.CavemanSuper;
                _loseSFX = MFSoundManager.EnumSFX.CavemanLose;
            }

            public override string GetGamerTag()
            {
                return "Goolog";
            }

            public override string GetLocationName()
            {
                return "Prehistoric";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 5;
            }

            protected override int  GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 6;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/Caveman/";
            }
        }



        private class SchoolTeacherTutor : Tutor
        {
            private T2DAnimationData[] _timeRunningOut = new T2DAnimationData[2];

            public SchoolTeacherTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "School_Teacher__IdleAnimation";
                _animNames[(int)EnumAnim.Damaged] = "School_Teacher__damagedAnimation";
                _animNames[(int)EnumAnim.Win] = "School_Teacher__winAnimation";
                _animNames[(int)EnumAnim.Lose] = "School_Teacher__LoseAnimation";
                _animNames[(int)EnumAnim.Impatient] = "School_Teacher__impatientAnimation";
                _animNames[(int)EnumAnim.TimeRunningOut] = null;    // this is a two part animation so we'll handle it ourselves                
                _animNames[(int)EnumAnim.Selected] = "School_Teacher__Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "School_Teacher__SuperAttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "School_Teacher_Setup__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "School_Teacher__AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "Teacher__attackDamageAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "Teacher__superAttackDamageAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "School_Teacher__Portrait_P2Material";
                _animNames[(int)EnumAnim.Taunt] = "School_Teacher__TauntAnimation";
                _animNames[(int)EnumAnim.Freeze] = "School_Teacher__FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "School_Teacher__UnFreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__teacherMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "Teacher_CupMaterial";

                _musicTrack = MFSoundManager.TEACHER_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.TeacherTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.TeacherAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.TeacherSuper;

                _flipNormalAttackDamageAnim = true;
                _flipSuperAttackDamageAnim = true;
                _loseSFX = MFSoundManager.EnumSFX.TeacherLose;
            }

            public override void CacheAnims()
            {
                base.CacheAnims();

                _timeRunningOut[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("School_Teacher___Time_Runnnig_outAnimation_part1");
                _timeRunningOut[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("School_Teacher___Time_Runnnig_outAnimation_part2");
            }

            public override void ReleaseCachedAnims()
            {
                base.ReleaseCachedAnims();

                _timeRunningOut[0] = null;
                _timeRunningOut[1] = null;
            }

            protected override IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(Tutor.EnumAnim anim, T2DSceneObject sprite)
            {
                if (anim == EnumAnim.TimeRunningOut)
                {
                    return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _timeRunningOut);
                }
                else
                {
                    return base.AsyncTask_PlayAnim(anim, sprite);
                }
            }

            public override string GetGamerTag()
            {
                return "Feba";
            }

            public override string GetLocationName()
            {
                return "School";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 4;
            }

            protected override int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 5;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/SchoolTeacher/";
            }
        }



        private class RobotTutor : Tutor
        {
            private T2DAnimationData[] _timeRunningOut = new T2DAnimationData[2];
            private T2DAnimationData[] _lose = new T2DAnimationData[2];
            private T2DAnimationData[] _win = new T2DAnimationData[2];

            public RobotTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "Robot_Idle_Anim";
                _animNames[(int)EnumAnim.Damaged] = "Robot_Taking_DamageAnimation";
                _animNames[(int)EnumAnim.Win] = null;   // this one is a two parter
                _animNames[(int)EnumAnim.Lose] = null;   // this one is a two parter
                _animNames[(int)EnumAnim.Impatient] = "Robot_ImpatientAnimation";
                _animNames[(int)EnumAnim.TimeRunningOut] = null;    // this one is a two parter
                _animNames[(int)EnumAnim.Selected] = "Robot_Wrong__Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "Robot_Super_AttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "Robot_Setup__freehand__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "Robot_AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "Robot_attack_FX__NormalAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "Robot_Attack_FX__SuperAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "Robot_Wrong__Portrait_P2Material";
                _animNames[(int)EnumAnim.Taunt] = "Robot_TauntAnimation";
                _animNames[(int)EnumAnim.Freeze] = "Robot_FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "Robot_UnFreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__robotMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "Robot_CupMaterial";

                _musicTrack = MFSoundManager.ROBOT_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.RobotTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.RobotAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.RobotSuper;
                _loseSFX = MFSoundManager.EnumSFX.RobotLose;
            }

            public override void CacheAnims()
            {
                base.CacheAnims();

                _timeRunningOut[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_TimeRunningAnimation_part1");
                _timeRunningOut[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_TimeRunningAnimation_part2");
                _lose[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_loseAnimation");
                _lose[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_loseAnimationpart2");
                _win[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_winAnimation");
                _win[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Robot_winAnimation_part2");                
            }

            public override void ReleaseCachedAnims()
            {
                base.ReleaseCachedAnims();

                _timeRunningOut[0] = null;
                _timeRunningOut[1] = null;
                _lose[0] = null;
                _lose[1] = null;
                _win[0] = null;
                _win[1] = null;
            }

            protected override IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(Tutor.EnumAnim anim, T2DSceneObject sprite)
            {
                switch (anim)
                {
                    case EnumAnim.TimeRunningOut:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _timeRunningOut);
                        //break;

                    case EnumAnim.Lose:
                        MFSoundManager.Instance.PlaySFX(_loseSFX);
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _lose);
                        //break;

                    case EnumAnim.Win:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _win);
                        //break;

                    default:
                        return base.AsyncTask_PlayAnim(anim, sprite);
                        //break;
                }
            }

            public override string GetGamerTag()
            {
                return "X-314";
            }

            public override string GetLocationName()
            {
                return "Factory";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 7;
            }

            protected override int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 11;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/Robot/";
            }
        }



        private class EinsteinTutor : Tutor
        {
            private T2DAnimationData[] _timeRunningOut = new T2DAnimationData[2];
            private T2DAnimationData[] _win = new T2DAnimationData[2];
            private T2DAnimationData[] _lose = new T2DAnimationData[2];

            public EinsteinTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "Einstein__idle_animation";
                _animNames[(int)EnumAnim.Damaged] = "Einstein__Taking_DamageAnimation";
                _animNames[(int)EnumAnim.Win] = null;   // this one is a two parter
                _animNames[(int)EnumAnim.Lose] = null;  // this one is a two parter
                _animNames[(int)EnumAnim.Impatient] = "Einstein_ImpatientAnimation";
                _animNames[(int)EnumAnim.TimeRunningOut] = null;    // this one is a two parter
                _animNames[(int)EnumAnim.Selected] = "Einstein_Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "Einstein__Super_AttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "Einstein_Setup__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "Einstein_AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "Attack__LightAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "Super_Attack__Nuclear_BlastAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "Einstein__Portrait_P2Material";
                _animNames[(int)EnumAnim.Taunt] = "Einstein__TauntAnimation";
                _animNames[(int)EnumAnim.Freeze] = "Einstein_FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "Einstein_UnFreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__einsteinMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "Einestein_CupMaterial";

                _musicTrack = MFSoundManager.EINSTEIN_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.EinsteinTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.EinsteinAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.EinsteinSuper;
                _loseSFX = MFSoundManager.EnumSFX.EinsteinLose;
            }

            public override void CacheAnims()
            {
                base.CacheAnims();

                _timeRunningOut[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein_Times_Running_outAnimation_part1");
                _timeRunningOut[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein_Times_Running_outAnimation_part2");
                _win[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein__WINAnimation_part1");
                _win[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein__WINAnimation_part2");
                _lose[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein__LoseAnimation_part1");
                _lose[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Einstein__LoseAnimation_part2");
            }


            public override void ReleaseCachedAnims()
            {
                base.ReleaseCachedAnims();

                _timeRunningOut[0] = null;
                _timeRunningOut[1] = null;
                _win[0] = null;
                _win[1] = null;
                _lose[0] = null;
                _lose[1] = null;
            }

            protected override IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(Tutor.EnumAnim anim, T2DSceneObject sprite)
            {
                switch (anim)
                {
                    case EnumAnim.TimeRunningOut:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _timeRunningOut);
                        //break;

                    case EnumAnim.Win:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _win);
                        //break;

                    case EnumAnim.Lose:
                        MFSoundManager.Instance.PlaySFX(_loseSFX);
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _lose);
                        //break;

                    default:
                        return base.AsyncTask_PlayAnim(anim, sprite);
                        //break;
                }
            }

            public override string GetGamerTag()
            {
                return "Einstein";
            }

            public override string GetLocationName()
            {
                return "The Lab";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 15;
            }

            protected override int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 22;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/Einstein/";
            }
        }



        private class AtomicKidTutor : Tutor
        {
            private T2DAnimationData[] _timeRunningOut = new T2DAnimationData[2];
            private T2DAnimationData[] _win = new T2DAnimationData[2];

            public AtomicKidTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "Atomic_Kid__idle_anim";
                _animNames[(int)EnumAnim.Damaged] = "Atomic_Kid__Taking_DamageAnimation";
                _animNames[(int)EnumAnim.Win] = null;   // this one is a two parter
                _animNames[(int)EnumAnim.Lose] = "Atomic_Kid__LoseAnimation";
                _animNames[(int)EnumAnim.Impatient] = "Atomic_Kid_ImpatientAnimation";
                _animNames[(int)EnumAnim.TimeRunningOut] = null;    // this one is a two parter
                _animNames[(int)EnumAnim.Selected] = "Atomic_Kid__Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "Atomic_Kid__Super_AttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "Atomic_Kid__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "Atomic_Kid__AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "Attack__WormholeAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "Super_Attack__Laser_BeamAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "Atomic_Kid__Portrait_P2Material";
                _animNames[(int)EnumAnim.Taunt] = "Atomic_Kid__Taunt0001Animation";
                _animNames[(int)EnumAnim.Freeze] = "Atomic_Kid__FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "Atomic_Kid__UnFreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__atomicMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "Atomic_Kid_CupMaterial";

                _musicTrack = MFSoundManager.ATOMICKID_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.AtomicKidTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.AtomicKidAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.AtomicKidSuper;
                _loseSFX = MFSoundManager.EnumSFX.AtomicKidLose;
            }

            public override void CacheAnims()
            {
                base.CacheAnims();

                _timeRunningOut[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Atomic_Kid__Times_running_outAnimationpart1");
                _timeRunningOut[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Atomic_Kid__Times_running_outAnimationpart2");
                _win[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Atomic_Kid__winAnimation");
                _win[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Atomic_Kid__winAnimationpart2");
            }

            public override void ReleaseCachedAnims()
            {
                base.ReleaseCachedAnims();

                _timeRunningOut[0] = null;
                _timeRunningOut[1] = null;
                _win[0] = null;
                _win[1] = null;
            }

            protected override IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(Tutor.EnumAnim anim, T2DSceneObject sprite)
            {
                switch (anim)
                {
                    case EnumAnim.TimeRunningOut:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _timeRunningOut);
                        //break;

                    case EnumAnim.Win:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _win);
                        //break;

                    default:
                        return base.AsyncTask_PlayAnim(anim, sprite);
                        //break;
                }
            }

            public override string GetGamerTag()
            {
                return "Yuri";
            }

            public override string GetLocationName()
            {
                return "Outer Space";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 6;
            }

            protected override int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 7;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/AtomicKid/";
            }
        }



        private class MathLordTutor : Tutor
        {
            private T2DAnimationData[] _win = new T2DAnimationData[2];

            public MathLordTutor()
            {
                _animNames[(int)EnumAnim.Idle] = "MathLord__IdleAnimation";
                _animNames[(int)EnumAnim.Damaged] = "MathLord__Taking_DamageAnimation";
                _animNames[(int)EnumAnim.Win] = null;   // this one is a two parter
                _animNames[(int)EnumAnim.Lose] = "MathLord__LoseAnimation";
                _animNames[(int)EnumAnim.Impatient] = "MathLord__ImpatientAnimation";
                _animNames[(int)EnumAnim.TimeRunningOut] = "MathLord__Time_Running_OutAnimation";
                _animNames[(int)EnumAnim.Selected] = "Math_Lord__Cell_VersionMaterial";
                _animNames[(int)EnumAnim.SuperAttack] = "MathLord__Super_AttackAnimation";
                _animNames[(int)EnumAnim.VsScreen] = "Math_Lord__VS_SCREENMaterial";
                _animNames[(int)EnumAnim.NormalAttack] = "MathLord__AttackAnimation";
                _animNames[(int)EnumAnim.NormalDamage] = "Attack_DarkEnergyAnimation";
                _animNames[(int)EnumAnim.SuperDamage] = "Super_attack__HowlAnimation";
                _animNames[(int)EnumAnim.GamerPic] = "Math_Lord_Tag_PicMaterial";
                _animNames[(int)EnumAnim.Taunt] = "MathLord__TauntAnimation";
                _animNames[(int)EnumAnim.Freeze] = "MathLord__FreezeAnimation";
                _animNames[(int)EnumAnim.Unfreeze] = "MathLord__UnFreezeAnimation";
                _animNames[(int)EnumAnim.BG] = "BG_01__bossMaterial";
                _animNames[(int)EnumAnim.CupEmboss] = "MathLord_CupMaterial";

                _musicTrack = MFSoundManager.MATHLORD_MUSIC;
                _tauntSFX = MFSoundManager.EnumSFX.BossTaunt;

                _normalAttackSFX = MFSoundManager.EnumSFX.BossAttack;
                _superAttackSFX = MFSoundManager.EnumSFX.BossSuperAttack;
                _loseSFX = MFSoundManager.EnumSFX.BossLose;
                _timeRunningOutSFX = MFSoundManager.EnumSFX.BossTimeRunningOut;
            }

            public override void CacheAnims()
            {
                base.CacheAnims();

                _win[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("MathLord__WinAnimationPart1");
                _win[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("MathLord__WinAnimationPart2");
            }

            public override void ReleaseCachedAnims()
            {
                base.ReleaseCachedAnims();

                _win[0] = null;
                _win[1] = null;
            }

            protected override IEnumerator<AsyncTaskStatus> AsyncTask_PlayAnim(Tutor.EnumAnim anim, T2DSceneObject sprite)
            {
                switch (anim)
                {
                    case EnumAnim.Win:
                        return AsyncTask_PlayMultiPartAnim(sprite as T2DAnimatedSprite, _win);
                    //break;

                    default:
                        return base.AsyncTask_PlayAnim(anim, sprite);
                    //break;
                }
            }

            public override string GetGamerTag()
            {
                return "Math Lord";
            }

            public override string GetLocationName()
            {
                return "Throne Room";
            }

            protected override int GetCriticalNormalAttackAnimFrame()
            {
                return 7;
            }

            protected override int GetCriticalNormalAttackDamageAnimFrame()
            {
                return 0;
            }

            protected override int GetCriticalSuperAttackAnimFrame()
            {
                return 11;
            }

            protected override int GetCriticalSuperAttackDamageAnimFrame()
            {
                return 0;
            }

            public override string GetAssetPath()
            {
                return "data/images/Tutors/MathLord/";
            }
        }



        public class TutorAttackAnimStatus
        {
            public enum EnumAnimStatus { Playing, ReachedDamageDealingFrame, Completed };
            public EnumAnimStatus AnimStatus;
        }
    }
}
