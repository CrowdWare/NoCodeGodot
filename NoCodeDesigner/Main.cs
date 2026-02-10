using Godot;
using System;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Started");
		GD.PrintErr("Error");
		PrintSuccess("OK");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>
	/// Prints a green, bold message to the console.
	/// </summary>
	/// <param name="what">
	/// Message that will be printed.
	/// </param>
	void PrintSuccess(string what)
	{
		GD.PrintRich($"[b][color=green]{what}[/color][/b]");
	}
}
