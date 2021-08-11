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

using System;
using OptimizelySDK.Event;
using OptimizelySDK.Tests.Utils;

namespace OptimizelySDK.Tests.EventTest
{
    /// <summary>
    /// Helper class for optimizely factory unit testing to expose private properties of BatchEventProcessor and its super classes.
    /// </summary>
    public class EventProcessorProps
    {
        public int BatchSize { get; set; }
        public TimeSpan FlushInterval { get; set; }
        public TimeSpan TimeoutInterval { get; set; }

        public EventProcessorProps(BatchEventProcessor eventProcessor)
        {
            var fieldsInfo = Reflection.GetAllFields(eventProcessor.GetType());
            BatchSize = Reflection.GetFieldValue<int, BatchEventProcessor>(eventProcessor, "BatchSize", fieldsInfo);
            FlushInterval = Reflection.GetFieldValue<TimeSpan, BatchEventProcessor>(eventProcessor, "FlushInterval", fieldsInfo);
            TimeoutInterval = Reflection.GetFieldValue<TimeSpan, BatchEventProcessor>(eventProcessor, "TimeoutInterval", fieldsInfo);
        }

        /// <summary>
        /// To create default instance of expected values.
        /// </summary>
        public EventProcessorProps()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var eventProcessor = obj as EventProcessorProps;
            if (eventProcessor == null)
            {
                return false;
            }

            if (BatchSize != eventProcessor.BatchSize ||
                FlushInterval.TotalMilliseconds != eventProcessor.FlushInterval.TotalMilliseconds ||
                TimeoutInterval.TotalMilliseconds != eventProcessor.TimeoutInterval.TotalMilliseconds)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
