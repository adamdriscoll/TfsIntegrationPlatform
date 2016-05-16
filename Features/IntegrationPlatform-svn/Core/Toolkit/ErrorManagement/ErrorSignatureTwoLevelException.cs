// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This signature matches the exception and its inner exception.
    /// </summary>
    public class ErrorSignatureTwoLevelException : ErrorSignatureOneLevelException
    {
        protected string m_innerExceptionType = null;
        protected string m_innerExceptionMessage = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionType"></param>
        /// <param name="innerExceptionType"></param>
        public ErrorSignatureTwoLevelException(
            Type exceptionType, 
            Type innerExceptionType)
            : base(exceptionType)
        {
            if (null == innerExceptionType)
            {
                throw new ArgumentNullException("innerExceptionType");
            }

            Initialize(innerExceptionType.FullName, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionTypeFullName"></param>
        /// <param name="innerExceptionTypeFullName"></param>
        public ErrorSignatureTwoLevelException(
            string exceptionTypeFullName,
            string innerExceptionTypeFullName)
            : base(exceptionTypeFullName)
        {
            Initialize(innerExceptionTypeFullName, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionType"></param>
        /// <param name="exceptionMessage"></param>
        /// <param name="innerExceptionType"></param>
        /// <param name="innerExceptionMessage"></param>
        public ErrorSignatureTwoLevelException(
            Type exceptionType, 
            string exceptionMessage, 
            Type innerExceptionType, 
            string innerExceptionMessage)
            : base(exceptionType, exceptionMessage)
        {
            if (null == innerExceptionType)
            {
                throw new ArgumentNullException("innerExceptionType");
            }

            Initialize(innerExceptionType.FullName, innerExceptionMessage);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exceptionTypeFullName"></param>
        /// <param name="exceptionMessage"></param>
        /// <param name="innerExceptionTypeFullName"></param>
        /// <param name="innerExceptionMessage"></param>
        public ErrorSignatureTwoLevelException(
            string exceptionTypeFullName,
            string exceptionMessage,
            string innerExceptionTypeFullName,
            string innerExceptionMessage)
            : base(exceptionTypeFullName, exceptionMessage)
        {
            Initialize(innerExceptionTypeFullName, innerExceptionMessage);
        }

        private void Initialize(string innerExceptionTypeFullName, string innerExceptionMessage)
        {
            if (string.IsNullOrEmpty(innerExceptionTypeFullName))
            {
                throw new ArgumentNullException("innerExceptionTypeFullName");
            }

            m_innerExceptionType = innerExceptionTypeFullName;
            m_innerExceptionMessage = innerExceptionMessage;
        }

        public override bool Matches(Exception e)
        {
            if (base.Matches(e) && null != e.InnerException)
            {
                if (ExceptionMatchesType(e.InnerException, m_innerExceptionType))
                {
                    if (string.IsNullOrEmpty(m_innerExceptionMessage) || m_innerExceptionMessage == WildcardAny)
                    {
                        return true;
                    }
                    else
                    {
                        return !string.IsNullOrEmpty(e.InnerException.Message) 
                            && e.InnerException.Message.Contains(m_innerExceptionMessage);
                    }
                }
            }

            return false;
        }
    }
}
