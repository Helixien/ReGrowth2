using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Xml;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(LoadedModManager))]
    [HarmonyPatch("ApplyPatches")]
    [HarmonyPatch(new[] { typeof(XmlDocument), typeof(Dictionary<XmlNode, LoadableXmlAsset>) })]
    public static class LoadedModManager_ApplyPatches_Patch
    {
        public static void Postfix(XmlDocument xmlDoc, Dictionary<XmlNode, LoadableXmlAsset> assetlookup)
        {

            var patchOpSwapExtension = new PatchOperationAttributeSet
            {
                xpath = "Defs/BiomeDef/modExtensions/li[@Class=\"BiomesKit.BiomesKitControls\"]",
                attribute = "Class",
                value = "ReGrowthCore.BiomesKitControl",
                success = PatchOperation.Success.Always,
            };
            patchOpSwapExtension.Apply(xmlDoc);
            string xmlText = "<workerClass>ReGrowthCore.UniversalBiomeWorker</workerClass>";
            XmlDocument xmlDoc2 = new XmlDocument();
            xmlDoc2.LoadXml(xmlText);
            var patchOpSwapBiomeWorker = new PatchOperationReplace
            {
                xpath = "Defs/BiomeDef[workerClass=\"BiomesKit.UniversalBiomeWorker\"]/workerClass",
                value = new XmlContainer
                {
                    node = xmlDoc2
                },
                success = PatchOperation.Success.Always,
            };
            patchOpSwapBiomeWorker.Apply(xmlDoc);
        }
    }
}
