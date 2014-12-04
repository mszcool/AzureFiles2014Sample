Param
(
	[Parameter(Mandatory=$true)]
	[string]
	$location,

    [Parameter(Mandatory=$true)]
    [string]
    $servicePrefixName,

    [Parameter(Mandatory=$true)]
    [string]
    $shareName,
    
    [Parameter(Mandatory=$true)]
    [string]
    $localDirectoryToCopy,

    [Parameter(Mandatory=$false)]
    [bool]
    $skipFileCopy = $false
)


#
# Craft the names for the assets based on the prefix name
#
$storageAccountName = $servicePrefixName + "storage"
$cloudServiceName = $servicePrefixName + "service"

$azCopyPath = "C:\Program Files (x86)\Microsoft SDKs\Azure\AzCopy\AzCopy.exe"
$workingDir = (Get-Location).Path
$solutionName = ".\AzureFilesTest.sln"
$cloudServiceProjectName = ".\AzureFilesTest\AzureFilesTest.ccproj"

$msBuildTargetConfig = "Cloud"
$msBuildPublishDir = $workingDir + "\publish\"
$msBuildPath = "C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"


#
# Print some help in terms of what the script is doing
#
Write-Host ""
Write-Host "This script will setup the Azure Files Demo to be ready to go!"
Write-Host "(1) Create a storage account '" $storageAccountName "' if it does not exist, yet!"
Write-Host "(2) Create an Azure Files share called '" $shareName "' and copy files from the local directory '" $localDirectoryToCopy "' to that share using azcopy!"
Write-Host "(3) Create a cloud service called '" $cloudServiceName "' if it does not exist, yet!"
Write-Host "(4) Build the Visual Studio solution and create a cloud service package for the web role!"
Write-Host "(5) Deploy the cloud service package if there's no deployment, yet!"
Write-Host ""


#
# Test if the current working directory is the solution directory
#
if(!(Test-Path -Path ([System.String]::Concat($workingDir, $solutionName))))
{
    throw "Solution " + $solutionName + " not found, make sure you're working in the directory of the sample solution!!"
}
if(!(Test-Path -PAth ([System.String]::Concat($workingDir, $cloudServiceProjectName))))
{
    throw "Azure Cloud Service Project " + $cloudServiceProjectName + " not found, make sure you're working in the directory of the sample solution!!"
}


#
# Test if msbuild is present
#
if(!(Test-Path -Path $msBuildPath))
{
    throw "Cannot find msbuild at path '" + $msBuildPath + "'! Please correct the msBuildPath variable in the script to match your msbuild-path!"
}


#
# Test if azcopy is present
#
if(!(Test-Path -Path $azCopyPath))
{
    throw "Cannot find azcopy at path '" + $azCopyPath + "'! Please make sure you have Azure Storage Command Prompt installed in its latest version on your machine or correct the script to point to the correct path!"
}


#
# Testing if the Azure Powershell CmdLets are installed (either resource manager or service management)
#
$azureModule = Get-Module -ListAvailable Azure*
if($azureModule -eq $null) 
{
    throw 'Need to install and setup the Azure Powershell CmdLets before executing this script!'
}


#
# Switch to Azure Service Management
#
Switch-AzureMode -Name AzureServiceManagement
$subscription = Get-AzureSubscription -Current
if( $subscription -eq $null) 
{
    Write-Host "You don't have an active subscription, please select one!" -ForegroundColor Yellow
    Select-AzureSubscription -Current
}


#
# Start creating the storage account
#
Write-Host
Write-Host "(1) Creating the storage account if it does not exist, yet..."
$storageAccount = Get-AzureStorageAccount -StorageAccountName $storageAccountName -ErrorAction SilentlyContinue
if( $storageAccount -eq $null )
{
    Write-Host "- Storage Account " $storageAccountName " does not exist, creating one..."
    $storageAccount = New-AzureStorageAccount -StorageAccountName $storageAccountName -Type Standard_LRS -Label $storageAccountName -Location $location
    Write-Host "- Succeeded creating storage account!"
}
else 
{
    Write-Host "- Storage Account " $storageAccountName " exists, already!"
}
Write-Host "- Retrieving Storage Account Keys..."
$storageAccountKeys = Get-AzureStorageKey -StorageAccountName $storageAccountName
if( $storageAccountKeys -eq $null) 
{
    throw "Unable to retrieve storage account keys for storage account supposed to be created during the script!"
}
Write-Host "- Succeeded getting storage Account Keys..."
Write-Host "- Setting current storage account for subscription to the previously created storage account..."
Set-AzureSubscription -SubscriptionName $subscription.SubscriptionName -CurrentStorageAccountName $storageAccountName


#
# Create the Azure Files Share 
#
Write-Host
Write-Host "(2) Creating a files share in the storage account..."
$storageAccountContext = New-AzureStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKeys.Primary
Write-Host "     - Created Storage Account Context for previously created storage account"
$storageShare = Get-AzureStorageShare -Name $shareName -Context $storageAccountContext -ErrorAction SilentlyContinue
if( $storageShare -eq $null )
{
    Write-Host "- Storage share does not exist, creating it..."
    New-AzureStorageShare -Name $shareName -Context $storageAccountContext
    Write-Host "- Succeeded creating storage account!"
}
else 
{
    Write-Host "- Skipping creation of files share as it does exist, already!"
}
Write-Host "- Using azcopy to copy files and directory sfrom " $localDirectoryToCopy " to the files share..."
if( $skipFileCopy -eq $false )
{
    $azCopyParameters = " /Source:" + $localDirectoryToCopy + `
                        " /Dest:https://" + $storageAccountName + ".file.core.windows.net/" + $shareName + `
                        " /DestKey:" + $storageAccountKeys.Primary + `
                        " /S"
    Write-Host "-" $azCopyPath $azCopyParameters
    $azCopyProcess = Start-Process -FilePath $azCopyPath `                                   -ArgumentList $azCopyParameters `
                                   -PassThru -Verbose -NoNewWindow -Wait
    if( $azCopyProcess.ExitCode -ne 0 )
    {
        throw "Failed copying files from the local directory to the azure files share!"
    }
}
else 
{
    Write-Host "Skipping azcopy process to copy files to the Azure Files Share!"
}


#
# Next create a cloud service if it does not exist, yet
#
Write-Host
Write-Host "(3) Creating cloud service if it does not exist, yet..."
$cloudService = Get-AzureService -ServiceName $cloudServiceName -ErrorAction SilentlyContinue
if( $cloudService -eq $null) 
{
    Write-Host "    - Cloud service " $cloudServiceName " does not exist, creating one..."
    New-AzureService -ServiceName $cloudServiceName -Location $location -Label $cloudServiceName
    Write-Host "    - Cloud service created successfully..."
}
$cloudService = Get-AzureService -ServiceName $cloudServiceName
if( $cloudService -eq $null )
{
    throw "Unable to retrieve cloud service, so creation must have failed!"
}


# 
# Now build the solution and create the cloud service package to be uploaded
#
Write-Host
Write-Host "(4) Building the solution and creating the cloud service package..."
$buildProcA = Start-Process -FilePath $msBuildPath -PassThru -Verbose -NoNewWindow -Wait
if( $buildProcA.ExitCode -ne 0 )
{
    throw "Failed building the solution using msbuild!"
}

$buildPackageArgs = $cloudServiceProjectName + " /t:Publish /p:PublishDir=" + $msBuildPublishDir + " /p:TargetProfile=" + $msBuildTargetConfig
$buildProcB = Start-Process -FilePath $msBuildPath `
                            -ArgumentList  $buildPackageArgs `
                            -PassThru -Verbose -Wait -NoNewWindow
if( $buildProcB.ExitCode -ne 0)
{
    throw "Failed creating the cloud service package for being published to Azure!"
}


#
# Finally publish the built package 
#
Write-Host
Write-Host "(5) Deploying the cloud service package to the previously created cloud service!"
$existingDeploy = Get-AzureDeployment -ServiceName $cloudServiceName -Slot Production -ErrorAction SilentlyContinue
if ( $existingDeploy -eq $null )
{
    Write-Host "- Creating new deployment..."
    New-AzureDeployment -ServiceName $cloudServiceName `
                        -Slot Production `
                        -Package ($msBuildPublishDir + "AzureFilesTest.cspkg") `
                        -Configuration ($msBuildPublishDir + "ServiceConfiguration." + $msBuildTargetConfig + ".cscfg") `
                        -Name 'AzureFilesSampleNew'
}
else
{
    Write-Host "- Upgrading existing deployment..."
    Set-AzureDeployment -ServiceName $cloudServiceName `                        -Slot Production `
                        -Upgrade `
                        -Package ($msBuildPublishDir + "\AzureFilesTest.cspkg") `
                        -Configuration ($msBuildPublishDir + "\ServiceConfiguration." + $msBuildTargetConfig + ".cscfg") `
                        -Name 'AzureFilesSampleUpgrade'
}
Write-Host "- Deployment done, getting deployment ID..."
$existingDeploy = Get-AzureDeployment -ServiceName $cloudServiceName -Slot Production
if( $existingDeploy -ne $null )
{
    Write-Host "- Successfully retrieved deployment, deployment ID = " $existingDeploy.DeploymentId " " $existingDeploy.DeploymentName
}