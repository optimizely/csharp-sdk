/**
 *
 *    Copyright 2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using OptimizelySDK.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.XUnitTests.EventTests
{
    public class CanonicalEvent
    {
        private string ExperimentId;
        private string VariationId;
        private string EventName;
        private string VisitorId;
        private UserAttributes Attributes;
        private EventTags Tags;

        public CanonicalEvent(string experimentId, string variationId, string eventName, string visitorId, UserAttributes attributes, EventTags tags)
        {
            ExperimentId = experimentId;
            VariationId = variationId;
            EventName = eventName;
            VisitorId = visitorId;

            Attributes = attributes ?? new UserAttributes();
            Tags = tags ?? new EventTags();
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            CanonicalEvent canonicalEvent = obj as CanonicalEvent;
            if (canonicalEvent == null)
                return false;

            if (ExperimentId != canonicalEvent.ExperimentId ||
                VariationId != canonicalEvent.VariationId ||
                EventName != canonicalEvent.EventName ||
                VisitorId != canonicalEvent.VisitorId)
                return false;

            if (!Attributes.OrderBy(pair => pair.Key)
                .SequenceEqual(canonicalEvent.Attributes.OrderBy(pair => pair.Key)))
                return false;

            if (!Tags.OrderBy(pair => pair.Key)
                .SequenceEqual(canonicalEvent.Tags.OrderBy(pair => pair.Key)))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = -907746114;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ExperimentId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VariationId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EventName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VisitorId);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, object>>.Default.GetHashCode(Attributes);
            hashCode = hashCode * -1521134295 + EqualityComparer<EventTags>.Default.GetHashCode(Tags);
            return hashCode;
        }

        public static bool operator ==(CanonicalEvent lhs, CanonicalEvent rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;

                return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(CanonicalEvent lhs, CanonicalEvent rhs)
        {
            return !(lhs == rhs);
        }
    }
}
