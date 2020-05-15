
# MQ2DotNetCore

This project is a re-write of MQ2DotNet using the .Net Core runtime host and targeting .Net Core 3.1

## Installation

1. The `MQ2DotNetCoreLoader.dll` needs to go in the MQ2 root folder alongside the other plugin dlls
2. The `nethost.dll` needs to go in the EQ game folder.
3. The build output for the `MQ2DotNetCore` project needs to go in an `MQ2DotNetCore` subfolder within the MQ2 root folder.
4. Any programs you wish to run with the `/netcorerun program` command have their build output placed in a folder `<mq2root>/MQ2DotNetCore/Programs/<programname>`

## Local Repo Setup

1. Clone the repo
2. In the repo root, add a `local.env.props` file. This is a `.gitignore` ignored file where you can define your local specific project properties.
3. In the new `local.env.props` define the shared variables listed in the `directory.build.props` file and specify your local values.
    a. `<MQ2SourceRootFolder />` - This build variable specifies where the mq2 repo is so the c++ loader plugin can find them to build against.
	b. `<EQInstallRootFolder />` - Not currently used. I plan to update the c++ loader plugin's project to support automatically copying the `nethost.dll` into this folder after building.
	c. `<MQ2InstallLiveRootFolder />` - Used by the post build tasks `<DeployMQ2DotNetCoreFilesAfterBuild />` or `<DeployProgramFilesAfterBuild />` are true. When true the post build tasks will automatically copy any modified files into the MQ2 release folder, renaming locked files if necessary.

