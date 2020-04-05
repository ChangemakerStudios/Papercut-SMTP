# Release Notes

## Papercut v5.7.0 [2020-04-05]
- Renamed product to "Papercut SMTP" to seperate from other products.
- Moved "Readme.eml" and logs to *\ProgramData\Changemaker Studios\Papercut SMTP* allowing it to be deleted.
- Changed default save directory for Papercut UI to *\%ApplicationData%\Changemaker Studios\Papercut SMTP*. Mail in legacy paths are still supported.
- Added support for embedded message mime types and the ability to save the embedded eml.
- Performance improvements loading messages.
- Misc bug fixes. 

## Papercut v5.6.0 [2020-04-02]
- [#143](https://github.com/ChangemakerStudios/Papercut/issues/143) Added "Delete All" button.
- Switch "Self Hosted Web" to use OWIN/Katana -- which should fix issues with binding.
- Added IP and port Papercut IPComm configuration in json settings.
- Improved message loading speed.
- Limited logging and added automatic logging pruning.

## Papercut v5.5.0 [2019-03-08]
- NOTE: Web UI is now only available in PapercutService.
- Switched to using [SmtpServer](https://github.com/cosullivan/SmtpServer) project for Papercut's SMTP Server.
- Added http:// link in log for Web UI improving discoverablity for the UI. Hopefully fixed the issues with the web ui binding.
- [#137](https://github.com/ChangemakerStudios/Papercut/issues/137) - Removed plugin architecture.
- [#96](https://github.com/ChangemakerStudios/Papercut/issues/96), [#132](https://github.com/ChangemakerStudios/Papercut/issues/132) - Fixed issue with receiving email as UTF-8 by default.
- [#105](https://github.com/ChangemakerStudios/Papercut/issues/105) - Fixed: Clicking target="blank" links in emails will now use the default browser.

## Papercut v5.2.16 [2019-02-01]
- Added http:// link in log for Web UI improving discoverablity for the UI. Hopefully fixed the issues with the web ui binding.
- StartHttpServer for the Web UI needs to be called in another thread -- it was causing hangs on loading.

## Papercut v5.1.88 [2018-12-02]
- Converted build to use Cake -- PR thanks to [tdue21](https://github.com/tdue21).
- Fix: Papercut Protocol failing during serialization due to incorrect JSON.NET settings PR thanks to [jennings](https://github.com/jennings).

## Papercut v5.1.76 [2018-08-16]
- [#122](https://github.com/ChangemakerStudios/Papercut/issues/122): Add "Papercut" to SMTP banner.

## Papercut v5.1.70 [2018-07-12]
- Hacky way to get a sort of "comment" into the settings file and keep them there. :)

## Papercut v5.1.23 [2018-03-18]
- [#50](https://github.com/ChangemakerStudios/Papercut/issues/50): When Message Sort Order is 'Descending' the 'current' message always moves to bottom.
- [#110](https://github.com/ChangemakerStudios/Papercut/issues/110): Support for AUTH LOGIN and AUTH PLAIN.
- Added authentication parameters to SmtpSession

## Papercut v5.1.19 [2018-03-04]
- [#94](https://github.com/ChangemakerStudios/Papercut/issues/94): Removed manifest/deployment for oneclick.

## Papercut v5.1.3 [2017-12-06]
- [#73](https://github.com/ChangemakerStudios/Papercut/issues/73): Feature enhancements for the WebUI plugin.
- [#81](https://github.com/ChangemakerStudios/Papercut/issues/81): Removed Click-Once.
- [#85](https://github.com/ChangemakerStudios/Papercut/issues/85): Forwaring rule to server with STARTTLS not supported.
- [#89](https://github.com/ChangemakerStudios/Papercut/issues/89): Forwarding messages to an SMTP server on a port other than 25.
- Added a check for the MessagePath configuration loader which will now soft fail if it cannot load the configuration.

## Papercut v5.0.9 [2017-09-13]
- [#4](https://github.com/ChangemakerStudios/Papercut/issues/4): URLs are decoded when clicking on link.
- [#46](https://github.com/ChangemakerStudios/Papercut/issues/46): Added support for SeqApiKey for issue #46 fixed.
- [#60](https://github.com/ChangemakerStudios/Papercut/issues/60): Simple bootstrapped MSI installer, thanks to [tdue21](https://github.com/tdue21).
- [#63](https://github.com/ChangemakerStudios/Papercut/issues/63): Basic web ui features, thanks to [jijiechen](https://github.com/jijiechen).
- Fixed the mispelling for Papercut.Module. 
- Upgrade to .NET v4.5 - Leaving the past behind. WebAPI layer is properly loading now.
- Plugin directory no longer needs to be copied.

## Papercut v4.8.0.3 [2017-03-29]
- [#46](https://github.com/ChangemakerStudios/Papercut/issues/46): Papercut.Service.json Options.
- [#52](https://github.com/ChangemakerStudios/Papercut/issues/52): ConnectionManager check for idle connections is incorrect.
- [#54](https://github.com/ChangemakerStudios/Papercut/issues/54): Border Color Doesn't Change with Theme Changes.
  
## Papercut v4.8.0.1 [2017-02-12]
- Removed "Always on disk logging"
- Theme Switcher
- Improved Option Handling
- Minimize to Tray Option

## Papercut v4.7.0.1 [2017-02-08]
- Bug fixes for Message Encoding

## Papercut v4.6.1.12 [2016-04-08]
- Merge branch 'master' into develop

## Papercut v4.6.1.8 [2016-04-07]
- Trying multiple artifacts.

## Papercut v4.5.0.0 [2016-01-26]
- Added Bcc support to message saving. Before the value was just getting thrown away.
- Fixes issue clicking some urls.
- Added conditional forward rule with retry.
- Fix for minor issue when saving and the window height/width is saved incorrectly.

## Papercut v4.4.0.0 [2015-06-25]
- Improvement: #9864 “Log View” added to show all internal logging information for that session.
- Fixed: #9874 Message loading exception was not properly handled causing an unhandled exception and failure.
- Fixed: #9872 On some systems there were permissions issues accessing the IP addresses for the local system causing an unhandled exception and failure.

Thanks to [Jstedfast](https://github.com/jstedfast) (author of [MimeKit](https://github.com/jstedfast/mailkit)) for his contribution:
- Rewrote and improved the logic that renders the email HTML.

Thanks to MikeWill34 for his contributions:
- Fixed: #9870 When Aero is disabled (on servers, for example), the windows didn’t have a drop shadow. Now they have a solid border.
  - Fixed: Mark of the Web “ActiveX” security warning on email display.
  - Improvement: Upgraded to Calburn Micro v2.0.

## Papercut v4.3.0.0 [2014-11-05]
  - Improvement: “Raw” message view re-added back as a view of the message data using AvalonEdit control for speed.

## Papercut v4.2.1.0 [2014-10-11]
  - Fixed: #9861 Exception if the specified file name for a mime part was duplicate.
  - Added: Web links in emails are launched in default browser. (Thanks Jamie Humphries!)
  - Added: Conditional Forward (RegEx) Rule based on body or headers.
  - Fixed: Added the "Mark-of-web" for the email viewer to remove IE security warnings.
  - Fixed: #9863 Papercut.Service was not correctly reporting it’s incoming mail directory to Papercut.Client.

## Papercut v4.2.0.0 [2014-8-23]
  - Fixed: #9859 Now using <pre> tag when displaying the text email so that carriage returns/linefeeds are displayed.
  - Fixed: #9858 Now won’t close the app when failing to delete a message. Displays the reason for the failure as well.
  - Improvement: Increased the robustness and error handling of the SMTP FIle Receiving.
  - Improvement: Now validates the existence of and creates (if necessary) the Default Message Save Path directory.
  - Improvement: Improved the loading so that it catches all assembly loading errors and attempts to write them to the Event Log in case app load is failing.
  - Changed: Set the protocol logging to "Verbose".
  - Changed: Papercut.Service default save location now defaults to %BaseDirectory%\Incoming.
  - Added: Remove the "read-only" flag from the message file when attempting to delete.
  - Added: Example Papercut.Service.json file with comments to explain the settings.

## Papercut v4.1.0.0 [2014-8-13]
  - Fixed: #9851 Exception on TCP/IP port binding failure.

## Papercut v4.0.0.0 [2014-7-24]
  - Added: Automatic “rules” system and configuration. Currently only supports a forward rule. But unlimited forwards can be specified.
  - Added: “Loading” UI
  - Added: Save window height/width.
  - Added support for rules to the backend service. Rules are configured by the front-end and pushed to the backend service automatically.
  - Switched to using MailKit instead of custom Smtp Client for additional options it offers.
  - Added authentication support to the forward rule.
  - Issue #9852: Converted Papercut.Service to use a json settings file. Papercut Client/UI will continue to use the .NET settings system do it it's complex system of overrides based on the current user account.
  - Improvement: Now properly disables the Message View if it’s disabled.
  - Improvement:  UI consistency -- added one place for Option and Exit links on the main UI.
  - Improvement: Added "Delete (count)" to the delete button.
  - Fixed: Issue #9849 – Null Reference in the Attachment/Mime Section
  - Improvement: SMTP Server failure handling and added additional logging to help with issue #9851.

## Papercut v3.0.0.0 [2014-6-8]

<p style="border: #000 1px solid; margin:4px; padding: 4px; width:50%;background-color: #A6A6A6">
<b>Papercut</b> has switched to semantic versioning! That means you will have to uninstall old "clickonce" versions to get the latest as it will see it as an older version.
</p>

  - Modern UI
  - MVVM Architecture
  - Watch Directories for New Messages
  - Optional Backend Papercut Service
  - Load on Windows Startup
  - Attachments/Mime Sections

## Papercut 2014-5-1
  - Fixed: Message list box scrolling caused unhandled exception.

## Papercut v2014-3-13
  - Fixed: Converted back to .NET v4 so that XP users are supported.
  - Fixed: Running papercut under SYSTEM account will now store messages in the application base directory instead of the \User\AppData\Papercut directory.
  - Change: Using MimeKitLite instead of full MimeKit.

## Papercut v2014-3-8
  - Change: Application now using the excellent [MimeKit](https://github.com/jstedfast/mimekit) library. Faster and better support for mime types.
  - Change: Removed the the slow and annoying "raw view" and added "header view.”
  - Change: Removed the Redundant "Html" view and added the "Text" view.
  - Change: Changed "Close" to "Exit" as the close or minimize window button has the close functionality. It's nice to be able to exit Papercut from the main app window.
  - Feature: Thanks to a little assistance from this answer on stack overflow (http://stackoverflow.com/questions/14103159/previewmouseleftbuttondown-interferes-with-clic-selection).
  - Finally implemented that drag&drop message file feature. Now you can drag and drop a message file.
  - Change: Saves and loads message in AppData\Papercut.
  - Feature: Now you can delete a Message with the Delete Key (#9835) - message content from last message is now correctly cleared (#9840)
  - Feature: Changed Processing to Buffered File output for speed, etc.
  - Fixed: Issue #6026 -- Supports "Minimize on Close" option.
  - Feature: Added CC and BCC to field list aka issue #9836.
  - Feature: Added support for "dot stuffing", per RFC 2821, section 4.5.2.
  - Fixed: FormatException on empty ReplyTo
  - Fixed: fixed crash on start when there is an email with 0 byte.
  - Fixed: Issue with email address parsing.