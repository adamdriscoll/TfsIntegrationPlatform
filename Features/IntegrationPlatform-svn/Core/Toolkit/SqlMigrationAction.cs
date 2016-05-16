// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SqlMigrationAction : MigrationAction
    {
        RTChangeAction m_RTChangeAction;
       
        /// <summary>
        /// This constructor should only used by internal assembly to realize the migration action from database.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="actionId"></param>
        /// <param name="action"></param>
        /// <param name="sourceItem"></param>
        /// <param name="version"></param>
        /// <param name="mergeVersionTo"></param>
        internal SqlMigrationAction(ChangeGroup parent, long actionId, Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails)
            : base(parent, actionId, action, sourceItem, fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails)
        {
        }

        /// <summary>
        /// This constructor should only used by internal assembly to realize the migration action from database.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="actionId"></param>
        /// <param name="action"></param>
        /// <param name="sourceItem"></param>
        /// <param name="version"></param>
        /// <param name="mergeVersionTo"></param>
        internal SqlMigrationAction(ChangeGroup parent, long actionId, Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails,
            ActionState actionState)
            : base(parent, actionId, action, sourceItem, fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails)
        {
            State = actionState;
        }

        /// <summary>
        /// Realize a SqlMigrationAction from DB RTChangeAction.
        /// Note properties ChangeGroup, SourceItem and ActionData are not populated. 
        /// </summary>
        /// <param name="RTChangeAction"></param>
        /// <returns></returns>
        internal static SqlMigrationAction RealizeFromDB(RTChangeAction RTChangeAction)
        {
            SqlMigrationAction migrationAction = new SqlMigrationAction(null, RTChangeAction.ChangeActionId, RTChangeAction.ActionId, null, RTChangeAction.FromPath, RTChangeAction.ToPath,
            RTChangeAction.Version, RTChangeAction.MergeVersionTo, RTChangeAction.ItemTypeReferenceName, null);

            return migrationAction;
        }

        public bool IsPersisted
        {
            get
            {
                return this.ActionId > 0;
            }
        }

        internal RTChangeAction RTChangeAction
        {
            get
            {
                return m_RTChangeAction;
            }
            set
            {
                m_RTChangeAction = value;
            }
        }

        private void RTChangeAction_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChangeActionId")
            {
                this.ActionId = m_RTChangeAction.ChangeActionId;
            }
        }

        internal override void CreateNew(IMigrationItemSerializer serializer)
        {
            // TODO: Consider renaming local RTChangeAction property to avoid conflict
            if (null == serializer)
            {
                throw new InvalidOperationException("IMigrationItem serializer is not registered");
            }

            m_RTChangeAction = Microsoft.TeamFoundation.Migration.EntityModel.RTChangeAction.CreateRTChangeAction
                (ChangeGroup.ChangeGroupId, ActionId, Recursive, (State == ActionState.Skipped), 1, Action,
                serializer.SerializeItem(SourceItem), Path, false, ItemTypeReferenceName);
            m_RTChangeAction.FromPath = FromPath;
            m_RTChangeAction.Version = this.Version;
            m_RTChangeAction.MergeVersionTo = this.MergeVersionTo;

            if (MigrationActionDescription != null && MigrationActionDescription.DocumentElement != null)
            {
                m_RTChangeAction.ActionData = MigrationActionDescription.DocumentElement.OuterXml;
            }

            m_RTChangeAction.PropertyChanged += 
                new System.ComponentModel.PropertyChangedEventHandler(RTChangeAction_PropertyChanged);
            /*
            Debug.Assert(ActionId == -1);

            MigrationSqlTransaction sqlTrx = (MigrationSqlTransaction)trx;

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
                    throw new MigrationException(MigrationToolkitResources.FailureCreatingNewActionRow);
                }
            }*/
        }

        internal override void UpdateExisting(IMigrationTransaction trx)
        {
            //ToDo
            throw new NotImplementedException();
            /*
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
                        MigrationToolkitResources.Culture,
                        MigrationToolkitResources.WrongRowCountUpdatingActions,
                        rows)
                        );
                }
            }*/
        }

        internal override void UpdateExisting()
        {
            // ToDo
            // throw new NotImplementedException();
            /*
            using (IMigrationTransaction trx = DataAccessManager.Current.StartTransaction())
            {
                UpdateExisting(trx);
                trx.Complete();
            }
             * */
        }
    }
}
