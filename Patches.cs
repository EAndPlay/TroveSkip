namespace TroveSkip
{
    public class Patches
    {
        public static Patch AutoLoot = new(PatchName.AutoLoot,
            new Patch.PatchPair(Signatures.AutoLootSignature, Signatures.AutoLootEnabledSignature));

        public static Patch AutoAttack = new(PatchName.AutoAttack,
            new Patch.PatchPair(Signatures.AutoAttackSignature, Signatures.AutoAttackEnabledSignature));

        public static Patch InstaMining = new(PatchName.InstaMining,
            new Patch.PatchPair(Signatures.MiningSlowSignature, Signatures.MiningSlowEnabledSignature),
            new Patch.PatchPair(Signatures.GeodeToolSignature, Signatures.GeodeToolEnabledSignature));

        public static Patch NoClip = new(PatchName.NoClip,
            new Patch.PatchPair(Signatures.NoClipSignature, Signatures.NoClipEnabledSignature));

        public static Patch MapHack = new(PatchName.MapHack,
            new Patch.PatchPair(Signatures.MapHackSignature, Signatures.MapHackEnabledSignature));
        
        public static Patch ZoomHack = new(PatchName.MapHack,
            new Patch.PatchPair(Signatures.ZoomHackSignature, Signatures.ZoomHackEnabledSignature));
    }
}