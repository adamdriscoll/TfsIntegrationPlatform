// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation;
using System.Text;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// This exception is raised when a configuration is invalid.
    /// </summary>
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException()
        {
        }

        public InvalidConfigurationException(string msg)
            :base(msg)
        {
        }

        public InvalidConfigurationException(string msg, Exception innerException)
            :base(msg, innerException)
        {

        }
    }

    /// <summary>
    /// This exception is raised when a configuration does not conform to the configuration schema.
    /// </summary>
    public class ConfigurationSchemaViolationException : InvalidConfigurationException
    {
        public ConfigurationValidationResult ConfigurationValidationResult
        {
            get;
            private set;
        }

        public ConfigurationSchemaViolationException(ConfigurationValidationResult configResult)
        {
            ConfigurationValidationResult = configResult;
        }

        public override string Message
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < ConfigurationValidationResult.SchemaValidationResults.Count; i++)
                {
                    stringBuilder.AppendLine(string.Format("{0}. {1}", i + 1, ConfigurationValidationResult.SchemaValidationResults[i].Message));
                }
                return stringBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// This exception is raised when a configuration fails the business rule evaluation.
    /// </summary>
    public class ConfigurationBusinessRuleViolationException : InvalidConfigurationException
    {
        public EvaluationResult ConfigurationValidationResult
        {
            get;
            private set;
        }

        public ConfigurationBusinessRuleViolationException(EvaluationResult configResult)
        {
            ConfigurationValidationResult = configResult;
        }

        public override string Message
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < ConfigurationValidationResult.ResultItems.Count; i++)
                {
                    stringBuilder.AppendLine(string.Format("{0}. {1}", i + 1, ConfigurationValidationResult.ResultItems[i].ToString()));
                }
                return stringBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// This exception is raised when a configuration fails the business rule evaluation.
    /// </summary>
    public class MismatchingMigrationSourceNativeIdException : InvalidConfigurationException
    {
        public string CurrentMigrationSourceNativeId
        {
            get;
            private set;
        }

        public string NewMigrationSourceNativeId
        {
            get;
            private set;
        }

        public MismatchingMigrationSourceNativeIdException(
            string currentMigrationSourceNativeId,
            string newMigrationSourceNativeId)
        {
            CurrentMigrationSourceNativeId = currentMigrationSourceNativeId;
            NewMigrationSourceNativeId = newMigrationSourceNativeId;
        }
    }
}
