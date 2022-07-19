﻿/* 
 * Copyright 2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace OptimizelySDK.Config
{
    public class Integration
    {
        public string Key { get; private set; }
        public string Host { get; private set; }
        public string PublicKey { get; private set; }

        public Integration(
            string key,
            string host,
            string publicKey
        )
        {
            Key = key;
            Host = host;
            PublicKey = publicKey;
        }

        public override string ToString()
        {
            return $"Integration{{key='{Key}', host='{Host}', publicKey='{PublicKey}'}}";
        }
    }
}