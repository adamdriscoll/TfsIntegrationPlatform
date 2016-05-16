// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class MigrationSqlTransaction : MigrationTransaction
    {
        RuntimeEntityModel m_context;

        internal MigrationSqlTransaction()
        {
            m_context = RuntimeEntityModel.CreateInstance();
        }

        public override void Complete()
        {
            m_context.TrySaveChanges();
        }

        /*public SqlCommand CreateCommand()
        {
            SqlCommand cmd = m_conn.CreateCommand();
            cmd.Transaction = m_tran;

            return cmd;
        }
        */

        protected override void Dispose(bool disposing)
        {
           /* try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    try
                    {
                        if (!m_isComplete)
                        {
                            m_context.
                        }

                        m_tran.Dispose();
                    }
                    finally
                    {
                        m_conn.Dispose();
                    }
                }
            }*/
        }
    }
}
