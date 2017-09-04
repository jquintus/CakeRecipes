# Recipes

Real world recipes and examples of using [Cake](http://CakeBuild.net)
in various environments to accomplish specifc tasks.

## Interacting with Build Servers

### TeamCity

[TeamCity](http://TeamCity.org) is a popular self-hosted CI server.
There is a [complete example](./samples/Recipes.TeamCity.cake) of a TeamCity aware script in the [samples](./samples) folder.

#### Only run a task when run on TeamCity

To ensure that an entire task is only run when you are on TeamCity build server, use the `.WithCriteria()` method on `Task` to selectively execute the Task and pass it in the statically available `BuildSystem.IsRunningOnTeamCity`.

```csharp
Task("Publish")
    .IsDependentOn("PackageArtifacts")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    string artifactsDir = @"./artifacts";
    BuildSystem.TeamCity.PublishArtifacts(artifactsDir);
});
```

#### Publish Build Output as a TeamCity Artifact

Build output of succesful builds can be marked as "artifacts" and will be saved by TeamCity so that they can be downloaded and used.  Typically this is used for installers, packaged coded, executables, or any other distributable file.  Artifacts can be created with the static method `BuildSystem.TeamCity.PublishArtifacts(pathToFile)`.

To publish a TeamCity artifact

1. Create a task to generate the artifact
1. Create a second task to publish that artifact
1. Use `WithCriteria` to only run the second task when in TeamCity

```csharp
Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreMSBuild(@"./HelloWorld/HelloWorld.csproj");
});

Task("PublishArtifact")
    .IsDependentOn("Build")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    string artifact = @"./HelloWorld/bin/Release/HelloWorld.exe";
    BuildSystem.TeamCity.PublishArtifacts(artifact);
});
```

#### Publish Packages to the TeamCity NuGet Feed

TeamCity comes with a NuGet feed server.  To configure it see the TeamCity documentation.  Once conifgured, any .nupkg file marked as an artifact in the build will be published to the feed.  From within cake, the `BuildSystem.TeamCity.PublishArtifacts(pathToFiles)` method will tell TeamCity which files to mark as artifacts for the build.

To publish a NuGet package to the TeamCity Feed:

1. Define an artifacts directory
1. Create a task to package your NuGet package
1. In that task set the output directory of the package to the artifacts directory
1. Create a second task to publish all the files in the artifacts directory
1. Use `WithCriteria` to only run the second task when in TeamCity

```csharp

// Define an artifacts directory
string artifactsDir = @"./artifacts";

Task("PackageArtifacts")
    .Does(() =>
{
    string slnFile = @"./HelloWorld.sln";
    var packSettings = new DotNetCorePackSettings
    {
        // Set the output directory of the
        // package to the artifacts directory
        OutputDirectory = artifactsDir,
    };

    DotNetCorePack(slnFile, packSettings);
});

Task("Publish")
    .IsDependentOn("PackageArtifacts")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    // Publish all the files in the artifacts directory
    BuildSystem.TeamCity.PublishArtifacts(artifactsDir);
});
```

#### Using the TeamCity Nuget Feed as a Source for Packages

There are two ways to configure TeamCity to use the included NuGet feed as a source

1. Configure a TeamCity Build Feature with the NuGet credentials
1. Log on to each TeamCity build agent and use the nuget.exe command line tool to add TeamCity as a source

##### Configure a TeamCity Build Feature

TODO

##### Use nuget.exe to add a source on each TeamCity build agent

Log on to each TeamCity build agent and run the following command at the command prompt.

```cmd
> nuget.exe sources -add -name "NuGet Feed" -source <FEED URL> -name <USERNAME> -password <PASSWORD>
```

Where:

* `<FEED URL>` is the path to the NuGet server on TeamCity
* `<USERNAME>` is the name of a TeamCity user that has been granted permissiosn for the NuGet feed
* `<PASSWORD>` is the password for the `<USERNAME>` user

#### Access TeamCity Parameters as Arguments

TeamCity exposes a wealth of environmetn and build information to each build, for example build numbers, source control branches, and passwords.  To access these you can either:

1. Pass them in from the TeamCity build step as script parameters
1. Set them as environment variables in the TeamCity parameters page and then read them from the environment in the cake script

TODO show screen shots setting up TC

```csharp
var config = Argument("branch", "<NONE>");
Information("Building the {0} branch", branch);
```
#### Access TeamCity Parameters as Environment Variables

TeamCity exposes a wealth of environmetn and build information to each build, for example build numbers, source control branches, and passwords.  To access these you can either:

1. Pass them in from the TeamCity build step as script parameters
1. Set them as environment variables in the TeamCity parameters page and then read them from the environment in the cake script

TODO show screen shots setting up TC

```csharp
var branch = EnvironmentVariable("vcsroot.branch");
Information("Building the {0} branch", branch);
```

#### Use the TeamCity Build Number If It's Available As the Build Number

#### Use the TeamCity Build Number If It's Available As the NuGet Package Version

#### Outputting to the TeamCity Build Log

## Dot Net Core

### Build

### Test

### Warn as Errors

