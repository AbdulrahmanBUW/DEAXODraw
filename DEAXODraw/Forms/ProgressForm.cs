using System;
using System.ComponentModel;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DEAXODraw.Forms
{
    public partial class ProgressForm : WinForms.Form
    {
        private BackgroundWorker backgroundWorker;
        private Action<BackgroundWorker> workAction;
        private bool isCancelled = false;

        public bool IsCancelled => isCancelled;
        public object Result { get; private set; }

        public ProgressForm(string title, string description)
        {
            InitializeComponent();
            this.Text = title;
            lblDescription.Text = description;

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;

            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        public void StartWork(Action<BackgroundWorker> action)
        {
            workAction = action;
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                workAction?.Invoke(backgroundWorker);
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(e)));
            }
            else
            {
                UpdateProgress(e);
            }
        }

        private void UpdateProgress(ProgressChangedEventArgs e)
        {
            progressBar.Value = Math.Min(Math.Max(e.ProgressPercentage, 0), 100);
            lblProgress.Text = $"{e.ProgressPercentage}%";

            if (e.UserState != null)
            {
                lblCurrentItem.Text = e.UserState.ToString();
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                WinForms.MessageBox.Show($"An error occurred: {e.Error.Message}", "Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }

            Result = e.Result;
            DialogResult = isCancelled ? WinForms.DialogResult.Cancel : WinForms.DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                isCancelled = true;
                backgroundWorker.CancelAsync();
                btnCancel.Text = "Cancelling...";
                btnCancel.Enabled = false;
            }
        }

        protected override void OnFormClosing(WinForms.FormClosingEventArgs e)
        {
            if (backgroundWorker.IsBusy && !isCancelled)
            {
                e.Cancel = true;
                btnCancel_Click(this, EventArgs.Empty);
            }
            base.OnFormClosing(e);
        }

        #region Designer Code
        private System.ComponentModel.IContainer components = null;
        private WinForms.ProgressBar progressBar;
        private WinForms.Label lblDescription;
        private WinForms.Label lblProgress;
        private WinForms.Label lblCurrentItem;
        private WinForms.Button btnCancel;
        private WinForms.Panel panel1;
        private WinForms.Label lblBrand;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                backgroundWorker?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.progressBar = new WinForms.ProgressBar();
            this.lblDescription = new WinForms.Label();
            this.lblProgress = new WinForms.Label();
            this.lblCurrentItem = new WinForms.Label();
            this.btnCancel = new WinForms.Button();
            this.panel1 = new WinForms.Panel();
            this.lblBrand = new WinForms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();

            // panel1
            this.panel1.Anchor = ((WinForms.AnchorStyles)(((WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Left) | WinForms.AnchorStyles.Right)));
            this.panel1.BackColor = Drawing.Color.FromArgb(248, 249, 250);
            this.panel1.Controls.Add(this.lblBrand);
            this.panel1.Controls.Add(this.lblDescription);
            this.panel1.Location = new Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Drawing.Size(450, 60);
            this.panel1.TabIndex = 0;

            // lblBrand
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = new Drawing.Font("Segoe UI", 10F, Drawing.FontStyle.Bold);
            this.lblBrand.ForeColor = Drawing.Color.FromArgb(220, 53, 69);
            this.lblBrand.Location = new Drawing.Point(15, 10);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new Drawing.Size(56, 19);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "DEAXO";

            // lblDescription
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new Drawing.Font("Segoe UI", 10F, Drawing.FontStyle.Bold);
            this.lblDescription.ForeColor = Drawing.Color.FromArgb(0, 122, 204);
            this.lblDescription.Location = new Drawing.Point(15, 30);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new Drawing.Size(89, 19);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "Processing...";

            // progressBar
            this.progressBar.Anchor = ((WinForms.AnchorStyles)(((WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Left) | WinForms.AnchorStyles.Right)));
            this.progressBar.Location = new Drawing.Point(30, 80);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Drawing.Size(390, 23);
            this.progressBar.Style = WinForms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 1;

            // lblProgress
            this.lblProgress.Anchor = ((WinForms.AnchorStyles)((WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Right)));
            this.lblProgress.Font = new Drawing.Font("Segoe UI", 9F, Drawing.FontStyle.Bold);
            this.lblProgress.ForeColor = Drawing.Color.FromArgb(0, 122, 204);
            this.lblProgress.Location = new Drawing.Point(350, 110);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new Drawing.Size(70, 15);
            this.lblProgress.TabIndex = 2;
            this.lblProgress.Text = "0%";
            this.lblProgress.TextAlign = Drawing.ContentAlignment.TopRight;

            // lblCurrentItem
            this.lblCurrentItem.Anchor = ((WinForms.AnchorStyles)(((WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Left) | WinForms.AnchorStyles.Right)));
            this.lblCurrentItem.Font = new Drawing.Font("Segoe UI", 9F);
            this.lblCurrentItem.ForeColor = Drawing.Color.FromArgb(108, 117, 125);
            this.lblCurrentItem.Location = new Drawing.Point(30, 110);
            this.lblCurrentItem.Name = "lblCurrentItem";
            this.lblCurrentItem.Size = new Drawing.Size(314, 15);
            this.lblCurrentItem.TabIndex = 3;
            this.lblCurrentItem.Text = "Initializing...";

            // btnCancel
            this.btnCancel.Anchor = ((WinForms.AnchorStyles)((WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right)));
            this.btnCancel.BackColor = Drawing.Color.FromArgb(220, 53, 69);
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = WinForms.FlatStyle.Flat;
            this.btnCancel.Font = new Drawing.Font("Segoe UI", 9F);
            this.btnCancel.ForeColor = Drawing.Color.White;
            this.btnCancel.Location = new Drawing.Point(345, 140);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            // ProgressForm
            this.AutoScaleDimensions = new Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = WinForms.AutoScaleMode.Font;
            this.BackColor = Drawing.Color.White;
            this.ClientSize = new Drawing.Size(450, 180);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblCurrentItem);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.panel1);
            this.Font = new Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = WinForms.FormStartPosition.CenterParent;
            this.Text = "DEAXO - Processing";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
        }
        #endregion
    }
}