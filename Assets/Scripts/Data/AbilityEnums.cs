namespace RPG.Data
{
    public enum AbilityEffect
    {
        Damage,
        Heal,
        RestoreMP
    }

    public enum DamageType
    {
        Physical,
        Magical
    }

    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }
}
