using System.ComponentModel.DataAnnotations;

internal sealed class CpuWorkloadOptions
{
    public const string SectionName = "CpuWorkload";

    [Range(0, 2000)]
    public int DurationMilliseconds { get; set; } = 50;
}
