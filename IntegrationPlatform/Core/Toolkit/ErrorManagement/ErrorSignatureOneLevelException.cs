// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This signature only matches the exception and optionally matches the message of the exception.
    /// </summary>
    public class ErrorSignatureOneLevelException : ErrorSignatureBase
    {
        protected string m_exceptionType = null;
        protected string m_exceptionMessage = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionType"></param>
        public ErrorSignatureOneLevelException(Type exceptionType)
        {
            if (null == exceptionType)
            {
                throw new ArgumentNullException("exceptionType");
            }

            Initialize(exceptionType.FullName, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionTypeFullName"></param>
        internal ErrorSignatureOneLevelException(string exceptionTypeFullName)
        {
            Initialize(exceptionTypeFullName, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionType"></param>
        /// <param name="exceptionMessage"></param>
        internal ErrorSignatureOneLevelException(Type exceptionType, string exceptionMessage)
        {
            if (null == exceptionType)
            {
                throw new ArgumentNullException("exceptionType");
            }

            Initialize(exceptionType.FullName, exceptionMessage);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionTypeFullName"></param>
        /// <param name="exceptionMessage"></param>
        public ErrorSignatureOneLevelException(string exceptionTypeFullName, string exceptionMessage)
        {
            Initialize(exceptionTypeFullName, exceptionMessage);
        }

        private void Initialize(string exceptionTypeFullName, string exceptionMessage)
        {
            m_exceptionType = exceptionTypeFullName;
            m_exceptionMessage = exceptionMessage;
        }

        public override bool Matches(Exception e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            if (ExceptionMatchesType(e, m_exceptionType))
            {
                if (string.IsNullOrEmpty(m_exceptionMessage) || m_exceptionMessage == WildcardAny)
                {
                    return true;
                }
                else
                {
                    return !string.IsNullOrEmpty(e.Message) && e.Message.Contains(m_exceptionMessage);
                }
            }

            return false;
        }

        protected bool ExceptionMatchesType(Exception e, string exceptionTypeFullName)
        {
            return (exceptionTypeFullName == WildcardAny) 
                || (e.GetType().FullName.Equals(exceptionTypeFullName, StringComparison.OrdinalIgnoreCase));
        }

        public override int CompareTo(object obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            if (obj is ErrorSignatureTwoLevelException)
            {
                // two level signature always are more specific
                return 1;
            }
            else if (obj is ErrorSignatureOneLevelException)
            {
                ErrorSignatureOneLevelException oneLvlSig = obj as ErrorSignatureOneLevelException;
                Debug.Assert(null != oneLvlSig, "null == oneLvlSig");

                int retVal = 0;
                if (oneLvlSig.m_exceptionType.Equals(m_exceptionType, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage) && string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage))
                    {
                        return retVal;
                    }
                    else if (!string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage) && string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage))
                    {
                        return 1;
                    }
                    else if (string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage) && !string.IsNullOrEmpty(oneLvlSig.m_exceptionMessage))
                    {
                        return -1;
                    }
                    else
                    {
                        return m_exceptionMessage.CompareTo(oneLvlSig.m_exceptionMessage);
                    }
                }
                else
                {
                    if (m_exceptionType.Equals(WildcardAny, StringComparison.OrdinalIgnoreCase))
                    {
                        // always shuffle wildcard to the end
                        return 1;
                    }
                    else
                    {
                        retVal = oneLvlSig.m_exceptionType.CompareTo(m_exceptionType);
                    }
                }

                return retVal;
            }
            else
            {
                // give ourselves privilege
                return -1;
            }
        }
    }
}
