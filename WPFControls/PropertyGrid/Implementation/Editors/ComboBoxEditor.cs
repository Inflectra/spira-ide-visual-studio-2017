















using System.Collections.Generic;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms.ToolKit.PropertyGrid.Editors
{
	public abstract class ComboBoxEditor : TypeEditor<System.Windows.Controls.ComboBox>
	{
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = System.Windows.Controls.ComboBox.SelectedItemProperty;
		}

		protected override void ResolveValueBinding(PropertyItem propertyItem)
		{
			SetItemsSource(propertyItem);
			base.ResolveValueBinding(propertyItem);
		}

		protected abstract IList<object> CreateItemsSource(PropertyItem propertyItem);

		private void SetItemsSource(PropertyItem propertyItem)
		{
			Editor.ItemsSource = CreateItemsSource(propertyItem);
		}
	}
}
