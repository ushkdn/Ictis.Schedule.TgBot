using Microsoft.Extensions.Configuration;

namespace ictis.schedule.core;

public static class DotEnvLoad
{
    public static IConfigurationRoot Load()
    {
        var projectDirectory = AppContext.BaseDirectory;

        var solutionDir = TryGetSolutionDirectoryInfo();
        var dotEnvPath = Path.Combine(solutionDir.FullName, ".env");

        if (!File.Exists(dotEnvPath))
        {
            throw new FileNotFoundException("Unable to find configuration (.env) file.");
        }

        foreach (var line in File.ReadAllLines(dotEnvPath))
        {
            var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                continue;
            }
            Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }

        return new ConfigurationBuilder().AddEnvironmentVariables().Build();
    }

    private static DirectoryInfo TryGetSolutionDirectoryInfo()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        return directory;
    }
}