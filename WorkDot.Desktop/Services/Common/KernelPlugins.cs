using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace WorkDot.Services.Common
{
    public class KernelPlugins
    {
        [KernelFunction, Description("Retrieves the current time in UTC.")]
        public string GetCurrentUtcTime()
            => DateTime.UtcNow.ToString("R");
    }
}