// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.TeamFoundation.Migration.Toolkit.VC
{
    internal class SqlMigrationAction : MigrationAction
    {
        public SqlMigrationAction(ChangeGrouping parent)
            : base(parent)
        {
        }

        public SqlMigrationAction(ChangeGrouping parent, int actionId)
            : base(parent, actionId)
        {
        }

        internal override void CreateNew()
        {
            using (IMigrationTransaction trx = DataAccessManager.Current.StartTransaction())
            {
                CreateNew(trx);
                trx.Complete();
            }
        }

        internal override void CreateNew(IMigrationTransaction trx)
        {
            Debug.Assert(ActionId == -1);

            MigrationSqlTransaction sqlTrx = (MigrationSqlTransaction)trx;

            /* this should only be run under an existing transaction scope */

            using (SqlCommand cmd = sqlTrx.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "prc_iiCreateActionData";

                cmd.Parameters.Add("@ChangeGroupID", SqlDbType.Int).Value = ChangeGroup.ChangeGroupId;

                cmd.Parameters.Add("@Order", SqlDbType.Int).Value = Order;

                cmd.Parameters.Add("@State", SqlDbType.Int).Value = State;

                cmd.Parameters.Add("@SourceItem", SqlDbType.Xml).Value
                    = (SourceItem != null) ? (object)ChangeGroup.Manager.SourceSerializer.SerializeItem(SourceItem) : (object)DBNull.Value;

                cmd.Parameters.Add("@TargetSourceItem", SqlDbType.Xml).Value
                    = (TargetSourceItem != null) ? (object)ChangeGroup.Manager.TargetSerializer.SerializeItem(TargetSourceItem) : (object)DBNull.Value;

                cmd.Parameters.Add("@TargetTargetItem", SqlDbType.Xml).Value
                    = (TargetTargetItem != null) ? (object)ChangeGroup.Manager.TargetSerializer.SerializeItem(TargetTargetItem) : (object)DBNull.Value;

                cmd.Parameters.Add("@BasicAction", SqlDbType.Int).Value = Action;
                cmd.Parameters.Add("@Recursivity", SqlDbType.Bit).Value = Recursive;

                cmd.Parameters.Add("@Label", SqlDbType.NVarChar).Value
                    = (Label != null) ? Label : (object)DBNull.Value;

                cmd.Parameters.Add("@Version", SqlDbType.NVarChar).Value
                    = (Version != null) ? Version : (object)DBNull.Value;

                cmd.Parameters.Add("@MergeVersionTo", SqlDbType.NVarChar).Value
                    = (MergeVersionTo != null) ? MergeVersionTo : (object)DBNull.Value;

                int identity;
                if (DataAccessManager.TryExecuteScalar<int>(cmd, out identity))
                {
                    ActionId = identity;
                }
                else
                {
                    throw new MigrationException(MigrationToolkitVCResources.FailureCreatingNewActionRow);
                }
            }
        }

        internal override void UpdateExisting(IMigrationTransaction trx)
        {
            Debug.Assert(ActionId > 0);

            MigrationSqlTransaction sqlTrx = (MigrationSqlTransaction)trx;

            using (SqlCommand cmd = sqlTrx.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "prc_iiUpdateActionState";

                cmd.Parameters.Add("@ActionId", SqlDbType.Int).Value = ActionId;
                cmd.Parameters.Add("@State", SqlDbType.Int).Value = State;

                int rows = DataAccessManager.ExecuteNonQuery(cmd);

                Debug.Assert(rows == 1);

                if (rows != 1)
                {
                    throw new MigrationException(
                        string.Format(
                        MigrationToolkitVCResources.Culture,
                        MigrationToolkitVCResources.WrongRowCountUpdatingActions,
                        rows)
                        );
                }
            }
        }

        internal override void UpdateExisting()
        {
            using (IMigrationTransaction trx = DataAccessManager.Current.StartTransaction())
            {
                UpdateExisting(trx);
                trx.Complete();
            }
        }
    }
}
