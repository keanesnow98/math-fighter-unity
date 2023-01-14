using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.AsyncTaskFramework;
using Microsoft.Xna.Framework.Net;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using System.Collections;

namespace MathFreak.Highscores
{
    /// <summary>
    /// Handles the p2p highscore stuff - updates highscores across the network (requires that
    /// the player has gold membership).  Any given gamertag can only be recorded once.
    /// There is no intelligence - it will just keep sending out data and recieving it without
    /// worrying if it is repeating the same data.  Tune DELAY_UPLOADDATA and SCORE_UPLOAD_CHUNKSIZE
    /// in particular to resolve bandwidth related perfomance issues if they arise.
    /// </summary>
    public class HighscoresP2P
    {
        private const int MAX_CLIENTS = 5;
        //private const int MAX_RECIEVEDCOUNT = 5;
        //private const int MAX_BROADCASTCOUNT = 10;
        private const float CONNECTION_CLIENT_TIMEOUT = 120.0f;
        private const float CONNECTION_HOST_TIMEOUT = 300.0f;
        private const float DELAY_UPLOADDATA = 1.0f;
        private const float DELAY_WAITBEFOREFINDINGSESSION = 5.0f;
        private const float DELAY_WAITBEFOREHOSTINGSESSION = 20.0f;
        private const string TASKLIST_P2PHIGHSCORES = "p2phighscores";
        private const int SCORE_INTEGRATION_CHUNKSIZE = 1;    // NOTE: integration and upload chunk sizes do not need to be the same - but note that if chunk size it much much smaller than the upload size then there might not be enough time between recieving uploads for the data to be all integrated (which would mean lost data but no crashes)
        private const int SCORE_UPLOAD_CHUNKSIZE = 5; // remember this is for each type of highscore (sp/mp) so double this figure to get the full amount of data that can be sent

        private bool _requestImmediateStop;

        private Random _rnd;
        private float _elapsedTime;     // so the async tasks can know the time elapsed since last doing something that needs a delay enforced to prevent xna 'throttling' stuff
        private float _dt;

        private NetworkMessage _networkMessage;
        private HighscoreData.PlayerScoreData _newScores;

        private int _nextScoreIndexToUpload;

        private static HighscoresP2P _instance;

        public static HighscoresP2P Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HighscoresP2P();
                }

                return _instance;
            }
        }

        public int NextScoreIndexToUpload
        {
            get { return _nextScoreIndexToUpload; }
            set { _nextScoreIndexToUpload = value; }
        }

        /// <summary>
        /// Returns true if p2p processing has been stopped - this status flag should be polled to
        /// determine when the p2p processing has actually stopped after a call to Stop() since processing
        /// may not stop immediately.
        /// </summary>
        public bool IsStopped
        {
            get { return _state == EnumP2PState.Idle; }
        }

        /// <summary>
        /// Enumerates the states that the P2P Highscore processing can be in.
        /// </summary>
        private enum EnumP2PState { Idle, FindSession, JoinSession, CreateSession, ProcessClientSession, ProcessHostSession };
        private EnumP2PState _state;

        private HighscoresP2P()
        {
            AsyncTaskManager.Instance.NewTaskList(TASKLIST_P2PHIGHSCORES);
            _rnd = new Random();
            _networkMessage = new NetworkMessage();
            MoveToState(EnumP2PState.Idle, null);
        }

        public NetworkSessionProperties GetSessionProperties()
        {
            NetworkSessionProperties properties = new NetworkSessionProperties();
            properties[(int)NetworkSessionManager.EnumSessionProperty.SessionType] = (int)NetworkSessionManager.EnumSessionType.Highscores;

            return properties;
        }

        // pattern of properties to match - null will match any value
        public NetworkSessionProperties GetSessionPropertiesPattern()
        {
            NetworkSessionProperties properties = new NetworkSessionProperties();
            properties[(int)NetworkSessionManager.EnumSessionProperty.SessionType] = (int)NetworkSessionManager.EnumSessionType.Highscores;

            return properties;
        }

        private void ResetElapsedTime()
        {
            _elapsedTime = 0.0f;
        }

        /// <summary>
        /// Called by the game to tick the p2p highscore processing
        /// </summary>
        /// <param name="dt">elapsed time since last call to Tick()</param>
        public void Tick(float dt)
        {
            _elapsedTime += dt;
            _dt = dt;
            
            // silliness check - if there are no tasks to tick then we *ought* to
            // be in the idle state!  (can hang the game otherwise!)
            if (!AsyncTaskManager.Instance.HasTasks(TASKLIST_P2PHIGHSCORES) && _state != EnumP2PState.Idle)
            {
                Assert.Fatal(false, "P2P highscores - no tasks exists to be ticked and not in the idle state!");
                MoveToState(EnumP2PState.Idle, null);
            }

            // There will be a single async task to tick and this task will depend upon the current
            // p2p state.  We're using a tasklist here, but there will only be one task in practice.
            // The task list stuff has been tried and tested and there isn't too much overhead in
            // using it so no need to reinvent the wheel just yet.
            AsyncTaskManager.Instance.Tick(TASKLIST_P2PHIGHSCORES);
        }

        /// <summary>
        /// Will start the p2p exchange of highscores if it isn't already active
        /// </summary>
        public void Start()
        {
//#if XBOX
            Debug.WriteLine("P2P: trying to start");

#if SYSTEMLINK
            // system link so can always do this - no gold membership required
            if (true)
#else
            // can only do P2P highscore stuff if the player has a Gold account
            if ((Game.Instance.GetLocalPlayer().GamerRef as SignedInGamer).Privileges.AllowOnlineSessions)
#endif
            {
                _requestImmediateStop = false;  // cancel any stop request

                // if we are not already doing p2p stuff then start doing it
                if (_state == EnumP2PState.Idle)
                {
                    Debug.WriteLine("P2P: starting...");

                    // we'll start by finding a session to join
                    MoveToState(EnumP2PState.FindSession, null);
                }
            }
//#endif
        }

        /// <summary>
        /// Will stop the p2p exchange of highscores if it is active
        /// </summary>
        public void Stop()
        {
//#if XBOX
            Debug.WriteLine("P2P: stop() was called");

            if (_state != EnumP2PState.Idle)
            {
                Debug.WriteLine("P2P: stopping...");

                // request that whatever p2p stuff is running should quit running asap
                _requestImmediateStop = true;
            }
//#endif
        }

        /// <summary>
        /// Called to move the p2p stuff to a new state - this will create an async task of the right type
        /// that will then be ticked.
        /// </summary>
        private void MoveToState(EnumP2PState state, object param)
        {
            _state = state;

            switch (_state)
            {
                case EnumP2PState.Idle:
                    _requestImmediateStop = false;   // if any state is stopping due to requestStop then it will move to the idle state after it has stopped, so make sure we reset the flag here
                    ResetElapsedTime();
                                            // other than that, nothing else to do here - there isn't any task to tick in the idle state so no need to create a task here
                    break;

                case EnumP2PState.FindSession:
                    AsyncTaskManager.Instance.AddTask(AsyncTask_DoFindSession(), TASKLIST_P2PHIGHSCORES, true);
                    break;

                case EnumP2PState.JoinSession:
                    AsyncTaskManager.Instance.AddTask(AsyncTask_DoJoinSession(param as AvailableNetworkSession), TASKLIST_P2PHIGHSCORES, true);
                    break;

                case EnumP2PState.CreateSession:
                    AsyncTaskManager.Instance.AddTask(AsyncTask_DoCreateSession(), TASKLIST_P2PHIGHSCORES, true);
                    break;

                case EnumP2PState.ProcessClientSession:
                    AsyncTaskManager.Instance.AddTask(AsyncTask_ProcessClientSession(param as NetworkSession), TASKLIST_P2PHIGHSCORES, true);
                    break;

                case EnumP2PState.ProcessHostSession:
                    AsyncTaskManager.Instance.AddTask(AsyncTask_ProcessHostSession(param as NetworkSession), TASKLIST_P2PHIGHSCORES, true);
                    break;
            }
        }

        /// <summary>
        /// Asynchronous session finding.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_DoFindSession()
        {
            ResetElapsedTime();

            // wait until the delay waiting time is reached or we are requested to stop the p2p procesing
            while (_elapsedTime < DELAY_WAITBEFOREFINDINGSESSION && !_requestImmediateStop) yield return null;

            if (_requestImmediateStop)
            {
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            Debug.WriteLine("P2P trying to find a session");

            // Start the async session finding
            IAsyncResult result;
            
            try
            {
                result = NetworkSession.BeginFind(NetworkSessionManager.PLAYERMATCH, 1, GetSessionPropertiesPattern(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - so we're gonna bail out.
                // The game will keep running, but p2p highscores processing
                // will just return to the idle state.
                Assert.Warn(false, "Warning: BeginFind() threw an exception:\n" + e.Message);
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Wait until either the async session finding operation has finished or we've been requested to stop the p2p stuff
            while (!result.IsCompleted && !_requestImmediateStop) yield return null;

            // If the p2p has been requested to stop then *stop*
            if (_requestImmediateStop)
            {
                // manually stop the find operation
                try
                {
                    NetworkSession.EndFind(result);
                }
                catch (Exception e) // EndFind() is not documented as throwing any exceptions but it does!!!
                {
                    Assert.Warn(false, "Warning: could not get available sessions list - EndFind() threw an exception:\n" + e.Message);
                }

                // move to the idle state
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Else if there is a session available to join then move to the JoinSession state
            AvailableNetworkSessionCollection availableSessions;

            try
            {
                availableSessions = NetworkSession.EndFind(result);
            }
            catch (Exception e) // EndFind() is not documented as throwing any exceptions but it does!!!
            {
                Assert.Warn(false, "Warning: could not get available sessions list - EndFind() threw an exception:\n" + e.Message);
                availableSessions = null;
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            if (availableSessions != null && availableSessions.Count > 0)
            {
                AvailableNetworkSession sessionToJoin = availableSessions[_rnd.Next(0, availableSessions.Count)];
                MoveToState(EnumP2PState.JoinSession, sessionToJoin);
            }
            // Otherwise move to the CreateSession state to try to host a session ourselves
            else
            {
                MoveToState(EnumP2PState.CreateSession, null);
            }
        }

        /// <summary>
        /// Asynchronous joining of a session.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_DoJoinSession(AvailableNetworkSession sessionToJoin)
        {
            if (_requestImmediateStop)
            {
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            Debug.WriteLine("P2P trying to join a session");

            // Begin the async session join
            IAsyncResult result;

            try
            {
                result = NetworkSession.BeginJoin(sessionToJoin, null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - so we're gonna bail out.
                // The game will keep running, but p2p highscores processing
                // will just return to the idle state.
                Assert.Warn(false, "Warning: BeginJoin() threw an exception:\n" + e.Message);
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Wait until either the async session joining operation has finished or we've been requested to stop the p2p stuff
            while (!result.IsCompleted && !_requestImmediateStop) yield return null;

            // If the p2p has been requested to stop then *stop*
            if (_requestImmediateStop)
            {
                // manually stop the join operation - can throw exceptions so we make sure we catch them
                try
                {
                    NetworkSession.EndJoin(result);
                }
                catch (Exception e)
                {
                    // we don't really care if exceptions happened as we are quitting the join operation anyway;
                    // we just want to make sure any exceptions are caught so that the rest of the game
                    // will keep on running.
                    Assert.Warn(false, "Warning: EndJoin() threw an exception, whilst stopping the join operation:\n" + e.Message);
                }

                // move to the idle state
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Else if we managed to join the session then move to the InClientSession state
            try
            {
                NetworkSession session = NetworkSession.EndJoin(result);
                NetworkSessionManager.Instance.Session = session;
                MoveToState(EnumP2PState.ProcessClientSession, session);
            }
            // else we couldn't join the session for some reason, so have a go at hosting our own session instead
            catch (Exception e)
            {
                if (e is GamerPrivilegeException)
                {
                    Assert.Warn(false, "Warning: EndJoin() threw an exception, after the join operation completed: " + e.Message);
                    MoveToState(EnumP2PState.Idle, null);   // abort if we get this kind of exception because won't be able to do any other network p2p stuff anyway!
                }
                else
                {
                    Assert.Warn(false, "Warning: EndJoin() threw an exception, after the join operation completed: " + e.Message);
                    MoveToState(EnumP2PState.CreateSession, null);
                }
            }
        }

        /// <summary>
        /// Asynchronous creation of a session.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_DoCreateSession()
        {
            ResetElapsedTime();

            // wait until the delay waiting time is reached or we are requested to stop the p2p procesing
            while (_elapsedTime < DELAY_WAITBEFOREHOSTINGSESSION && !_requestImmediateStop) yield return null;

            if (_requestImmediateStop)
            {
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            Debug.WriteLine("P2P trying to create a session");

            // Start the async session creation
            IAsyncResult result;
            
            try
            {
                result = NetworkSession.BeginCreate(NetworkSessionManager.PLAYERMATCH, 1, 8, 0, GetSessionProperties(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - so we're gonna bail out.
                // The game will keep running, but p2p highscores processing
                // will just return to the idle state.
                Assert.Warn(false, "Warning: BeginCreate() threw an exception:\n" + e.Message);
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Wait until either the async session creation operation has finished or we've been requested to stop the p2p stuff
            while (!result.IsCompleted && !_requestImmediateStop) yield return null;

            // If the p2p has been requested to stop then *stop*
            if (_requestImmediateStop)
            {
                // manually stop the find operation
                try
                {
                    NetworkSession.EndCreate(result);
                }
                catch (Exception e) // EndCreate() is not documented as throwing any exceptions but it does!!!
                {
                    Assert.Warn(false, "Warning: could not get the session - EndCreate() threw an exception:\n" + e.Message);
                }

                // move to the idle state
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            // Else if we successfully created a session move to the ProcessHostSession state
            NetworkSession session;

            try
            {
                session = NetworkSession.EndCreate(result);
            }
            catch (Exception e) // EndCreate() is not documented as throwing any exceptions but it does!!!
            {
                Assert.Warn(false, "Warning: could not get the session - EndCreate() threw an exception:\n" + e.Message);
                session = null;
            }
            
            if (session != null)
            {
                NetworkSessionManager.Instance.Session = session;
                MoveToState(EnumP2PState.ProcessHostSession, session);
            }
            // Otherwise try to join someone else's session
            else
            {
                MoveToState(EnumP2PState.FindSession, null);
            }
        }

        /// <summary>
        /// Does the client processing for an active session.  Clients recieve data from the host;
        /// they do not broadcast any data themselves.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_ProcessClientSession(NetworkSession session)
        {
            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            Debug.WriteLine("P2P processing client session");

            // process messages and events until we have spent long enough in the session
            // or the session has stopped (or we've been asked to stop P2P)
            bool sessionEnded = false;
            float timeoutCounter = 0.0f;

            while (timeoutCounter < CONNECTION_CLIENT_TIMEOUT && !sessionEnded && !_requestImmediateStop)
            {
                timeoutCounter += _dt;

                // ...if session has ended then we're done here
                if (!NetworkSessionManager.Instance.IsValidSession)
                {
                    sessionEnded = true;
                    continue;
                }

                // ...process events
                ProcessSessionEvents();

                // ...process next message
                ProcessMessages();

                // ...integrate new score data
                if (_newScores != null)
                {
                    HighscoreData.PlayerScoreData scores = _newScores.Clone();

                    // ...one manageable chunk at a time so that it doesn't impact the
                    // ...performance of the game
                    int count = _newScores.SinglePlayerScores.Length;

                    for (int i = 0; i < count; i += SCORE_INTEGRATION_CHUNKSIZE)
                    {
                        //Debug.WriteLine("recieved i is " + i + " vs count " + count + " and chunksize " + SCORE_INTEGRATION_CHUNKSIZE);
                        HighscoreData.Instance.IntegrateNewSinglePlayerScores(_newScores.SinglePlayerScores, i, SCORE_INTEGRATION_CHUNKSIZE);
                        if (_requestImmediateStop) continue;    // if requested to stop then do so - it doesn't matter if we haven't integrated all the data (all it means is we have less score data than we would have had, but what we do have will still be valid)
                        yield return null;
                    }

                    count = _newScores.MultiplayerScores.Length;

                    for (int i = 0; i < count; i += SCORE_INTEGRATION_CHUNKSIZE)
                    {
                        HighscoreData.Instance.IntegrateNewMultiplayerScores(_newScores.MultiplayerScores, i, SCORE_INTEGRATION_CHUNKSIZE);
                        if (_requestImmediateStop) continue;    // if requested to stop then do so - it doesn't matter if we haven't integrated all the data (all it means is we have less score data than we would have had, but what we do have will still be valid)
                        yield return null;
                    }

                    _newScores = null;
                }

                yield return null;
            }

            if (NetworkSessionManager.Instance.IsValidSession)
            {
                NetworkSessionManager.Instance.ShutdownP2PHighscoresSession();
            }

            // if we were requested to stop processing p2p stuff then move to the idle state
            if (_requestImmediateStop)
            {
                MoveToState(EnumP2PState.Idle, null);
            }
            // else either we recieved X broadcasts or the host quit the session, so
            // either look for another session or host our own session
            else
            {
                if (_rnd.Next(3) == 0)  // 33% chance we will decide to host our own session
                {
                    MoveToState(EnumP2PState.CreateSession, null);
                }
                else
                {
                    MoveToState(EnumP2PState.FindSession, null);
                }
            }
        }

        /// <summary>
        /// Does the host processing for an active session.  Host broadcasts data to clients;
        /// does not recieve score data from clients.  (we read/write the dictionaries directly
        /// and they cannot be modified whilst we are enumerating them hence the host/client split
        /// on uploading/downloading data so things do not mess up - there are alternative approaches,
        /// but this one is simple and sufficient enough for our current needs)
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_ProcessHostSession(NetworkSession session)
        {
            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                MoveToState(EnumP2PState.Idle, null);
                yield break;
            }

            Debug.WriteLine("P2P processing host session");

            // process messages and events until we have spent long enough in the session
            // or the session has stopped (or we've been asked to stop P2P)
            bool sessionEnded = false;
            bool timeToQuitHosting = false;
            float timeoutCounter = 0.0f;

            while (!timeToQuitHosting && !sessionEnded && !_requestImmediateStop)
            {
                timeoutCounter += _dt;

                // ...if session has ended then we're done here
                if (!NetworkSessionManager.Instance.IsValidSession)
                {
                    sessionEnded = true;
                    continue;
                }

                // ...process events
                ProcessSessionEvents();

                // ...if session has ended then we're done here (checking again in case session got shutdown by an event)
                if (!NetworkSessionManager.Instance.IsValidSession)
                {
                    sessionEnded = true;
                    continue;
                }

                // ...we just ignore any data (there shouldn't be any messages, but if there are just ignore them)
                GetNextNetworkMessage();
                _networkMessage.Consume();

                // ...send our own data out to whoever is listening
                if (_elapsedTime >= DELAY_UPLOADDATA)
                {
                    ResetElapsedTime();

                    HighscoreData.PlayerScoreData data = GetNextDataChunkForUploading();

                    if (data != null)
                    {
                        Debug.WriteLine("Host is sending score data");
                        NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.Highscores, data);
                        LocalNetworkGamer sender = NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer;
                        NetworkSessionManager.Instance.SendDataToAll(msg, sender);
                    }
                    else
                    {
                        // we've sent all our data, but is it time to quit hosting?  (otherwise just start sending again from the beginning)                        
                        timeToQuitHosting = (timeoutCounter >= CONNECTION_HOST_TIMEOUT);
                    }
                }

                yield return null;
            }

            if (NetworkSessionManager.Instance.IsValidSession)
            {
                NetworkSessionManager.Instance.ShutdownP2PHighscoresSession();
            }

            // if we were requested to stop processing p2p stuff then move to the idle state
            if (_requestImmediateStop)
            {
                MoveToState(EnumP2PState.Idle, null);
            }
            // else either we sent X broadcasts or the session ended prematurely so
            // look for a session to join
            else
            {
                MoveToState(EnumP2PState.FindSession, null);
            }
        }

        private void ProcessSessionEvents()
        {
            // if the session isn't valid then don't process any events as the session already shutdown
            if (!NetworkSessionManager.Instance.IsActiveSession) return;

            // pull the next event from the queue and process it
            if (NetworkSessionManager.Instance.EventCount == 0) return;

            SessionEventInfo eventInfo = NetworkSessionManager.Instance.GetNextEvent();

            // only interested in session ending
            if (eventInfo.Type == SessionEventInfo.EnumSessionEvent.SessionEnded)
            {
                NetworkSessionManager.Instance.ShutdownP2PHighscoresSession();
            }
        }

        /// <summary>
        /// Will get the next available network message if the P2P highscores network message
        /// property isn't already filled with a valid message.
        /// </summary>
        protected void GetNextNetworkMessage()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            if (_networkMessage.Type == NetworkMessage.EnumType.Undefined)
            {
                LocalNetworkGamer localGamer;

                if (NetworkSessionManager.Instance.IsHosting())
                {
                    localGamer = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                }
                else
                {
                    localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];
                }

                if (localGamer.IsDataAvailable)
                {
                    NetworkSessionManager.Instance.RecieveDataFromAny(_networkMessage, localGamer);

                    // ignore messages from ourselves
                    if (_networkMessage.Sender.Gamertag == Game.Instance.Gamertag)
                    {
                        _networkMessage.Consume();
                    }
                }
            }
        }

        private void ProcessMessages()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            GetNextNetworkMessage();

            switch (_networkMessage.Type)
            {
                case NetworkMessage.EnumType.Highscores:
                    _newScores = _networkMessage.Data as HighscoreData.PlayerScoreData;
                    break;

                case NetworkMessage.EnumType.Undefined:
                    // do nothing
                    break;

                default:
                    Assert.Warn(false, "p2p highscores: Network message not valid in this context: " + _networkMessage.Type);
                    break;
            }

            _networkMessage.Consume();
        }

        /// <summary>
        /// Returns the next chunk of score data.  Returns null if no data left.
        /// If 'reset' is true then the data reading will be reset to the start.
        /// </summary>
        private HighscoreData.PlayerScoreData GetNextDataChunkForUploading()
        {
            HighscoreData.PlayerScoreData scoreData = new HighscoreData.PlayerScoreData();
            scoreData.SinglePlayerScores = new HighscoreData.SinglePlayerScoreData[SCORE_UPLOAD_CHUNKSIZE];
            scoreData.MultiplayerScores = new HighscoreData.MultiplayerScoreData[SCORE_UPLOAD_CHUNKSIZE];
            int spCount = 0;
            int mpCount = 0;
            int index = 0;

            for (int i = 0; i < SCORE_UPLOAD_CHUNKSIZE; i++)
            {
                bool foundData = false;
                index = _nextScoreIndexToUpload + i;

                if (index < HighscoreData.Instance.SinglePlayerScores.Count)
                {
                    scoreData.SinglePlayerScores[i] = HighscoreData.Instance.SinglePlayerScores[index];
                    spCount++;
                    foundData = true;
                }

                if (index < HighscoreData.Instance.MultiplayerScores.Count)
                {
                    scoreData.MultiplayerScores[i] = HighscoreData.Instance.MultiplayerScores[index];
                    mpCount++;
                    foundData = true;
                }

                if (!foundData) break;
            }

            if (spCount == 0 && mpCount == 0)
            {
                _nextScoreIndexToUpload = 0;
                return null;
            }
            else
            {
                _nextScoreIndexToUpload = index + 1;
                scoreData.SpCount = spCount;
                scoreData.MpCount = mpCount;
                return scoreData;
            }
        }
    }
} 
