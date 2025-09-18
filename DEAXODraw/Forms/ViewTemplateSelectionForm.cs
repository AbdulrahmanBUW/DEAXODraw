using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;

namespace DEAXODraw.Forms
{
    public partial class ViewTemplateSelectionForm : Form
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
                MessageBox.Show($"Error loading view templates: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CustomizeAppearance()
        {
            // Set modern styling
            this.BackColor = System.Drawing.Color.White;

            // Style the combo box
            comboBoxTemplates.FlatStyle = FlatStyle.Flat;

            // Add hover effects to buttons
            AddButtonHoverEffects();
        }

        private void AddButtonHoverEffects()
        {
            // Add hover effects for modern look
            foreach (Button btn in new[] { btnOK, btnCancel })
            {
                var originalColor = btn.BackColor;

                btn.MouseEnter += (s, e) =>
                {
                    btn.BackColor = ControlPaint.Light(originalColor, 0.1f);
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
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a view template.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // Keyboard shortcuts
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    btnOK_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Escape:
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