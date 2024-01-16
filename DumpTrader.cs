using System;
using System.Linq;
using System.Text;
using Eremite;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Orders;
using Eremite.Model.Trade;
using Eremite.WorldMap;
using JetBrains.Annotations;

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
            index.AppendLine($@"<tr><th>Trader</th></tr>");

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
        }

        private void DumpNameInfo(StringBuilder index){
            index.Tagged("div", ()=> model.SmallIcon() + model.displayName.Text);
            index.Append("<div>");
            index.Append(model.description.Text);
            index.Append("</div>");
        }
    }
}