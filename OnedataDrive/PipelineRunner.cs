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
            catch (OperationCanceledException ex)
            {
                Debug.Print("Startup CANCELED: {0}", ex);
                // Undo in reverse order
                foreach (var step in executedSteps)
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
            }
            catch (Exception ex)
            {
                Debug.Print("Startup FAILED: {0}", ex);
                throw;
            }
        }
    }

    internal class Step
    {
        public required string Name { get; init; }
        public required Func<CancellationToken, Task> Run { get; init; }
        public required Func<Task> Undo { get; init; }
    }
}
