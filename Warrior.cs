using Godot;
using System;
using System.Threading.Tasks;
/*
 * The script for the Warrior enemy character
 */

/*
 * The Warrior class
 * A CharacterBody3D (the node used to determine the characters movements and interactions)
 */
public partial class Warrior : CharacterBody3D
{
    public AnimationPlayer player;

    public int health = 5;

    public NavigationAgent3D agent;

    public int speed = 3;

    public int acceleration = 7;

    public bool attacking = false;

    public Timer limitTime;
    /*
     * Initializes different variables used throughout the rest of the script (navigation agent and timer)
     */
    public override void _Ready()
    {
        player = GetNode<Node3D>("Skeleton_Warrior").GetNode<AnimationPlayer>("AnimationPlayer");
        agent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        limitTime = GetParent<Node3D>().GetParent<Node3D>().GetNode<Timer>("LimitTime");
        //Increases the total count of enemies
        Globals.totalEnemies++;
    }

    /*
     * Controls how the enemy warrior interacts with the environment
     */ 
    public override void _PhysicsProcess(double delta)
    {
        var direction = new Vector3();
        //Enemy is targetting the player
        agent.TargetPosition = GetParent<Node3D>().GetParent<Node3D>().GetNode<CharacterBody3D>("Player").Position;

        //Direction is toward the player
        direction = agent.GetNextPathPosition() - GlobalPosition;
        direction = direction.Normalized();

        if (direction != Vector3.Zero)
        {
            //direction = direction.Normalized();
            Basis = Basis.LookingAt(direction);
        }

        var velocity = new Vector3();
        //Stops the enemy just before (2 meters) the player
        if(Position.DistanceTo(agent.TargetPosition) <= 2)
        {
            speed = 0;
        }
        //Resets speed when they are far enough away
        else
        {
            speed = 3;
        }
        //Smoothly moves the enemy towards the player
        velocity.X = Mathf.Lerp(Velocity.X, direction.X * speed, acceleration * (float)delta);
        velocity.Y = Mathf.Lerp(Velocity.Y, direction.Y * speed, acceleration * (float)delta);
        velocity.Z = Mathf.Lerp(Velocity.Z, direction.Z * speed, acceleration * (float)delta);

        Velocity = velocity;

        MoveAndSlide();

        Animate();
    }

    /*
     * Animates the enemy
     */
    private async Task Animate()
    {
        var collider = GetNode<CollisionShape3D>("Skeleton_Warrior/Rig/Skeleton3D/BoneAttachment3D/Fist/CollisionShape3D");
        if (!attacking) 
        {
            collider.Disabled = true;
            player.Play("Walking_D_Skeletons");
        }
        else
        {
            collider.Disabled = false;
            player.Play("Unarmed_Melee_Attack_Punch_A");
        }
    }

    /*
     * After being hit by the player, takes damage
     */
    public void TakeDamage()
    {
        //If the enemy was just hit by a limit ability, die
        if(Globals.isLimit)
        {
            health = 0;
            Globals.isLimit = false;
        }
        //Otherwise lose a health point
        else
        {
            health--;
        }

        if (health <= 0)
        {
            Globals.totalEnemies--;
            QueueFree();
        }
    }
    /*
     * If an area entered the enemy area
     */
    public void _on_area_3d_area_entered(Area3D area)
    {
        //If the area was a sword or blade beam take damage
        if(area.Name == "Sword" || area.Name == "Beam")
        {
            TakeDamage();
        }

    }
    /*
     * If an area entered the fist area
     */
    public void _on_fist_area_entered(Area3D area)
    {
        if (area.Name == "Sword" || area.Name == "Beam")
        {
            TakeDamage();
        }

    }
    /*
     * When the enemy spots the player
     */
    public void _on_detection_area_entered(Area3D area)
    {
        attacking = true;   
    }


}
