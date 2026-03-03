# Viewport / Code switcher
In order to be able to also edit the scene directly in SML we need a second tab "Source" where we display a CodeEditor with SyntaxHightling for SML.
The user can switch Tabs from Viewport to Source.

If the user adjusts the source also the scene shall be changed and will be visible immediatly. Viewport and Source Panel can be floating to put them on a different monitor, which is already normal behaviour of the DockingPanel.

Events shall be scripted in SMS.

The SML Source is the source of truth.

So the editor will also be filled with the scene code on scene load.

Based on the code changes in the editor we can now use history (undo/redo).
This shall also work for viewport changes. I mean, when an object has been moved. A move will only be tracked starting with mouse down and ends with mouse up. This shall be only one single undo/redo.

So easiest way is to alter the sourcecode only after mouseup.


## DND
The tabs Viewport and Source shall be drag and dropable in group 2 only.  So dont mix them with other panels.