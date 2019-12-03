using System;
using System.Linq;
using Thijs.Framework.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Thijs.Framework.MarchingSquares
{
    public class TileTerrainToolbar : VisualComponent
    {
        public override string StyleSheetPath => "TileTerrain/toolbar_style";
        public override string TemplatePath => "TileTerrain/toolbar_template";

        [ViewChild("option-type")] private EnumField optionType = null;
        [ViewChild("options")] private VisualElement optionsContainer = null;
        [ViewChild("option-shape")] private EnumField optionShape = null;
        [ViewChild("option-size")] private FloatField optionSize = null;
        private PopupField<string> optionFill;

        public TileTerrain Terrain { get; private set; }
        public ModifierType SelectedType { get; private set; }
        public ModifierShape SelectedShape { get; private set; }
        public FillType SelectedFillType { get; private set; }
        public float SelectedSize { get; private set; }

        public TileTerrainToolbar() : base()
        {
            optionShape.Init(SelectedShape);
            optionShape.RegisterCallback<ChangeEvent<Enum>>((evt) => { SelectedShape = (ModifierShape)evt.newValue; });
            
            optionType.Init(SelectedType);
            optionType.RegisterCallback<ChangeEvent<Enum>>((evt) => { SelectedType = (ModifierType)evt.newValue; });
            
            optionSize.RegisterCallback<ChangeEvent<float>>((evt) => { SelectedSize = evt.newValue; });
            SelectedSize = optionSize.value;
        }

        public void SetActiveTerrain(TileTerrain newTerrain)
        {
            if (Terrain == newTerrain)
                return;
            Terrain = newTerrain;

            if (Terrain == null)
            {
                SetEnabled(false);
                return;
            }
            
            TileTemplate template = Terrain.TileTemplate;
            if (template == null)
            {
                SetEnabled(false);
                return;
            }
            
            SetEnabled(true);
            UpdateTypeOptions(template);
        }

        private void UpdateTypeOptions(TileTemplate template)
        {
            //Have not found a way yet to reinitialize the choises
            if (optionFill != null)
                optionsContainer.Remove(optionFill);
            
            if (template == null)
                return;
            
            optionFill = new PopupField<string>(null, template.Names.ToList(), 0);
            optionType.name = "option-fill";
            optionsContainer.Add(optionFill);
            
            optionFill.RegisterCallback<ChangeEvent<string>>((evt) =>
                {
                    SelectedFillType = (FillType) optionFill.index;
                });
        }
    }
}