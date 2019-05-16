using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;

namespace ExpertTools.TechLog
{
    public class TechLogEventReader
    {
        string[] logFiles;
        Action<string> callback;

        public TechLogEventReader(string logParentFolder, Action<string> callback)
        {
            logFiles = TechLogHelper.GetLogFiles(logParentFolder);
            this.callback = callback;
        }

        public TechLogEventReader(LogCfg logcfg, Action<string> callback)
        {
            logFiles = TechLogHelper.GetLogFiles(logcfg);
            this.callback = callback;
        }

        public async Task Start()
        {
            // Options of the many-threads blocks
            // Limit a max bounded capacity to improve a consumption of memory
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 1000 };
            
            // Blocks of the processing data
            var eventBlock = new ActionBlock<string>((text) => callback(text), parallelBlockOptions);

            // Reading tech log block
            var readBlock = new ActionBlock<string>((filePath) => TechLogHelper.ReadLogFile(filePath, eventBlock), parallelBlockOptions);

            foreach (var file in logFiles)
            {
                await readBlock.SendAsync(file);
            }

            // Mark block as completed
            readBlock.Complete();

            await readBlock.Completion.ContinueWith(c => eventBlock.Complete());

            await eventBlock.Completion;
        }
    }
}
