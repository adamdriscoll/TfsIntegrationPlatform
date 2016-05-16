// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    public partial class WorkFlowType
    {
        const int SyncContextValueBase = 10 ;
        const int DirectionOfFlowValueBase = 100;
        const int FrequencyValueBase = 1000;

        public WorkFlowType(int workFlowTypeStorageValue)
        {
            /*
             * Note:
             * We used to used the following hard-coded strings for WorkFlowType. Hence, values 
             * ranging from 0 to 5 are used in configurations with old schema
             public int WorkFlowTypeDBStorageValue
             {
                get
                {
                    switch (this.WorkFlowType)
                    {
                        case WorkFlowTypeEnum.OneDirectionalMigration:
                            return 0;
                        case WorkFlowTypeEnum.BidirectionalMigration:
                            return 1;
                        case WorkFlowTypeEnum.OneDirectionalSynchronization:
                            return 2;
                        case WorkFlowTypeEnum.BidirectionalSynchronization:
                            return 3;
                        case WorkFlowTypeEnum.OneDirectionalSynchronizationWithoutContextSync:
                            return 4;
                        case WorkFlowTypeEnum.BidirectionalSynchronizationWithOneWayContextSync:
                            return 5;
                        default:
                            throw new ArgumentException("type");
                    }
                }
             }
             * 
             * The new schema uses attributes, i.e. Frequency, DirectionOfFlow, and SyncContext 
             * to specify the work flow type in more detailed granularity.
             * 
             * The WorkFlowType storage value (integer) is partitioned in the following way
             * 10 ^ 0: 0-5 used up by old schema
             * 10 ^ 1: SyncContext - Unidirectional<->1, Bidirectional<->2, Disabled<->3, 
             * 10 ^ 2: DirectionOfFlow - Unidirectional<->1, Bidirectional<->2
             * 10 ^ 3: Frequency - OneTime<->1, Manual<->2, Continuous<->3
             * 
             * Example:
             * "Decimal value"  and   WorkFlowType
             *   1                    (or 0001) is Manual, Bidirectional (Flow), Bidirectional (Context Sync)
             *   5                    (or 0005) is Continuous, Bidirectional (Flow), Unidirectional (Context Sync)
             *   1220                 (or 1220) is OneTime, Bidirectional (Flow), Bidirectional (Context Sync)
             */

            if (workFlowTypeStorageValue < 10)
            {
                switch (workFlowTypeStorageValue)
                {
                    case 0: // WorkFlowTypeEnum.OneDirectionalMigration
                        this.DirectionOfFlow = DirectionOfFlow.Unidirectional;
                        this.Frequency = Frequency.ContinuousManual;
                        this.SyncContext = SyncContext.Unidirectional;
                        break;
                    case 1: // WorkFlowTypeEnum.BidirectionalMigration
                        this.DirectionOfFlow = DirectionOfFlow.Bidirectional;
                        this.Frequency = Frequency.ContinuousManual;
                        this.SyncContext = SyncContext.Bidirectional;
                        break;
                    case 2: // WorkFlowTypeEnum.OneDirectionalSynchronization:
                        this.DirectionOfFlow = DirectionOfFlow.Unidirectional;
                        this.Frequency = Frequency.ContinuousAutomatic;
                        this.SyncContext = SyncContext.Unidirectional;
                        break;
                    case 3: // WorkFlowTypeEnum.BidirectionalSynchronization:
                        this.DirectionOfFlow = DirectionOfFlow.Bidirectional;
                        this.Frequency = Frequency.ContinuousAutomatic;
                        this.SyncContext = SyncContext.Bidirectional;
                        break;
                    case 4: // WorkFlowTypeEnum.OneDirectionalSynchronizationWithoutContextSync:
                        this.DirectionOfFlow = DirectionOfFlow.Unidirectional;
                        this.Frequency = Frequency.ContinuousAutomatic;
                        this.SyncContext = SyncContext.Disabled;
                        break;
                    case 5: // WorkFlowTypeEnum.BidirectionalSynchronizationWithOneWayContextSync:
                        this.DirectionOfFlow = DirectionOfFlow.Bidirectional;
                        this.Frequency = Frequency.ContinuousAutomatic;
                        this.SyncContext = SyncContext.Unidirectional;
                        break;
                    default:
                        throw new ArgumentException("workFlowTypeStorageValue");
                }
            }
            else
            {
                SetFlagsWithStorageValue(workFlowTypeStorageValue);
            }
        }

        [XmlIgnore]
        public int StorageValue
        {
            get
            {
                return FrequencyValueBase * FrequencyStorageValueDigit
                     + DirectionOfFlowValueBase * DirectionOfFlowStorageValueDigit
                     + SyncContextValueBase * SyncContextStorageValueDigit;
            }
        }

        private void SetFlagsWithStorageValue(int workFlowTypeStorageValue)
        {
            int modValue = workFlowTypeStorageValue / FrequencyValueBase;
            this.Frequency = ExtractFrequency(modValue);

            workFlowTypeStorageValue -= (FrequencyValueBase * modValue);
            modValue = workFlowTypeStorageValue / DirectionOfFlowValueBase;
            this.DirectionOfFlow = ExtractDirectionOfFlow(modValue);

            workFlowTypeStorageValue -= (DirectionOfFlowValueBase * modValue);
            modValue = workFlowTypeStorageValue / SyncContextValueBase;
            this.SyncContext = ExtractSyncContext(modValue);
        }

        private SyncContext ExtractSyncContext(int syncContextValue)
        {
            switch (syncContextValue)
            {
                case 1:
                    return SyncContext.Unidirectional;
                case 2:
                    return SyncContext.Bidirectional;
                case 3:
                    return SyncContext.Disabled;
                default:
                    throw new ArgumentException(string.Format("syncContextValue = {0}", syncContextValue));
            }
        }

        private Frequency ExtractFrequency(int frequencyValue)
        {
            switch (frequencyValue)
            {
                case 1:
                    return Frequency.OneTime;
                case 2:
                    return Frequency.ContinuousManual;
                case 3:
                    return Frequency.ContinuousAutomatic;
                default:
                    throw new ArgumentException(string.Format("frequencyValue = {0}", frequencyValue));
            }
        }

        private DirectionOfFlow ExtractDirectionOfFlow(int directionOfFlowValue)
        {
            switch (directionOfFlowValue)
            {
                case 1:
                    return DirectionOfFlow.Unidirectional;
                case 2:
                    return DirectionOfFlow.Bidirectional;
                default:
                    throw new ArgumentException(string.Format("directionOfFlowValue = {0}", directionOfFlowValue));
            }
        }

        private int SyncContextStorageValueDigit
        {
            get
            {
                switch (this.SyncContext)
                {
                    case SyncContext.Unidirectional:
                        return 1;
                    case SyncContext.Bidirectional:
                        return 2;
                    case SyncContext.Disabled:
                        return 3;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private int FrequencyStorageValueDigit
        {
            get
            {
                switch (this.Frequency)
                {
                    case Frequency.OneTime:
                        return 1;
                    case Frequency.ContinuousManual:
                        return 2;
                    case Frequency.ContinuousAutomatic:
                        return 3;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private int DirectionOfFlowStorageValueDigit
        {
            get
            {
                switch (this.DirectionOfFlow)
                {
                    case DirectionOfFlow.Unidirectional:
                        return 1;
                    case DirectionOfFlow.Bidirectional:
                        return 2;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
    }
}
