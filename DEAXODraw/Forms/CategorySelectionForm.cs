using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DEAXODraw.Utilities;

namespace DEAXODraw.Forms
{
    public partial class CategorySelectionForm : Form
    {
        public List<object> SelectedCategories { get; private set; }

        public CategorySelectionForm()
        {
            InitializeComponent();
            SelectedCategories = new List<object>();
            LoadCategories();
            CustomizeAppearance();
        }

        private void LoadCategories()
        {
            var categories = CategoryHelper.GetSelectionOptions();

            foreach (var category in categories)
            {
                checkedListBox1.Items.Add(category.Key, false);
            }
        }

        private void CustomizeAppearance()
        {
            // Set modern colors and styling
            this.BackColor = System.Drawing.Color.White;

            // Style the checked list box
            checkedListBox1.BorderStyle = BorderStyle.FixedSingle;
            checkedListBox1.BackColor = System.Drawing.Color.White;

            // Add hover effects to buttons
            AddButtonHoverEffects();
        }

        private void AddButtonHoverEffects()
        {
            // Add hover effects for modern look
            foreach (Button btn in new[] { btnOK, btnCancel, btnSelectAll, btnDeselectAll })
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
            var categories = CategoryHelper.GetSelectionOptions();
            SelectedCategories.Clear();

            foreach (string selectedItem in checkedListBox1.CheckedItems)
            {
                if (categories.ContainsKey(selectedItem))
                {
                    SelectedCategories.Add(categories[selectedItem]);
                }
            }

            // Flatten the list to handle nested categories
            SelectedCategories = CategoryHelper.FlattenList(SelectedCategories);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        // Keyboard shortcuts
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.A:
                    btnSelectAll_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.D:
                    btnDeselectAll_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Enter:
                    if (checkedListBox1.CheckedItems.Count > 0)
                        btnOK_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Escape:
                    btnCancel_Click(this, EventArgs.Empty);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}