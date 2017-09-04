# Recipes

Real world recipes and examples of using [Cake](http://CakeBuild.net)
in various environments to accomplish specifc tasks.

## Interacting with Build Servers

### TeamCity

[TeamCity](http://TeamCity.org) is a popular self-hosted CI server.
There is a [complete example](./samples/Recipes.TeamCity.cake) of a TeamCity aware script in the [samples](./samples) folder.

#### Only run a task when run on TeamCity

To ensure that an entire task is only run when you are on TeamCity build server, use the `.WithCriteria()` method on `Task` to selectively execute the Task and pass it in the statically available `BuildSystem.IsRunningOnTeamCity`.

##### Example

```csharp
Task("Publish")
    .IsDependentOn("PackageArtifacts")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    string artifactsDir = @"./artifacts";

    // Publishing a nuget package to TeamCity is
    //  as easy as adding it to the artifacts for a build
    BuildSystem.TeamCity.PublishArtifacts(artifactsDir);
});
```

#### Publish Packages to the TeamCity NuGet Feed

TeamCity comes with a a

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
