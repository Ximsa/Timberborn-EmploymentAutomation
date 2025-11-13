#nullable enable
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using TimberUi.CommonUi;
using UnityEngine.UIElements;

namespace EmploymentAutomation
{
    public class AutomationFragment(VisualElementInitializer initializer) : IEntityPanelFragment
    {
        private EntityPanelFragmentElement panel = null!;
        private EmploymentComponent? curr;
        private Label foo = null!;

        public VisualElement InitializeFragment()
        {
            panel = new EntityPanelFragmentElement();
            panel.Initialize(initializer);
            var productPanel = panel.AddHorizontalContainer();
            foo = productPanel.AddGameLabel("goobar");
            return panel;
        }

        public void ShowFragment(BaseComponent entity)
        {
            curr = entity.GetComponent<EmploymentComponent>();
            panel.Visible = curr is not null;

            UpdateFragment();
        }

        public void ClearFragment()
        {
            panel.Visible = false;
        }

        public void UpdateFragment()
        {
            if (curr is null) return;
            foo.text = (curr.Coordinates.x + curr.Coordinates.y + curr.Coordinates.z).ToString();
        }
    }
}