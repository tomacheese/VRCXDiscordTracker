namespace VRCXDiscordTracker
{
    partial class SettingsForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxDatabasePath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxDiscordWebhookUrl = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(118, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "DatabasePath:";
            // 
            // textBoxDatabasePath
            // 
            this.textBoxDatabasePath.Location = new System.Drawing.Point(33, 30);
            this.textBoxDatabasePath.Multiline = true;
            this.textBoxDatabasePath.Name = "textBoxDatabasePath";
            this.textBoxDatabasePath.Size = new System.Drawing.Size(726, 50);
            this.textBoxDatabasePath.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(178, 18);
            this.label3.TabIndex = 4;
            this.label3.Text = "Discord Webhook URL:";
            // 
            // textBoxDiscordWebhookUrl
            // 
            this.textBoxDiscordWebhookUrl.Location = new System.Drawing.Point(33, 117);
            this.textBoxDiscordWebhookUrl.Multiline = true;
            this.textBoxDiscordWebhookUrl.Name = "textBoxDiscordWebhookUrl";
            this.textBoxDiscordWebhookUrl.Size = new System.Drawing.Size(726, 50);
            this.textBoxDiscordWebhookUrl.TabIndex = 5;
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(653, 183);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(103, 38);
            this.buttonSave.TabIndex = 6;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.OnSaveButtonClicked);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 237);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.textBoxDiscordWebhookUrl);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxDatabasePath);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "VRCXDiscordTracker Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxDatabasePath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxDiscordWebhookUrl;
        private System.Windows.Forms.Button buttonSave;
    }
}