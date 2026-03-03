# Scene View
In order to be able to hide or lock objects we need two toggle buttons in the treeview.

Also we need an event handler in main.sms

Eye
![eye](/ForgePoser/assets/images/eye_open.png)
![eye](/ForgePoser/assets/images/eye_closed.png)

Lock (TBD)


Next thing is the ability to rename objects. Doubleclick and edit inplace.
New name should be stored in SML on save.
Instead of the star icon, highlight der Character with different color like a lighter color than the default.

Display the bones panel only when the character ist selected.

In case a character is selected display the bones panel on dockRight and inspector on dockRightBottom
In case an asset is selected display an inspector panel on dockRightBootom.

Inspector panel shall show Position, Rotation, Scale and Pivot Point.
The icon next to PivotPoint centers the pivot (with tooltip ""Center Pivot).

![](/images/transform.png)
![](/images/transform_detail.png)

The widget behaves like the following. Click into and drage left right to change value. We need this Element reusable in ForgeRunner.

Changing value here will immediatly change the object selected in Viewport.