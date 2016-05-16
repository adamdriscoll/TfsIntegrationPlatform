// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    [Serializable]
    public class ClearQuestConnectionConfig
    {
        public ClearQuestConnectionConfig(
            string user,
            string pwd,
            string userDb,
            string dbSet)
        {
            if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(userDb))
            {
                throw new ArgumentNullException("userDb");
            }

            User = user;
            Password = pwd;
            UserDB = userDb;
            DBSet = dbSet;
        }

        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public ClearQuestConnectionConfig()
        {
        }

        /// <summary>
        /// User name.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// User DB
        /// </summary>
        public string UserDB { get; set; }

        /// <summary>
        /// DB connection
        /// *NOTE* that for a local CQ installation
        /// user can connect to CQ without providing a connection
        /// </summary>
        public string DBSet { get; set; }
    }
}
