
== Summary ==
When having set up a continuous export to a Storage Account - 
this sample will give an interface to observe these logs in real time,
or let you fetch logs from the last hour, day, or whatever.
You can distinct them by traces, exceptions and events.

== Export == 
When the data is available in AI, it is available in the Storage Account almost instantly.
Why export to a Storage Account when it is available in AI and its API? It's much cheaper.

== Usage ==
You can either use the AiObserver class and hookup the events to stream data (polling with interval),
or you can use the BlobReader to fetch whatever you need.

See program.cs  for usage example.
	* How to start polling
	* How to fetch all logs from the latest hour
	* How to fetch all logs from the latest day and its hours

Included a Windows Forms app as a bonus, to write logs to Application Insights so you can test easily.