using System;
using System.Text;
using UnityEngine.UIElements;

namespace BubbleStormTweaks
{
    public static class Html{
        public static void Tagged(this StringBuilder builder, string tag, Action<StringBuilder> action){
            builder.Append($"<{tag}>");
            action(builder);
            builder.Append($"</{tag}>");
        }

        public static void Tagged(this StringBuilder builder, string tag, Func<string> producer){
            builder.Append($"<{tag}>");
            builder.Append(producer());
            builder.Append($"</{tag}>");
        }
    }
}