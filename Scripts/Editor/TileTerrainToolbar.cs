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

        private TileTerrain terrain;

        [ViewChild("options")] private VisualElement optionsContainer;
        [ViewChild("option-shape")] private EnumField optionShape;
        private PopupField<string> optionType;

        private ModifierShape selectedShape;
        private FillType selectedFillType;

        public TileTerrainToolbar() : base()
        {
            optionShape.Init(selectedShape);
            optionShape.RegisterCallback<ChangeEvent<Enum>>((evt) => { selectedShape = (ModifierShape)evt.newValue; });
        }

        public void SetActiveTerrain(TileTerrain newTerrain)
        {
            if (terrain == newTerrain)
                return;
            terrain = newTerrain;

            if (terrain == null)
            {
                SetEnabled(false);
                return;
            }
            
            TileTemplate template = terrain.TileTemplate;
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
            if (optionType != null)
                optionsContainer.Remove(optionType);
            
            if (template == null)
                return;
            
            optionType = new PopupField<string>(null, template.Names.ToList(), 0);
            optionType.name = "option-type";
            optionsContainer.Add(optionType);
            
            optionType.RegisterCallback<ChangeEvent<string>>((evt) =>
                {
                    selectedFillType = (FillType) optionType.index;
                });
        }
    }
}