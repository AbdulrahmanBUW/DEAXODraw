using System;
using System.Collections.Generic;
using System.Linq;
using WinForms = System.Windows.Forms;
using Autodesk.Revit.DB;

namespace DEAXODraw.Forms
{
    public partial class ViewTemplateSelectionForm : WinForms.Form
    {
        public Autodesk.Revit.DB.View SelectedViewTemplate { get; private set; }

        public ViewTemplateSelectionForm(Document doc)
        {
            InitializeComponent();
            LoadViewTemplates(doc);
            CustomizeAppearance();
        }

        private void LoadViewTemplates(Document doc)
        {
            try
            {
                var views = new FilteredElementCollector(doc)
                    .OfClass(typeof(Autodesk.Revit.DB.View))
                    .Cast<Autodesk.Revit.DB.View>()
                    .Where(v => v.IsTemplate)
                    .OrderBy(v => v.Name)
                    .ToList();

                // Add "None" option
                comboBoxTemplates.Items.Add(new ViewTemplateItem { Name = "None", View = null });

                // Add view templates
                foreach (var view in views)
                {
                    comboBoxTemplates.Items.Add(new ViewTemplateItem { Name = view.Name, View = view });
                }

                if (comboBoxTemplates.Items.Count > 0)
                {
                    comboBoxTemplates.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show($"Error loading view templates: {ex.Message}", "Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
            }
        }

        private void CustomizeAppearance()
        {
            // Set modern styling
            this.BackColor = System.Drawing.Color.White;

            // Style the combo box
            comboBoxTemplates.FlatStyle = WinForms.FlatStyle.Flat;

            // Add hover effects to buttons
            AddButtonHoverEffects();
        }

        private void AddButtonHoverEffects()
        {
            // Add hover effects for modern look
            foreach (WinForms.Button btn in new[] { btnOK, btnCancel })
            {
                var originalColor = btn.BackColor;

                btn.MouseEnter += (s, e) =>
                {
                    btn.BackColor = WinForms.ControlPaint.Light(originalColor, 0.1f);
                };

                btn.MouseLeave += (s, e) =>
                {
                    btn.BackColor = originalColor;
                };
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (comboBoxTemplates.SelectedItem is ViewTemplateItem selectedItem)
            {
                SelectedViewTemplate = selectedItem.View;
                DialogResult = WinForms.DialogResult.OK;
                Close();
            }
            else
            {
                WinForms.MessageBox.Show("Please select a view template.", "Selection Required",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = WinForms.DialogResult.Cancel;
            Close();
        }

        // Keyboard shortcuts
        protected override bool ProcessCmdKey(ref WinForms.Message msg, WinForms.Keys keyData)
        {
            switch (keyData)
            {
                case WinForms.Keys.Enter:
                    btnOK_Click(this, EventArgs.Empty);
                    return true;
                case WinForms.Keys.Escape:
                    btnCancel_Click(this, EventArgs.Empty);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private class ViewTemplateItem
        {
            public string Name { get; set; }
            public Autodesk.Revit.DB.View View { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}