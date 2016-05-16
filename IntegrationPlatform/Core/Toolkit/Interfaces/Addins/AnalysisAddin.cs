// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
/// <summary>
/// The AnalysisAddin abstract class provides extensibility points for Addins to be written to extend the
/// functionality of the AnalysisProvider portion of an Integration Platform adapter
/// </summary>
public abstract class AnalysisAddin : IAddin
{
    #region IAddin members
    public abstract Guid ReferenceName { get; }

    public abstract string FriendlyName { get; }

    /// <summary>
    /// An implementation of IAddin may support some CustomSettings in the configuration file
    /// Returning the names of the CustomSettingKeys supported by the Addin via this property allows
    /// the user interface to prompt the user for values for each of these CustomSettings.
    /// If the Addin implementation does not support any CustomSettings, it may return null or an empty list.
    /// </summary>
    public virtual ReadOnlyCollection<string> CustomSettingKeys
    {
        get { return null; }
    }

    /// <summary>
    /// Some IAddin implementations may be written in a generic way such that they will work if configured
    /// with any MigrationSource.   However, other Addin implementations may be migration source or adapter
    /// specific, such as an Addin that communicates with ClearCase to gather information about ClearCase
    /// symbolic links.  This Addin would not work correctly if configured with a MigrationSource that does
    /// not use the ClearCaseDetailedHistory adapter.  The SupportedMigrationProviderNames allows an Addin
    /// to declare the MigrationProviders that it is intended to work with by specifying a list of their
    /// reference names.
    /// If the Addin can work with any migration provider, it may return null or an empty list.
    /// </summary>
    public virtual ReadOnlyCollection<Guid> SupportedMigrationProviderNames
    {
        get { return null; }
    }

    /// <summary>
    /// Called by the platform to initialize this addin with the configuration of the running session group.
    /// </summary>
    /// <param name="configuration"></param>
    public virtual void Initialize(Configuration configuration) { }
    #endregion

    #region IServiceProvider members
    public virtual object GetService(Type serviceType)
    {
        return null;
    }
    #endregion

    #region IDisposable members
    public virtual void Dispose()
    {
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    public virtual void PreAnalysis(AnalysisContext analysisContext) { }

    /// <summary>
    /// This method allows an Addin to specify whether or not the processing of a sync or migration pass should
    /// proceed to the Analysis phase or exit (skipping the entire pass)
    /// </summary>
    /// <param name="analysisContext"></param>
    /// <returns></returns>
    public virtual bool ProceedToAnalysis(AnalysisContext analysisContext)
    {
        return true;
    }

    /// <summary>
    /// This method is called just after an adapter's AnalysisProvider has created a ChangeGroup delta computation and is saving it
    /// The implementation of this method has the opportunity to:
    /// 1. Inspect the ChangeGroup
    /// 2. Optionally add new ChangeActions to it before it is saved
    /// </summary>
    /// <param name="analysisContext"></param>
    /// <param name="changeGroup"></param>
    public virtual void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    public virtual void PostDeltaComputation(AnalysisContext analysisContext)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    /// <param name="changeGroup"></param>
    public virtual void PostChangeGroupConflictDetection(AnalysisContext analysisContext, ChangeGroup changeGroup) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    public virtual void PostConflictDetection(AnalysisContext analysisContext) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    /// <param name="changeGroup"></param>
    public virtual void PostChangeGroupAnalysis(AnalysisContext analysisContext, ChangeGroup changeGroup) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="analysisContext"></param>
    public virtual void PostAnalysis(AnalysisContext analysisContext) { }

}
}
