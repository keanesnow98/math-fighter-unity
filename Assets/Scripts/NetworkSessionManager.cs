using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Net;
using GarageGames.Torque.Core;
using System.Diagnostics;
using MathFreak.Math.Categories;
using MathFreak.GamePlay;
using MathFreak.GameStates;
using MathFreak.Highscores;
using MathFreak.AsyncTaskFramework;
using System.Threading;
using MathFreak.GameStateFramework;



namespace MathFreak
{
    /// <summary>
    /// Handles a network session (can only have one network session object in XNA).  In particular, the
    /// manager facilitates updating the session, persisting the session across multiple gamestates that
    /// deal with the session, and storing the session events in a queue which the gamestates can poll
    /// rather than having to add event handlers themselves (which tends to get overly complex because
    /// gamestates come and go and there is a "no man's land" for network event handling during transitions
    /// between states - it's much better to have one central queue that gamestates can poll).
    /// 
    /// Also wraps the begin/end async tasks and shutdown for network sessions so that the P2P
    /// highscores can be automatically started/stopped in perfect synchronisation with all the
    /// other network stuff we are doing in the game.
    /// </summary>
    public class NetworkSessionManager
    {
        public enum EnumSessionProperty { SessionType, Grades };
        public enum EnumSessionType { Game, Highscores };

#if SYSTEMLINK
        public const NetworkSessionType PLAYERMATCH = NetworkSessionType.SystemLink;
#else
        public const NetworkSessionType PLAYERMATCH = NetworkSessionType.PlayerMatch;
#endif

        private static NetworkSessionManager _instance;
        
        private NetworkSession _session;
        private Queue<SessionEventInfo> _eventQueue;
        private bool _sessionHasEnded;
        private InviteAcceptedEventArgs _inviteAcceptedNotification;
        private bool _invitesEnabled;

        private PacketWriter _writer;
        private PacketReader _reader;

        public static NetworkSessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetworkSessionManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// True if the session is valid for game use - there is a session, that session has
        /// not been disposed, and we have not received a session ended event.
        /// </summary>
        public bool IsValidSession
        {
            get { return _session != null && !_session.IsDisposed && !_sessionHasEnded; }
        }

        /// <summary>
        /// True if the session is still active.  IMPORTANT: being active does not mean the
        /// session is necessarily valid.  A session can be active, but have recieved the
        /// session ended event and thus no longer a valid session for game use, beyond processing
        /// the session ended event itself.
        /// </summary>
        public bool IsActiveSession
        {
            get { return _session != null && !_session.IsDisposed; }
        }

        public NetworkSession Session
        {
            get { return _session; }

            set
            {
                if (!IsActiveSession)
                {
                    _session = value;

                    // empty the queue
                    _eventQueue.Clear();

                    // add our event handlers
                    _sessionHasEnded = false;
                    _session.GameEnded += new EventHandler<GameEndedEventArgs>(OnGameEnded);
                    _session.GamerJoined += new EventHandler<GamerJoinedEventArgs>(OnGamerJoined);
                    _session.GamerLeft += new EventHandler<GamerLeftEventArgs>(OnGamerLeft);
                    _session.GameStarted += new EventHandler<GameStartedEventArgs>(OnGameStarted);
                    _session.SessionEnded += new EventHandler<NetworkSessionEndedEventArgs>(OnSessionEnded);
                }
                else
                {
                    // shouldn't really happen - xna only allows one active network session at a time anyway
                    Assert.Fatal(false, "Trying to set the network session when one already exists");
                }
            }
        }

        public int EventCount
        {
            get { return _eventQueue.Count; }
        }


        private NetworkSessionManager()
        {
            _eventQueue = new Queue<SessionEventInfo>();
        }

        public void Init()
        {
            _writer = new PacketWriter();
            _reader = new PacketReader();
        }

        private void InviteAccepted(object sender, InviteAcceptedEventArgs e)
        {
            if (e.IsCurrentSession) return; // we don't have the feature for local players to play in LIVE matches so this shouldn't happen anyway - and if it does just ignore it

            _inviteAcceptedNotification = e;    // we'll poll for this in the tick() method
        }

        // called from the main menu to enable invites/joining - MUST wait until on main menu
        // to do this as everything needs to be loaded and the player signed in, before the
        // can start joining (that is, if the player is joining from outside the game the game
        // has to load first - duh!)
        public void EnableInvites()
        {
            // invites already enabled?
            if (_invitesEnabled) return;

            // else enable invites
            _invitesEnabled = true;
            NetworkSession.InviteAccepted += new EventHandler<InviteAcceptedEventArgs>(InviteAccepted);
        }

        public void Tick()
        {
            if (_inviteAcceptedNotification != null)
            {
                ProcessInviteAccepted();
            }

            if (IsActiveSession)
            {
                _session.Update();
            }
        }

        // tries to accept the invite - i.e. join the session
        private void ProcessInviteAccepted()
        {
            if (GameStateManager.Instance.CanAcceptInvite())
            {
                _inviteAcceptedNotification = null;
                GameStateManager.Instance.OnInviteAccepted();
                Game.Instance.OnInviteAccepted();
            }
        }

        /// <summary>
        /// Shutsdown the active session (if there is one)
        /// </summary>
        public void ShutdownSession()
        {
            if (IsActiveSession)
            {
                Debug.WriteLine("ShutdownSession() - shutting down network session");
                _session.Dispose();
                _session = null;
                _sessionHasEnded = false;

                // start the P2P highscore stuff (note: it will wait a while before starting any network stuff so as not to trigger throttling)
                if (_inviteAcceptedNotification == null)
                {
                    HighscoresP2P.Instance.Start();
                }
            }
            else
            {
                Debug.WriteLine("ShutdownSession() - network session is already shutdown");
            }
        }

        // should only be called from the P2P highscores stuff - will shutdown the session
        // without triggering P2P highscores stuff starting up.
        public void ShutdownP2PHighscoresSession()
        {
            if (IsActiveSession)
            {
                Debug.WriteLine("ShutdownP2PHighscoresSession() - shutting down network session");
                _session.Dispose();
                _session = null;
                _sessionHasEnded = false;
            }
            else
            {
                Debug.WriteLine("ShutdownP2PHighscoresSession() - network session is already shutdown");
            }
        }

        /// <summary>
        /// Gets the next event in the queue of events recieved from the active network session - this
        /// will 'consume' the event.  Returns null if no more events left in the queue.
        /// </summary>
        public SessionEventInfo GetNextEvent()
        {
            if (_eventQueue.Count > 0)
            {
                return _eventQueue.Dequeue();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Allows pushing an event back onto the queue - e.g. the event could not or should
        /// not be processed just yet so the caller puts it back.
        /// </summary>
        /// <param name="eventInfo"></param>
        public void PushBackEvent(SessionEventInfo eventInfo)
        {
            // no method to unget an item from the queue so we'll convert to a list and back again
            //
            // NOTE: this is not a method that will get called in performance critical parts of the
            // game - typically it will be called only when a menu is showing or a gamestate
            // is in the process of exiting.
            List<SessionEventInfo> listVersion = _eventQueue.ToList<SessionEventInfo>();
            listVersion.Insert(0, eventInfo);
            _eventQueue = new Queue<SessionEventInfo>(listVersion);
        }

        public bool IsHosting()
        {
            Assert.Fatal(_session != null && !_session.IsDisposed, "Testing for IsHosting() when session is not valid");

            return NetworkSessionManager.Instance.Session.IsHost;
            //return (Game.Instance.FindSignedInGamer(NetworkSessionManager.Instance.Session.Host.Gamertag) != null);
        }

        private void OnGameStarted(object sender, GameStartedEventArgs e)
        {
            _eventQueue.Enqueue(new SessionEventInfo(SessionEventInfo.EnumSessionEvent.GameStarted, e));
        }

        private void OnGameEnded(object sender, GameEndedEventArgs e)
        {
            _eventQueue.Enqueue(new SessionEventInfo(SessionEventInfo.EnumSessionEvent.GameEnded, e));
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs e)
        {
            _eventQueue.Enqueue(new SessionEventInfo(SessionEventInfo.EnumSessionEvent.GamerJoined, e));
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs e)
        {
            _eventQueue.Enqueue(new SessionEventInfo(SessionEventInfo.EnumSessionEvent.GamerLeft, e));
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs e)
        {
            // clear the event queue and put the session ended event in the queue - any other event is a moot point as the session is over anyway
            _eventQueue.Clear();
            _sessionHasEnded = true;
            _eventQueue.Enqueue(new SessionEventInfo(SessionEventInfo.EnumSessionEvent.SessionEnded, e));
        }

        public void SendDataToHost(NetworkMessage msg, LocalNetworkGamer sender)
        {
            msg.Write(_writer);
            sender.SendData(_writer, SendDataOptions.ReliableInOrder, NetworkSessionManager.Instance.Session.Host);
        }

        // NOTE: this will also send the data to the sender (localGamer)
        public void SendDataToAll(NetworkMessage msg, LocalNetworkGamer sender)
        {
            msg.Write(_writer);
            sender.SendData(_writer, SendDataOptions.ReliableInOrder);
        }

        public void SendData(NetworkMessage msg, LocalNetworkGamer sender, NetworkGamer recipient)
        {
            msg.Write(_writer);
            sender.SendData(_writer, SendDataOptions.ReliableInOrder, recipient);
        }

        // will return false if data was not from the host and will not store the data
        public bool RecieveDataFromHost(NetworkMessage msgToReadDataInto, LocalNetworkGamer receiver)
        {
            NetworkGamer sender;
            
            receiver.ReceiveData(_reader, out sender);

            if (sender == _session.Host)
            {
                msgToReadDataInto.Read(_reader, sender);
                //Debug.WriteLine("Recieved data from HOST[" + sender.Gamertag + "]\n" + this.ToString());
                return true;
            }
            else
            {
                msgToReadDataInto.Clear();
                //Debug.WriteLine("Data recieved, but not from HOST - data ignored");
                return false;
            }
        }

        // will return false if data was not from a client and will not store the data
        public bool RecieveDataFromClient(NetworkMessage msgToReadDataInto, LocalNetworkGamer receiver)
        {
            NetworkGamer sender;

            receiver.ReceiveData(_reader, out sender);

            if (sender != NetworkSessionManager.Instance.Session.Host)
            {
                msgToReadDataInto.Read(_reader, sender);
                //Debug.WriteLine("Recieved data from CLIENT[" + sender.Gamertag + "]\n" + this.ToString());
                return true;
            }
            else
            {
                msgToReadDataInto.Clear();
                //Debug.WriteLine("Data recieved, but not from a CLIENT - data ignored");
                return false;
            }
        }

        public void RecieveDataFromAny(NetworkMessage msgToReadDataInto, LocalNetworkGamer receiver)
        {
            NetworkGamer sender;

            receiver.ReceiveData(_reader, out sender);
            msgToReadDataInto.Read(_reader, sender);
            //Debug.WriteLine("Recieved data from GAMER[" + sender.Gamertag + "]\n" + this.ToString());
        }

        public IAsyncResult BeginCreate(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateSlots, NetworkSessionProperties properties, AsyncCallback callback, object asyncState)
        {
            NetworkSessionAsyncResult result = new NetworkSessionAsyncResult();
            Game.Instance.AddAsyncTask(AsyncTask_BeginCreate(sessionType, maxLocalGamers, maxGamers, privateSlots, properties, callback, asyncState, result), true);
            return result;
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_BeginCreate(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateSlots, NetworkSessionProperties properties, AsyncCallback callback, object asyncState, NetworkSessionAsyncResult result)
        {
            // request p2p highscores to stop
            HighscoresP2P.Instance.Stop();

            // wait for p2p highscores to stop
            while (!HighscoresP2P.Instance.IsStopped) yield return null;

            // check network session not active (incase an errant session left active for any reason and is not p2p, so was not shutdown when we did the p2p shutting down bit)
            if (IsActiveSession)
            {
                ShutdownSession();
            }

            // do the begin create stuff - don't catch exceptions; the caller will be handling those
            result.Result = NetworkSession.BeginCreate(sessionType, maxLocalGamers, maxGamers, privateSlots, properties, callback, asyncState);

            // NOTE: no need to wait for the async task to finish as our custom async result that
            // we returned to the caller just delegates to the real async result, so the caller
            // will get all the right info with no further effort on our part.
        }

        public NetworkSession EndCreate(IAsyncResult result)
        {
            return NetworkSession.EndCreate((result as NetworkSessionAsyncResult).Result);
        }

        public IAsyncResult BeginFind(NetworkSessionType sessionType, int maxLocalGamers, NetworkSessionProperties searchProperties, AsyncCallback callback, object asyncState)
        {
            NetworkSessionAsyncResult result = new NetworkSessionAsyncResult();
            Game.Instance.AddAsyncTask(AsyncTask_BeginFind(sessionType, maxLocalGamers, searchProperties, callback, asyncState, result), true);
            return result;
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_BeginFind(NetworkSessionType sessionType, int maxLocalGamers, NetworkSessionProperties searchProperties, AsyncCallback callback, object asyncState, NetworkSessionAsyncResult result)
        {
            // request p2p highscores to stop
            HighscoresP2P.Instance.Stop();

            // wait for p2p highscores to stop
            while (!HighscoresP2P.Instance.IsStopped) yield return null;

            // check network session not active (incase an errant session left active for any reason and is not p2p, so was not shutdown when we did the p2p shutting down bit)
            if (IsActiveSession)
            {
                ShutdownSession();
            }

            // do the begin find stuff - don't catch exceptions; the caller will be handling those
            result.Result = NetworkSession.BeginFind(sessionType, maxLocalGamers, searchProperties, callback, asyncState);

            // NOTE: no need to wait for the async task to finish as our custom async result that
            // we returned to the caller just delegates to the real async result, so the caller
            // will get all the right info with no further effort on our part.
        }

        public AvailableNetworkSessionCollection EndFind(IAsyncResult result)
        {
            return NetworkSession.EndFind((result as NetworkSessionAsyncResult).Result);
        }

        public IAsyncResult BeginJoin(AvailableNetworkSession availableSession, AsyncCallback callback, object asyncState)
        {
            NetworkSessionAsyncResult result = new NetworkSessionAsyncResult();
            Game.Instance.AddAsyncTask(AsyncTask_BeginJoin(availableSession, callback, asyncState, result), true);
            return result;
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_BeginJoin(AvailableNetworkSession availableSession, AsyncCallback callback, object asyncState, NetworkSessionAsyncResult result)
        {
            // request p2p highscores to stop
            HighscoresP2P.Instance.Stop();

            // wait for p2p highscores to stop
            while (!HighscoresP2P.Instance.IsStopped) yield return null;

            // check network session not active (incase an errant session left active for any reason and is not p2p, so was not shutdown when we did the p2p shutting down bit)
            if (IsActiveSession)
            {
                ShutdownSession();
            }

            // do the begin join stuff - don't catch exceptions; the caller will be handling those
            result.Result = NetworkSession.BeginJoin(availableSession, callback, asyncState);

            // NOTE: no need to wait for the async task to finish as our custom async result that
            // we returned to the caller just delegates to the real async result, so the caller
            // will get all the right info with no further effort on our part.
        }

        public NetworkSession EndJoin(IAsyncResult result)
        {
            return NetworkSession.EndJoin((result as NetworkSessionAsyncResult).Result);
        }

        public IAsyncResult BeginJoinInvited(AsyncCallback callback, object asyncState)
        {
            NetworkSessionAsyncResult result = new NetworkSessionAsyncResult();
            Game.Instance.AddAsyncTask(AsyncTask_BeginJoinInvited(callback, asyncState, result), true);
            return result;
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_BeginJoinInvited(AsyncCallback callback, object asyncState, NetworkSessionAsyncResult result)
        {
            // request p2p highscores to stop
            HighscoresP2P.Instance.Stop();

            // wait for p2p highscores to stop
            while (!HighscoresP2P.Instance.IsStopped) yield return null;

            // check network session not active (incase an errant session left active for any reason and is not p2p, so was not shutdown when we did the p2p shutting down bit)
            if (IsActiveSession)
            {
                ShutdownSession();
            }

            // do the begin join stuff - don't catch exceptions; the caller will be handling those
            result.Result = NetworkSession.BeginJoinInvited(1, callback, asyncState);

            // NOTE: no need to wait for the async task to finish as our custom async result that
            // we returned to the caller just delegates to the real async result, so the caller
            // will get all the right info with no further effort on our part.
        }

        public NetworkSession EndJoinInvited(IAsyncResult result)
        {
            return NetworkSession.EndJoinInvited((result as NetworkSessionAsyncResult).Result);
        }
    }

    
    
    public class NetworkSessionAsyncResult : IAsyncResult
    {
        public IAsyncResult Result;

        public object AsyncState { get { return Result.AsyncState; } }
        public WaitHandle AsyncWaitHandle { get { return Result.AsyncWaitHandle; } }
        public bool CompletedSynchronously { get { return Result.CompletedSynchronously; } }
        public bool IsCompleted { get { return (Result != null ? Result.IsCompleted : false); } }
    }



    /// <summary>
    /// Stores the info for a session event
    /// </summary>
    public class SessionEventInfo
    {
        public enum EnumSessionEvent { GameStarted, GameEnded, GamerJoined, GamerLeft, SessionEnded };

        public SessionEventInfo(EnumSessionEvent type, EventArgs args)
        {
            Type = type;
            Args = args;
        }

        public EnumSessionEvent Type;
        public EventArgs Args;
    }



    public class NetworkMessage
    {
        public enum EnumType { Undefined, ClientIsReadyForNextQuestion, SyncDataForNewClients, QuestionContent, PlayerInputs, SettingsChanged, PlayerList, SelectionMovement, SelectionsChanged, GotoSelectionScreen, GotoVsScreen, GameStateJoined, SelectionWelcomePack, GamePlayWelcomePack, PlayerXXXXWins, Highscores };

        private EnumType _type;
        private Object _data;
        private NetworkGamer _sender;

        public EnumType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public Object Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public NetworkGamer Sender
        {
            get { return _sender; }
        }


        public NetworkMessage()
        {
            Clear();
        }

        public NetworkMessage(EnumType type, Object data)
        {
            SetContent(type, data);
        }

        public void Clear()
        {
            _type = EnumType.Undefined;
            _data = null;
        }

        public void Consume()
        {
            Clear();
        }

        public void SetContent(EnumType type, Object data)
        {
            _type = type;
            _data = data;
        }

        public void Write(PacketWriter writer)
        {
            // how the data is written will depend on the message type
            switch (_type)
            {
                case EnumType.ClientIsReadyForNextQuestion:
                    WriteHeader(writer);
                    WriteClientIsReadyForNextQuestion(writer);
                    break;

                case EnumType.SyncDataForNewClients:
                    WriteHeader(writer);
                    WriteSyncDataForNewClients(writer);
                    break;

                case EnumType.QuestionContent:
                    WriteHeader(writer);
                    WriteQuestionContent(writer);
                    break;

                case EnumType.PlayerInputs:
                    WriteHeader(writer);
                    WritePlayerInputs(writer);
                    break;

                case EnumType.SettingsChanged:
                    WriteHeader(writer);
                    WriteSettingsChanged(writer);
                    break;

                case EnumType.PlayerList:
                    WriteHeader(writer);
                    WritePlayerList(writer);
                    break;

                case EnumType.SelectionMovement:
                    WriteHeader(writer);
                    WriteSelectionMovement(writer);
                    break;

                case EnumType.SelectionsChanged:
                    WriteHeader(writer);
                    WriteSelectionsChanged(writer);
                    break;

                case EnumType.GotoVsScreen:
                    WriteHeader(writer);
                    WriteGotoVsScreen(writer);
                    break;

                case EnumType.GotoSelectionScreen:
                    WriteHeader(writer);
                    WriteGotoSelectionScreen(writer);
                    break;

                case EnumType.GameStateJoined:
                    WriteHeader(writer);
                    WriteGameStateJoined(writer);
                    break;

                case EnumType.SelectionWelcomePack:
                    WriteHeader(writer);
                    WriteSelectionWelcomePack(writer);
                    break;

                case EnumType.PlayerXXXXWins:
                    WriteHeader(writer);
                    WritePlayerXXXXWins(writer);
                    break;

                case EnumType.GamePlayWelcomePack:
                    WriteHeader(writer);
                    WriteGamePlayWelcomePack(writer);
                    break;

                case EnumType.Highscores:
                    WriteHeader(writer);
                    WriteHighscores(writer);
                    break;

                default:
                    Assert.Fatal(false, "NetworkMessage.Write() - message type is undefined");
                    Clear();
                    break;
            }
        }

        public void Read(PacketReader reader, NetworkGamer sender)
        {
            _sender = sender;

            // how the data is read will depend on the message type
            ReadHeader(reader);

            switch (_type)
            {
                case EnumType.ClientIsReadyForNextQuestion:
                    ReadClientIsReadyForNextQuestion(reader);
                    break;

                case EnumType.SyncDataForNewClients:
                    ReadSyncDataForNewClients(reader);
                    break;

                case EnumType.QuestionContent:
                    ReadQuestionContent(reader);
                    break;

                case EnumType.PlayerInputs:
                    ReadPlayerInputs(reader);
                    break;

                case EnumType.SettingsChanged:
                    ReadSettingsChanged(reader);
                    break;

                case EnumType.PlayerList:
                    ReadPlayerList(reader);
                    break;

                case EnumType.SelectionMovement:
                    ReadSelectionMovement(reader);
                    break;

                case EnumType.SelectionsChanged:
                    ReadSelectionsChanged(reader);
                    break;

                case EnumType.GotoVsScreen:
                    ReadGotoVsScreen(reader);
                    break;

                case EnumType.GotoSelectionScreen:
                    ReadGotoSelectionScreen(reader);
                    break;

                case EnumType.GameStateJoined:
                    ReadGameStateJoined(reader);
                    break;

                case EnumType.SelectionWelcomePack:
                    ReadSelectionWelcomePack(reader);
                    break;

                case EnumType.PlayerXXXXWins:
                    ReadPlayerXXXXWins(reader);
                    break;

                case EnumType.GamePlayWelcomePack:
                    ReadGamePlayWelcomePack(reader);
                    break;

                case EnumType.Highscores:
                    ReadHighscores(reader);
                    break;

                default:
                    Assert.Fatal(false, "NetworkMessage.Read() - message type is undefined");
                    Clear();
                    break;
            }
        }

        private void WriteHeader(PacketWriter writer)
        {
            writer.Write(_type.ToString());
        }

        private void WriteClientIsReadyForNextQuestion(PacketWriter writer)
        {
            // no content - this is a simple notification message and the message type conveys all the required info
        }

        private void WriteSyncDataForNewClients(PacketWriter writer)
        {
            int[] health = _data as int[];

            writer.Write(health[0]);
            writer.Write(health[1]);
        }

        private void WriteQuestionContent(PacketWriter writer)
        {
            Assert.Fatal(_data != null, "WriteQuestionContent() - data is null");

            QuestionContent question = _data as QuestionContent;

            writer.Write(question.CatName);
            writer.Write(question.LevelName);
            writer.Write(question.Question);
            writer.Write(question.RightAnswer);

            for (int i = 0; i < 4; i++)
            {
                writer.Write(question.Answers[i]);
            }

            writer.Write((double)question.TimeAllowed);
            writer.Write(question.Points);
            writer.Write(question.Tutor.ToString());
            writer.Write(question.Hint);
        }

        private void WritePlayerInputs(PacketWriter writer)
        {
            object[] inputs = _data as object[];

            Player.EnumGamepadButton[] playerInput = inputs[0] as Player.EnumGamepadButton[];
            bool[] hintpressed = inputs[1] as bool[];
            bool[] tauntpressed = inputs[2] as bool[];
            bool[] superattackpressed = inputs[3] as bool[];
            float countdownRemaining = (float)inputs[4];

            writer.Write(playerInput[0].ToString());
            writer.Write(playerInput[1].ToString());
            writer.Write(hintpressed[0]);
            writer.Write(hintpressed[1]);
            writer.Write(tauntpressed[0]);
            writer.Write(tauntpressed[1]);
            writer.Write(superattackpressed[0]);
            writer.Write(superattackpressed[1]);
            writer.Write((double)countdownRemaining);
        }

        private void WriteSettingsChanged(PacketWriter writer)
        {
            GamePlaySettings settings = _data as GamePlaySettings;

            // energy bar
            writer.Write(settings.EnergyBar);

            // math grade type (easy, medium, custom, etc)
            writer.Write(settings.MathGradesSetting.ToString());

            // math grades
            bool[] mathGrades = settings.GetMathGrades();
            int count = mathGrades.Length;

            writer.Write(count);

            for (int i = 0; i < count; i++)
            {
                writer.Write(mathGrades[i]);
            }

            // location
            writer.Write(settings.Location.ToString());
        }

        private void WritePlayerList(PacketWriter writer)
        {
            // we could use player ids instead of gamertags, but the info on how the ids are assigned
            // is limited so I don't currently trust it that it won't happen that one player leaves
            // and another arrives and have the same id (there are only 256 available so not sure how
            // unique they are garaunteed to be)
            string[] players = _data as string[];

            writer.Write(players.Length);

            for (int i = 0; i < players.Length; i++)
            {
                writer.Write(players[i]);
            }
        }

        private void WriteSelectionMovement(PacketWriter writer)
        {
            GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo info = _data as GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo;

            writer.Write(info.Player);
            writer.Write(info.Character.ToString());
        }

        private void WriteSelectionsChanged(PacketWriter writer)
        {
            GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo info = _data as GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo;

            writer.Write(info.Player);
            writer.Write(info.Character.ToString());
        }

        private void WriteGotoVsScreen(PacketWriter writer)
        {
            // no content - this is a simple notification message and the message type conveys all the required info
        }

        private void WriteGotoSelectionScreen(PacketWriter writer)
        {
            writer.Write(_data as string);
        }

        private void WritePlayerXXXXWins(PacketWriter writer)
        {
            writer.Write((int)_data);
        }

        private void WriteGameStateJoined(PacketWriter writer)
        {
            writer.Write(_data as string);
        }

        private void WriteSelectionWelcomePack(PacketWriter writer)
        {
            object[] info = _data as object[];

            string[] players = info[0] as string[];
            TutorManager.EnumTutor player1_highlighted = (TutorManager.EnumTutor)info[1];
            TutorManager.EnumTutor player2_highlighted = (TutorManager.EnumTutor)info[2];
            TutorManager.EnumTutor player1_selected = (TutorManager.EnumTutor)info[3];
            TutorManager.EnumTutor player2_selected = (TutorManager.EnumTutor)info[4];

            writer.Write(players.Length);

            for (int i = 0; i < players.Length; i++)
            {
                writer.Write(players[i]);
            }

            writer.Write(player1_highlighted.ToString());
            writer.Write(player2_highlighted.ToString());
            writer.Write(player1_selected.ToString());
            writer.Write(player2_selected.ToString());
        }

        private void WriteGamePlayWelcomePack(PacketWriter writer)
        {
            object[] info = _data as object[];

            string[] allPlayers = info[0] as string[];
            string[] players = info[1] as string[];
            TutorManager.EnumTutor character1 = (TutorManager.EnumTutor)info[2];
            TutorManager.EnumTutor character2 = (TutorManager.EnumTutor)info[3];
            int health1 = (int)info[4];
            int health2 = (int)info[5];
            int questionNumber = (int)info[6];
            int energybar = (int)info[7];
            int mathGrade = (int)info[8];
            TutorManager.EnumTutor location = (TutorManager.EnumTutor)info[9];
            TutorManager.EnumTutor locationToUse = (TutorManager.EnumTutor)info[10];

            writer.Write(allPlayers.Length);

            for (int i = 0; i < allPlayers.Length; i++)
            {
                writer.Write(allPlayers[i]);
            }

            writer.Write(players[0]);
            writer.Write(players[1]);
            writer.Write(character1.ToString());
            writer.Write(character2.ToString());
            writer.Write(health1);
            writer.Write(health2);
            writer.Write(questionNumber);
            writer.Write(energybar);
            writer.Write(mathGrade);
            writer.Write(location.ToString());
            writer.Write(locationToUse.ToString());
        }

        // NOTE: this one writes a dictionary out as an array and the reader will read it in as
        // an array, so this is unusual for math fighter in being an asymetric network message.
        private void WriteHighscores(PacketWriter writer)
        {
            HighscoreData.PlayerScoreData scoreData = _data as HighscoreData.PlayerScoreData;

            int count = scoreData.SpCount;
            writer.Write(count);

            for (int i = 0; i < count; i++)
            {
                HighscoreData.SinglePlayerScoreData.Write(writer, scoreData.SinglePlayerScores[i]);
            }

            count = scoreData.MpCount;
            writer.Write(count);

            for (int i = 0; i < count; i++)
            {
                HighscoreData.MultiplayerScoreData.Write(writer, scoreData.MultiplayerScores[i]);
            }
        }

        private void ReadHeader(PacketReader reader)
        {
            _type = Util.StringToEnum<EnumType>(reader.ReadString());
        }

        private void ReadClientIsReadyForNextQuestion(PacketReader reader)
        {
            // no content - this is a simple notification message and the message type conveys all the required info
        }

        private void ReadSyncDataForNewClients(PacketReader reader)
        {
            int[] health = new int[2];

            health[0] = reader.ReadInt32();
            health[1] = reader.ReadInt32();
        }

        private void ReadQuestionContent(PacketReader reader)
        {
            QuestionContent question = new QuestionContent();

            try
            {
                question.CatName = reader.ReadString();
                question.LevelName = reader.ReadString();
                question.Question = reader.ReadString();
                question.RightAnswer = reader.ReadInt32();

                question.Answers = new string[4];

                for (int i = 0; i < 4; i++)
                {
                    question.Answers[i] = reader.ReadString();
                }

                question.TimeAllowed = (float)reader.ReadDouble();
                question.Points = reader.ReadInt32();
                question.Tutor = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
                question.Hint = reader.ReadString();

                _data = question;
            }
            catch(Exception e)
            {
                Assert.Fatal(false, "ReadQuestionContent() - problem reading data - threw exception: " + e.Message);

                _type = EnumType.Undefined;
                _data = null;
            }
        }

        private void ReadPlayerInputs(PacketReader reader)
        {
            Player.EnumGamepadButton[] playerInput = new Player.EnumGamepadButton[2];
            bool[] hintpressed = new bool[2];
            bool[] tauntpressed = new bool[2];
            bool[] superattackpressed = new bool[2];
            float countdownRemaining;

            playerInput[0] = Util.StringToEnum<Player.EnumGamepadButton>(reader.ReadString());
            playerInput[1] = Util.StringToEnum<Player.EnumGamepadButton>(reader.ReadString());
            hintpressed[0] = reader.ReadBoolean();
            hintpressed[1] = reader.ReadBoolean();
            tauntpressed[0] = reader.ReadBoolean();
            tauntpressed[1] = reader.ReadBoolean();
            superattackpressed[0] = reader.ReadBoolean();
            superattackpressed[1] = reader.ReadBoolean();
            countdownRemaining = (float)reader.ReadDouble();

            _data = new object[5] { playerInput, hintpressed, tauntpressed, superattackpressed, countdownRemaining };
        }

        private void ReadSettingsChanged(PacketReader reader)
        {
            GamePlaySettings settings = new GamePlaySettings();

            // energy bar
            settings.EnergyBar = reader.ReadInt32();

            // math grade type (easy, medium, custom, etc)
            settings.MathGradesSetting = Util.StringToEnum<GamePlaySettings.EnumMathGradesSetting>(reader.ReadString());

            // math grades
            int count = reader.ReadInt32();
            bool[] mathGrades = new bool[count];

            for (int i = 0; i < count; i++)
            {
                mathGrades[i] = reader.ReadBoolean();
            }

            settings.EnableMathGrades(mathGrades);

            // location
            settings.Location = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());

            _data = settings;
        }
        
        private void ReadPlayerList(PacketReader reader)
        {
            int length = reader.ReadInt32();

            string[] players = new string[length];

            for (int i = 0; i < length; i++)
            {
                players[i] = reader.ReadString();
            }

            _data = players;
        }

        private void ReadSelectionMovement(PacketReader reader)
        {
            GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo info = new GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo();

            info.Player = reader.ReadInt32();
            info.Character = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());

            _data = info;
        }

        private void ReadSelectionsChanged(PacketReader reader)
        {
            GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo info = new GameStateCharacterSelectionLobbyMultiplayerLIVE.SelectionInfo();

            info.Player = reader.ReadInt32();
            info.Character = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());

            _data = info;
        }
    
        private void ReadGotoVsScreen(PacketReader reader)
        {
            // no content - this is a simple notification message and the message type conveys all the required info
        }

        private void ReadGotoSelectionScreen(PacketReader reader)
        {
            _data = reader.ReadString();
        }

        private void ReadPlayerXXXXWins(PacketReader reader)
        {
            _data = reader.ReadInt32();
        }

        private void ReadGameStateJoined(PacketReader reader)
        {
            _data = reader.ReadString();
        }
    
        private void ReadSelectionWelcomePack(PacketReader reader)
        {
            int length = reader.ReadInt32();
            string[] players = new string[length];

            for (int i = 0; i < length; i++)
            {
                players[i] = reader.ReadString();
            }

            TutorManager.EnumTutor player1_highlighted = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            TutorManager.EnumTutor player2_highlighted = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            TutorManager.EnumTutor player1_selected = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            TutorManager.EnumTutor player2_selected = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());

            _data = new object[5] { players, player1_highlighted, player2_highlighted, player1_selected, player2_selected };
        }

        private void ReadGamePlayWelcomePack(PacketReader reader)
        {
            int length = reader.ReadInt32();
            string[] allPlayers = new string[length];

            for (int i = 0; i < length; i++)
            {
                allPlayers[i] = reader.ReadString();
            }

            string[] players = new string[2];

            players[0] = reader.ReadString();
            players[1] = reader.ReadString();
            TutorManager.EnumTutor character1 = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            TutorManager.EnumTutor character2 = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            int health1 = reader.ReadInt32();
            int health2 = reader.ReadInt32();
            int questionNumber = reader.ReadInt32();
            int energybar = reader.ReadInt32();
            int mathGrade = reader.ReadInt32();
            TutorManager.EnumTutor location = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());
            TutorManager.EnumTutor locationToUse = Util.StringToEnum<TutorManager.EnumTutor>(reader.ReadString());

            _data = new object[11] { allPlayers, players, character1, character2, health1, health2, questionNumber, energybar, mathGrade, location, locationToUse };
        }
 
        private void ReadHighscores(PacketReader reader)
        {
            HighscoreData.PlayerScoreData scoreData = new HighscoreData.PlayerScoreData();

            int length = reader.ReadInt32();
            scoreData.SinglePlayerScores = new HighscoreData.SinglePlayerScoreData[length];

            for (int i = 0; i < length; i++)
            {
                scoreData.SinglePlayerScores[i] = HighscoreData.SinglePlayerScoreData.Read(reader);
            }

            length = reader.ReadInt32();
            scoreData.MultiplayerScores = new HighscoreData.MultiplayerScoreData[length];

            for (int i = 0; i < length; i++)
            {
                scoreData.MultiplayerScores[i] = HighscoreData.MultiplayerScoreData.Read(reader);
            }

            _data = scoreData;
        }
    }
}