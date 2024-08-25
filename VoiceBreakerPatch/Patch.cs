using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using UnityEngine;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;
using Player = Exiled.API.Features.Player;

namespace VoiceBreakerPatch;

[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public static class VoiceBreakerPatch
{
    static readonly OpusDecoder Decoder = new();
    static readonly OpusEncoder Encoder = new(OpusApplicationType.Voip);

    static readonly ConcurrentDictionary<Player, int> ExploitMessages = [];
    static readonly HashSet<Player> BannedPlayers = [];
    
    static readonly float[] Buffer = new float[480];

    public static bool Prefix(NetworkConnection conn, ref VoiceMessage msg)
    {
        try
        {
            if (msg.Speaker == null || conn.identity.netId != msg.Speaker.netId)
                return false;
            
            int length = Decoder.Decode(msg.Data, msg.DataLength, Buffer);
            if (length != 480) return false;

            var (min, max) = (Buffer.Min(), Buffer.Max());
            float maxVolume = Mathf.Max(max, -min);

            if (maxVolume > 1)
            {
                var speaker = Player.Get(msg.Speaker);
                if (maxVolume > 100 && !speaker.IsTransmitting)
                {
                    RecordExploitAttempt(speaker);
                }

                //adjust volume so that max will be 1
                ScaleSamples(Buffer, 1 / maxVolume);
                msg = msg with { Data = Encode(Buffer) };
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while checking voice message: {e}");
            return false;
        }

        return true;
    }

    static void RecordExploitAttempt(Player player)
    {
        int exploitAttempts = ExploitMessages.AddOrUpdate(player, 1, (_, count) => count + 1);
        if (exploitAttempts >= 10)
        {
            if (Plugin.Instance.Config.BanForVoiceExploit && BannedPlayers.Add(player))
            {
                Log.Error($"Banning {player.Nickname} for voice exploit");
                player.Ban(DateTime.Now.AddYears(50) - DateTime.Now, Plugin.Instance.Config.VoiceExploitBanReason);
            }
        }
    }

    static void ScaleSamples(float[] samples, float scale)
    {
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= scale;
    }

    static byte[] Encode(float[] samples)
    {
        var data = new byte[512];
        Encoder.Encode(samples, data);
        return data;
    }
}