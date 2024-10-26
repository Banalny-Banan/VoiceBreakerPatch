using Exiled.API.Features;
using System;
using HarmonyLib;

namespace VoiceBreakerPatch;

public class Plugin : Plugin<Config>
{
    public override string Prefix => "VoiceBreakerPatch";
    public override string Name => Prefix;
    public override string Author => "BanalnyBanan";
    public override Version Version { get; } = new (1, 1, 0);
    
    static readonly Harmony Harmony = new ("VoiceBreakerPatch");
    public static Plugin Instance;

    public override void OnEnabled()
    {
        Instance = this;
        Harmony.PatchAll();
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Instance = null;
        Harmony.UnpatchAll();
        base.OnDisabled();
    }
}