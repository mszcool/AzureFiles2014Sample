Simple example demonstrating how-to use Azure Files in a Web/Worker Role.

The share needs to be mounted in code so that the application under which the application runs, has access to the share. This is specifically important if you are running a WebRole since the application then runs in a w3wp.exe under a different user as e.g. a Startup Task.

Execute the following PowerShell script included in the repository to setup the complete environment:

.\DeploySample.ps1 -location "North Europe" `
                   -servicePrefixName "mszfilestest" `
                   -shareName "fileshare" `
                   -localDirectoryToCopy W:\local\AzureFilesTest