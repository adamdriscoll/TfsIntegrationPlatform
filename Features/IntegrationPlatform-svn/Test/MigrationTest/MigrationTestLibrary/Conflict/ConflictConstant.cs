// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace MigrationTestLibrary.Conflict
{
    public static class ConflictConstant
    {
        // Conflicts
        public static Guid ChainOnConflict = new Guid("F6BFB484-EE70-4ffc-AAB3-4F659B0CAF7F");
        public static Guid TFSZeroCheckinConflict = new Guid("B271FAFE-54B8-45e3-B311-9A477CE13B31");
        public static Guid TfsCheckinConflict = new Guid("14AF7EB3-7D1E-48cd-8ADC-6496EFC796D2");
        public static Guid VCInvalidPathConflict = new Guid("D661A693-6F2D-447e-A175-AAB682A9B769");

        public static Guid InvalidFieldValueConflictType = new Guid("8FA45DDA-60E3-4015-A1AA-66D538060080");
        public static Guid InvalidFieldConflictType = new Guid("EA1A518B-248B-43e3-90B2-62FE7EB5F366");

        public static Guid WitHistoryNotFoundConflictType = new Guid("1722DF87-AB61-4ad0-8B41-531D3D804089");
        public static Guid TFSModifyLockedWorkItemLinkConflictType = new Guid("62A55241-8853-4402-A1B7-18F3A76332A3");
        public static Guid TFSLinkAccessViolationConflictType = new Guid("C2C3832B-414D-4ebe-844B-3A7C316E2592");

        // Resolution actions
        public static Guid TfsCheckinSkipAction = new Guid("8697DB59-ADBA-4e60-ACD7-2B6E6EDAC128");
        public static Guid TFSHistoryNotFoundSkipAction = new Guid("2B510A2A-BA08-4bcd-891D-07186E352C0D");
        public static Guid InvalidFieldConflictUseFieldMapAction = new Guid("FE028FAC-6DD8-400a-B8EE-26CF63F8AAEE");
        public static Guid InvalidFieldValueConflictUseValueMapAction = new Guid("F3AFE975-4111-43dd-A7FC-B6FC0E0E738B");
        public static Guid HistoryNotFoundUpdateConversionHistoryAction = new Guid("58C2252B-CDB5-4511-9676-45103BE5ACC3");
        public static Guid TFSModifyLockedWorkItemLinkConflict_ResolveByForceDeleteAction = new Guid("48D03A59-DBA2-47cd-8938-D7BBAD695B65");
    }
}
