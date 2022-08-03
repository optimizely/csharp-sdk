namespace OptimizelySDK.Odp.Entity
{
    public class Error
    {
        public string Message { get; set; }
        
        public Location[] Locations { get; set; }
        
        public string[] Path { get; set; }
        
        public Extension Extensions { get; set; }

        public override string ToString()
        {
            return $"{Message}";
        }
    }
}
