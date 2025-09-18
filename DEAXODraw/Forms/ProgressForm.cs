using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace DEAXODraw.Forms
{
    public partial class ProgressForm : Form
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
                MessageBox.Show($"An error occurred: {e.Error.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Result = e.Result;
            DialogResult = isCancelled ? DialogResult.Cancel : DialogResult.OK;
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

        protected override void OnFormClosing(FormClosingEventArgs e)
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
        private ProgressBar progressBar;
        private Label lblDescription;
        private Label lblProgress;
        private Label lblCurrentItem;
        private Button btnCancel;
        private Panel panel1;
        private PictureBox pictureBox1;

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
            this.progressBar = new ProgressBar();
            this.lblDescription = new Label();
            this.lblProgress = new Label();
            this.lblCurrentItem = new Label();
            this.btnCancel = new Button();
            this.panel1 = new Panel();
            this.pictureBox1 = new PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();

            // panel1
            this.panel1.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.panel1.BackColor = Color.FromArgb(248, 249, 250);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.lblDescription);
            this.panel1.Location = new Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Size(450, 60);
            this.panel1.TabIndex = 0;

            // pictureBox1
            this.pictureBox1.BackgroundImage = global::DEAXODraw.Properties.Resources.DEAXO_Logo;
            this.pictureBox1.BackgroundImageLayout = ImageLayout.Zoom;
            this.pictureBox1.Location = new Point(15, 15);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Size(40, 30);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;

            // lblDescription
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblDescription.ForeColor = Color.FromArgb(0, 122, 204);
            this.lblDescription.Location = new Point(65, 20);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new Size(89, 19);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "Processing...";

            // progressBar
            this.progressBar.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.progressBar.Location = new Point(30, 80);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(390, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 1;

            // lblProgress
            this.lblProgress.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.lblProgress.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblProgress.ForeColor = Color.FromArgb(0, 122, 204);
            this.lblProgress.Location = new Point(350, 110);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new Size(70, 15);
            this.lblProgress.TabIndex = 2;
            this.lblProgress.Text = "0%";
            this.lblProgress.TextAlign = ContentAlignment.TopRight;

            // lblCurrentItem
            this.lblCurrentItem.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.lblCurrentItem.Font = new Font("Segoe UI", 9F);
            this.lblCurrentItem.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblCurrentItem.Location = new Point(30, 110);
            this.lblCurrentItem.Name = "lblCurrentItem";
            this.lblCurrentItem.Size = new Size(314, 15);
            this.lblCurrentItem.TabIndex = 3;
            this.lblCurrentItem.Text = "Initializing...";

            // btnCancel
            this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.btnCancel.BackColor = Color.FromArgb(220, 53, 69);
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.Font = new Font("Segoe UI", 9F);
            this.btnCancel.ForeColor = Color.White;
            this.btnCancel.Location = new Point(345, 140);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            // ProgressForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(450, 180);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblCurrentItem);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.panel1);
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "DEAXO - Processing";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
        }
        #endregion
    }
}