using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class HudWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.Hud;
        
        public override void Close()
        {
            Destroy(gameObject);
        }
    }
}
