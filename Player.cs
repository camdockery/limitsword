using Godot;
using System;
using System.Threading.Tasks;
/**
 * The Script for the Player character
 * Contains functionality for:
 * basic movement (walking, turning, jumping)
 * attacking
 * limit abilities (timer based abilities that do extra damage)
 * healthbar
 * and animations
 */

/*
 * The Player class
 * A CharacterBody3D (the node used to control the character)
 */
public partial class Player : CharacterBody3D
{
    // How fast the player moves in meters per second.
    [Export]
    public int Speed { get; set; } = 7;

    public int health = 6;
    // The downward acceleration when in the air, in meters per second squared.
    [Export]
    public int FallAcceleration { get; set; } = 12;

    private Vector3 _targetVelocity = Vector3.Zero;

    public int jumpVelocity { get; set; } = 6;

    public AnimationPlayer player;

    public bool justJumped = false;

    public ProgressBar healthbar;

    public ProgressBar limitBar;
    //public Area3D hitbox;
    public Timer limitTime;

    public Marker3D marker;

    public bool oneShotAvailable = false;

    public ResourcePreloader preloader;
    
    /*
     * Initializes different variables used throughout the rest of the script (healthbar, limit timer, etc)
     */
    public override void _Ready()
    {
        player = GetNode<Node3D>("Knight").GetNode<AnimationPlayer>("AnimationPlayer");
        healthbar = GetParent<Node3D>().GetNode<CanvasLayer>("CanvasLayer").GetNode<ProgressBar>("HealthBar");
        limitBar = GetParent<Node3D>().GetNode<CanvasLayer>("CanvasLayer").GetNode<ProgressBar>("LimitBar");
        //limitTime = GetNode<Timer>("LimitTime");
        limitTime = GetParent<Node3D>().GetNode<Timer>("LimitTime");
        marker = GetNode<Node3D>("Knight").GetNode<Marker3D>("Marker3D");
        //Adds the ResourcePreloader to the Player
        preloader = new ResourcePreloader();
        AddChild(preloader);
        //Preloads a beam to be used in the scene
        var Beam = GD.Load<PackedScene>("res://Beam.tscn");
        preloader.AddResource("Beam", Beam);
    }
    /*
     * Controls the physics of the player within the environment and how it interacts with other entities and objects
     */
    public override void _PhysicsProcess(double delta)
    {
        //Takes player to the next level, or loops back to the beginning
        if(Globals.totalEnemies <= 0 && GetTree().GetCurrentScene().Name == "Level1" && health > 0)
        {
            GetTree().ChangeSceneToFile("res://level_2.tscn");
        }
        else if (Globals.totalEnemies <= 0 && GetTree().GetCurrentScene().Name == "Level2" && health > 0)
        {
            GetTree().ChangeSceneToFile("res://level_3.tscn");
        }
        else if (Globals.totalEnemies <= 0 && GetTree().GetCurrentScene().Name == "Level3" && health > 0)
        {
            GetTree().ChangeSceneToFile("res://main.tscn");
        }
        else
        {
            //Makes the bar for the limit percentage based on how much time is left on the timer out of 15
            limitBar.Value = limitTime.TimeLeft;
            var direction = Vector3.Zero;
            //Changes direction based on user input
            if (Input.IsActionPressed("right"))
            {
                direction.X += 1.0f;
            }
            if (Input.IsActionPressed("left"))
            {
                direction.X -= 1.0f;
            }
            if (Input.IsActionPressed("down"))
            {
                direction.Z += 1.0f;
            }
            if (Input.IsActionPressed("up"))
            {
                direction.Z -= 1.0f;
            }
            //Normalizes the direction and makes the model for the character look at the current direction
            if (direction != Vector3.Zero)
            {
                direction = direction.Normalized();
                GetNode<Node3D>("Knight").Basis = Basis.LookingAt(direction);
            }

            _targetVelocity.X = direction.X * Speed;
            _targetVelocity.Z = direction.Z * Speed;
            // If in the air, fall towards the floor. Literally gravity
            if (!IsOnFloor()) 
            {
                _targetVelocity.Y -= FallAcceleration * (float)delta;
            }
            else
            {
                //Makes the player jump if the user pressed space
                if (Input.IsActionJustPressed("jump"))
                {
                    _targetVelocity.Y += jumpVelocity;
                }
                //Otherwise ensures the current Y velocity is 0 (on the ground)
                else
                {
                    _targetVelocity.Y = (float)0.0;
                }
            }


            // Moving the character
            Velocity = _targetVelocity;

            MoveAndSlide();
            Animate();

            //Smoothly moves the camera closer to the players current location
            double cameraX = Mathf.Lerp(GetNode<Node3D>("CameraController").Position.X, Position.X, 0.15);
            double cameraY = Mathf.Lerp(GetNode<Node3D>("CameraController").Position.Y, Position.Y, 0.15);
            double cameraZ = Mathf.Lerp(GetNode<Node3D>("CameraController").Position.Z, Position.Z, 0.15);
            GetNode<Node3D>("CameraController").Position = new Vector3((float)cameraX, (float)cameraY, (float)cameraZ);
        }
        
    }
    /*
     * Animates the players model based on what needs to be done
     */
    private async Task Animate()
    {
        //Gets the swords hitbox
        var collider = GetNode<CollisionShape3D>("Knight/Skeleton3D/2H_Sword/Sword/CollisionShape3D");
        if (Input.IsActionJustPressed("attack"))
        {
            //Enables the sword's hitbox
            collider.Disabled = false;
            player.Play("1H_Melee_Attack_Chop");
        }
        //Player uses the stab limit ability
        else if (Input.IsActionPressed("LimitBreak") && limitTime.TimeLeft == 0)
        {
            collider.Disabled = false;
            //Sets the global value to true (limit is being used)
            Globals.isLimit = true;
            player.Play("1H_Melee_Attack_Stab");
            //Restarts the limit timer
            limitTime.Start();
        }
        //Player uses the blade beam limit ability
        else if (Input.IsActionPressed("shoot") && limitTime.TimeLeft == 0)
        {
            Globals.isLimit = true;
            player.Play("Spellcast_Shoot");
            limitTime.Start();
            var scene = preloader.GetResource("Beam") as PackedScene;
            var instance = scene.Instantiate() as Area3D;
            AddChild(instance);
            //Sets the new instance of a beam to the set marker position (the tip of the blade)
            instance.GlobalPosition = marker.GlobalPosition;
        }
        //Plays the falling animation
        else if (!IsOnFloor())
        {
            //Waits for any attack animations to finish
            if (player.CurrentAnimation == "1H_Melee_Attack_Chop" || player.CurrentAnimation == "1H_Melee_Attack_Stab" || player.CurrentAnimation == "Spellcast_Shoot")
            {
                await ToSignal(player, "animation_finished");
            }
            //The sword hitbox is disabled
            collider.Disabled = true;
            player.Play("Jump_Idle");
        }
        //If the player isn't moving, play the idle animation
        else if (Velocity.X == 0.0f && Velocity.Z == 0.0f)
        {
            if (player.CurrentAnimation == "1H_Melee_Attack_Chop" || player.CurrentAnimation == "1H_Melee_Attack_Stab" || player.CurrentAnimation == "Spellcast_Shoot")
            {
                await ToSignal(player, "animation_finished");
            }
            collider.Disabled = true;
            player.Play("Idle");
        }
        //Otherwise they are now just walking
        else
        {
            if (player.CurrentAnimation == "1H_Melee_Attack_Chop" || player.CurrentAnimation == "1H_Melee_Attack_Stab" || player.CurrentAnimation == "Spellcast_Shoot")
            {
                await ToSignal(player, "animation_finished");
            }
            collider.Disabled = true;
            player.Play("Walking_A");
        }

    }
    /*
     * Decreases health when punched by enemy
     */
    public void TakeDamage()
    {
        health--;
        //Sets the healthbar percentage to current health
        healthbar.Value = health;
        //Restarts the current level when player dies
        if (health <= 0)
        {
            GetTree().CallDeferred("reload_current_scene");
        }
    }


    /*
     * When a fist enters the players hitbox
     */
    public void _on_hitbox_area_entered(Area3D area)
    {
        if(area.Name == "Fist")
        {
            TakeDamage();
        }
    }
}