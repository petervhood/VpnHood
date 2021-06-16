﻿# v1.2.246
* Feature: Add UDP Protocol
* Update: Improve datagram performance
* Update: Improve overall performance
* Update: Improve messaging security
* Update: Improve Stability
* Fix: Problem in sending some UDP packets
* Fix: Json length is too big

### Developer
* Upgrade to SharpPcap 6.0

# v1.1.242
### Client
* Update: Windows: Installer check for new updates before installation

# v1.1.241
### Client
* Fix: Freeze in Disconnecting state
* Fix: Reconnection

# v1.1.240
### Client
* Fix: Diangnostic report "No Internet", when there is internet 
* Update: Windows: Change Updater

# v1.1.238
### Client
* Feature: Set allowed or disallowed Apps that can use VPN
* Update: Windows & Linux: Check TargetFramework before update
* Update: Show warning for Public Server

# v1.1.236
### Client
* Fix: Android: Crash when sending feedback on Android 11
* Fix: Connection already in progress error when changing server
* Update: Show traffic speed

### Server
* Update: Auto restart if VpnHoodServer stops unexpectedly
* Fix: Typo error in default.pfx filename for FileAccessServer
* Fix: linux: Stop working after server update

# v1.1.235
### Client
* New: New public server
* New: Windows: Bypass local network from tunneling

# v1.1.232
### Client
* New: Android: Prevent landscape orientation
* Update: Significantly improve speed and stability
* Update: Automatically remove profiles when token does not exist
* Update: add some log EventId
* Fix: UDP loss in mass UDP traffic

### Server
* New: Send ClientVersion to AccessServer
* Update: drop Hello version 1 support
* Update: Significantly improve speed and stability
* Update: Automatically remove profiles when token does not exist
* Update: add some log EventId
* Fix: token is ignored when created by FileAccessServer
* Fix: UDP loss in mass UDP traffic

# v1.1.217
### Server
* New: Rest server validate Self-Signed certificates by RestCertificateThumbprint property in appsettings

# v1.1.216
* New: Updater has completely changed

### Server
* New: Add stop command to stop all server instance
* New: Linux: Add installation script
* New: Linux: Run server as a service
* Change: rename "run" command to "start"

# v1.1.202
### Client
* New: Change server list page
* New: Android: Change system status bar color to match UI
* New: Windows: Change icon on notification area by connection status
* Fix: Big UI on some devices
* Update: Change Public Server Name

### Server
* Update: Start new log file on every run

# v1.1.197
### Client
* Fix: rejecting accesskey with vh://

### Server
* New: Report Linux Distribution info
* New: Report connected ClientVersion
* Fix: "Permission Denied" error in Linux while sending some UDP packets

# v1.1.195
### Client
* Feature: Modern UI
* Feature: Show usage if there is any limitation
* Feature: Windows: reconnect last connection after auto update
* Fix: Windows: Fix main window size
* Fix: Windows: launch application after installation

### Server
* Fix: Use last command line argument after auto update

# v1.1.187
### Client
* Feature: Windows: Use new standalone UI
* Feature: Windows: Add Context menu to system tray
* Update: Add Microsoft WebView2 Edge to Windows Installer prerequisites
* Update: Send ClientVersion to server
* Fix: accesskey prefix

### Server
* Fix: Reading server port number from appsettings.json
* Update: Support multiple public IP and Amazon ElasticIP

# v1.1.184
### Client
* Feature: Auto Configure Windows Defender Firewall
* Update: Improve diagnosing
* Fix: Significantly Improve connection stability & speed
* Fix: Displaying connection state

### Server
* Fix: Unhandled NullReferenceException on ping packets
* Fix: Improve server memory cleanup
* Fix: Prevent new conenction after session disposed
* Fix: Speed Monitor and connection idle state
* Fix: Improve connection stability and lost packets
* Feature: ICMP logging for client and server with IsDiagnoseMode
* Feature: Use NLog for logging
* Feature: Auto initialize NLog config and appsettings.json

# v1.1.177
* Fix: Client close the entire VPN connection when a requested site refuse a connection

# v1.1.176
* Feature: Client can detect its expired session

### Client
* Change: Always Open the main window at start if App is already running

### Developer
* Change: Update TcpDatagramHeader from binary to TcpDatagramChannelRequest json
* Change: Move IDevice and IPacketCapture to VpnHood.Client.Device module
* Developer: Add Simple Sample for Windows Client usage
* Developer: Fix PublishApps.ps1 scripts to create publish folder when it does not exist

# v1.1.138
* Fix: Checking update from the Internet

### Server
* Update: add subdomain when creating self-signed certifiates with random CN

# v1.1.91
* Fix: AppUpdater throw error if UpdateUrl in publish.json was empty string

### Client
* Update: Add client prefix to Bug Report File Name
* Update: Close Bug Report bottom page after sending report
* Update: Separate SPA from VpnHood.Client.App.UI. Make it easier for developers to use custom SPA
* Update: Change Anonymous IP masking from *.*.x.x to "*.x.x.*"
* Update: Diagnose set Last error to "Diagnose has been finished" if there is not other error
* Fix: Dark Icon
* Fix: Open BugReport page on external web browser
* Fix: Disable Diagnose button when a connection already diagnosing
* Fix: Reporting .NET version instead of App Version

# v1.1.75
* Initial Release








































