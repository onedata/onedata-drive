# Onedata Cloud Sync

## Requirements
- dotnet 8 must be installed
- Windows version -  **Windows 10, version 1803** and later
- might **not work** in **Windows Sandbox**
- virtual machines (e.g. Hyper-V) should work fine
- you should not need to run as admin, as long you have access to root_path (see config)

## How to launch compiled app
- launch from console (powershell)

    ``
    ./cloudSync.exe -p "C:\Users\User\Desktop\config.json"
    ``

- .exe file takes max 1 argument -> path to the config file
    - if not specified, it looks for config file in folder where is located

### Launch arguments
- **-p %PATH%**
    - path to the config file
- **-r**
    - when app crashes, launch with -r option to unregister dead SyncRoot
- **-r %ID%**
    - **ID** of SyncRoot, which you want to unregister
- **-d**
    - can be used only in combination with **-p**
    - deletes existing SyncRoot folder during launch

### config.json
    {
    "zone_host" : "datahub.egi.eu",
    "provider_token" : "abcd...",
    "root_path" : "C:\\Users\\User\\Desktop\\onedata-cloud-sync\\syncRoot\\"
    }
- fields must not be empty
- **root_path** must end with `\` (double `\\` -> character escaping)
    - path to the local client folder
    - folder must be empty
- **provider token** must have access to providers (onezone access is not required)
    - limiting token access to specific paths (during token creation) might not work as expected with spaces of which you are not owner

## Running the app
- when launched, wait for the app to create placeholders, after that you can work with 
files
- when you trigger any event (e.g. delete file) wait until there are no more new log messages
### Controls
- `R` gets latest placeholders from server
- `ENTER` terminates the app

## Required packages (for develompent and compilation)

``
dotnet add package Vanara.PInvoke.CldApi
``
- install to use win32 api (cloudfilter)