using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.FinalIK;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DecaSDK.Move;
using static FastNoise;
using static FrooxEngine.FinalIK.IKSolverVR;

namespace RawTextImporter;

public class RawTextImporter : ResoniteMod
{
    public override string Name => "RawTextImporter";
    public override string Author => "dfgHiatus";
    public override string Version => "1.0.0";
    public override string Link => "https://github.com/dfgHiatus/RawTextImporter/";

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> Enabled =
        new("enabled", "Enabled", () => true);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<string> Files =
        new("extensions", "Semicolon-separated list of file extensions to force as a raw text import, with the starting '.'", () => ".txt;.srt;");

    internal static ModConfiguration Config;

    public override void OnEngineInit()
    {
        new Harmony($"{Author}.{Name}").PatchAll();
        Config = GetConfiguration();
    }

    [HarmonyPatch(typeof(UniversalImporter),
        "ImportTask",
        typeof(AssetClass),
        typeof(IEnumerable<string>),
        typeof(World),
        typeof(float3),
        typeof(floatQ),
        typeof(float3),
        typeof(bool))]
    public class UniversalImporterPatch
    {
        public static bool Prefix(ref IEnumerable<string> files, ref Task __result)
        {
            if (!Config.GetValue(Enabled))
                return true;

            List<string> notHasText = new();
            List<string> hasText = new();

            var extensionsToLookFor = Config.GetValue(Files).Split(';').ToHashSet();

            foreach (var file in files)
            {
                if (extensionsToLookFor.Contains(Path.GetExtension(file)))
                    hasText.Add(file);
                else
                    notHasText.Add(file);
            }

            if (hasText.Count > 0)
            {
                __result = ImportRawFile(hasText);
                files = notHasText;
            }

            return true;
        }

        private static async Task ImportRawFile(List<string> hasText)
        {
            const int rowSize = 5;
            const float scale = 0.2f;

            for (int i = 0; i < hasText.Count; i++)
            {
                string item = hasText[i];

                await default(ToWorld);
                var slot = Engine.Current.WorldManager.FocusedWorld.RootSlot.AddSlot(Path.GetFileName(item));
                var offset = new float3(i % rowSize * scale, i / rowSize * scale);

                slot.PositionInFrontOfUser();
                slot.LocalPosition += offset;

                await default(ToBackground);
                await UniversalImporter.ImportRawFile(slot, new ImportItem(item));
            }
        }
    }
}
