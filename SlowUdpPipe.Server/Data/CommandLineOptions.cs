using CommandLine;

namespace SlowUdpPipe.Server.Data;

public class CommandLineOptions
{
    [Option('c', "config", Required = true, HelpText = "Path to config file")]
    public string? ConfigFilePath { get; set; }

}