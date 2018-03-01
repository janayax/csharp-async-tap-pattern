using System;
using System.Threading;
using System.Threading.Tasks;

namespace TapPatterns
{
    class Program
    {
        // Sync Execution
        /* static void Main(string[] args)
        {
            Console.WriteLine("Start");
            var manager = new TapPatternManager();
            manager.Launch();
            Console.ReadLine();
        } */

        // Async Execution        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Start");
            var manager = new TapPatternManager();
            await manager.LaunchAsync();
            await manager.LaunchMiningMethodWithCancellationAsync();
            await manager.LaunchMiningMethodWithReportingAsync();
            Console.ReadLine();
        }
         
    }
}
