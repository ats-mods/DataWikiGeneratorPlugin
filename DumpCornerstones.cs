using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Eremite.Model;
using Eremite.Services;
using Eremite.WorldMap;
using QFSW.QC.Containers;
using Sirenix.Serialization;
using UnityEngine.Analytics;
using UnityEngine.Pool;

namespace BubbleStormTweaks
{

    public static class CornerstoneDumper{

        public static HashSet<string> allBiomes = new();

        public static void Dump(StringBuilder index){
            var cornerstones = GatherCornerstones();
            var altarCornerstones = GatherAltarCornerstones();
            index.AppendLine($@"<html>{Dumper.HTML_HEAD}<body> <header>{Dumper.NAV}</header><main><div>");
            index.Tagged("table", sb=>DumpTable(sb, cornerstones, altarCornerstones));
            index.AppendLine("</div></main></body></html>");
            Dumper.Write(index, "cornerstones", "index"); 
        }
        
        public static IEnumerable<Cornerstone> GatherCornerstones(){
            var stones = new Dictionary<string, Cornerstone>();

            foreach (var biome in Serviceable.Settings.biomes){
                var biomeName = biome.displayName.Text;
                if(biomeName.Contains("Missing")){
                    continue;
                }
                allBiomes.Add(biomeName);
                foreach (var effectHolder in biome.seasons.SeasonRewards.SelectMany(season => season.effectsTable.effects)){
                    GetOrAdd(stones, effectHolder, biome);
                }
            }
            return stones.Values.OrderBy(i=>i);
        }

         public static IEnumerable<Cornerstone> GatherAltarCornerstones(){
            var stones = new Dictionary<string, Cornerstone>();
            foreach(var altarModel in Serviceable.Settings.altarEffects){
                stones[altarModel.upgradedEffect.Name] = new AltarCornerstone(altarModel);
            }
            return stones.Values.OrderBy(i=>i);;
         }



        private static Cornerstone GetOrAdd(Dictionary<string, Cornerstone> stones, EffectsTableEntity effectHolder, BiomeModel biome){
            var name = effectHolder.effect.Name;
            var stone = stones.ContainsKey(name)? stones[name] : (stones[name] = new Cornerstone(effectHolder));
            stone.biomes.Add(biome.displayName.Text);
            return stone;
        }

        private static void DumpTable(StringBuilder index, IEnumerable<Cornerstone> cornerstones, IEnumerable<Cornerstone> altarCornerstones){
            DumpSection(index, "Epic", cornerstones.Where(cs=>cs.Rarity == EffectRarity.Epic));
            DumpSection(index, "Legendary", cornerstones.Where(cs=>cs.Rarity == EffectRarity.Legendary));
            DumpSection(index, "Stormforged", altarCornerstones);
        }

        private static void DumpSection(StringBuilder index, string title, IEnumerable<Cornerstone> cornerstones){
            index.Tagged("h1", title);
            foreach (var cornerstone in cornerstones){
                index.Div("cornerstone", cornerstone.Dump);
            }
        }

    }

    public class Cornerstone : IComparable<Cornerstone>{
        private EffectsTableEntity effectHolder;
        public HashSet<string> biomes = new();

        public Cornerstone(EffectsTableEntity effectHolder){
            this.effectHolder = effectHolder;
        }

        public virtual EffectRarity Rarity => effectHolder.Rarity;
        public virtual EffectModel Effect => effectHolder.effect;

        public virtual void Dump(StringBuilder index){
            index.Append($@"<a class=""section-anchor"" href=""#{Effect.Name.Sane()}"" id=""{Effect.Name.Sane()}"">");
            index.Tagged("div", NameWithIcon);
            index.Append("</a>");
            index.Tagged("div", Effect.Description);
            if(biomes.Count < CornerstoneDumper.allBiomes.Count){
                index.Tagged("div", "<b><em>Not available in</em></b>: " + (string.Join(", ", CornerstoneDumper.allBiomes.Except(biomes))));
            }
        }
        
        private void NameWithIcon(StringBuilder index){
            index.Tagged("h3", @$"{Effect.SmallIcon()} <span style=""pad-left:16px"">{Effect.DisplayName}</span>");
        }

        public int CompareTo(Cornerstone other)
        {
            return Effect.DisplayName.CompareTo(other.Effect.DisplayName);
        }
    }

    public class AltarCornerstone : Cornerstone {
        private AltarEffectModel altarModel;

        public AltarCornerstone(AltarEffectModel altarModel) : base(null){
            base.biomes = CornerstoneDumper.allBiomes;
            this.altarModel = altarModel;
        }

        public override EffectRarity Rarity => EffectRarity.Mythic;
        public override EffectModel Effect => altarModel.upgradedEffect;

        public override void Dump(StringBuilder index){
            base.Dump(index);
            var replaced = altarModel.regularEffect;
            if(replaced != null){
                index.Tagged("div", @$"<b>Replaces:</b> <a href=""#{replaced.Name.Sane()}"">{replaced.displayName.Text}</a>");
            }
        }
    }
}