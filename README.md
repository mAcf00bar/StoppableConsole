# StoppableConsole

Requirements:

- c#, 
- .net framework 4.7.2
- console application
- the application has to check if it is the first or any other consecutive running instance
- the first instance must have the ability to run a long running background task
- the background task has to be stoppable by receiving a "stop" command sent from any other instance via 
    - named pipes
- the check if a message has been received should be implemented in it's own background thread
- do not use async/await.
- use Serilog for logging
