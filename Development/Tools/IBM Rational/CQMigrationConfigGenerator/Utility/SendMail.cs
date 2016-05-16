// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Utility class for sending mail through SMTP

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Security.Principal;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Converters.Utility
{
    public class SendMail
    {
        private string m_fromAddress;
        private string m_body;
        private string m_subject;
        private SmtpClient m_smtpClient;

        static char[] delims = new char[] { ';', ',' };

        internal void SendEmail(string smtpServer, string from, string to,
            string subject, string body, string [] attachment)
        {
            Display.DisplayMessage("Sending Mail....");
            Logger.Write(LogSource.Common, TraceLevel.Info, "Sending Mail");

            //TODO- Validate all the inputs

            m_fromAddress = from;
            m_body = body;
            m_subject = subject;


            m_smtpClient = new SmtpClient(smtpServer);
            m_smtpClient.UseDefaultCredentials = true;
            m_smtpClient.Timeout = 10000; //10 secs time out

            string[] addressArray = to.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            foreach (string address in addressArray)
            {
                SendOneMail(address, attachment);
            }
            Logger.Write(LogSource.Common, TraceLevel.Verbose, "Done Sending Mail");
        }

        private void SendOneMail(string to, string [] attachment)
        {
            Logger.Write(LogSource.Common, TraceLevel.Verbose, "Sending Mail to {0}", to);

            MailMessage message = new MailMessage(m_fromAddress, to);
            message.Body = m_body;
            message.Subject = m_subject;

            if (attachment != null)
            {
                for (int i = 0; i < attachment.Length; i++)
                {
                    message.Attachments.Add(new Attachment(attachment[i]));
                }
            }

            try
            {
                m_smtpClient.Send(message);
            }
            catch (SmtpFailedRecipientsException e)
            {
                Logger.Write(LogSource.Common, TraceLevel.Error,
                    "Error in sending mail to - {0}. Error - {1}", e.FailedRecipient, e.Message);
            }
            catch (SmtpException e)
            {
                Logger.Write(LogSource.Common, TraceLevel.Error,
                    "Error in send mail - {0}", e.Message);
            }
        }
    }
}
