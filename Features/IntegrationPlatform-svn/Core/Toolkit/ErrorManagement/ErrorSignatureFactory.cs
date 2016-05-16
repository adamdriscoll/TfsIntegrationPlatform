// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This helper class understands the error signature settings in the error management configuration
    /// section, and helps create the corresponding ErrorSignatureBase instances.
    /// </summary>
    static class ErrorSignatureFactory
    {
        public static ErrorSignatureBase CreateErrorSignaure(BM.Signature errorSignatureConfig)
        {
            if (errorSignatureConfig == null || string.IsNullOrEmpty(errorSignatureConfig.Exception))
            {
                return null;
            }

            if (string.IsNullOrEmpty(errorSignatureConfig.InnerException))
            {
                // one level exception/error signature
                return new ErrorSignatureOneLevelException(
                    errorSignatureConfig.Exception, errorSignatureConfig.Message);
            }
            else
            {
                return new ErrorSignatureTwoLevelException(
                    errorSignatureConfig.Exception, errorSignatureConfig.Message,
                    errorSignatureConfig.InnerException, errorSignatureConfig.InnerMessage);
            }
        }
    }
}
