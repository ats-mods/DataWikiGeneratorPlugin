using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eremite;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Orders;
using Eremite.Model.Trade;
using Eremite.WorldMap;
using UnityEngine;

namespace BubbleStormTweaks 
{

    public static class Ext
    {
        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>() {
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
                { typeof(object), "object" },
                { typeof(bool), "bool" },
                { typeof(char), "char" },
                { typeof(string), "string" },
                { typeof(void), "void" }
        };
        public static string SugarName(this Type type)
        {
            if (Aliases.TryGetValue(type, out var name))
                return name;
            else
                return type.Name;
        }
        public static string Safe(this string str)
        {
            if (str == null) return "[null]";

            return str.Replace(">Missing key<", "[unknown]");
        }
        private static Regex goodWithCategory = new(@"\[(.*?)\] (.*)");

        public static string Div(this string contents) => contents.Surround("div");

        public static string Cost(this GoodRef good, string costType)
        {
            return Cost(good.ToGood(), good.good, costType);
        }

        public static string Cost(Good good, GoodModel model, string costType)
        {
            return @$"<span class=""cost-{costType}"" data-base-cost=""{good.amount}"">{good.amount}</span>× " + ShowGood(model);
        }

        public static string ShowGood(GoodModel model)
        {
            return @$"{model.SmallIcon()} <a href=""{model.Link()}""> {model.displayName.Text}</a>";
        }

        public static string ShowEffect(EffectModel model)
        {
            return @$"{model.SmallIcon()} <a href=""{model.Link()}""> {model.displayName.Text}</a>";
        }

        public static string AsSummary(this string str) => str.Surround("summary");

        public static string Surround(this string str, string with)
        {
            return $"<{with}>{str}</{with}>";
        }

        public static string GatherIn(this IEnumerable<string> contents, string with)
        {
            return $"<{with}>{string.Join("\n", contents)}</{with}>";
        }

        public static string GatherIn(this IEnumerable<string> contents, string with, string attribs)
        {
            return $"<{with} {attribs}>{string.Join("\n", contents)}</{with}>";
        }

        public static string Surround(this string str, string with, string attribs)
        {
            return $"<{with} {attribs}>{str}</{with}>";
        }

        public static string DescriptionOrLink(this EffectModel effect)
        {
            if (effect is GoodsEffectModel goods)
            {
                return $@"{goods.good.amount}× {goods.good.SmallIcon()} <a href=""{goods.good.good.Link()}"">{goods.good.good.Name.StripCategory()}</a>";
            }
            else
            {
                return effect.Description;
            }
        }

        public static string DataAttrib(this object obj, string suffix)
        {
            return $@"data-{suffix}='{JSON.ToJson(obj).Replace("'", "`")}'";
        }

        public static string Minutes(this float seconds)
        {
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);

            return mins.ToString().PadLeft(2, '0') + ':' + secs.ToString().PadLeft(2, '0');
        }

        public static string StripCategory(this string s)
        {
            var match = goodWithCategory.Match(s);
            if (match.Success)
                return goodWithCategory.Match(s).Groups[2].Value;
            else
                return s;
        }

        public static string Link(this string s, string directory)
        {
            return $"../{directory}/{s.Sane()}.html";
        }

        public static string Sane(this string s)
        {
            return s.Replace(' ', '_');
        }

        public static string Link(this SO so)
        {
            if (so is BuildingModel)
                return so.Name.Link("buildings");
            else if (so is GoodModel)
                return so.Name.Link("goods");
            else if (so is RecipeModel recipe)
                return recipe.GetProducedGood().Link("goods");
            else if (so is EffectModel){
                // anchor corresponds to a header id in the effects/index.html page
                // If you change the anchor name here, also change the anchor in DumpEffects
                return $"../effects/#{so.Name.Sane()}";
            }
            else
                throw new NotSupportedException();
        }

        public class SpriteReference : IEquatable<SpriteReference>
        {
            public string group;
            public string source_image;
            public float source_x, source_y;
            public float source_w, source_h;
            public int out_w, out_h;

            public string Render => $"{group};{source_image};{source_x};{source_y};{source_w};{source_h};{out_w};{out_h}";

            public String FilenameOut()
            {
                string filename = $"{source_image}-{out_w}x{out_h}";
                if (source_x > 0 || source_y > 0)
                    filename += $"-{source_x}x{source_y}";
                return $"{filename}.png";
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as SpriteReference);
            }

            public bool Equals(SpriteReference other)
            {
                return other is not null &&
                       group == other.group &&
                       source_image == other.source_image &&
                       source_x == other.source_x &&
                       source_y == other.source_y &&
                       source_w == other.source_w &&
                       source_h == other.source_h &&
                       out_w == other.out_w &&
                       out_h == other.out_h;
            }

            public override int GetHashCode()
            {
                int hashCode = 1965452847;
                hashCode = hashCode * -1521134295 + group.GetHashCode();
                hashCode = hashCode * -1521134295 + source_image.GetHashCode();
                hashCode = hashCode * -1521134295 + source_x.GetHashCode();
                hashCode = hashCode * -1521134295 + source_y.GetHashCode();
                hashCode = hashCode * -1521134295 + source_w.GetHashCode();
                hashCode = hashCode * -1521134295 + source_h.GetHashCode();
                hashCode = hashCode * -1521134295 + out_w.GetHashCode();
                hashCode = hashCode * -1521134295 + out_h.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(SpriteReference left, SpriteReference right)
            {
                return EqualityComparer<SpriteReference>.Default.Equals(left, right);
            }

            public static bool operator !=(SpriteReference left, SpriteReference right)
            {
                return !(left == right);
            }
        }

        public static HashSet<SpriteReference> spritesUsed = new();

        public static string ImageScaled(this Sprite icon, string group, int target, string title)
        {
            Dumper.Write(icon.texture);
            float scale = (icon.textureRect.width / target);
            int w = target;
            int h = target;

            SpriteReference spriteRef = new()
            {
                group = $"{group}-{icon.name}",
                source_image = icon.texture.name,
                source_w = icon.textureRect.width,
                source_h = icon.textureRect.height,
                source_x = icon.textureRect.x,
                source_y = icon.textureRect.y,
                out_w = w,
                out_h = h,
            };

            spritesUsed.Add(spriteRef);
            string imgSrc = $"/data-wiki/img/{spriteRef.FilenameOut()}";
            return $@"<img title=""{title}"" src=""{imgSrc}""/>";
        }


        public static string SmallIcon(this TraderModel trader) => trader.icon.Small("trader", trader.displayName.Text);
        public static string SmallIcon(this BiomeModel biome) => biome.icon.Small("biome", biome.displayName.Text);
        public static string SmallIcon(this OrderModel order) => order.GetIcon().Small("order", order.displayName.Text);
        public static string SmallIcon(this GoodModel good) => good.icon.Small("good", good.displayName.Text);
        public static string SmallIcon(this GoodRef good) => good.good.SmallIcon();
        public static string SmallIcon(this BuildingModel b) => b.icon.Small("building", b.displayName.Text);
        public static string SmallIcon(this EffectModel e) => e.GetIcon().Small("effect", e.DisplayName);
        public static string SmallIcon(this ISeasonalEffectModel e) => e.Icon.Small("mystery", e.DisplayName);




        public static string Small(this Sprite sprite, string group, string title = null) => sprite.ImageScaled(group, 32, title.Safe());
        public static string Normal(this Sprite sprite, string group, string title = null) => sprite.ImageScaled(group, 128, title.Safe());
    }
}