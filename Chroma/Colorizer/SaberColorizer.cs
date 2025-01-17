﻿namespace Chroma.Colorizer
{
    using System;
    using System.Collections.Generic;
    using IPA.Utilities;
    using SiraUtil.Interfaces;
    using UnityEngine;

    public class SaberColorizer : ObjectColorizer
    {
        private static readonly FieldAccessor<SaberModelController, ColorManager>.Accessor _colorManagerAccessor = FieldAccessor<SaberModelController, ColorManager>.GetAccessor("_colorManager");

        private static readonly FieldAccessor<SaberTrail, Color>.Accessor _colorAccessor = FieldAccessor<SaberTrail, Color>.GetAccessor("_color");
        private static readonly FieldAccessor<SaberModelController, SaberModelController.InitData>.Accessor _initDataAccessor = FieldAccessor<SaberModelController, SaberModelController.InitData>.GetAccessor("_initData");
        private static readonly FieldAccessor<SaberModelController, SetSaberGlowColor[]>.Accessor _setSaberGlowColorsAccessor = FieldAccessor<SaberModelController, SetSaberGlowColor[]>.GetAccessor("_setSaberGlowColors");
        private static readonly FieldAccessor<SaberModelController, SaberTrail>.Accessor _saberTrailAccessor = FieldAccessor<SaberModelController, SaberTrail>.GetAccessor("_saberTrail");
        private static readonly FieldAccessor<SetSaberGlowColor, SetSaberGlowColor.PropertyTintColorPair[]>.Accessor _propertyTintColorPairsAccessor = FieldAccessor<SetSaberGlowColor, SetSaberGlowColor.PropertyTintColorPair[]>.GetAccessor("_propertyTintColorPairs");
        private static readonly FieldAccessor<SetSaberGlowColor, MaterialPropertyBlock>.Accessor _materialPropertyBlockAccessor = FieldAccessor<SetSaberGlowColor, MaterialPropertyBlock>.GetAccessor("_materialPropertyBlock");
        private static readonly FieldAccessor<SetSaberGlowColor, MeshRenderer>.Accessor _meshRendererAccessor = FieldAccessor<SetSaberGlowColor, MeshRenderer>.GetAccessor("_meshRenderer");
        private static readonly FieldAccessor<SaberModelController, SetSaberFakeGlowColor[]>.Accessor _setSaberFakeGlowColorsAccessor = FieldAccessor<SaberModelController, SetSaberFakeGlowColor[]>.GetAccessor("_setSaberFakeGlowColors");
        private static readonly FieldAccessor<SetSaberFakeGlowColor, Parametric3SliceSpriteController>.Accessor _parametric3SliceSpriteAccessor = FieldAccessor<SetSaberFakeGlowColor, Parametric3SliceSpriteController>.GetAccessor("_parametric3SliceSprite");
        private static readonly FieldAccessor<SetSaberFakeGlowColor, Color>.Accessor _tintColorAccessor = FieldAccessor<SetSaberFakeGlowColor, Color>.GetAccessor("_tintColor");
        private static readonly FieldAccessor<SaberModelController, TubeBloomPrePassLight>.Accessor _saberLightAccessor = FieldAccessor<SaberModelController, TubeBloomPrePassLight>.GetAccessor("_saberLight");

        private readonly SaberTrail? _saberTrail;
        private readonly Color _trailTintColor;
        private readonly SetSaberGlowColor[]? _setSaberGlowColors;
        private readonly SetSaberFakeGlowColor[]? _setSaberFakeGlowColors;
        private readonly TubeBloomPrePassLight? _saberLight;

        private readonly SaberModelController? _saberModelController;

        private readonly SaberType _saberType;
        private readonly bool _doColor;
        private Color _lastColor;

        internal SaberColorizer(Saber saber)
        {
            _saberType = saber.saberType;

            SaberModelController saberModelController = saber.gameObject.GetComponentInChildren<SaberModelController>(true);
            if (Plugin.SiraUtilInstalled && IsColorable(saberModelController))
            {
                _doColor = false;

                _saberModelController = saberModelController;
            }
            else
            {
                _doColor = true;

                _saberTrail = _saberTrailAccessor(ref saberModelController);
                _trailTintColor = _initDataAccessor(ref saberModelController).trailTintColor;
                _setSaberGlowColors = _setSaberGlowColorsAccessor(ref saberModelController);
                _setSaberFakeGlowColors = _setSaberFakeGlowColorsAccessor(ref saberModelController);
                _saberLight = _saberLightAccessor(ref saberModelController);
            }

            _lastColor = _colorManagerAccessor(ref saberModelController).ColorForSaberType(_saberType);
            OriginalColor = _lastColor;

            GetOrCreateColorizerList(_saberType).Add(this);
        }

        internal static event Action<SaberType, Color>? SaberColorChanged;

        public static Dictionary<SaberType, List<SaberColorizer>> Colorizers { get; } = new Dictionary<SaberType, List<SaberColorizer>>();

        public static Color?[] GlobalColor { get; private set; } = new Color?[2];

        protected override Color? GlobalColorGetter => GlobalColor[(int)_saberType];

        public static void GlobalColorize(Color? color, SaberType saberType)
        {
            GlobalColor[(int)saberType] = color;
            saberType.GetSaberColorizers().ForEach(n => n.Refresh());
        }

        internal static void Reset()
        {
            GlobalColor[0] = null;
            GlobalColor[1] = null;
        }

        protected override void Refresh()
        {
            Color color = Color;
            if (color == _lastColor)
            {
                return;
            }

            _lastColor = color;
            if (_doColor)
            {
                SaberTrail saberTrail = _saberTrail!;
                _colorAccessor(ref saberTrail!) = (color * _trailTintColor).linear;

                for (int i = 0; i < _setSaberGlowColors!.Length; i++)
                {
                    SetSaberGlowColor setSaberGlowColor = _setSaberGlowColors[i];
                    SetSaberGlowColor.PropertyTintColorPair[] propertyTintColorPairs = _propertyTintColorPairsAccessor(ref setSaberGlowColor);
                    MaterialPropertyBlock materialPropertyBlock = _materialPropertyBlockAccessor(ref setSaberGlowColor);
                    if (materialPropertyBlock == null)
                    {
                        materialPropertyBlock = new MaterialPropertyBlock();
                        _materialPropertyBlockAccessor(ref setSaberGlowColor) = materialPropertyBlock;
                    }

                    foreach (SetSaberGlowColor.PropertyTintColorPair propertyTintColorPair in propertyTintColorPairs)
                    {
                        materialPropertyBlock.SetColor(propertyTintColorPair.property, color * propertyTintColorPair.tintColor);
                    }

                    _meshRendererAccessor(ref setSaberGlowColor).SetPropertyBlock(materialPropertyBlock);
                }

                for (int i = 0; i < _setSaberFakeGlowColors!.Length; i++)
                {
                    SetSaberFakeGlowColor setSaberFakeGlowColor = _setSaberFakeGlowColors[i];
                    Parametric3SliceSpriteController parametric3SliceSprite = _parametric3SliceSpriteAccessor(ref setSaberFakeGlowColor);
                    parametric3SliceSprite.color = color * _tintColorAccessor(ref setSaberFakeGlowColor);
                    parametric3SliceSprite.Refresh();
                }

                if (_saberLight != null)
                {
                    _saberLight.color = color;
                }
            }
            else
            {
                ColorColorable(color);
            }

            Color.RGBToHSV(color, out float h, out float s, out _);
            Color effectColor = Color.HSVToRGB(h, s, 1);
            SaberColorChanged?.Invoke(_saberType, effectColor);
        }

        private static List<SaberColorizer> GetOrCreateColorizerList(SaberType saberType)
        {
            if (!Colorizers.TryGetValue(saberType, out List<SaberColorizer> colorizers))
            {
                colorizers = new List<SaberColorizer>();
                Colorizers.Add(saberType, colorizers);
            }

            return colorizers;
        }

        // SiraUtil stuff
        private bool IsColorable(SaberModelController saberModelController)
        {
            return saberModelController is IColorable;
        }

        private void ColorColorable(Color color)
        {
            if (_saberModelController is IColorable colorable)
            {
                colorable.SetColor(color);
            }
        }
    }
}
