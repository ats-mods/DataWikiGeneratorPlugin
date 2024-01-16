using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Eremite.Model;
using Eremite.Services;
using Eremite.WorldMap;
using UnityEngine.Analytics;
using UnityEngine.Pool;

namespace BubbleStormTweaks
{

    public static class CornerstoneDumper{

        public static HashSet<string> allBiomes = new();

        public static void Dump(StringBuilder index){
            var cornerstones = GatherCornerstones();
            index.AppendLine($@"<html>{Dumper.HTML_HEAD}<body> <header>{Dumper.NAV}</header><main><div>");
            index.Tagged("table", sb=>DumpTable(sb, cornerstones));
            index.AppendLine("</div></main></body></html>");
            Dumper.Write(index, "cornerstones", "index"); 
        }
        
        public static IEnumerable<Cornerstone> GatherCornerstones(){
            var stones = new Dictionary<string, Cornerstone>();

            foreach (var biome in Serviceable.Settings.biomes){
                var biomeName = biome.Name;
                if(biomeName.Contains("Tutorial") || biomeName.Contains("Capital")){
                    continue;
                }
                allBiomes.Add(biomeName);
                foreach (var effectHolder in biome.seasons.SeasonRewards.SelectMany(season => season.effectsTable.effects)){
                    GetOrAdd(stones, effectHolder, biome);
                }
            }

            return stones.Values.OrderBy(i=>i);
        }

        private static Cornerstone GetOrAdd(Dictionary<string, Cornerstone> stones, EffectsTableEntity effectHolder, BiomeModel biome){
            var name = effectHolder.effect.Name;
            var stone = stones.ContainsKey(name)? stones[name] : (stones[name] = new Cornerstone(effectHolder));
            stone.biomes.Add(biome.displayName.Text);
            return stone;
        }

        private static void DumpTable(StringBuilder index, IEnumerable<Cornerstone> cornerstones){
            index.Tagged("h1", "Epic");
            foreach (var Cornerstone in cornerstones.Where(cs=>cs.Rarity == EffectRarity.Epic)){
                index.Div("cornerstone", Cornerstone.Dump);
            }
            index.Tagged("h1", "Legendary");
            foreach (var Cornerstone in cornerstones.Where(cs=>cs.Rarity == EffectRarity.Legendary)){
                index.Div("cornerstone", Cornerstone.Dump);
            }
        }

    }

    public class Cornerstone : IComparable<Cornerstone>{
        private EffectsTableEntity effectHolder;
        public HashSet<string> biomes = new();

        public Cornerstone(EffectsTableEntity effectHolder){
            this.effectHolder = effectHolder;
        }

        public EffectRarity Rarity => effectHolder.Rarity;
        public EffectModel Effect => effectHolder.effect;

        public void Dump(StringBuilder index){
            index.Append($@"<a class=""section-anchor"" href=""#{Effect.Name.Sane()}"" id=""{Effect.Name.Sane()}"">");
            index.Tagged("div", NameWithIcon);
            index.Append("</a>");
            index.Tagged("p", Effect.Description);
            if(biomes.Count < CornerstoneDumper.allBiomes.Count){
                index.Tagged("p", "<em>Not available in</em>: " + (string.Join(", ", CornerstoneDumper.allBiomes.Except(biomes))));
            }
        }
        
        private void NameWithIcon(StringBuilder index){
            index.Append($"{Effect.SmallIcon()}<h3>{Effect.DisplayName}</h3>");
        }

        public int CompareTo(Cornerstone other)
        {
            return Effect.DisplayName.CompareTo(other.Effect.DisplayName);
        }
    }
}