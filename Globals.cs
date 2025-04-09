using Godot;
using System;
/*
 * Contains two global variables used across the different scripts
 */

/*
 * Holds the globals
 */
public partial class Globals : Node
{
    //True when a limit ability is being used, false when not
    public static bool isLimit = false;
    //Total enemies in the scene
    public static int totalEnemies = 0;
}
