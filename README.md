# NAudio ASIO PatchBay Sample

This simple sample project shows how to perform low-level routing of audio using ASIO in NAudio. 
The WPF demo UI lets you route and pan the first two inputs of your ASIO device to the first two outputs.
It could easily be extended for ASIO devices with much larger numbers of inputs and outputs.
Supports the most common ASIO Sample Formats.

## Future improvement ideas

* Update UI to support mixing any amount of each input into any output
* Aggressive performance optimisation of `AsioInputPatcher` class
* Support for addition ASIO sample formats


