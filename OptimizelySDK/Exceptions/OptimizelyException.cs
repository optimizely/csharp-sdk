/* 
 * Copyright 2017, 2020, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;


namespace OptimizelySDK.Exceptions
{
    public class OptimizelyException : Exception
    {
        public OptimizelyException(string message)
            : base(message) { }
    }

    public class OptimizelyRuntimeException : OptimizelyException
    {
        public OptimizelyRuntimeException(string message)
            : base(message) { }
    }

    public class InvalidJsonException : OptimizelyException
    {
        public InvalidJsonException(string message)
            : base(message) { }
    }

    public class InvalidAttributeException : OptimizelyException
    {
        public InvalidAttributeException(string message)
            : base(message) { }
    }

    public class InvalidAudienceException : OptimizelyException
    {
        public InvalidAudienceException(string message)
            : base(message) { }
    }

    public class InvalidEventException : OptimizelyException
    {
        public InvalidEventException(string message)
            : base(message) { }
    }

    public class InvalidExperimentException : OptimizelyException
    {
        public InvalidExperimentException(string message)
            : base(message) { }
    }

    public class InvalidGroupException : OptimizelyException
    {
        public InvalidGroupException(string message)
            : base(message) { }
    }

    public class InvalidInputException : OptimizelyException
    {
        public InvalidInputException(string message)
            : base(message) { }
    }

    public class InvalidVariationException : OptimizelyException
    {
        public InvalidVariationException(string message)
            : base(message) { }
    }

    public class InvalidFeatureException : OptimizelyException
    {
        public InvalidFeatureException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Base exception for CMAB client errors.
    /// </summary>
    public class CmabException : OptimizelyException
    {
        public CmabException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Exception thrown when CMAB decision fetch fails (network/non-2xx/exhausted retries).
    /// </summary>
    public class CmabFetchException : CmabException
    {
        public CmabFetchException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Exception thrown when CMAB response is invalid or cannot be parsed.
    /// </summary>
    public class CmabInvalidResponseException : CmabException
    {
        public CmabInvalidResponseException(string message)
            : base(message) { }
    }

    public class InvalidRolloutException : OptimizelyException
    {
        public InvalidRolloutException(string message)
            : base(message) { }
    }

    public class ConfigParseException : OptimizelyException
    {
        public ConfigParseException(string message)
            : base(message) { }
    }

    public class ParseException : OptimizelyException
    {
        public ParseException(string message)
            : base(message) { }
    }
    public class InvalidHoldoutException : OptimizelyException
    {
        public InvalidHoldoutException(string message)
            : base(message) { }
    }
}

