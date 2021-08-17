//Import Optimizely SDK
using OptimizelySDK;
using OptimizelySDK.Entity;
using System;

namespace ConsoleDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Instantiate an Optimizely client
            var optimizelyInstance = OptimizelyFactory.NewDefaultInstance("<Your SDK key here>");
            var attributes = new UserAttributes();
            attributes.Add("logged_in", true);

            // Create a user context
            var user = optimizelyInstance.CreateUserContext("user123", attributes);

            var decision = user.Decide("product_sort");

            Console.WriteLine("Enabled: " + decision.Enabled);
            Console.WriteLine("Flag Key: " + decision.FlagKey);

            Environment.Exit(0);
        }
    }
}