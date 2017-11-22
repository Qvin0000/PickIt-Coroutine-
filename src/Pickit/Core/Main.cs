#region Header
/*
 * THIS PICK IT VERSION WORK ONLY WITH Qvin0000 HUD
 * Idea/Code from Qvin's auto pickup
 * Reworked into a more configurable version * 
*/
#endregion


using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PoeHUD.Framework;
using Utilities;

namespace Pickit
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private readonly Stopwatch ShowHideLabelTimer = Stopwatch.StartNew();
        private readonly Stopwatch Pick_Up_Timer = Stopwatch.StartNew();
        private readonly List<Tuple<int, long, EntityWrapper>> _sortedByDistDropItems = new List<Tuple<int, long, EntityWrapper>>();
        private readonly HashSet<EntityWrapper> entities = new HashSet<EntityWrapper>();
        private HashSet<string> NonUniques;
        private HashSet<string> Uniques;
        private Vector2 _clickWindowOffset;
        private readonly int PIXEL_BORDER = 3;
        long _lastItemClick;
        private int _coroutineCounter = 0;
        public Main()
        {
            PluginName = "Pickit";
        }

        public override void Initialise()
        {
            NonUniques = LoadPickit("Non Uniques");
            Uniques = LoadPickit("Uniques");
            
            _coroutine = new Coroutine(InfinityCoroutineLoop(),nameof(Pickit),"Pick Up Items",false){AutoResume = false};
            _coroutine.AutoRestart();
            _coroutine.Run().Stop();
            _coroutine.UpdateTicks(_coroutineCounter);
        }
        Coroutine _coroutine;

        IEnumerator InfinityCoroutineLoop()
        {
            while (true)
            {
              yield return  NewPickUp();
                _coroutineCounter++;
                _coroutine.UpdateTicks(_coroutineCounter);
            }
        }
        public override void Render()
        {
            if (Settings.Enable)
            {
                if (Keyboard.IsKeyDown((int) Settings.PickUpKey.Value))
                {
                    if (_coroutine.IsDone)
                    {
                        var firstOrDefault = Runner.Instance.Coroutines.FirstOrDefault(x => x.Name == "Pick Up Items");
                        if (firstOrDefault != null)
                        {
                            _coroutine = firstOrDefault;
                        }
                    }
                    _coroutine.Resume();
                }
                else
                {
                    _coroutine.Stop();
                }
            }
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            string PickitFile = $@"{PluginDirectory}\Pickit\{fileName}.txt";
            if (!File.Exists(PickitFile))
            {
                return null;
            }
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(PickitFile);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));
            return hashSet;
        }

        private int GetEntityDistance(EntityWrapper entity)
        {
          return  GetEntityDistance(entity.InternalEntity);
        }
        private int GetEntityDistance(Entity entity)
        {
            var PlayerPosition = GameController.Player.GetComponent<Positioned>();
            var MonsterPosition = entity.GetComponent<Positioned>();
            var distanceToEntity = Math.Sqrt(Math.Pow(PlayerPosition.X - MonsterPosition.X, 2) +
                                             Math.Pow(PlayerPosition.Y - MonsterPosition.Y, 2));

            return (int)distanceToEntity;
        }

        public bool InListNonUnique(ItemsOnGroundLabelElement ItemEntity)
        {
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(Item.Path).BaseName;
                ItemRarity Rarity = Item.GetComponent<Mods>().ItemRarity;
                if (NonUniques.Contains(ItemEntityName) && Rarity != ItemRarity.Unique)
                    return true;
            

            return false;
        }

        public bool InListUnique(ItemsOnGroundLabelElement ItemEntity)
        {
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(Item.Path).BaseName;
                ItemRarity Rarity = Item.GetComponent<Mods>().ItemRarity;
                if (Uniques.Contains(ItemEntityName) && Rarity == ItemRarity.Unique)
                    return true;
         

            return false;
        }

        public bool MiscChecks(ItemsOnGroundLabelElement ItemEntity)
        {
           
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
                var ClassName = GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName;
                
                if (Settings.Rares && Item.GetComponent<Mods>().ItemRarity == ItemRarity.Rare)
                {
                    if (Settings.RareJewels && ClassName == "Jewel")
                        return true;
                    if (Settings.RareRings && ClassName == "Ring" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareRingsilvl)
                        return true;
                    if (Settings.RareAmulets && ClassName == "Amulet" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareAmuletsilvl)
                        return true;
                    if (Settings.RareBelts && ClassName == "Belt" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareBeltsilvl)
                        return true;
                    if (Settings.RareGloves && ClassName == "Gloves" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareGlovesilvl)
                        return true;
                    if (Settings.RareBoots && ClassName == "Boots" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareBootsilvl)
                        return true;
                    if (Settings.RareHelmets && ClassName == "Helmet" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareHelmetsilvl)
                        return true;
                    if (Settings.RareArmour && ClassName == "Body Armour" && Item.GetComponent<Mods>().ItemLevel >= Settings.RareArmourilvl)
                        return true;
                }

                if (Settings.SixSocket && Item.GetComponent<Sockets>().NumberOfSockets == 6)
                    return true;
                if (Settings.SixLink && Item.GetComponent<Sockets>().LargestLinkSize == 6)
                    return true;
                if (Settings.RGB && Item.GetComponent<Sockets>().IsRGB)
                    return true;
                if (Settings.AllDivs && ClassName == "DivinationCard")
                    return true;
                if (Settings.AllCurrency && ClassName == "StackableCurrency")
                    return true;
                if (Settings.AllUniques && Item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.Maps && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Settings.MapFragments && ClassName == "MapFragment")
                    return true;
                if (Settings.Maps && Settings.UniqueMap && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= 1 && Item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.QuestItems && ClassName == "QuestItem")
                    return true;
                if (Settings.Gems && Item.GetComponent<Quality>().ItemQuality >= Settings.GemQuality.Value && ClassName.Contains("Skill Gem"))
                    return true;
            

            return false;
        }

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            entities.Add(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            entities.Remove(entityWrapper);
        }
        private IEnumerator NewPickUp()
        {
            if (Settings.ShowHideToggle && ShowHideLabelTimer.ElapsedMilliseconds > 2000)
            {
              yield return  Keyboard.KeyPress(Settings.ShowHideKey.Value, 20);
              yield return  Keyboard.KeyPress(Settings.ShowHideKey.Value, 20);
                ShowHideLabelTimer.Restart();
            }

            yield return new WaitTime(Settings.PickupTimerDelay);

            var currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .Where(x => x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible && (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds == 0))
                .Select(x => new Tuple<int, ItemsOnGroundLabelElement>(GetEntityDistance(x.ItemOnGround), x))
                .OrderBy(x => x.Item1)
                .ToList();
           
            var pickUpThisItem = (from x in currentLabels
                                  where (InListNonUnique(x.Item2) || InListUnique(x.Item2) || MiscChecks(x.Item2))
                                  && x.Item1 < Settings.PickupRange
                                  select x).FirstOrDefault();
            if (pickUpThisItem != null)
            {
                if (pickUpThisItem.Item1 > Settings.PickupRange) yield break;
                var vect = pickUpThisItem.Item2.Label.GetClientRect().Center;
                var vectWindow = GameController.Window.GetWindowRectangle();
                if (vect.Y + PIXEL_BORDER > vectWindow.Bottom || vect.Y - PIXEL_BORDER < vectWindow.Top ||
                    vect.X + PIXEL_BORDER > vectWindow.Right || vect.X - PIXEL_BORDER < vectWindow.Left)
                {
                    yield break;
                }
                if (pickUpThisItem.Item2.Address == _lastItemClick && Pick_Up_Timer.ElapsedMilliseconds <= 333)
                 yield break;
                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                yield return Mouse.SetCursorPosAndLeftClick(vect + _clickWindowOffset, Settings.ExtraDelay);
                _lastItemClick = pickUpThisItem.Item2.Address;
                Pick_Up_Timer.Restart();
            }
            else if(Settings.GroundChests)
            {
             yield return   ClickOnChests();
            }
        }

        // Copy-Paste - Qvin0000's version
        private IEnumerator ClickOnChests()
        {
            var sortedByDistChest = new List<Tuple<int, long, EntityWrapper>>();

            foreach (var entity in entities)
            {
                if (entity.Path.ToLower().Contains("chests") && entity.IsAlive && entity.IsHostile)
                {
                    if (!entity.HasComponent<Chest>()) continue;
                    var ch = entity.GetComponent<Chest>();
                    if (ch.IsStrongbox) continue;
                    if (ch.IsOpened) continue;
                    var d = GetEntityDistance(entity);

                    var t = new Tuple<int, long, EntityWrapper>(d, entity.Address, entity);
                    if (sortedByDistChest.Any(x => x.Item2 == entity.Address)) continue;

                    sortedByDistChest.Add(t);
                }
            }

            var tempList = sortedByDistChest.OrderBy(x => x.Item1).ToList();
            if (tempList.Count <= 0) yield break;
            if (tempList[0].Item1 >= Settings.ChestRange) yield break;
            yield return  SetCursorToEntityAndClick(tempList[0].Item3);
          /*  var centerScreen = GameController.Window.GetWindowRectangle().Center;
            Mouse.SetCursorPos(centerScreen);*/
        }
        //Copy-Paste - Sithylis_QoL
        private IEnumerator SetCursorToEntityAndClick(EntityWrapper entity)
        {
            var camera = GameController.Game.IngameState.Camera;
            var chestScreenCoords =
                camera.WorldToScreen(entity.Pos.Translate(0, 0, 0), entity);
            if (chestScreenCoords != new Vector2())
            {
                var pos = Mouse.GetCursorPosition();
                var iconRect1 = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
                yield return  Mouse.SetCursorPosAndLeftClick(iconRect1, 100);
                Mouse.SetCursorPos(pos.X, pos.Y);

            }
        }

        private IEnumerator SetCursorToEntityAndClick(Vector2 rect)
        {
            var _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            var finalRect = rect + _clickWindowOffset;
          yield return  Mouse.SetCursorPosAndLeftClick(finalRect, 30);
        }
    }
}
