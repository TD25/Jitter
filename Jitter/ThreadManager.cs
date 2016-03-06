/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
* 
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution. 
*/

#region Using Statements
using System;
using System.Collections.Generic;

using Jitter.Dynamics;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace Jitter
{

    /// <summary>
    /// Jitters ThreadManager class handles the internal multithreading of the
    /// engine.
    /// </summary>
    public class ThreadManager
    {
        public const int ThreadsPerProcessor = 1;

        volatile List<Action<object>> tasks = new List<Action<object>>();
        volatile List<object> parameters = new List<object>();

        private int currentTaskIndex;

        internal int threadCount;

        /// <summary>
        /// Get the number of threads used by the ThreadManager to execute
        /// tasks.
        /// </summary>
        public int ThreadCount { private set { this.threadCount = value; } get { return threadCount; } }

        static ThreadManager instance = null;

        public static ThreadManager Instance 
        { 
            get 
            {
                if (instance == null)
                {
                    instance = new ThreadManager();
                    instance.Initialize();
                }

                return instance;
            }
        }

        private ThreadManager() { }

        private void Initialize()
        {

#if WINDOWS_PHONE
            ThreadCount = 2;
#else
            threadCount = System.Environment.ProcessorCount * ThreadsPerProcessor;
#endif
        }

        /// <summary>
        /// Executes all tasks previously added to the ThreadManager.
        /// The method finishes when all tasks are complete.
        /// </summary>
        public void Execute()
        {
            if (tasks.Count <= 0)
                return;

            currentTaskIndex = 0;

            List<Task> threads = new List<Task>();
            for (int i = 1; i < threadCount && i < tasks.Count; i++)
            {
                threads.Add(Task.Run(() =>
                {
                    PumpTasks();
                }));
            }

            PumpTasks();

            Task.WaitAll(threads.ToArray());

            tasks.Clear();
            parameters.Clear();
        }

        /// <summary>
        /// Adds a task to the ThreadManager. The task and the parameter
        /// is added to an internal list. Call <see cref="Execute"/>
        /// to execute and remove the tasks from the internal list.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="param"></param>
        public void AddTask(Action<object> task, object param)
        {
            tasks.Add(task);
            parameters.Add(param);
        }

        private void PumpTasks()
        {
            int count = tasks.Count;

            while (currentTaskIndex < count)
            {
                int taskIndex = currentTaskIndex;

                if (taskIndex == Interlocked.CompareExchange(ref currentTaskIndex, taskIndex + 1, taskIndex)
                    && taskIndex < count)
                {
                    tasks[taskIndex](parameters[taskIndex]);
                }
            }
        }

    }

}
