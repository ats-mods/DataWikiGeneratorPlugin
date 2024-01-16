using System.Text;
using System;
using System.Collections.Generic;
using Eremite.Buildings;
using System.Linq;
using Eremite.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BubbleStormTweaks
{
    public static class RelicDumper{

        public static void Dump(StringBuilder index)
        {
            index.AppendLine($@"<html>{Dumper.HTML_HEAD}<body> <header>{Dumper.NAV}</header><main><div>");
            Serviceable.Settings.Relics.GroupBy(GetRelicGroup)
                .Where(group => group.Key.Item2 > 0)
                .OrderBy(group => group.Key.Item2)
                .ToList()
                .ForEach( group=> DumpRelicGroup(index, group));

            index.AppendLine(@"</div></main></body>");
            AddCustomStyle(index);
            index.AppendLine("</html>");
            Dumper.Write(index, "relics", "index");
        }

        private static Tuple<string, int> GetRelicGroup(RelicModel relic){
            if(relic.Name.StartsWith("DebugNode"))
                return new("Debug", 0);
            if(relic.dangerLevel == DangerLevel.Forbidden)
                return new("Forbidden", 1);
            if (relic.dangerLevel == DangerLevel.Dangerous)
                return new("Dangerous", 2);
            if (relic.orderModel != null)
                return new ("Ghosts", 4);

            return new("None", 99);
        }

        private static void DumpRelicGroup(StringBuilder index, IGrouping<Tuple<string, int>, RelicModel> group){
            index.Tagged("table", index=>DumpRelicGroup(index, group.Key.Item1, group.ToList()));
        }

        private static void DumpRelicGroup(StringBuilder index, string groupName, List<RelicModel> relics){
            index.AppendLine($@"<h3>{groupName}</h3>");
            index.AppendLine(Html.TableColumns("Name", "Time Needed", "Materials Needed (1 per column)", "Bad Stuff"));
            relics.ForEach(relic => DumpRelic(index, relic));
        }

        private static void DumpRelic(StringBuilder index, RelicModel relic){
            List<string>[] diffclasses = GetRelicDifficultyTiers(relic);
            index.AppendLine($@"<tr><td>{relic.displayName.Text}</td>");

            // TIME NEEDED
            index.AppendLine("<td>");
            for (int i = 0; i < relic.difficulties.Length; i++)
            {
                RelicDifficulty diff = relic.difficulties[i];
                List<string> diffclass = diffclasses[i];
                var hasMultipleWorkingTimes = (diff.decisions.Select(d=>d.workingTime).Distinct().Count() > 1);
                foreach (var decision in diff.decisions)
                {
                    index.AppendLine($@"<div class=""{string.Join(" ", diffclass)}"">");
                    if (hasMultipleWorkingTimes)
                        index.AppendLine($@"<div><b class=""relic-effect-category"">{decision.label}:</b></div>");
                    float baseSeconds = diff.effectTimeToStartRatio * decision.workingTime;
                    index.AppendLine(baseSeconds.Minutes().Surround("span", $@"class=""relic-time"" data-base-time=""{baseSeconds}"""));
                    index.AppendLine($@"</div>");
                    if(!hasMultipleWorkingTimes)
                        break;
                }
            }
            index.AppendLine("</td>");

            // MATERIALS NEEDED
            index.AppendLine("<td>");
            for (int i = 0; i < relic.difficulties.Length; i++)
            {
                RelicDifficulty diff = relic.difficulties[i];
                List<string> diffclass = diffclasses[i];
                index.AppendLine($@"<div class=""{string.Join(" ", diffclass)}"">");
                foreach (var decision in diff.decisions)
                {
                    if(decision.label != null)
                        index.AppendLine($@"<div><b class=""relic-effect-category"">{decision.label}:</b>");
                    if(decision.decisionTag != null)
                        index.AppendLine($@"<span class=""relic-effect-tag"">({decision.decisionTag.displayName.Text})</span>");
                    index.AppendLine("</div>");
                    
                    if (decision.requriedGoods?.sets.Length == 0)
                    {
                        index.AppendLine($@"<div class=""to-solve-sets""><em>none</em></div>");
                    }
                    else
                    {
                        index.AppendLine($@"<div class=""to-solve-sets"">");
                        foreach (var set in decision.requriedGoods.sets)
                        {
                            index.AppendLine($@"<div class=""to-solve-set"">");
                            foreach (var g in set.goods)
                                index.AppendLine(g.Cost("solve").Surround("Div"));
                            index.AppendLine($@"</div>");
                        }
                        index.AppendLine($@"</div>");
                    }
                }
                index.AppendLine($@"</div>");
            }
            index.AppendLine("</td>");

            // BAD STUFF COLUMN
            index.AppendLine("<td><div>");
            foreach (var tier in relic.effectsTiers.Where(tier => tier.effect.Length > 0))
            {
                index.AppendLine($@"<div><b class=""relic-effect-category"">Every {tier.timeToStart.Minutes()}:</b></div>");
                foreach (var effect in tier.effect)
                {
                    index.AppendLine($@"<div class=""relic-effect"">{effect.Description}</div>");
                }
            }

            for (int i = 0; i < relic.difficulties.Length; i++)
            {
                RelicDifficulty diff = relic.difficulties[i];
                foreach (var decision in diff.decisions)
                {
                    if (decision.workingEffects?.Length > 0)
                    {
                        List<string> diffclass = diffclasses[i];
                        index.AppendLine($@"<div class=""{string.Join(" ", diffclass)}"">");
                        index.AppendLine($@"<div><b class=""relic-effect-category"">During '{decision.label}':</b></div>");
                        foreach (var effect in decision.workingEffects)
                        {
                            index.AppendLine($@"<div class=""relic-effect"">{effect.Description}</div>");
                        }
                        index.AppendLine($@"</div>");
                    }
                }
            }
            index.AppendLine("</div></td></tr>");
        }

        private static List<String>[] GetRelicDifficultyTiers(RelicModel relic){
            Dictionary<int, string> difficultyByIndex = Serviceable.Settings.difficulties.Where(d => d.index >= 0).ToDictionary(d => d.index, d => "difficulty-" + d.Name.Sane());

            List<string>[] diffclasses = new List<string>[relic.difficulties.Length];
            for (int i = 0; i < relic.difficulties.Length; i++){
                RelicDifficulty diff = relic.difficulties[i];

                List<string> diffclass = new() { "filter-difficulty", difficultyByIndex[diff.difficulty] };

                if (i == relic.difficulties.Length - 1)
                {
                    int diffIndex = diff.difficulty + 1;
                    while (difficultyByIndex.TryGetValue(diffIndex, out var next))
                    {
                        diffclass.Add(next);
                        diffIndex += 1;
                    }
                }

                if (i == 0)
                {
                    int diffIndex = diff.difficulty - 1;
                    while (difficultyByIndex.TryGetValue(diffIndex, out var next))
                    {
                        diffclass.Add(next);
                        diffIndex -= 1;
                    }
                }

                diffclasses[i] = diffclass;
            }

            return diffclasses;
        }

        private static void AddCustomStyle(StringBuilder index){
            index.AppendLine(@"
        <style>
.relic {
padding: 4px;
border: 1px solid black;
}

ul { margin: 4px; padding-left: 12px }
img { vertical-align: middle;}
a {padding-left: 4px;}

        </style>");
        }
    }
}