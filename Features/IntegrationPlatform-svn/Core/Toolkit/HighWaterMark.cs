// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class HighWaterMarkUpdatedEventArgs<T> : EventArgs
        where T : IConvertible
    {
        public HighWaterMarkUpdatedEventArgs(T previousValue, T newValue)
        {
            m_previous = previousValue;
            m_new = newValue;
        }

        public T Current
        {
            get
            {
                return m_previous;
            }
        }

        public T New
        {
            get
            {
                return m_new;
            }
        }

        T m_previous;
        T m_new;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IHighWaterMark
    {
        Guid SessionUniqueId { get;  set; }
        Guid SourceUniqueId { get;  set; }
    }

    public class HighWaterMark<T> : IHighWaterMark
        where T : IConvertible
    {
        T m_current;
        Guid m_sessionUniqueId = Guid.Empty;
        Guid m_sourceUniqueId = Guid.Empty;
        string m_name;

        public HighWaterMark(string name)
        {
            m_name = name;
        }

        public HighWaterMark(Guid SessionUniqueId, Guid SourceUniqueId, string name)
            : this(SessionUniqueId, SourceUniqueId, name, default(T))
        {
        }

        public HighWaterMark(Guid SessionUniqueId, Guid SourceUniqueId, string name, T defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            m_sessionUniqueId = SessionUniqueId;
            m_sourceUniqueId = SourceUniqueId;
            m_name = name;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var highwaterMarksQuery = from hwm in context.RTHighWaterMarkSet
                                          where hwm.SessionUniqueId.Equals(m_sessionUniqueId)
                                          && hwm.SourceUniqueId.Equals(m_sourceUniqueId)
                                          && hwm.Name.Equals(m_name)
                                          select hwm;

                RTHighWaterMark rtHighWaterMark = null;
                if (highwaterMarksQuery.Count() > 0)
                {
                    rtHighWaterMark = highwaterMarksQuery.First();
                    if (rtHighWaterMark.Value != null)
                    {
                        m_current = CreateValueFromString(rtHighWaterMark.Value);
                    }
                }
                else
                {
                    rtHighWaterMark = RTHighWaterMark.CreateRTHighWaterMark(0, m_sessionUniqueId, m_sourceUniqueId, m_name);
                    context.TrySaveChanges();
                }
            }

            //using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            //{
            //    HighWaterMarkEntity highWaterMarkEntity = context.CreateHighWaterMark(m_sessionUniqueId, m_sourceUniqueId, m_name).First<HighWaterMarkEntity>();

            //    if (highWaterMarkEntity.Value != null)
            //    {
            //        m_current = CreateValueFromString(highWaterMarkEntity.Value);
            //    }
            //}
        }

        public event EventHandler<HighWaterMarkUpdatedEventArgs<T>> BeforeUpdate;

        public virtual T Value
        {
            get
            {
                return m_current;
            }
            protected set
            {
                m_current = value;
            }
        }

        public Guid SessionUniqueId
        {
            get
            {
                return m_sessionUniqueId;
            }
            set
            {
                m_sessionUniqueId = value;
            }
        }

        public Guid SourceUniqueId
        {
            get
            {
                return m_sourceUniqueId;
            }
            set
            {
                m_sourceUniqueId = value;
            }
        }
        
        public virtual void Reload()
        {
            if ((m_sessionUniqueId == Guid.Empty)||(m_sourceUniqueId == Guid.Empty))
            {
                throw new MigrationException(
                    string.Format(MigrationToolkitResources.Culture, MigrationToolkitResources.UninitializedHighWaterMark, m_name));
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var highwaterMarksQuery = from hwm in context.RTHighWaterMarkSet
                                          where hwm.SessionUniqueId.Equals(m_sessionUniqueId)
                                          && hwm.SourceUniqueId.Equals(m_sourceUniqueId)
                                          && hwm.Name.Equals(m_name)
                                          select hwm;

                RTHighWaterMark rtHighWaterMark = null;
                if (highwaterMarksQuery.Count() > 0)
                {
                    rtHighWaterMark = highwaterMarksQuery.First();
                    if (rtHighWaterMark.Value != null)
                    {
                        m_current = CreateValueFromString(rtHighWaterMark.Value);
                    }
                    else
                    {
                        m_current = default(T);
                    }
                }
                else
                {
                    rtHighWaterMark = RTHighWaterMark.CreateRTHighWaterMark(0, m_sessionUniqueId, m_sourceUniqueId, m_name);
                    context.AddToRTHighWaterMarkSet(rtHighWaterMark);
                    context.TrySaveChanges();
                    m_current = default(T);
                }
            }

            /*
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                HighWaterMarkEntity highWaterMarkEntity = context.CreateHighWaterMark
                    (m_sessionUniqueId,
                    m_sourceUniqueId,
                    m_name).First<HighWaterMarkEntity>();


                // ToDo error handling
                if (highWaterMarkEntity.Value == null)
                {
                    m_current = default(T);
                }
                else
                {
                    m_current = CreateValueFromString(highWaterMarkEntity.Value);
                }
            }*/
        }

        public virtual void Update(T newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException("newValue");
            }

            if ((m_sessionUniqueId == Guid.Empty) || (m_sourceUniqueId == Guid.Empty))
            {
                throw new MigrationException(
                    string.Format(MigrationToolkitResources.Culture, MigrationToolkitResources.UninitializedHighWaterMark, m_name));
            }

            string newValueString = GetValueAsString(newValue);
            // Todo We probably could use EDM eventing , OnBeforeUpdate(newValue);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var highwaterMarksQuery = from hwm in context.RTHighWaterMarkSet
                                          where hwm.SessionUniqueId.Equals(m_sessionUniqueId)
                                          && hwm.SourceUniqueId.Equals(m_sourceUniqueId)
                                          && hwm.Name.Equals(m_name)
                                          select hwm;

                if (highwaterMarksQuery.Count() > 0)
                {
                    RTHighWaterMark rtHighWaterMark = highwaterMarksQuery.First();
                    rtHighWaterMark.Value = newValueString;
                    context.TrySaveChanges();
                }
                else
                {
                    RTHighWaterMark rtHighWaterMark = new RTHighWaterMark();
                    rtHighWaterMark.SessionUniqueId = m_sessionUniqueId;
                    rtHighWaterMark.SourceUniqueId = m_sourceUniqueId;
                    rtHighWaterMark.Name = m_name;
                    rtHighWaterMark.Value = newValueString;
                    context.AddToRTHighWaterMarkSet(rtHighWaterMark);
                    context.TrySaveChanges();
                }
            }

            m_current = newValue;            
        }

        protected virtual string GetValueAsString(T value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        protected virtual T CreateValueFromString(string str)
        {
            return (T)Convert.ChangeType(str, typeof(T), CultureInfo.CurrentCulture);
        }

        private void OnBeforeUpdate(T newValue)
        {
            if (BeforeUpdate != null)
            {
                BeforeUpdate(this, new HighWaterMarkUpdatedEventArgs<T>(m_current, newValue));
            }
        }
        
        private bool tryGetLastAnalyzedTimeFromSessionVariable()
        {
            // ToDo
            throw new NotImplementedException();
            /*
            string tempString = SessionVariables.GetSessionVariable(m_session.SessionUniqueId, m_name);

            if (tempString != null)
            {
                T temp = CreateValueFromString(tempString);
                if (temp != null)
                {
                    m_current = temp;
                    return true;
                }
            }

            return false;
             * */
        }
        public override string ToString()
        {
            return m_current.ToString(CultureInfo.CurrentCulture);
        }
    }
}
