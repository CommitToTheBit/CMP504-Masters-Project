using Godot;
using System;

public partial class DialogueEntry : RichTextLabel
{
    public override void _Ready()
    {
        Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.33f);
        tween.Play();
    }
}
