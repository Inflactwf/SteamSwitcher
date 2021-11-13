namespace Steam_Switcher
{
    partial class CrashWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ExitBtn = new System.Windows.Forms.Button();
            this.MakeDmpBtn = new System.Windows.Forms.Button();
            this.DumpWorker = new System.ComponentModel.BackgroundWorker();
            this.TipLabel = new System.Windows.Forms.Label();
            this.LoadingImg = new System.Windows.Forms.PictureBox();
            this.LoadingGrid = new System.Windows.Forms.Panel();
            this.DumpBar = new System.Windows.Forms.ProgressBar();
            this.TimerLabel = new System.Windows.Forms.Label();
            this.ExitTimer = new System.Windows.Forms.Timer(this.components);
            this.AnimationTimer = new System.Windows.Forms.Timer(this.components);
            this.DumpTimer = new System.Windows.Forms.Timer(this.components);
            this.ExceptionLabel = new System.Windows.Forms.TextBox();
            this.WriteDumpWorker = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.LoadingImg)).BeginInit();
            this.LoadingGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // ExitBtn
            // 
            this.ExitBtn.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ExitBtn.FlatAppearance.BorderSize = 0;
            this.ExitBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ExitBtn.Font = new System.Drawing.Font("Segoe UI Semilight", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ExitBtn.Location = new System.Drawing.Point(319, 236);
            this.ExitBtn.Name = "ExitBtn";
            this.ExitBtn.Size = new System.Drawing.Size(75, 26);
            this.ExitBtn.TabIndex = 0;
            this.ExitBtn.Text = "Выход";
            this.ExitBtn.UseVisualStyleBackColor = false;
            this.ExitBtn.Click += new System.EventHandler(this.ExitBtn_Click);
            // 
            // MakeDmpBtn
            // 
            this.MakeDmpBtn.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.MakeDmpBtn.FlatAppearance.BorderSize = 0;
            this.MakeDmpBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.MakeDmpBtn.Font = new System.Drawing.Font("Segoe UI Semilight", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MakeDmpBtn.Location = new System.Drawing.Point(185, 236);
            this.MakeDmpBtn.Name = "MakeDmpBtn";
            this.MakeDmpBtn.Size = new System.Drawing.Size(128, 26);
            this.MakeDmpBtn.TabIndex = 1;
            this.MakeDmpBtn.Text = "Сохранить отчёт";
            this.MakeDmpBtn.UseVisualStyleBackColor = false;
            this.MakeDmpBtn.Click += new System.EventHandler(this.MakeDmpBtn_Click);
            // 
            // DumpWorker
            // 
            this.DumpWorker.WorkerReportsProgress = true;
            this.DumpWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.DumpWorker_DoWork);
            this.DumpWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.DumpWorker_RunWorkerCompleted);
            // 
            // TipLabel
            // 
            this.TipLabel.BackColor = System.Drawing.Color.Transparent;
            this.TipLabel.Font = new System.Drawing.Font("Segoe UI Semilight", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TipLabel.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.TipLabel.Location = new System.Drawing.Point(12, 179);
            this.TipLabel.Name = "TipLabel";
            this.TipLabel.Size = new System.Drawing.Size(382, 48);
            this.TipLabel.TabIndex = 2;
            this.TipLabel.Text = "В ходе работы программы возникло необработанное исключение, если Вы желаете завер" +
    "шить работу с программой и сделать копию отчёта, нажмите \'Сохранить отчёт\'. В пр" +
    "отивном случае нажмите \'Выход\'.";
            // 
            // LoadingImg
            // 
            this.LoadingImg.BackColor = System.Drawing.Color.Transparent;
            this.LoadingImg.Image = global::Steam_Switcher.Properties.Resources.gears;
            this.LoadingImg.Location = new System.Drawing.Point(171, 32);
            this.LoadingImg.Name = "LoadingImg";
            this.LoadingImg.Size = new System.Drawing.Size(64, 64);
            this.LoadingImg.TabIndex = 0;
            this.LoadingImg.TabStop = false;
            // 
            // LoadingGrid
            // 
            this.LoadingGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(45)))), ((int)(((byte)(52)))));
            this.LoadingGrid.Controls.Add(this.DumpBar);
            this.LoadingGrid.Controls.Add(this.TimerLabel);
            this.LoadingGrid.Controls.Add(this.LoadingImg);
            this.LoadingGrid.Location = new System.Drawing.Point(0, 0);
            this.LoadingGrid.Name = "LoadingGrid";
            this.LoadingGrid.Size = new System.Drawing.Size(406, 169);
            this.LoadingGrid.TabIndex = 3;
            this.LoadingGrid.Visible = false;
            // 
            // DumpBar
            // 
            this.DumpBar.Location = new System.Drawing.Point(12, 137);
            this.DumpBar.Name = "DumpBar";
            this.DumpBar.Size = new System.Drawing.Size(382, 23);
            this.DumpBar.Step = 20;
            this.DumpBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.DumpBar.TabIndex = 5;
            // 
            // TimerLabel
            // 
            this.TimerLabel.BackColor = System.Drawing.Color.Transparent;
            this.TimerLabel.Font = new System.Drawing.Font("Segoe UI Light", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TimerLabel.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.TimerLabel.Location = new System.Drawing.Point(70, 116);
            this.TimerLabel.Name = "TimerLabel";
            this.TimerLabel.Size = new System.Drawing.Size(270, 18);
            this.TimerLabel.TabIndex = 4;
            this.TimerLabel.Text = "Отчёт успешно создан. Завершение работы... (3)";
            this.TimerLabel.Visible = false;
            // 
            // ExitTimer
            // 
            this.ExitTimer.Interval = 1000;
            this.ExitTimer.Tick += new System.EventHandler(this.ExitTimer_Tick);
            // 
            // AnimationTimer
            // 
            this.AnimationTimer.Interval = 1;
            this.AnimationTimer.Tick += new System.EventHandler(this.AnimationTimer_Tick);
            // 
            // DumpTimer
            // 
            this.DumpTimer.Interval = 1500;
            this.DumpTimer.Tick += new System.EventHandler(this.DumpTimer_Tick);
            // 
            // ExceptionLabel
            // 
            this.ExceptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(38)))), ((int)(((byte)(62)))));
            this.ExceptionLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ExceptionLabel.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.ExceptionLabel.Font = new System.Drawing.Font("Segoe UI Semilight", 8.25F);
            this.ExceptionLabel.ForeColor = System.Drawing.Color.White;
            this.ExceptionLabel.Location = new System.Drawing.Point(12, 198);
            this.ExceptionLabel.Multiline = true;
            this.ExceptionLabel.Name = "ExceptionLabel";
            this.ExceptionLabel.ReadOnly = true;
            this.ExceptionLabel.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ExceptionLabel.Size = new System.Drawing.Size(382, 32);
            this.ExceptionLabel.TabIndex = 6;
            this.ExceptionLabel.Text = "ExceptionMessage";
            // 
            // WriteDumpWorker
            // 
            this.WriteDumpWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.WriteDumpWorker_DoWork);
            this.WriteDumpWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.WriteDumpWorker_RunWorkerCompleted);
            // 
            // CrashWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(45)))), ((int)(((byte)(52)))));
            this.ClientSize = new System.Drawing.Size(406, 274);
            this.Controls.Add(this.ExceptionLabel);
            this.Controls.Add(this.LoadingGrid);
            this.Controls.Add(this.TipLabel);
            this.Controls.Add(this.MakeDmpBtn);
            this.Controls.Add(this.ExitBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CrashWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Обработчик исключений Steam Switcher";
            ((System.ComponentModel.ISupportInitialize)(this.LoadingImg)).EndInit();
            this.LoadingGrid.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ExitBtn;
        private System.Windows.Forms.Button MakeDmpBtn;
        private System.ComponentModel.BackgroundWorker DumpWorker;
        private System.Windows.Forms.Label TipLabel;
        private System.Windows.Forms.PictureBox LoadingImg;
        private System.Windows.Forms.Panel LoadingGrid;
        private System.Windows.Forms.Timer ExitTimer;
        private System.Windows.Forms.Label TimerLabel;
        private System.Windows.Forms.Timer AnimationTimer;
        private System.Windows.Forms.Timer DumpTimer;
        private System.Windows.Forms.TextBox ExceptionLabel;
        public System.Windows.Forms.ProgressBar DumpBar;
        private System.ComponentModel.BackgroundWorker WriteDumpWorker;
    }
}