using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using Eremite;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Orders;
using Eremite.Model.Trade;

namespace BubbleStormTweaks
{

    public static class TraderDumper{

        public static void Dump(StringBuilder index){
            index.AppendLine($@"<html>{Dumper.HTML_HEAD}<body> <header>{Dumper.NAV}</header><main><div>");
            index.Tagged("table", DumpTable);
            index.AppendLine("</div></main></body></html>");
            Dumper.Write(index, "traders", "index");
        }

        private static void DumpTable(StringBuilder index){
            index.AppendLine(Html.TableColumns("Name", "Offered Goods"));

            foreach(var model in Plugin.GameSettings.traders){
                var trader = new Trader(model);
                index.Tagged("tr", trader.Dump);
            }
        }
    }

    public class Trader {
        public readonly TraderModel model;

        public Trader(TraderModel model) {
            this.model = model;
        }

        public void Dump(StringBuilder index) {
            index.Tagged("td", DumpNameInfo);
            index.Tagged("td", DumpPotentialGoods);
        }

        private void DumpNameInfo(StringBuilder index){
            index.Tagged("div", ()=> model.SmallIcon() + model.displayName.Text);
            index.Append("<div>");
            index.Append(model.description.Text);
            index.Append("</div>");
        }

        private void DumpPotentialGoods(StringBuilder index){
            index.AppendLine($@"<div><b class=""relic-effect-category"">Guaranteed:</b></div>");
            index.AppendLine(@"<div class=""to-solve-sets"">");
            foreach(var good in model.guaranteedOfferedGoods){
                index.Tagged("div", ()=>Ext.Cost(good, "trader"));
            }
            index.AppendLine(@"</div>");

            index.AppendLine($@"<div><b class=""relic-effect-category"">Potential:</b> (with weight)</div>");
            index.AppendLine(@"<div class=""to-solve-sets"">");
            foreach(var goodWeight in model.offeredGoods){
                var good = goodWeight.ToGood();
                index.Tagged(
                    "div", ()=>(Ext.Cost(good, goodWeight.good, "trader"))
                    + @$"<span class=""good-weight-addendum"">({goodWeight.weight:0})</span>"
                );
            }
            index.AppendLine(@"</div>");
        }

        private void DumpDesiredGoods(StringBuilder index){
            index.AppendLine(@"<div class=""to-solve-sets"">");
            foreach(var goodWeight in model.offeredGoods){
                var good = goodWeight.ToGood();
                index.Tagged("div", ()=>(Ext.Cost(good, goodWeight.good, "trader")) + $"({goodWeight.weight:0})");
            }
            index.AppendLine(@"</div>");
        }
    }
}