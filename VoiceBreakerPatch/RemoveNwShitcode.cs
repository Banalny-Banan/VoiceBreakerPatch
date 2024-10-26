using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NorthwoodLib.Pools;
using VoiceChat.Networking;

namespace VoiceBreakerPatch;

[Obfuscation(Exclude = true),
 HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
internal static class RemoveNwShitcode
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

        Label skipLabel = generator.DefineLabel();

        int startIndex = newInstructions.FindIndex(i => i.LoadsField(AccessTools.Field(typeof(VoiceTransceiver), nameof(VoiceTransceiver._playerDecoders))));
        newInstructions.Insert(startIndex, new CodeInstruction(OpCodes.Br_S, skipLabel)); // Jump to label

        int endIndex = newInstructions.FindIndex(i => i.Is(OpCodes.Ldc_I4, 480)) + 2; // if (num != 480) return;
        newInstructions[endIndex + 1].labels.Add(skipLabel); // Add label after NW samples check

        foreach (CodeInstruction instruction in newInstructions)
            yield return instruction;

        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}