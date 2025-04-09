using Godot;
using System;
using static Godot.TextServer;
/*
 * The script for the beam
 */

/*
 * The beam is an Area3D (detects when this enters or exits an object with an area)
 */
public partial class Beam : Area3D
{
    public int speed = 4;

    /*
     * How the beam interacts in the environment
     */
    public override void _PhysicsProcess(double delta)
    {
        //Finds the current direction of the knight and sets the beam to the same one
        Node3D player = GetParent<CharacterBody3D>().GetNode<Node3D>("Knight");
        Vector3 direction = player.GlobalTransform.Basis.Z.Normalized();
        var newPos = new Vector3();
        newPos.X += (float)(direction.X * speed * delta);
        newPos.Y += (float)(direction.Y * speed * delta);
        newPos.Z += (float)(direction.Z * speed * delta);
        GlobalPosition += newPos;

    }
    /*
     * When the beam collides with an enemy's fist or hitbox destroy the beam
     */
    public void _on_area_entered(Area3D area)
    {
        if(area.Name == "Fist" || area.Name == "EBox")
        {
            QueueFree();
        }
    }
}
