extends Control


	
func _ready():
	DisplayServer.window_set_size(Vector2i(1600, 900))

func _notification(what):
	if what == NOTIFICATION_RESIZED:
		print("ROOT:", size)
