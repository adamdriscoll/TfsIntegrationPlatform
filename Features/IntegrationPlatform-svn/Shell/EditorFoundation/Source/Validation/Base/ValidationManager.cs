// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Indicates the status of the asynchronous indexing.
    /// </summary>
    public enum ValidationStatus : byte
    {
        /// <summary>
        /// Indicates that validation is in progress.
        /// </summary>
        Validating,

        /// <summary>
        /// Indicates that validation is idle.
        /// </summary>
        Ready
    }

    /// <summary>
    /// Manages data validation.
    /// </summary>
    public abstract class ValidationManager : IValidationManager
    {
        #region Fields
        private ValidationStatus status;
        private readonly HashCollection<ISupportValidation> objects;
        private readonly Dictionary<ISupportValidation, HashCollection<ValidationResult>> validationResults;
        private readonly HashList<ISupportValidation> validationQueue;
        private readonly ManualResetEvent queueResetEvent;
        private readonly AsyncOperation asyncOperation;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationManager"/> class.
        /// </summary>
        protected ValidationManager ()
        {
            this.objects = new HashCollection<ISupportValidation> ();
            this.validationResults = new Dictionary<ISupportValidation, HashCollection<ValidationResult>> ();
            this.validationQueue = new HashList<ISupportValidation> ();
            this.queueResetEvent = new ManualResetEvent (false);
            this.asyncOperation = AsyncOperationManager.CreateOperation (null);
            this.status = ValidationStatus.Ready;

            this.BeginValidation ();
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the status of the Validation Manager changes.
        /// </summary>
        public event EventHandler StatusChanged;

        /// <summary>
        /// Raised when a <see cref="ValidationResult"/> is added to the Validation Manager.
        /// </summary>
        public event EventHandler<ValidationResultsChangedEvent> ValidationResultAdded;

        /// <summary>
        /// Raised when a <see cref="ValidationResult"/> is removed from the Validation Manager.
        /// </summary>
        public event EventHandler<ValidationResultsChangedEvent> ValidationResultRemoved;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the status of the Validation Manager.
        /// </summary>
        public ValidationStatus Status
        {
            get
            {
                return this.status;
            }
            private set
            {
                if (value != this.status)
                {
                    this.status = value;

                    SendOrPostCallback raiseEvent = delegate
                    {
                        this.RaiseStatusChangedEvent ();
                    };

                    this.asyncOperation.Post (raiseEvent, null);
                }
            }
        }

        /// <summary>
        /// Gets the current number of validation results.
        /// </summary>
        /// <value>The number of validation results.</value>
        public int ValidationResultCount
        {
            get
            {
                int validationCount = 0;
                foreach (HashCollection<ValidationResult> validationResults in this.validationResults.Values)
                {
                    validationCount += validationResults.Count;
                }
                return validationCount;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts a complete validation of all registered data.
        /// </summary>
        public void Validate ()
        {
            lock (this.objects)
            {
                foreach (ISupportValidation obj in this.objects)
                {
                    this.BeginValidation (obj);
                }
            }
        }

        /// <summary>
        /// Enumerates the current validation results for the specified object.
        /// </summary>
        /// <param name="obj">The object for which validation results are needed.</param>
        /// <returns>The validation results.</returns>
        public IEnumerable<ValidationResult> EnumerateValidationResults (ISupportValidation obj)
        {
            HashCollection<ValidationResult> validationResults;
            if (this.validationResults.TryGetValue (obj, out validationResults))
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    yield return validationResult;
                }
            }
        }

        /// <summary>
        /// Enumerates the current validation results for all registered data.
        /// </summary>
        /// <returns>The validation results.</returns>
        public IEnumerable<ValidationResult> EnumerateValidationResults ()
        {
            foreach (HashCollection<ValidationResult> validationResults in this.validationResults.Values)
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    yield return validationResult;
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds an object to the Validation Manager.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        protected void AddObject (ISupportValidation obj)
        {
            lock (this.objects)
            {
                lock (this.validationResults)
                {
                    if (!this.objects.Contains (obj))
                    {
                        this.objects.Add (obj);
                        ISupportValidationNotification notifyValidityAffected = obj as ISupportValidationNotification;
                        if (notifyValidityAffected != null)
                        {
                            notifyValidityAffected.ValidityAffected += this.OnValidityAffected;
                        }

                        this.validationResults.Add (obj, new HashCollection<ValidationResult> ());
                    }
                }
            }
        }

        /// <summary>
        /// Removes an object from the Validation Manager.
        /// </summary>
        /// <param name="obj">The object to be removed.</param>
        protected void RemoveObject (ISupportValidation obj)
        {
            lock (this.objects)
            {
                lock (this.validationResults)
                {
                    lock (this.validationQueue)
                    {
                        if (this.objects.Contains (obj))
                        {
                            this.objects.Remove (obj);
                            ISupportValidationNotification notifyValidityAffected = obj as ISupportValidationNotification;
                            if (notifyValidityAffected != null)
                            {
                                notifyValidityAffected.ValidityAffected -= this.OnValidityAffected;
                            }

                            HashCollection<ValidationResult> validationResults = this.validationResults[obj];
                            foreach (ValidationResult validationResult in new List<ValidationResult> (validationResults))
                            {
                                validationResults.Remove (validationResult);
                                ValidationResult currentValidationResult = validationResult;
                                SendOrPostCallback raiseRemovedEvent = delegate
                                {
                                    this.RaiseValidationResultRemovedEvent (currentValidationResult);
                                };
                                this.asyncOperation.Post (raiseRemovedEvent, null);
                            }
                            this.validationResults.Remove (obj);

                            if (this.validationQueue.Contains (obj))
                            {
                                this.validationQueue.Remove (obj);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears all registered objects from the Validation Manager.
        /// </summary>
        protected void ClearObjects ()
        {
            lock (this.objects)
            {
                foreach (ISupportValidation obj in new List<ISupportValidation> (this.objects))
                {
                    this.RemoveObject (obj);
                }
            }
        }

        /// <summary>
        /// Starts a complete validation of the specified data object.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        protected void BeginValidation (ISupportValidation obj)
        {
            if (obj != null)
            {
                lock (this.validationQueue)
                {
                    if (!this.validationQueue.Contains (obj))
                    {
                        this.validationQueue.Add (obj);
                        this.Status = ValidationStatus.Validating;
                        this.queueResetEvent.Set ();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void BeginValidation ()
        {
            ThreadStart beginValidation = delegate
            {
                while (true)
                {
                    // Wait if there is no work to do
                    this.queueResetEvent.WaitOne ();

                    ISupportValidation obj = null;
                    lock (this.validationQueue)
                    {
                        if (this.validationQueue.Count == 0)
                        {
                            this.Status = ValidationStatus.Ready;
                            this.queueResetEvent.Reset ();
                            continue;
                        }
                        else
                        {
                            obj = this.validationQueue[0];
                            this.validationQueue.RemoveAt (0);
                        }
                    }

                    lock (this.validationResults)
                    {
                        // Try to get the validation results (they may have been removed by a different
                        // thread calling RemoveObject)
                        HashCollection<ValidationResult> oldValidationResults;
                        if (this.validationResults.TryGetValue (obj, out oldValidationResults))
                        {
                            HashCollection<ValidationResult> newValidationResults = new HashCollection<ValidationResult> ();
                            foreach (ValidationResult validationResult in obj.Validate ())
                            {
                                if (!newValidationResults.Contains (validationResult))
                                {
                                    newValidationResults.Add (validationResult);
                                }
                            }

                            foreach (ValidationResult validationResult in new List<ValidationResult> (oldValidationResults))
                            {
                                if (!newValidationResults.Contains (validationResult))
                                {
                                    oldValidationResults.Remove (validationResult);
                                    ValidationResult currentValidationResult = validationResult;
                                    SendOrPostCallback raiseRemovedEvent = delegate
                                    {
                                        this.RaiseValidationResultRemovedEvent(currentValidationResult);
                                    };
                                    this.asyncOperation.Post (raiseRemovedEvent, null);
                                }
                            }

                            foreach (ValidationResult validationResult in newValidationResults)
                            {
                                if (!oldValidationResults.Contains (validationResult))
                                {
                                    oldValidationResults.Add (validationResult);
                                    ValidationResult currentValidationResult = validationResult;
                                    SendOrPostCallback raiseAddedEvent = delegate
                                    {
                                       this.RaiseValidationResultAddedEvent(currentValidationResult);
                                    };
                                    this.asyncOperation.Post (raiseAddedEvent, null);
                                }
                            }
                        }
                    }
                }
            };

            Thread thread = new Thread (beginValidation);
            thread.Name = "Validation Manager";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start ();
        }

        private void OnValidityAffected (object sender, EventArgs e)
        {
            this.BeginValidation (sender as ISupportValidation);
        }

        private void RaiseStatusChangedEvent ()
        {
            if (this.StatusChanged != null)
            {
                this.StatusChanged (this, EventArgs.Empty);
            }
        }    

        private void RaiseValidationResultAddedEvent (ValidationResult validationResult)
        {
            if (this.ValidationResultAdded != null)
            {
                this.ValidationResultAdded (this, new ValidationResultsChangedEvent (validationResult));
            }
        }

        private void RaiseValidationResultRemovedEvent (ValidationResult validationResult)
        {
            if (this.ValidationResultRemoved != null)
            {
                this.ValidationResultRemoved (this, new ValidationResultsChangedEvent (validationResult));
            }
        }
        #endregion
    }
}
