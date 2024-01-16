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
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Pool;

namespace BubbleStormTweaks
{

    public static class MysteryDumper{

        public static HashSet<BiomeModel> allBiomes = new();

        public static void Dump(StringBuilder index){
            var mysteries = GatherMysteries();
            index.AppendLine($@"<html>{Dumper.HTML_HEAD}<body> <header>{Dumper.NAV}</header><main><div>");
            index.Tagged("table", index=>DumpTable(index, mysteries));
            index.AppendLine("</div></main></body></html>");
            Dumper.Write(index, "mysteries", "index");
        }
        
        public static IEnumerable<Mystery> GatherMysteries(){
            var mysteries = new Dictionary<string, Mystery>();

            foreach (var biome in Serviceable.Settings.biomes){
                var biomeName = biome.displayName.Text;
                if(biomeName.Contains("Missing")){
                    continue;
                }

                allBiomes.Add(biome);
                var config = biome.seasons;
                foreach (var mystery in config.simpleEffects){
                    GetOrAdd(mysteries, mystery, biome);
                }
                foreach (var mystery in config.conditionalEffects){
                    GetOrAdd(mysteries, mystery, biome);
                }
            }
            return mysteries.Values.OrderBy(i=>i);
        }

        private static Mystery GetOrAdd(Dictionary<string, Mystery> mysteries, ISeasonalEffectModel effect, BiomeModel biome){
            var name = effect.Name;
            Mystery mystery = null;
            if(mysteries.ContainsKey(name)){
                mystery = mysteries[name];
            } else {
                if(effect is SimpleSeasonalEffectModel sse) mystery = new SimpleMystery(sse);
                else mystery = new ConditionalMystery(effect as ConditionalSeasonalEffectModel);
                mysteries[name] = mystery;
            }
            mystery.biomes.Add(biome);
            return mystery;
        }

        private static void DumpTable(StringBuilder index, IEnumerable<Mystery> mysteries){
            index.AppendLine(Html.TableColumns("Name", "Description", "Biomes", "Difficulty Cost"));
            DumpSection(index, "Drizzle", mysteries.Where(m=>m.Effect.IsPositive));
            index.Append("</table><table>");
            index.AppendLine(Html.TableColumns("Name", "Hostility", "Description", "Biomes", "Difficulty Cost"));
            DumpSection(index, "Storm", mysteries.Where(m=>!m.Effect.IsPositive));
        }

        private static void DumpSection(StringBuilder index, string title, IEnumerable<Mystery> mysteries){
            index.Tagged("h1", title);
            foreach (var mystery in mysteries){
                index.Tagged("tr", mystery.Dump);
            }
        }

    }

    public abstract class Mystery : IComparable<Mystery>{
        public virtual ISeasonalEffectModel Effect {get;}
        public Sprite Icon => Effect.Icon;
        public String DisplayName => Effect.DisplayName;
        public virtual int DifficultyCost {get;}
        public HashSet<BiomeModel> biomes = new();
        
        public virtual void Dump(StringBuilder index){
            index.Tagged("td", NameWithIcon);
            if(!Effect.IsPositive)
                index.Tagged("td", Effect.HostilityLevel.ToString());
            index.Tagged("td", Description);
            index.Tagged("td", BiomeIcons);
            index.Tagged("td", DifficultyCost.ToString());
        }
        
        private void NameWithIcon(StringBuilder index){
            index.Tagged("b", @$"{Effect.SmallIcon()} <span style=""pad-left:16px"">{Effect.DisplayName}</span>");
        }

        protected abstract void Description(StringBuilder index);

        private void BiomeIcons(StringBuilder index){
            foreach (var biome in biomes){
                index.Append(biome.SmallIcon());
            }
        }

        public int CompareTo(Mystery other)
        {
            if(Effect.HostilityLevel != other.Effect.HostilityLevel){
                return Effect.HostilityLevel - other.Effect.HostilityLevel;
            }
            return string.Compare(Effect.DisplayName, other.Effect.DisplayName);
        }
    }

    public class SimpleMystery : Mystery {
        private SimpleSeasonalEffectModel effect;
        public override ISeasonalEffectModel Effect => effect;
        public override int DifficultyCost => effect.difficultyCost;

        public SimpleMystery(SimpleSeasonalEffectModel effect){
            this.effect = effect;
        }

        protected override void Description(StringBuilder index){
            index.Tagged("div", effect.Description);
        }
    }
    
    public class ConditionalMystery : Mystery {
        private ConditionalSeasonalEffectModel effect;
        public override ISeasonalEffectModel Effect => effect;
        public override int DifficultyCost => effect.difficultyCost;

        public ConditionalMystery(ConditionalSeasonalEffectModel effect){
            this.effect = effect;
        }

        protected override void Description(StringBuilder index){
            index.Div("flavor-text", effect.Description);
            index.Tagged("p", EffectDesc);
            index.Tagged("p", Conditions);
        }

        private void EffectDesc(StringBuilder index){
            index.Append(effect.villagerEffect.Description);
        }

        private void Conditions(StringBuilder index){
            index.Append(string.Format($"{effect.statusText}: ", ""));
            foreach (NeedCategoryCondition need in effect.conditions)
			{
				index.Append(string.Format(" {0} x{1};", need.category.displayName.Text.ToUpper(), need.amount));
			}
			index.Remove(index.Length - 1, 1);
        }
    }
}