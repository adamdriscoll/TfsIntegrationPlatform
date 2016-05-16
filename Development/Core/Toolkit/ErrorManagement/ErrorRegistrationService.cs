// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This class is responsible for managing the registration of error routers 
    /// </summary>
    class ErrorRegistrationService
    {
        List<ErrorSignatureBase> m_signatures = new List<ErrorSignatureBase>(); // this list contains the registered signatures
        List<ErrorRouter> m_routers = new List<ErrorRouter>(); // this list contains a list of registered error routers

        BM.ErrorRouters m_customRouterSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="customRouters"></param>
        public ErrorRegistrationService(BM.ErrorRouters customRouters)
        {
            m_customRouterSettings = customRouters; // note: null is acceptable

            RegisterDefaultErrors();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="routingPolicy"></param>
        /// <remarks>Parameter routingPolicy can be NULL. In that case, we will first check 
        /// if there is an config in the config file and use it. If setting is not found, 
        /// we will always treat an error that matches the signature as a fatal error.</remarks>
        public void RegisterError(
            ErrorSignatureBase signature,
            ErrorRoutingPolicy routingPolicy)
        {
            Debug.Assert(null != signature, "null == signature");
            if (null == signature)
            {
                throw new ArgumentNullException("signature");
            }

            if (!m_signatures.Contains(signature, signature as IEqualityComparer<ErrorSignatureBase>))
            {
                // new signature
                m_signatures.Add(signature);
                m_routers.Add(new ErrorRouter(signature, routingPolicy));
            }
            else
            {
                // The error signature is registered already.
                // We *always* replace the existing policy with the new one.
                // The caller (sync orchestrator) makes sure the policies in the configuration file
                // overwrites those registered by adapter in the code
                foreach (ErrorRouter router in m_routers)
                {
                    if (router.Signature.CompareTo(signature) == 0)
                    {
                        router.RoutingPolicy = routingPolicy;
                    }
                }
            }

            // sort the signatures to make the
            m_signatures.Sort(new ErrorSignatureComparer());
        }

        /// <summary>
        /// Gets all the registered routers
        /// </summary>
        public ReadOnlyCollection<ErrorRouter> RegisteredRouters
        {
            get
            {
                return m_routers.AsReadOnly();
            }
        }

        /// <summary>
        /// Register the error routers that are configured in the configuration file
        /// </summary>
        /// <remarks>
        /// Calling this method may overwrite the "hard-coded policy" registered by the adapter. This method
        /// should be called after all the adapters are given a chance to register their "hard-coded policy"
        /// </remarks>
        internal void RegisterErrorsInConfigurationFile()
        {
            if (null != m_customRouterSettings)
            {
                foreach (BM.ErrorRouter routerConfig in m_customRouterSettings.ErrorRouter)
                {
                    ErrorSignatureBase signature = ErrorSignatureFactory.CreateErrorSignaure(routerConfig.Signature);
                    if (null != signature)
                    {
                        ErrorRoutingPolicy policy = ErrorRoutingPolicyFactory.CreateRoutingPolicy(routerConfig.Policy);
                        this.RegisterError(signature, policy);
                    }
                }
            }

            // last step, call register wild card error routing policy
            DefaultErrorRoutingPolicies.RegisterImplicitDefaultErrors(this);
        }

        /// <summary>
        /// Registers the default errors such as OOM, NullReference, etc.
        /// </summary>
        private void RegisterDefaultErrors()
        {
            DefaultErrorRoutingPolicies.RegisterDefaultErrors(this);
        }

    }
}
