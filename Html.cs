using System;
using System.Diagnostics.Tracing;
using System.Text;
using HarmonyLib;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace BubbleStormTweaks
{
    public static class Html{

        public static string TableColumns(params string[] columnNames){
            StringBuilder sb = new();
            sb.Append("<tr>");
            columnNames.Do(s=>sb.Tagged("th", s));
            sb.Append("</tr>");
            return sb.ToString();
        }

        public static StringBuilder Tagged(this StringBuilder builder, string tag, Func<string> producer){
            return builder.Tagged(tag, producer());
        }

        public static StringBuilder Tagged(this StringBuilder builder, string tag, string content){
            return builder.Tagged(tag, s=>s.Append(content));
        }        
        
        public static StringBuilder Tagged(this StringBuilder builder, string tag, Action<StringBuilder> action){
            builder.Append($"<{tag}>");
            action(builder);
            builder.Append($"</{tag}>");
            return builder;
        }

        public static StringBuilder Div(this StringBuilder builder, string divClass, string content){
            if(divClass == null)
                return builder.Tagged("div", content);
            return builder.Div(divClass, s=>s.Append(content));
        }

        public static StringBuilder Div(this StringBuilder builder, string divClass, Action<StringBuilder> action){
            if(divClass == null)
                return builder.Tagged("div", action);

            builder.Append(@$"<div class=""{divClass}"">");
            action(builder);
            builder.Append("</div>");
            return builder;
        }
    }
}