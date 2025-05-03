using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[assembly: MelonInfo(typeof(AutoWater.AutoWaterMod), "AutoSoil", "0.1.0", "Michiyo")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace AutoWater
{
    public class AutoWaterMod : MelonMod
    {

        private MelonPreferences_Category _category;
        private MelonPreferences_Entry<double> _checkInterval;

        private float _lastCheckTime = 0f;

        public override void OnInitializeMelon()
        {
            _category = MelonPreferences.CreateCategory("AutoSoil", "AutoSoil Settings");

            _checkInterval = _category.CreateEntry("CheckInterval", 60.0);
            _checkInterval.Comment = "Time (in seconds) between soil checks";

            MelonLogger.Msg($"[AutoSoil] Config loaded: Interval={_checkInterval.Value}");
        }

        public override void OnUpdate()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "Main") return;

            float now = Time.realtimeSinceStartup;
            float elapsed = now - _lastCheckTime;


            if (elapsed >= _checkInterval.Value)
            {
                _lastCheckTime = now;
                MelonCoroutines.Start(ProcessAllSoilPourers());
            }
        }

        private IEnumerator ProcessAllSoilPourers()
        {
            var allObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            int pourerCount = 0;
            int activatedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj == null || obj.GetType().FullName != "ScheduleOne.ObjectScripts.SoilPourer")
                    continue;

                pourerCount++;

                var type = obj.GetType();
                var pourMethod = type.GetMethod("SendPourSoil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var getPotsMethod = type.GetMethod("GetPots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (pourMethod == null || getPotsMethod == null)
                    continue;

                var potList = getPotsMethod.Invoke(obj, null);
                if (potList == null) continue;

                foreach (var pot in (IEnumerable)potList)
                {
                    if (pot == null) continue;

                    var potType = pot.GetType();
                    var isFilledProp = potType.GetProperty("IsFilledWithSoil", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (isFilledProp == null) continue;

                    bool isFilled = (bool)isFilledProp.GetValue(pot);

                    if (!isFilled)
                    {
                        pourMethod.Invoke(obj, null);
                        activatedCount++;
                        break; // Only need one empty pot to trigger
                    }
                }
            }
            yield return null;
        }

    }
}
