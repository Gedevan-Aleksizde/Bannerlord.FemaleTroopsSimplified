﻿using Bannerlord.FemaleTroopsSimplified.Configuration;
using HarmonyLib;
using Helpers;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Localization;

namespace Bannerlord.FemaleTroopsSimplified.Behaviors
{
    internal class FemaleTroopsSimplifiedBehavior : CampaignBehaviorBase
    {
        internal class TroopRename
        {
            public string Original;
            public string New;

            public TroopRename(string original, string @new)
            {
                Original = original;
                New = @new;
            }
        }

        private List<TroopRename>? _renames;
        private Dictionary<CharacterObject, string>? _reverts;

        public FemaleTroopsSimplifiedBehavior()
        {
            NativeOptions.OnNativeOptionsApplied += NativeOptions_OnNativeOptionsApplied;
        }

        void Initialize()
        {
            Settings.Initialize();

            LoadTroopRenames("ModuleData/common_troop_renames.xml");

            if (Settings.Instance != null && Settings.Instance.UseGenderNeutral)
            {
                ApplyTroopRenames();
            }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                Initialize();
            });

            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, (cgs) =>
            {
                Initialize();
            });
        }

        internal void LoadTroopRenames(string path)
        {
            _renames = new();

            var modulePath = Utilities.GetFullModulePath("Bannerlord.FemaleTroopsSimplified");
            var xmlFile = MiscHelper.LoadXmlFile(modulePath + path);

            var nodes = xmlFile.SelectNodes("renames/rename");

            foreach (XmlNode node in nodes)
            {
                string? original = node.Attributes["original"]?.Value;
                string? @new = node.Attributes["new"]?.Value;

                if (original == null || original == "")
                    continue;

                if (@new == null || @new == "")
                    continue;

                _renames.Add(new(original, @new));
            }
        }

        internal void ApplyTroopRenames()
        {
            AccessTools.FieldRef<TextObject, string> textObjectValue =
                AccessTools.FieldRefAccess<TextObject, string>("Value");

            if (_renames == null) return;

            if (Settings.Instance == null) return;

            _reverts = new();

            foreach (var character in CharacterObject.All)
            {
                if (character.IsHero) continue;

                if (!character.IsFemale && Settings.Instance.GetCharacterCoverage(character) == 0)
                    continue;

                string name = textObjectValue(character.Name);

                var rename = _renames.Find((x) => name.Contains(x.Original));

                if (rename == null) continue;

                _reverts.Add(character, name);

                textObjectValue(character.Name) = name.Replace(rename.Original, rename.New);
                character.Name.CacheTokens();
            }
        }

        internal void RevertTroopRenames()
        {
            AccessTools.FieldRef<TextObject, string> textObjectValue =
                AccessTools.FieldRefAccess<TextObject, string>("Value");

            if (_reverts == null) return;

            foreach (var revert in _reverts)
            {
                textObjectValue(revert.Key.Name) = revert.Value;
                revert.Key.Name.CacheTokens();
            }

            _reverts = null;
        }

        private void NativeOptions_OnNativeOptionsApplied()
        {
            if (Settings.Instance == null) return;

            RevertTroopRenames();

            if (Settings.Instance.UseGenderNeutral)
                ApplyTroopRenames();
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
