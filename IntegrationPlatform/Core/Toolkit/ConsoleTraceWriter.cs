// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// TraceWriter that writes the trace messages to the Console.
    /// </summary>
    internal class ConsoleTraceWriter : TraceWriterBase
    {
        /// <summary>
        /// Gets the name of this trace writer.
        /// </summary>
        public override string Name
        {
            get
            {
                return "ConsoleTraceWriter";
            }
        }

        /// <summary>
        /// Writes a line of trace message.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("[{0}] {1} ", DateTime.Now, message));

            Console.WriteLine(sb.ToString());
        }

        protected override void WriteTraceEntries(List<string> traceEntries)
        {
            foreach (string line in traceEntries)
            {
                WriteLine(line);
            }
        }
    }
}
