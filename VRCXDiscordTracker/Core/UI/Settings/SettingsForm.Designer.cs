namespace VRCXDiscordTracker.Core;

partial class SettingsForm
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
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
        label2 = new Label();
        textBoxDatabasePath = new TextBox();
        label3 = new Label();
        textBoxDiscordWebhookUrl = new TextBox();
        buttonSave = new Button();
        checkBoxNotifyOnStart = new CheckBox();
        checkBoxNotifyOnExit = new CheckBox();
        SuspendLayout();
        //
        // label2
        //
        label2.AutoSize = true;
        label2.Location = new Point(30, 14);
        label2.Name = "label2";
        label2.Size = new Size(125, 25);
        label2.TabIndex = 2;
        label2.Text = "DatabasePath:";
        //
        // textBoxDatabasePath
        //
        textBoxDatabasePath.Location = new Point(33, 42);
        textBoxDatabasePath.Margin = new Padding(3, 4, 3, 4);
        textBoxDatabasePath.Multiline = true;
        textBoxDatabasePath.Name = "textBoxDatabasePath";
        textBoxDatabasePath.Size = new Size(726, 68);
        textBoxDatabasePath.TabIndex = 3;
        //
        // label3
        //
        label3.AutoSize = true;
        label3.Location = new Point(30, 135);
        label3.Name = "label3";
        label3.Size = new Size(195, 25);
        label3.TabIndex = 4;
        label3.Text = "Discord Webhook URL:";
        //
        // textBoxDiscordWebhookUrl
        //
        textBoxDiscordWebhookUrl.Location = new Point(33, 162);
        textBoxDiscordWebhookUrl.Margin = new Padding(3, 4, 3, 4);
        textBoxDiscordWebhookUrl.Multiline = true;
        textBoxDiscordWebhookUrl.Name = "textBoxDiscordWebhookUrl";
        textBoxDiscordWebhookUrl.Size = new Size(726, 68);
        textBoxDiscordWebhookUrl.TabIndex = 5;
        //
        // buttonSave
        //
        buttonSave.Location = new Point(656, 353);
        buttonSave.Margin = new Padding(3, 4, 3, 4);
        buttonSave.Name = "buttonSave";
        buttonSave.Size = new Size(103, 53);
        buttonSave.TabIndex = 6;
        buttonSave.Text = "Save";
        buttonSave.UseVisualStyleBackColor = true;
        buttonSave.Click += OnSaveButtonClicked;
        //
        // checkBoxNotifyOnStart
        //
        checkBoxNotifyOnStart.AutoSize = true;
        checkBoxNotifyOnStart.Location = new Point(30, 268);
        checkBoxNotifyOnStart.Name = "checkBoxNotifyOnStart";
        checkBoxNotifyOnStart.Size = new Size(413, 29);
        checkBoxNotifyOnStart.TabIndex = 7;
        checkBoxNotifyOnStart.Text = "Send a message when the application is started";
        checkBoxNotifyOnStart.UseVisualStyleBackColor = true;
        //
        // checkBoxNotifyOnExit
        //
        checkBoxNotifyOnExit.AutoSize = true;
        checkBoxNotifyOnExit.Location = new Point(30, 303);
        checkBoxNotifyOnExit.Name = "checkBoxNotifyOnExit";
        checkBoxNotifyOnExit.Size = new Size(405, 29);
        checkBoxNotifyOnExit.TabIndex = 8;
        checkBoxNotifyOnExit.Text = "Send a message when the application is exited";
        checkBoxNotifyOnExit.UseVisualStyleBackColor = true;
        //
        // SettingsForm
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 419);
        Controls.Add(checkBoxNotifyOnExit);
        Controls.Add(checkBoxNotifyOnStart);
        Controls.Add(buttonSave);
        Controls.Add(textBoxDiscordWebhookUrl);
        Controls.Add(label3);
        Controls.Add(textBoxDatabasePath);
        Controls.Add(label2);
        FormBorderStyle = FormBorderStyle.Fixed3D;
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(3, 4, 3, 4);
        MaximizeBox = false;
        Name = "SettingsForm";
        Text = AppConstants.AppName + " Settings";
        FormClosing += OnFormClosing;
        Load += OnLoad;
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox textBoxDatabasePath;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.TextBox textBoxDiscordWebhookUrl;
    private System.Windows.Forms.Button buttonSave;
    private CheckBox checkBoxNotifyOnStart;
    private CheckBox checkBoxNotifyOnExit;
}
