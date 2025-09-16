using System.Collections.Generic;

namespace CopyPolish
{
    internal static class ModelConfiguration
    {
        public const string DefaultPrimaryModel = "qwen/qwen3-coder:free";
        public const string DefaultFallbackModel1 = "deepseek/deepseek-chat-v3.1:free";
        public const string DefaultFallbackModel2 = "openai/gpt-oss-120b:free";
        public const string DefaultFallbackModel3 = "nvidia/nemotron-nano-9b-v2:free";

        public static IReadOnlyList<string> GetModelChain()
        {
            var defaults = new[]
            {
                DefaultPrimaryModel,
                DefaultFallbackModel1,
                DefaultFallbackModel2,
                DefaultFallbackModel3
            };

            var settings = Properties.Settings.Default;
            var configured = new[]
            {
                settings.PrimaryModelName,
                settings.FallbackModelName1,
                settings.FallbackModelName2,
                settings.FallbackModelName3
            };

            var chain = new List<string>(defaults.Length);
            for (int i = 0; i < defaults.Length; i++)
            {
                var candidate = string.IsNullOrWhiteSpace(configured[i]) ? defaults[i] : configured[i];
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    chain.Add(candidate.Trim());
                }
            }

            return chain;
        }
    }
}
