/*
 * Recipes for builds that interact with TeamCity <add link to TeamCity>
 */

var treatWarningsAs = (warnAsError.ToLower() == "true")
    ? MSBuildTreatAllWarningsAs.Error
    : MSBuildTreatAllWarningsAs.Default;

versionSuffix = GetVersionSuffix(versionSuffix);
var buildNumber = GetBuildNumber();
var version = buildNumber + versionSuffix;
var artifactsDir = @"./artifacts/";

// *****************************************************************************
// Create Build Settings
// *****************************************************************************
var buildSettings = new DotNetCoreMSBuildSettings().TreatAllWarningsAs(treatWarningsAs)
                                                   .SetConfiguration(config)
                                                   .SetVersion(version);
var packSettings = new DotNetCorePackSettings
{
    MSBuildSettings = buildSettings,
    OutputDirectory = artifactsDir,
};

// *****************************************************************************
// Helper Methods
// *****************************************************************************

string GetBuildNumber()
{
    // Read the build number from the environment
    var buildNumber = EnvironmentVariable("BUILD_NUMBER");
    if (string.IsNullOrEmpty(buildNumber))
    {
        // If we are building locally, we want to give the package an artificially high
        // version number that auto increments with each build.
        // High because we want to be higher than any version number we'd get from the server
        // Auto incrementing because we want to be able to upgrade to the newly built package easily
        // The scheme we're going with is:  9 followed by the year, day of year (date), and then the time in seconds
        var now = DateTime.UtcNow;
        buildNumber = string.Format("9.{0}.{1}.{2}"
            , now.Year
            , now.DayOfYear
            , now.Hour * 60 * 60 + now.Minute * 60 + now.Second);
    }

    return buildNumber;
}

string GetVersionSuffix(string versionSuffix = null)
{
    versionSuffix = versionSuffix
        ?? GetCurrentGitBranch()
        ?? GetCurrentBranchFromEnvironmentVariable()
        ?? string.Empty;

    if (versionSuffix.ToLower() == "ship")
    {
        versionSuffix = "";
    }

    versionSuffix = AfterLast(versionSuffix, '/');

    if (string.Empty != versionSuffix)
    {
        versionSuffix = "-" + versionSuffix;
    }

    return versionSuffix;
}

public string AfterLast(string input, char marker)
{
    var markerIndex = input.LastIndexOf(marker);

    if (markerIndex < 0)
    {
        return input;
    }
    else if(markerIndex >= input.Length)
    {
        return string.Empty;
    }
    else
    {
        return input.Substring(markerIndex + 1);
    }
}

string GetCurrentBranchFromEnvironmentVariable()
{
    var branch = EnvironmentVariable("vcsroot.branch");
    if (string.IsNullOrEmpty(branch)) branch = null;
    return branch;
}

string GetCurrentGitBranch()
{
    try
    {
        // Try to read the branch name to get the version suffix
        var branch = GitBranchCurrent(".");
        return branch.FriendlyName;
    }
    catch
    {
        return null;
    }
}

// *****************************************************************************
// ***************************** TASKS *****************************************
// *****************************************************************************
Task("Hello_World")
    .Does(() =>
{
    Information("Hello World");
});

// *****************************************************************************
// Pre-build Tasks
// *****************************************************************************
Task("Information")
    .Does(() =>
{
    Information("Building Solution   {0}", slnFile);
    Information("Build Configuration {0}", config);
    Information("Build Number        {0}", buildNumber);
    Information("Version Suffix      {0}", versionSuffix);
    Information("Version             {0}", version);
    Information("Warnings as         {0}", treatWarningsAs);
});

Task("Restore-NuGet-Packages")
    .WithCriteria(!string.IsNullOrEmpty(slnFile))
    .Does(() =>
{
    DotNetCoreRestore(slnFile);
});

Task("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => {

    // Create directories in case they don't exist
    CreateDirectory(artifactsDir);

    // Clean directories
    if (!string.IsNullOrEmpty(slnFile))
    {
        DotNetCoreClean(slnFile);
    }
    CleanDirectory(artifactsDir);
});

// *****************************************************************************
// Build, Sign, and Package Tasks
// *****************************************************************************
Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreMSBuild(slnFile, buildSettings);
});

Task("Sign")
    .IsDependentOn("Build")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    var strongPwd = EnvironmentVariable("StrongName003Password");
    var settings = new SignToolSignSettings
    {
        TimeStampUri = new Uri("http://timestamp.digicert.com"),
        CertPath = @"C:\share\Certificates\StrongNameSigning003.pfx",
        Password = strongPwd
    };

    Information("Signing dlls with certificate: {0}", settings.CertPath);

    var dlls = GetFiles("./**/bin/**/net4*/Dot.*.dll");
    foreach(var file in dlls)
    {
        Information("Strong-name signing {0}", file.FullPath);
        Sign(file.FullPath, settings);
    }
});

Task("CoreNuPack")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Sign")
    .Does(() =>
{
    DotNetCorePack(slnFile, packSettings);
});

// *****************************************************************************
// Testing Tasks
// *****************************************************************************
Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(HasTestProjects)
    .Does(() =>
{
    var settings = new DotNetCoreTestSettings
    {
        Configuration = config,
    };

    foreach(var testProject in TestProjects)
    {
        DotNetCoreTest(testProject, settings);
    }
});

bool HasTestProjects
{
    get { return TestProjects.Any(); }
}

IEnumerable<string> TestProjects
{
    get
    {
        var projectFilter = "./Tests/**/*Tests.csproj";
        var projectFiles = GetFiles(projectFilter);
        return projectFiles.Select(f => f.FullPath);
    }
}

// *****************************************************************************
// Publishing Tasks
// *****************************************************************************
Task("Publish")
    .IsDependentOn("CoreNuPack")
    .IsDependentOn("PublishTeamCity")
    .IsDependentOn("PublishLocal");

Task("PublishLocal")
    .IsDependentOn("CoreNuPack")
    .WithCriteria(!BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    // We'll want to implement this to copy the nuget packages to a
    // local folder that can be used as a local nuget package source
    Warning("Publishing locally has not been implemented yet");
});

Task("PublishTeamCity")
    .IsDependentOn("CoreNuPack")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    // Publishing a nuget package to TeamCity is as easy as adding it to the artifacts for a build
    BuildSystem.TeamCity.PublishArtifacts(artifactsDir);
});

// *****************************************************************************
// Entry Point Tasks
// *****************************************************************************
Task("DotNetCore_NugetPackage")
    .IsDependentOn("Information")
    .IsDependentOn("CoreNuPack")
    .IsDependentOn("Publish")
    .Does(() =>
{
    Information("Build and package successfully completed");
});
