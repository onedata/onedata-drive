# Onedata Drive

## Requirements
- dotnet 8 must be installed
- Windows version -  **Windows 10, version 1803** and later
- might **not work** in **Windows Sandbox**
- virtual machines (e.g. Hyper-V) should work fine
- you should not need to run as admin, as long you have access to root_path (see config)

## Running the app
- in order to access files Onedata Drive must be running
- every time you restart computer/app and connect again new session is created
- connecting to big spaces (with many folders) might take a long time (up to several minutes)
- **Oneprovider Token** must have REST/CDMI access

### Filling the connect form
You can fill the connect form manually or you can create [config file](#config-file) and load it from menu (Advanced -> Load configuration from file)

### Config file
JSON file type

    {
    "onezone" : "datahub.egi.eu",
    "provider_token" : "abcd...",
    "root_path" : "C:\\Users\\User\\Desktop\\onedata-cloud-sync\\syncRoot\\"
    }
- **root_path** (double `\\` -> character escaping)
    - path to the local client folder
    - original content of the folder will be deleted
- **provider token**
    - must have REST/CDMI access
    - must have access to providers (onezone access is not required)
    - limiting token access to specific paths (during token creation) might not work as expected with spaces of which you are not owner

## Required packages (for develompent and compilation)

``
dotnet add package Vanara.PInvoke.CldApi
``
- install to use win32 api (cloudfilter)