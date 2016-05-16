// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class TextBoxTraceWriter : TraceWriterBase
    {
        private readonly int m_bufferSize = 100;
        private TextBox m_textBox;

        public TextBoxTraceWriter(TextBox textBox)
            : base()
        {
            m_textBox = textBox;
        }

        /// <summary>
        /// Gets the name of this trace writer.
        /// </summary>
        public override string Name
        {
            get
            {
                return "TextBoxTraceWriter";
            }
        }

        /// <summary>
        /// Writes a line of trace message.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
            m_textBox.Dispatcher.Invoke(new Action(
                delegate()
                {
                    int lineOverflow = m_textBox.LineCount - m_bufferSize;
                    if (lineOverflow > 0)
                    {
                        m_textBox.Text = m_textBox.Text.Substring(m_textBox.GetCharacterIndexFromLineIndex(lineOverflow));
                    }
                    m_textBox.AppendText(message);
                }
            ));
        }

        protected override void WriteTraceEntries(List<string> traceEntries)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in traceEntries)
            {
                sb.AppendLine(String.Format("[{0}] {1} ", DateTime.Now, line));
            }
            WriteLine(sb.ToString());
        }
    }
}
