using Godot;
using System;

[GlobalClass]
public partial class DamageManager : Node
{
    
    public static String FarmerForceName = "Farmer";
    public static String BurgerForceName = "Burger";
    public static String MyForceMetaName = "MyForce";
    public static String DamageMetaName = "Damage";
    public static String DamageApplyToMetaName = "DamageAppliesTo";
    
    /// Sets the amount of damage that will be done.
    public static void SetDamage(Node obj, int amount)
    {
        obj.SetMeta(DamageManager.DamageMetaName, amount);
    }
    
    /// Sets the force that this damage will get applied to.
    /// Set to DamageManager.FarmerForceName if the player should be damaged.
    /// Set to DamageManager.BurgerForceName if the enemy should be damaged.
    public static void SetDamageApplyTo(Node obj, String force)
    {
        obj.SetMeta(DamageManager.DamageApplyToMetaName, force);
    }
    
    /// Set the force of me.
    /// Set to DamageManager.FarmerForceName if it belongs to the player.
    /// Set to DamageManager.BurgerForceName if it is an enemy.
    public static void SetMyForce(Node me, String force)
    {
        me.SetMeta(DamageManager.MyForceMetaName, force);
    }
    
    /// Check if the obj can damage me.
    public static bool CanDamageMe(Node me, Node obj)
    {
        if (!me.HasMeta(DamageManager.MyForceMetaName)) return false;
        if (!obj.HasMeta(DamageManager.DamageApplyToMetaName)) return false;
        return (String)me.GetMeta(DamageManager.MyForceMetaName) == (String)obj.GetMeta(DamageManager.DamageApplyToMetaName);
    }
    
    /// Get the amount of damage the obj does.
    public static int GetDamageAmount(Node obj)
    {
        if (!obj.HasMeta(DamageManager.DamageMetaName)) return 0;
        return (int)obj.GetMeta(DamageManager.DamageMetaName);
    }
    
    /// Removes any damage meta from obj.
    public static void ClearDamageMeta(Node obj)
    {
        obj.RemoveMeta(MyForceMetaName);
        obj.RemoveMeta(DamageMetaName);
        obj.RemoveMeta(DamageApplyToMetaName);
    }
    
}
