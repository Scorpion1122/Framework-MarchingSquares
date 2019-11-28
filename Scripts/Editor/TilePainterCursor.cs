using Thijs.Framework.UI;
using UnityEngine.UIElements;

namespace Thijs.Framework.MarchingSquares
{
    public class TilePainterCursor : VisualComponent
    {
        public override string StyleSheetPath => "TileTerrain/cursor_style";
        public override string TemplatePath => "TileTerrain/cursor_template";

        [ViewChild("square")] private VisualElement square;
        [ViewChild("circle")] private VisualElement circle;

        public TilePainterCursor(VisualElement root) : base()
        {
            Element.AddManipulator(new SnapToMouseManipulator(root));
        }
    }
}