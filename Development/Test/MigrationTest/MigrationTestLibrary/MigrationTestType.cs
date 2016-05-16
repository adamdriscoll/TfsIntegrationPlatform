using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    public enum MigrationTestType
    {
        /// <summary>
        /// Alter the Left side and migrate Left->Right
        /// </summary>
        OneWay,
        /// <summary>
        /// Alter the Left side and sync Left->Right then Right->Left
        /// </summary>
        TwoWayLeft,
        /// <summary>
        /// Alter the Right side and sync Left->Right then Right->Left
        /// </summary>
        TwoWayRight,
    }
}
