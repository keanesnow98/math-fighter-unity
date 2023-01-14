using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;

namespace MathFreak.AsyncTaskFramework
{
    /// <summary>
    /// This class uses the way Enumerators work in C#2.0 and above in order to allow asynchronous
    /// tasks to be coded in a way that is more expressive - particularly in regard to sequences of
    /// events where between the events it is okay to 'yield' to other processing.
    /// 
    /// There can be multiple named task lists.
    /// </summary>
    public class AsyncTaskManager
    {
        private const string PREFIX_PENDING = "pending_";

        private static AsyncTaskManager _instance;

        public static AsyncTaskManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AsyncTaskManager();
                }

                return _instance;
            }
        }

        private Dictionary<String, List<AsyncTaskInfo>> _taskLists;

        private AsyncTaskManager()
        {
            _taskLists = new Dictionary<String, List<AsyncTaskInfo>>();
        }

        // create a new named list if it doesn't already exist
        public bool NewTaskList(String listName)
        {
            if (_taskLists.ContainsKey(listName))
            {
                return false;
            }
            else
            {
                Assert.Fatal(!listName.StartsWith(PREFIX_PENDING), "the prefix 'pending_' is reserved internally for use by the asynchronous taskmanager");

                _taskLists.Add(listName, new List<AsyncTaskInfo>());
                _taskLists.Add(PREFIX_PENDING + listName, new List<AsyncTaskInfo>());
                return true;
            }
        }

        // add a task
        public void AddTask(IEnumerator<AsyncTaskStatus> task, String listName, bool startImmediately)
        {
            Assert.Fatal(_taskLists.ContainsKey(listName), "Trying to add a task to non-existent task list: " + listName);

            AsyncTaskInfo taskinfo = new AsyncTaskInfo(task);
            _taskLists[PREFIX_PENDING + listName].Add(taskinfo);

            if (startImmediately)
            {
                if (!taskinfo.Task.MoveNext())
                {
                    taskinfo.markForDisposal = true;
                }
            }
        }

        // kill a task early
        public bool KillTask(IEnumerator<AsyncTaskStatus> task, String listName)
        {
            Assert.Fatal(_taskLists.ContainsKey(listName), "Trying to kill a task in a task list that does not exist: " + listName);

            List<AsyncTaskInfo> taskList = _taskLists[listName];

            foreach (AsyncTaskInfo t in taskList)
            {
                if (t.Task == task)
                {
                    t.markForDisposal = true;
                    return true;
                }
            }

            List<AsyncTaskInfo> pendingTaskList = _taskLists[PREFIX_PENDING + listName];

            foreach (AsyncTaskInfo t in pendingTaskList)
            {
                if (t.Task == task)
                {
                    t.markForDisposal = true;
                    return true;
                }
            }

            return false;
        }

        // kills the task - will search all lists for the task
        public bool KillTask(IEnumerator<AsyncTaskStatus> task)
        {
            foreach (KeyValuePair<string, List<AsyncTaskInfo>> taskList in _taskLists)
            {
                if (!taskList.Key.StartsWith(PREFIX_PENDING))
                {
                    if (KillTask(task, taskList.Key)) return true;
                }
            }

            return false;
        }

        // kills all the tasks in a list
        public void KillAllTasks(String listName, bool disposeTasksImediately)
        {
            Assert.Fatal(_taskLists.ContainsKey(listName), "Trying to kill tasks in a non-existent task list: " + listName);
            List<AsyncTaskInfo> taskList = _taskLists[listName];
            List<AsyncTaskInfo> pendingTaskList = _taskLists[PREFIX_PENDING + listName];

            // mark all tasks for disposal
            foreach (AsyncTaskInfo t in taskList)
            {
                t.markForDisposal = true;
            }

            foreach (AsyncTaskInfo t in pendingTaskList)
            {
                t.markForDisposal = true;
            }

            if (disposeTasksImediately)
            {
                // tick the task list - which will remove all the tasks for us since they are marked for disposal
                Tick(listName);
            }
        }

        public void Tick(String listName)
        {
            Assert.Fatal(_taskLists.ContainsKey(listName), "Trying to tick a non-existent task list: " + listName);
            List<AsyncTaskInfo> taskList = _taskLists[listName];
            List<AsyncTaskInfo> pendingTaskList = _taskLists[PREFIX_PENDING + listName];

            // move any pending tasks to the task list proper
            if (pendingTaskList.Count > 0)
            {
                foreach (AsyncTaskInfo t in pendingTaskList)
                {
                    taskList.Add(t);
                }

                pendingTaskList.Clear();
            }

            // tick the tasks
            List<AsyncTaskInfo> disposalList = new List<AsyncTaskInfo>();

            foreach (AsyncTaskInfo t in taskList)
            {
                if (t.markForDisposal)
                {
                    disposalList.Add(t);
                    continue;
                }

                if (!t.Task.MoveNext())
                {
                    t.markForDisposal = true;
                }
            }

            // dispose of some tasks if necessary
            if (disposalList.Count > 0)
            {
                foreach (AsyncTaskInfo t in disposalList)
                {
                    t.Task.Dispose();
                    taskList.Remove(t);
                }
            }
        }

        public bool HasTasks(string listName)
        {
            Assert.Fatal(_taskLists.ContainsKey(listName), "Querying the tasklist for a list that does not exist: " + listName);
            List<AsyncTaskInfo> taskList = _taskLists[listName];
            List<AsyncTaskInfo> pendingTaskList = _taskLists[PREFIX_PENDING + listName];

            return (taskList.Count + pendingTaskList.Count) > 0;
        }



        private class AsyncTaskInfo
        {
            public AsyncTaskInfo(IEnumerator<AsyncTaskStatus> task)
            {
                Task = task;
                markForDisposal = false;
            }

            public IEnumerator<AsyncTaskStatus> Task;
            public bool markForDisposal;
        }
    }

    // not used yet and might never be
    public class AsyncTaskStatus
    {
    }
}