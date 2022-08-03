﻿namespace OptimizelySDK.Odp.Entities
{
    public class Error
    {
        public string Message { get; set; }
        
        public Location[] Locations { get; set; }
        
        public string[] Path { get; set; }
        
        public Extension Extensions { get; set; }
    }
}