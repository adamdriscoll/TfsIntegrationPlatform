// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.CompressCmd
{
    internal class CompressCmd : CommandBase
    {
        const string DurationSwitchL = "/Duration:";    // in minutes
        const string DurationSwitchS = "/D:";           // in minutes

        int m_durationInMin;
        bool m_execInTimeWindow;
        Exception m_exceptionThrownByWorker = null;
        bool m_cancelKeyPressed = false;
        object m_cancelKeyPressedLock = new object();
        RuntimeEntityModel m_context = RuntimeEntityModel.CreateInstance();

        public override string CommandName
        {
            get { return "Compress"; /* do not localize */ }
        }

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length == 1
                && (cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch1, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch2, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch3, StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp = true;
                return true;
            }
            else
            {
                return TryParseAsCompressOptions(cmdSpecificArgs);
            }
        }
        
        public override Interfaces.ICommandResult Run()
        {
            try
            {
                Thread worker = new Thread(DoCompression);
                worker.IsBackground = true;
                worker.Start();

                Console.CancelKeyPress += delegate
                {
                    // The application terminates directly after executing this delegate

                    CancelKeyPressed = true;
                    worker.Join(1000 * 60); // wait for 60 seconds

                    Console.WriteLine(CreateExecutionResult().Print());
                };

                while (worker.IsAlive)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                m_exceptionThrownByWorker = e;
            }

            return CreateExecutionResult();
        }

        private bool TryParseAsCompressOptions(string[] cmdSpecificArgs)
        {
            bool durationOptionFound = false;

            foreach (string cmdOption in cmdSpecificArgs)
            {
                if (cmdOption.StartsWith(DurationSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (durationOptionFound)
                    {
                        return false;
                    }

                    string durationStr;
                    if (TryParseArg(cmdOption, DurationSwitchL, out durationStr)
                        && int.TryParse(durationStr, out m_durationInMin))
                    {
                        durationOptionFound = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (cmdOption.StartsWith(DurationSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (durationOptionFound)
                    {
                        return false;
                    }

                    string durationStr;
                    if (TryParseArg(cmdOption, DurationSwitchS, out durationStr)
                        && int.TryParse(durationStr, out m_durationInMin))
                    {
                        durationOptionFound = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            m_execInTimeWindow = durationOptionFound;
            return true;
        }

        private bool CancelKeyPressed
        {
            get
            {
                lock (m_cancelKeyPressedLock)
                {
                    return m_cancelKeyPressed;
                }
            }
            set
            {
                lock (m_cancelKeyPressedLock)
                {
                    m_cancelKeyPressed = true;
                }
            }
        }                   

        private Interfaces.ICommandResult CreateExecutionResult()
        {
            return new CompressRslt(m_exceptionThrownByWorker, this);
        }

        private void DoCompression()
        {
            try
            {
                DateTime startTime = DateTime.Now;

                Console.WriteLine(ResourceStrings.CancelKeyInfo);
                Console.WriteLine();

                int remainingGroupCount = 0;
                int batchIndex = 0;
                do
                {
                    

                    Console.WriteLine("Deleting processed change group: Batch " + (++batchIndex).ToString());
                    remainingGroupCount = SqlHandler.ExecuteScalar<int>(
                        GlobalConfiguration.TfsMigrationDbConnectionString, SqlScripts.BatchDeleteProcessedMigrationInstruction, null);
                } while (CanProceed(startTime, remainingGroupCount));
            }
            catch (Exception e)
            {
                m_exceptionThrownByWorker = e;
            }
        }

        private void IsAnyGroupRunning()
        {
            int completed = (int)BusinessModelManager.SessionStateEnum.Completed;
            int oneTimeCompleted = (int)BusinessModelManager.SessionStateEnum.OneTimeCompleted;
            int markedDeleted = (int)BusinessModelManager.SessionStateEnum.MarkedForDeletion;
            int initialized = (int)BusinessModelManager.SessionStateEnum.Initialized;

            var rtSessionGroup = m_context.RTSessionGroupSet.Where(g => 
                g.State != completed && g.State != oneTimeCompleted && g.State != markedDeleted && g.State == initialized);

            if (rtSessionGroup.Count() > 0)
            {
                throw new InvalidOperationException(ResourceStrings.ErrorSessionGroupIsRunning);
            }
        }

        private bool CanProceed(DateTime startTime, int remainingGroupCount)
        {
            if (CancelKeyPressed)
            {
                return false;
            }

            if (m_execInTimeWindow)
            {
                if (remainingGroupCount > 0)
                {
                    return DateTime.Now.Subtract(startTime) < new TimeSpan(TimeSpan.TicksPerMinute * m_durationInMin);
                }
                else
                {
                    // no more work to do
                    return false;
                }
            }
            else
            {
                // only stop when there is no more work to do
                return remainingGroupCount > 0;
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Delete all the processed data in the integration platform Database:");
            sb.AppendFormat("  {0} {1} [{2}<duration of execution in minutes>]", Program.ProgramName, CommandName, DurationSwitchL);
            sb.AppendFormat("  {0} {1} [{2}<duration of execution in minutes>]", Program.ProgramName, CommandName, DurationSwitchS);
            return sb.ToString();
        }
    }
}
