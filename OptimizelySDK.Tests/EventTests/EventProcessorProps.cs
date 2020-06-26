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
    public class EventProcessorProps
    {
        public int BatchSize { get; set; }
        public TimeSpan FlushInterval { get; set; }
        public TimeSpan TimeoutInterval { get; set; }

        public EventProcessorProps(BatchEventProcessor eventProcessor)
        {
            BatchSize = Reflection.GetFieldValue<int, BatchEventProcessor>(eventProcessor, "BatchSize");
            FlushInterval = Reflection.GetFieldValue<TimeSpan, BatchEventProcessor>(eventProcessor, "FlushInterval");
            TimeoutInterval = Reflection.GetFieldValue<TimeSpan, BatchEventProcessor>(eventProcessor, "TimeoutInterval");
        }
        public EventProcessorProps()
        {

        }
    }
}
