// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Text;


namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    internal class CustomConfigChannelFactory<T> : ChannelFactory<T>
    {
        public CustomConfigChannelFactory()
            : base(typeof(T))
        {
            base.InitializeEndpoint((string)null, null);
        }

        protected override ServiceEndpoint CreateDescription()
        {
            Configuration config = GlobalConfiguration.Configuration;

            ServiceModelSectionGroup serviceModeGroup = ServiceModelSectionGroup.GetSectionGroup(config);

            ServiceEndpoint serviceEndpoint = base.CreateDescription();
            ChannelEndpointElement clientEndpointElem = null;
            foreach (ChannelEndpointElement endpoint in serviceModeGroup.Client.Endpoints)
            {
                if (endpoint.Contract == serviceEndpoint.Contract.ConfigurationName)
                {
                    clientEndpointElem = endpoint;
                    break;
                }
            }

            if (clientEndpointElem == null)
            {
                TraceManager.TraceError("Cannot find the endpoint configuration in the configuration file. Try using default app.config.");
                return serviceEndpoint;
            }
            else
            {
                serviceEndpoint.Name = clientEndpointElem.Contract;

                if (serviceEndpoint.Binding == null)
                {
                    serviceEndpoint.Binding = CreateBinding(clientEndpointElem.Binding, serviceModeGroup);
                }

                if (serviceEndpoint.Address == null)
                {
                    serviceEndpoint.Address = CreateEndpointAddress(clientEndpointElem);
                }

                if (serviceEndpoint.Behaviors.Count == 0 
                    && !String.IsNullOrEmpty(clientEndpointElem.BehaviorConfiguration))
                {
                    CreateBehaviors(clientEndpointElem.BehaviorConfiguration, serviceEndpoint, serviceModeGroup);
                }
            }

            return serviceEndpoint;
        }
        
        private Binding CreateBinding(
            string bindingName, 
            ServiceModelSectionGroup group)
        {
            BindingCollectionElement bindingCollectionElem = group.Bindings[bindingName];
            if (bindingCollectionElem.ConfiguredBindings.Count == 0)
            {
                return null;
            }

            IBindingConfigurationElement bindingElem = bindingCollectionElem.ConfiguredBindings[0];
            var binding = GetBinding(bindingElem);
            if (bindingElem != null && binding != null)
            {
                bindingElem.ApplyConfiguration(binding);
            }

            return binding;            
        }

        private Binding GetBinding(
            IBindingConfigurationElement bindingConfigElem)
        {
            if (bindingConfigElem is CustomBindingElement)
                return new CustomBinding();
            else if (bindingConfigElem is BasicHttpBindingElement)
                return new BasicHttpBinding();
            else if (bindingConfigElem is NetMsmqBindingElement)
                return new NetMsmqBinding();
            else if (bindingConfigElem is NetNamedPipeBindingElement)
                return new NetNamedPipeBinding();
            else if (bindingConfigElem is NetPeerTcpBindingElement)
                return new NetPeerTcpBinding();
            else if (bindingConfigElem is NetTcpBindingElement)
                return new NetTcpBinding();
            else if (bindingConfigElem is WSDualHttpBindingElement)
                return new WSDualHttpBinding();
            else if (bindingConfigElem is WSHttpBindingElement)
                return new WSHttpBinding();
            else if (bindingConfigElem is WSFederationHttpBindingElement)
                return new WSFederationHttpBinding();

            return null;
        }
        
        private EndpointAddress CreateEndpointAddress(
            ChannelEndpointElement clientEndpointElem)
        {
            EndpointIdentity identity = GetIdentity(clientEndpointElem.Identity);
            return new EndpointAddress(clientEndpointElem.Address, identity, clientEndpointElem.Headers.Headers);
        }

        private EndpointIdentity GetIdentity(
            IdentityElement element)
        {
            EndpointIdentity identity = null;
            PropertyInformationCollection properties = element.ElementInformation.Properties;
            if (properties["userPrincipalName"].ValueOrigin != PropertyValueOrigin.Default)
            {
                return EndpointIdentity.CreateUpnIdentity(element.UserPrincipalName.Value);
            }
            if (properties["servicePrincipalName"].ValueOrigin != PropertyValueOrigin.Default)
            {
                return EndpointIdentity.CreateSpnIdentity(element.ServicePrincipalName.Value);
            }
            if (properties["dns"].ValueOrigin != PropertyValueOrigin.Default)
            {
                return EndpointIdentity.CreateDnsIdentity(element.Dns.Value);
            }
            if (properties["rsa"].ValueOrigin != PropertyValueOrigin.Default)
            {
                return EndpointIdentity.CreateRsaIdentity(element.Rsa.Value);
            }
            if (properties["certificate"].ValueOrigin != PropertyValueOrigin.Default)
            {
                X509Certificate2Collection certCollection = new X509Certificate2Collection();
                certCollection.Import(Convert.FromBase64String(element.Certificate.EncodedValue));
                if (certCollection.Count == 0)
                {
                    throw new InvalidOperationException(MigrationToolkitResources.ErrorCannotLoadCertificateIdentity);
                }
                X509Certificate2 primaryCertificate = certCollection[0];
                certCollection.RemoveAt(0);
                return EndpointIdentity.CreateX509CertificateIdentity(primaryCertificate, certCollection);
            }

            return identity;
        }

        private void CreateBehaviors(
            string behaviorConfigName,
            ServiceEndpoint serviceEndpoint,
            ServiceModelSectionGroup group)
        {
            EndpointBehaviorElement behaviorElement = group.Behaviors.EndpointBehaviors[behaviorConfigName];
            // for (int i = 0; i < behaviorElement.Count; i++)
            foreach (var behaviorExtensionElem in behaviorElement)
            {
                // BehaviorExtensionElement behaviorExtension = behaviorElement[i];
                IEndpointBehavior endpointBehavior = behaviorExtensionElem.GetType().InvokeMember(
                    "CreateBehavior",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    behaviorExtensionElem,
                    null) as IEndpointBehavior;
                if (endpointBehavior != null)
                {
                    serviceEndpoint.Behaviors.Add(endpointBehavior);
                }
            }
        }
    }
}
