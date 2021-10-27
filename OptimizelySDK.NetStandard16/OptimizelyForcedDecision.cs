﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK
{
    public class OptimizelyForcedDecision
    {
        private string variationKey;

        public OptimizelyForcedDecision(string variationKey)
        {
            this.variationKey = variationKey;
        }

        public string VariationKey { get { return variationKey; } }
    }
}
