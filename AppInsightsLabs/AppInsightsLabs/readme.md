== Summary ==
When having set up a continuous export to a Storage Account - this sample will give an interface to observe these logs in real time.
Or; let you read all logs from the last hour, day, or whatever.
You can distinct what you want by traces, exceptions and events. There are more AI types, but not supported here yet. Feel free to create
pull requests.

== Export == 
When the data is available in AI, it is available in the Storage Account almost instantly.
Why export to a Storage Account when it is available in AI and its API? Because it's much cheaper.

== Features ==
* Real time polling of a storage account containing AI logs
* Fetch all logs from a given hour or day
* Support for custom dimensions (properties of logs)

== Usage ==
You can either use the AiObserver class and hookup the events to stream data (polling with interval),
or you can use the BlobReader to fetch whatever you need.

See program.cs  for usage example.
	* How to start polling
	* How to fetch all logs from the latest hour
	* How to fetch all logs from the latest day and its hours

Included a Windows Forms app as a bonus, to write logs to Application Insights so you can test easily.

== Points of interest ==
Flushing away stuff to Application Insights is not instant. An internal buffer is built up, and depending on
the TelemetryChannel, batches of telemetry gets sent to the AI endpoint. I have tried to manipulate this
in many ways to have it more "real time" - but to no avail. For this solution, it may take betwen 1-20 minutes before
anything actually shows up in AI Analytics portal.
However - the export to the Storage Account seem instant (and probably the AI API).