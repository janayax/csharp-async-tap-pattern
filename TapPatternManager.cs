using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace TapPatterns 
{

    public class TapPatternManager 
    {

        #region launch methods

        public void Launch() 
        {
            //LaunchMiningMethod(); //Sync
            LaunchMiningMethodLocalWithTasks(); //Sync with Parallel Execution
        }

        public void LaunchMiningMethod() 
        {
            var result = RentTimeOnMiningServer("S3cr3tT0k3n",4, out double elapsedSeconds);
            System.Console.WriteLine($"Mining result: {result}");
            System.Console.WriteLine($"Elapsed seconds: {elapsedSeconds:N}");
        }

        public async Task LaunchAsync() 
        {
            await LaunchMiningMethodAsync();      
            await LaunchMiningMethodLocalAsync();      
        }

        public async Task LaunchMiningMethodAsync()
        {
            MiningResultDto result = await RentTimeOnMiningServerAsync("S3cr3tT0k3n",4);
            System.Console.WriteLine($"Mining result: {result.MiningText}");
            System.Console.WriteLine($"Elapsed seconds: {result.ElapsedSeconds:N}");
        }

        public void LaunchMiningMethodLocalWithTasks()
        {
            /*TAP guidance: It's up to the client code to handle threading for a compute-bound method */
            var localMiningTaskList = new List<Task<MiningResultDto>>();
            for (int i=0; i <3;i++) {
                Task<MiningResultDto> task = Task.Run( ()=> RentTimeOnLocalMiningServer("S3cr3tT0k3n",4) );
                localMiningTaskList.Add(task);
            }

            var localMiningArray = localMiningTaskList.ToArray();

            Task.WaitAll(localMiningArray);

            foreach(var task in localMiningArray) {
                System.Console.WriteLine($"Mining result:{task.Result.MiningText}");
                System.Console.WriteLine($"Elapsed seconds:{task.Result.ElapsedSeconds:N}");
            }

        }

        public async Task LaunchMiningMethodLocalAsync()
        {
            MiningResultDto result = await RentTimeOnLocalMiningServerTask("SecretToken", 5);
            Console.WriteLine($"Mining result:{result.MiningText}");
            Console.WriteLine($"Elapsed seconds:{result.ElapsedSeconds:N}");
        }

        #endregion

        #region  Task 2 - introduction

        public string RentTimeOnMiningServer(string authToken, int requestedAmount, out Double elpasedSeconds) 
        {
            elpasedSeconds=0;
            if (!AuthorizeTheToken(authToken)) 
            {
                throw new Exception("Failed Authorization");
            }

            var starTime = DateTime.UtcNow;
            var coinResult = CallCoinService(requestedAmount);
            elpasedSeconds = (DateTime.UtcNow - starTime).TotalSeconds;

            return coinResult;
        }

        public async Task<MiningResultDto> RentTimeOnMiningServerAsync(string authToken, int requestedAmount)         
        {
            /*  The class runs synchronous code for authorization and throws an error if authorization fails. 
                TAP guidance: “An asynchronous method should only directly raise an exception to be thrown out of 
                the MethodNameAsync call in response to a usage error. For all other errors, exceptions occurring 
                during the execution of an asynchronous method should be assigned to the returned Task.” */
            if (!AuthorizeTheToken(authToken)) {
                throw new Exception("Failed Authorization");
            }
            var result = new MiningResultDto();
            var startTime = DateTime.UtcNow;
            var coinResult = await CallCoinServiceAsync(requestedAmount);
            var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

            result.ElapsedSeconds = elapsedSeconds;
            result.MiningText = coinResult;
            
            return result;
        }


        /* TAP guidance: “If a method is purely compute-bound, it should be exposed only as a synchronous implementation; 
            a consumer may then choose whether to wrap an invocation of that synchronous method into a Task for their own 
            purposes of offloading the work to another thread and/or to achieve parallelism.” */
        public MiningResultDto RentTimeOnLocalMiningServer(string authToken, int requestedIterations)
        {
            if (!AuthorizeTheToken(authToken))
            {
                throw new Exception("Failed Authorization");
            }

            var result = new MiningResultDto();
            var startTime = DateTime.UtcNow;
            var coinAmount = MineAsyncCoinsWithNthRoot(requestedIterations);
            var elpasedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            
            result.MiningText = $"You've got {coinAmount:N} AsyncCoin!";
            result.ElapsedSeconds = elpasedSeconds;

            return result;
        }


        public Task<MiningResultDto> RentTimeOnLocalMiningServerTask(string authToken, int requestedIterations)
        {
            /* Parallel compute-bound method that runs multiple Tasks
            
            In this method there is a big difference with how the Task is created. Notice that you start with TaskCompletionSource<MiningResultDto>.
            This object allows you more control of the content and behavior of a Task. By writing code then using the SetResult method, your method 
            creates a Task<T> and give it back to the consumer. 
            
            Consuming code can simply await the completion of the task, or can use Task.Run to launch it on a background thread. */
            if (!AuthorizeTheToken(authToken))
            {
                throw new Exception("Failed Authorization");
            }

            var tcs = new TaskCompletionSource<MiningResultDto>();
            var result = new MiningResultDto();
            var starTime = DateTime.UtcNow;

            var localMiningTaskList = new List<Task<MiningResultDto>>();
            for (int i=0;i<3;i++) {
                Task<MiningResultDto> task = Task.Run( ()=> RentTimeOnLocalMiningServer("S3cr3tT0k3n",5) );
                localMiningTaskList.Add(task);
            }

            var localMiningArray = localMiningTaskList.ToArray();

            Task.WaitAll(localMiningArray);

            foreach(var task in localMiningArray) {
                result.MiningText += task.Result.MiningText + Environment.NewLine;
            }

            result.ElapsedSeconds = (DateTime.UtcNow - starTime).TotalSeconds;

            tcs.SetResult(result);
            return tcs.Task;
        }

        #endregion

        #region TAP Advanced (Cancellation)

        public async Task LaunchMiningMethodWithCancellationAsync() 
        {
            /* The line new CancellationTokenSource(1000) puts a 1 second (1,000 millisecond) timeout on the Token that it creates. 
            There are other methods for requesting cancellation from the CancellationTokenSource. 
            You can use the cancellation methods from any thread that has access to the CancellationTokenSource 
            – which is essentially the parent object of the cancellation token you pass to a TAP method */
            var cts = new CancellationTokenSource(1000); 

            Task<MiningResultDto> asyncTask = RentTimeOnMiningServerAsync("S3cr3tT0k3n",4, cts.Token);

            try
            {
                MiningResultDto result = await asyncTask;
                System.Console.WriteLine($"Mining result: {result.MiningText}");
                System.Console.WriteLine($"Elapsed seconds: {result.ElapsedSeconds:N}");
                System.Console.WriteLine("End");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine($"Task status:{asyncTask.Status}");                                
            }

            /* This is a fairly straightforward example of cancellation. There are a multitude of other ways to set up 
               methods that are cancelable and to request cancellation. These techniques can be used for both async and 
               parallel code. 
               
               Further information can be found here: https://docs.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads */
        }

        public async Task<MiningResultDto> RentTimeOnMiningServerAsync(string authToken, int requestedAmount, CancellationToken cancellationToken) 
        {
            if (!AuthorizeTheToken(authToken))
            {
                throw new Exception("Failed Authorization");
            }

            Thread.Sleep(1500);
            var result = new MiningResultDto();
            var startTime = DateTime.UtcNow;
            var asyncTask = CallCoinServiceAsync(requestedAmount);
            if(cancellationToken.IsCancellationRequested) {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var coinResult = await asyncTask;
            var elapsedSeconds = (DateTime.UtcNow-startTime).TotalSeconds;

            result.ElapsedSeconds = elapsedSeconds;
            result.MiningText = coinResult;

            return result;
        }

        #endregion

        #region TAP Advanced (Reporting)

        public async Task LaunchMiningMethodWithReportingAsync()
        {
            var progress = new Progress<int>(total => System.Console.WriteLine($"Progress: {total}%"));
            var result = await RentTimeOnMiningServerWithProgressAsync("S3cr3tT0k3n", 5, progress);
            System.Console.WriteLine($"Elapased seconds: {result.ElapsedSeconds:N}");
            System.Console.WriteLine($"Mining result: {result.MiningText}");
            System.Console.WriteLine("End");
        }

        public async Task<MiningResultDto> RentTimeOnMiningServerWithProgressAsync(string authToken, int requestedAmount, IProgress<int> progress)
        {
            if (!AuthorizeTheToken(authToken))
            {
                throw new Exception("Failed Authorization");                
            }

            var result = new MiningResultDto();
            var startTime = DateTime.UtcNow;
            var coinResult = await CallCoinServiceWithProgressAsync(requestedAmount, progress);
            var elapsedSeconds = (DateTime.UtcNow -  startTime).TotalSeconds;

            result.ElapsedSeconds = elapsedSeconds;
            result.MiningText = coinResult;

            return result;
        }
       
        private async Task<string> CallCoinServiceWithProgressAsync(int howMany, IProgress<int> progress)
        {
        /* Notice that this method takes IProgress<T> as a parameter. That is a .NET interface with a Report method. 
           Report simply runs a callback method that you define for it. By defining the T data type as int, we are telling 
           the consuming method that our callback method will return an integer. It's up to you as a method author to decide
           what kind of datatype the progress callback will return 
            - IProgress<string>, IProgress<double>, IProgress<someCustomClass>, */
            var result = new StringBuilder($"Your mining operation started at UTC {DateTime.UtcNow}.");
            double progressPercentage = 0;
            double progressChunks = 100 / howMany;

            for(int i=0; i<howMany; i++) {
                await Task.Delay(1000);
                progressPercentage += progressChunks;

                // Implementation of IReport
                progress.Report((int)progressPercentage);
            }

            result.AppendLine($"Your mining operation ended at UTC {DateTime.UtcNow}.");
            result.AppendLine($"You've got {howMany} AsyncCoin!");
            return result.ToString();
        }

        #endregion

        #region base methods 

        private string CallCoinService(int howMany)
        {
            var uri = new Uri($"https://asynccoinfunction.azurewebsites.net/api/asynccoin/{howMany}");
            var webClient = new WebClient();
            var result = webClient.DownloadString(uri);
            return result;
        }

        private async Task<string> CallCoinServiceAsync(int howMany)
        {
            var uri = new Uri($"https://asynccoinfunction.azurewebsites.net/api/asynccoin/{howMany}");
            var webClient = new WebClient();
            var result = await webClient.DownloadStringTaskAsync(uri);
            return result;
        }

        private async Task CallCoinServiceNoResponseAsync(int howMany)
        {
            for (int i = 0; i < howMany; i++)
            {
                await Task.Delay(1000);
            }
        }

        private double MineAsyncCoinsWithNthRoot(int iterationMultiplier)
        {
            double allCoins = 0;
            for(int i = 1; i < iterationMultiplier * 2500; i++)
            {
                for(int j = 0; j <= i; j++)
                {
                    Math.Pow(i, 1.0 / j);
                    allCoins += .000001;
                }
            }
            return allCoins;
        }

        private Boolean AuthorizeTheToken(string token)
        {
            if(string.IsNullOrEmpty(token))
            {
                return false;
            }
            return true;
        }

        #endregion

    }

}