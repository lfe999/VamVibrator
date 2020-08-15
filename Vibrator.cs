using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LFE {

	public class Vibrator : MVRScript {

        GenerateDAZMorphsControlUI morphsControl;

        Dictionary<string, float> vibrationStrengths = new Dictionary<string, float> {
            { "Genital-Open-2-b", 1.0f },
            { "Labia majora-spread-RUp", 0.2f },
            { "Labia majora-spread-LUp", 0.2f },
            { "Labia majora-spread-RLow", 0.2f },
            { "Labia majora-spread-LLow", 0.2f },
            { "Labia majora-spread-RMid", 0.2f },
            { "Labia majora-spread-LMid", 0.2f },
            { "Labia minora-spread-RUp", 0.2f },
            { "Labia minora-spread-LUp", 0.2f },
            { "Labia minora-spread-RLow", 0.2f },
            { "Labia minora-spread-LLow", 0.2f },
            { "Labia minora-spread-RMid", 0.2f },
            { "Labia minora-spread-LMid", 0.2f },
            { "Clitoris-erection", 0.6f },
            { "202-Clit-Hood-In.Out", 0.5f },
            { "201-Clit-Hood-Shorter", 0.5f },
            { "Clitoris-size", 0.2f },
            { "PubicAreaOut", 3.0f },
        };

        Dictionary<string, float> previousMorphChange = new Dictionary<string, float>();

        JSONStorableFloat vibrationStrengthStorable;
        JSONStorableFloat vibrationFrequencyStorable;
        JSONStorableBool vibrationAutoStrength;

        public override void Init() {
			if (containingAtom.type != "Person") {
                SuperController.LogError($"This plugin needs to be put on a 'Person' atom only, not a '{containingAtom.type}'");
                return;
            }

            InitializeMorphs();
            InitializeUI();

        }

        private void InitializeMorphs() {
            var atom = containingAtom;

            JSONStorable geometry = atom.GetStorableByID("geometry");
            if (geometry == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            DAZCharacterSelector dcs = geometry as DAZCharacterSelector;
            if (dcs == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            morphsControl = dcs.morphsControlUI;
            foreach(var item in vibrationStrengths) {
                var morph = morphsControl.GetMorphByDisplayName(item.Key);
                if(morph != null) {
                    previousMorphChange[item.Key] = 0.0f;
                }
                else {
                    SuperController.LogError($"No morph {item.Key}");
                }
            }
        }

        private void InitializeUI() {
            vibrationFrequencyStorable = new JSONStorableFloat("Frequency", 0.2f,
                (float value) => {
                    if(vibrationAutoStrength.val) {
                        // 1-Mathf.Pow(valueFrom0to1,3)
                        vibrationStrengthStorable.valNoCallback = StrengthFromFrequency(value);
                    }
                },
                0f, 1f);
            RegisterFloat(vibrationFrequencyStorable);
            CreateSlider(vibrationFrequencyStorable);

            vibrationStrengthStorable = new JSONStorableFloat("Strength", 0.03f,
                (float value) => {
                    vibrationAutoStrength.val = false;
                },
                0f, 1f);
            RegisterFloat(vibrationStrengthStorable);
            var strengthSlider = CreateSlider(vibrationStrengthStorable, rightSide: true);


            vibrationAutoStrength = new JSONStorableBool("AutoStrength", true, (bool value) => {
                if(value) {
                    vibrationStrengthStorable.valNoCallback = StrengthFromFrequency(vibrationFrequencyStorable.val);
                }
            });
            RegisterBool(vibrationAutoStrength);
            CreateToggle(vibrationAutoStrength, rightSide: true);
        }

        private float StrengthFromFrequency(float frequency) {
            var smoothing = Mathf.Pow(frequency, 2.5f);
            var target = Mathf.Lerp(0f, 0.7f, smoothing);
            if(frequency > 0 && target < 0.03f) {
                return Mathf.Max(0.03f, target);
            }
            return target;
        }

        float waited = 0;
        int previousDirection = 1;
		private void Update() {
            waited += Time.deltaTime;

            var delay = Mathf.Lerp(0.05f, 0.001f, vibrationFrequencyStorable.val);
            var changeStrength = vibrationStrengthStorable.val;

            if(delay > 0 && changeStrength > 0 && waited > delay) {
                waited = 0;
                previousDirection  = previousDirection * -1;
                foreach(var item in vibrationStrengths) {
                    var morphName = item.Key;
                    var morph = morphsControl.GetMorphByDisplayName(morphName);
                    if(morph != null) {
                        var changeRandomizer = Mathf.Lerp(0.6f, 1.0f, UnityEngine.Random.value);
                        var previousChange = previousMorphChange[morphName];

                        var changeMultiplier = item.Value;
                        var originalValue = (morph.morphValue - previousChange);
                        var change = (previousDirection * changeStrength * changeMultiplier * changeRandomizer);
                        morph.morphValueAdjustLimits = originalValue + change;

                        previousMorphChange[morphName] = change;
                    }
                }
            }
        }

        private void OnDestroy() {
            foreach(var item in previousMorphChange) {
                var morph = morphsControl.GetMorphByDisplayName(item.Key);
                if(morph != null) {
                    morph.morphValue -= item.Value;
                }
            }
        }
    }

}
