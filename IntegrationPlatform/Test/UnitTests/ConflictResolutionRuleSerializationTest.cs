// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass()]
    public class ConflictResolutionRuleSerializationTest
    {
        [TestMethod(), Priority(2), Owner("teyang")]
        public void SerializeConflictResolutionRule()
        {
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                var rules = from r in context.ConfigConflictResolutionRuleSet
                            select r;

                var serializer = new GenericSerializer<SerializableConflictResolutionRule>();
                foreach (ConfigConflictResolutionRule rule in rules)
                {
                    SerializableConflictResolutionRule serializableRule = new SerializableConflictResolutionRule(rule);
                    string serializedText = serializer.Serialize(serializableRule);

                    Trace.WriteLine(serializedText);
                }
            }
        }

        [TestMethod(), Priority(2), Owner("teyang")]
        public void SerializeConflictResolutionRuleCollection()
        {
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                var rules = from r in context.ConfigConflictResolutionRuleSet
                            select r;

                SerializableConflictResolutionRuleCollection collection = new SerializableConflictResolutionRuleCollection();
                var serializer = new GenericSerializer<SerializableConflictResolutionRuleCollection>();
                foreach (ConfigConflictResolutionRule rule in rules)
                {
                    SerializableConflictResolutionRule serializableRule = new SerializableConflictResolutionRule(rule);
                    collection.AddRule(serializableRule);
                }

                string serializedText = serializer.Serialize(collection);

                Trace.WriteLine(serializedText);
            }
        }

        [TestMethod(), Priority(2), Owner("teyang")]
        public void DeserializeConflictResolutionRuleCollection()
        {
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                var rules = from r in context.ConfigConflictResolutionRuleSet
                            select r;

                SerializableConflictResolutionRuleCollection collection = new SerializableConflictResolutionRuleCollection();
                var serializer = new GenericSerializer<SerializableConflictResolutionRuleCollection>();
                foreach (ConfigConflictResolutionRule rule in rules)
                {
                    SerializableConflictResolutionRule serializableRule = new SerializableConflictResolutionRule(rule);
                    collection.AddRule(serializableRule);
                }

                string serializedText = serializer.Serialize(collection);
                var ruleSerializer = new GenericSerializer<SerializableConflictResolutionRule>();
                var newCollection = serializer.Deserialize(serializedText);
                foreach (var serializableRule in newCollection.Rules)
                {
                    Trace.WriteLine("============================");
                    string ruleText = ruleSerializer.Serialize(serializableRule);
                    Trace.WriteLine(ruleText);
                }
            }
        }
    }
}
