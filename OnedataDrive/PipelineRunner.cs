using NLog;
using System.Diagnostics;

namespace OnedataDrive
{
    internal class PipelineRunner
    {
        private List<Step> _step = new();
        private Logger logger;

        public PipelineRunner(Logger logger)
        {
            this.logger = logger;
        }

        public void AddStep(Step step)
        {
            _step.Add(step);
        }

        public void AddSteps(IEnumerable<Step> steps)
        {
            _step.AddRange(steps);
        }

        public async Task RunAsync(CancellationToken token)
        {
            Stack<Step> executedSteps = new();
            try
            {
                foreach (Step step in _step)
                {
                    token.ThrowIfCancellationRequested();
                    await step.Run(token);
                    executedSteps.Push(step);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Undo steps.");
                foreach (Step step in executedSteps)
                {
                    try
                    {
                        await step.Undo();
                    }
                    catch (Exception undoEx)
                    {
                        // Log the exception from Undo, but continue with other undos
                        Debug.Print($"Error during undo of step '{step.Name}': {undoEx}");
                    }
                }
                if (ex is OperationCanceledException)
                {
                    Debug.Print("Startup CANCELED: {0}", ex);
                }
                else
                {
                    Debug.Print("Startup FAILED: {0}", ex);
                }
                throw;
            }
            Debug.Print("Startup SUCCEEDED.");
        }
    }

    public class Step
    {
        public required string Name { get; init; }
        public required Func<CancellationToken, Task> Run { get; init; }
        public required Func<Task> Undo { get; init; }
    }
}
