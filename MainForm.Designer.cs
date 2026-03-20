using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CidToolRenamer
{
    partial class MainForm
    {
        private IContainer components = null;
        private Label lblRootFolder;
        private TextBox txtRootFolder;
        private Button btnBrowseFolder;
        private CheckBox chkIncludeSubfolders;
        private CheckBox chkCreateBackups;
        private CheckBox chkDryRun;
        private CheckBox chkCidFiles;
        private CheckBox chkBppFiles;
        private CheckBox chkIsoFiles;
        private Button btnScanTools;
        private Button btnLoadMapping;
        private Button btnSaveMapping;
        private Button btnApply;
        private DataGridView gridTools;
        private ProgressBar progressBarFiles;
        private Label lblStatus;
        private TableLayoutPanel tableLayoutPanel;
        private FlowLayoutPanel optionsPanel;
        private FlowLayoutPanel actionsPanel;
        private Panel rootPanel;
        private Panel statusPanel;
        private DataGridViewTextBoxColumn colOldName;
        private DataGridViewTextBoxColumn colNewName;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            tableLayoutPanel = new TableLayoutPanel();
            rootPanel = new Panel();
            lblRootFolder = new Label();
            txtRootFolder = new TextBox();
            btnBrowseFolder = new Button();
            optionsPanel = new FlowLayoutPanel();
            chkIncludeSubfolders = new CheckBox();
            chkCreateBackups = new CheckBox();
            chkDryRun = new CheckBox();
            chkCidFiles = new CheckBox();
            chkBppFiles = new CheckBox();
            actionsPanel = new FlowLayoutPanel();
            btnScanTools = new Button();
            btnLoadMapping = new Button();
            btnSaveMapping = new Button();
            btnApply = new Button();
            gridTools = new DataGridView();
            colOldName = new DataGridViewTextBoxColumn();
            colNewName = new DataGridViewTextBoxColumn();
            statusPanel = new Panel();
            progressBarFiles = new ProgressBar();
            lblStatus = new Label();
            tableLayoutPanel.SuspendLayout();
            rootPanel.SuspendLayout();
            optionsPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            ((ISupportInitialize)gridTools).BeginInit();
            statusPanel.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(rootPanel, 0, 0);
            tableLayoutPanel.Controls.Add(optionsPanel, 0, 1);
            tableLayoutPanel.Controls.Add(actionsPanel, 0, 2);
            tableLayoutPanel.Controls.Add(gridTools, 0, 3);
            tableLayoutPanel.Controls.Add(statusPanel, 0, 4);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 5;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel.Size = new System.Drawing.Size(884, 561);
            tableLayoutPanel.TabIndex = 0;
            // 
            // rootPanel
            // 
            rootPanel.Controls.Add(lblRootFolder);
            rootPanel.Controls.Add(txtRootFolder);
            rootPanel.Controls.Add(btnBrowseFolder);
            rootPanel.Dock = DockStyle.Fill;
            rootPanel.Location = new System.Drawing.Point(3, 3);
            rootPanel.Name = "rootPanel";
            rootPanel.Size = new System.Drawing.Size(878, 34);
            rootPanel.TabIndex = 0;
            // 
            // lblRootFolder
            // 
            lblRootFolder.AutoSize = true;
            lblRootFolder.Location = new System.Drawing.Point(3, 9);
            lblRootFolder.Name = "lblRootFolder";
            lblRootFolder.Size = new System.Drawing.Size(74, 15);
            lblRootFolder.TabIndex = 0;
            lblRootFolder.Text = "Root Folder:";
            // 
            // txtRootFolder
            // 
            txtRootFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtRootFolder.Location = new System.Drawing.Point(83, 6);
            txtRootFolder.Name = "txtRootFolder";
            txtRootFolder.Size = new System.Drawing.Size(700, 23);
            txtRootFolder.TabIndex = 1;
            // 
            // btnBrowseFolder
            // 
            btnBrowseFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseFolder.Location = new System.Drawing.Point(789, 5);
            btnBrowseFolder.Name = "btnBrowseFolder";
            btnBrowseFolder.Size = new System.Drawing.Size(86, 25);
            btnBrowseFolder.TabIndex = 2;
            btnBrowseFolder.Text = "Browse...";
            btnBrowseFolder.UseVisualStyleBackColor = true;
            btnBrowseFolder.Click += btnBrowseFolder_Click;
            // 
            // optionsPanel
            // 
            optionsPanel.AutoSize = true;
            optionsPanel.Controls.Add(chkIncludeSubfolders);
            optionsPanel.Controls.Add(chkCreateBackups);
            optionsPanel.Controls.Add(chkDryRun);
            optionsPanel.Controls.Add(chkCidFiles);
            optionsPanel.Controls.Add(chkBppFiles);
            optionsPanel.Controls.Add(chkIsoFiles);
            optionsPanel.Dock = DockStyle.Fill;
            optionsPanel.Location = new System.Drawing.Point(3, 43);
            optionsPanel.Name = "optionsPanel";
            optionsPanel.Size = new System.Drawing.Size(878, 26);
            optionsPanel.TabIndex = 1;
            // 
            // chkIncludeSubfolders
            // 
            chkIncludeSubfolders.AutoSize = true;
            chkIncludeSubfolders.Checked = true;
            chkIncludeSubfolders.CheckState = CheckState.Checked;
            chkIncludeSubfolders.Location = new System.Drawing.Point(3, 3);
            chkIncludeSubfolders.Name = "chkIncludeSubfolders";
            chkIncludeSubfolders.Size = new System.Drawing.Size(130, 19);
            chkIncludeSubfolders.TabIndex = 0;
            chkIncludeSubfolders.Text = "Include subfolders";
            chkIncludeSubfolders.UseVisualStyleBackColor = true;
            // 
            // chkCreateBackups
            // 
            chkCreateBackups.AutoSize = true;
            chkCreateBackups.Checked = true;
            chkCreateBackups.CheckState = CheckState.Checked;
            chkCreateBackups.Location = new System.Drawing.Point(139, 3);
            chkCreateBackups.Name = "chkCreateBackups";
            chkCreateBackups.Size = new System.Drawing.Size(136, 19);
            chkCreateBackups.TabIndex = 1;
            chkCreateBackups.Text = "Create .bak backups";
            chkCreateBackups.UseVisualStyleBackColor = true;
            // 
            // chkDryRun
            // 
            chkDryRun.AutoSize = true;
            chkDryRun.Location = new System.Drawing.Point(281, 3);
            chkDryRun.Name = "chkDryRun";
            chkDryRun.Size = new System.Drawing.Size(140, 19);
            chkDryRun.TabIndex = 2;
            chkDryRun.Text = "Dry run (don’t write)";
            chkDryRun.UseVisualStyleBackColor = true;
            // 
            // chkCidFiles
            // 
            chkCidFiles.AutoSize = true;
            chkCidFiles.Checked = true;
            chkCidFiles.CheckState = CheckState.Checked;
            chkCidFiles.Location = new System.Drawing.Point(427, 3);
            chkCidFiles.Name = "chkCidFiles";
            chkCidFiles.Size = new System.Drawing.Size(73, 19);
            chkCidFiles.TabIndex = 3;
            chkCidFiles.Text = ".CID files";
            chkCidFiles.UseVisualStyleBackColor = true;
            // 
            // chkBppFiles
            // 
            chkBppFiles.AutoSize = true;
            chkBppFiles.Checked = true;
            chkBppFiles.CheckState = CheckState.Checked;
            chkBppFiles.Location = new System.Drawing.Point(506, 3);
            chkBppFiles.Name = "chkBppFiles";
            chkBppFiles.Size = new System.Drawing.Size(75, 19);
            chkBppFiles.TabIndex = 4;
            chkBppFiles.Text = ".BPP files";
            chkBppFiles.UseVisualStyleBackColor = true;
            // 
            // chkIsoFiles
            // 
            chkIsoFiles.AutoSize = true;
            chkIsoFiles.Checked = true;
            chkIsoFiles.CheckState = CheckState.Checked;
            chkIsoFiles.Location = new System.Drawing.Point(587, 3);
            chkIsoFiles.Name = "chkIsoFiles";
            chkIsoFiles.Size = new System.Drawing.Size(75, 19);
            chkIsoFiles.TabIndex = 5;
            chkIsoFiles.Text = ".ISO/TAP files";
            chkIsoFiles.UseVisualStyleBackColor = true;
            // 
            // actionsPanel
            // 
            actionsPanel.AutoSize = true;
            actionsPanel.Controls.Add(btnScanTools);
            actionsPanel.Controls.Add(btnLoadMapping);
            actionsPanel.Controls.Add(btnSaveMapping);
            actionsPanel.Controls.Add(btnApply);
            actionsPanel.Dock = DockStyle.Fill;
            actionsPanel.Location = new System.Drawing.Point(3, 75);
            actionsPanel.Name = "actionsPanel";
            actionsPanel.Size = new System.Drawing.Size(878, 34);
            actionsPanel.TabIndex = 2;
            // 
            // btnScanTools
            // 
            btnScanTools.Location = new System.Drawing.Point(3, 3);
            btnScanTools.Name = "btnScanTools";
            btnScanTools.Size = new System.Drawing.Size(90, 27);
            btnScanTools.TabIndex = 0;
            btnScanTools.Text = "Scan Tools";
            btnScanTools.UseVisualStyleBackColor = true;
            btnScanTools.Click += btnScanTools_Click;
            // 
            // btnLoadMapping
            // 
            btnLoadMapping.Location = new System.Drawing.Point(99, 3);
            btnLoadMapping.Name = "btnLoadMapping";
            btnLoadMapping.Size = new System.Drawing.Size(114, 27);
            btnLoadMapping.TabIndex = 1;
            btnLoadMapping.Text = "Load Mapping...";
            btnLoadMapping.UseVisualStyleBackColor = true;
            btnLoadMapping.Click += btnLoadMapping_Click;
            // 
            // btnSaveMapping
            // 
            btnSaveMapping.Location = new System.Drawing.Point(219, 3);
            btnSaveMapping.Name = "btnSaveMapping";
            btnSaveMapping.Size = new System.Drawing.Size(114, 27);
            btnSaveMapping.TabIndex = 2;
            btnSaveMapping.Text = "Save Mapping...";
            btnSaveMapping.UseVisualStyleBackColor = true;
            btnSaveMapping.Click += btnSaveMapping_Click;
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(339, 3);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(104, 27);
            btnApply.TabIndex = 3;
            btnApply.Text = "Apply Mapping";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // gridTools
            // 
            gridTools.AllowUserToAddRows = false;
            gridTools.AllowUserToDeleteRows = false;
            gridTools.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridTools.Columns.AddRange(new DataGridViewColumn[] { colOldName, colNewName });
            gridTools.Dock = DockStyle.Fill;
            gridTools.Location = new System.Drawing.Point(3, 115);
            gridTools.Name = "gridTools";
            gridTools.RowHeadersVisible = false;
            gridTools.RowTemplate.Height = 25;
            gridTools.Size = new System.Drawing.Size(878, 411);
            gridTools.TabIndex = 3;
            // 
            // colOldName
            // 
            colOldName.DataPropertyName = "OldName";
            colOldName.HeaderText = "Old Name";
            colOldName.Name = "colOldName";
            colOldName.ReadOnly = true;
            colOldName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // 
            // colNewName
            // 
            colNewName.DataPropertyName = "NewName";
            colNewName.HeaderText = "New Name";
            colNewName.Name = "colNewName";
            colNewName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // 
            // statusPanel
            // 
            statusPanel.Controls.Add(progressBarFiles);
            statusPanel.Controls.Add(lblStatus);
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.Location = new System.Drawing.Point(3, 532);
            statusPanel.Name = "statusPanel";
            statusPanel.Size = new System.Drawing.Size(878, 26);
            statusPanel.TabIndex = 4;
            // 
            // progressBarFiles
            // 
            progressBarFiles.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            progressBarFiles.Location = new System.Drawing.Point(3, 3);
            progressBarFiles.Name = "progressBarFiles";
            progressBarFiles.Size = new System.Drawing.Size(650, 20);
            progressBarFiles.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(659, 5);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(39, 15);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Ready";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(884, 561);
            Controls.Add(tableLayoutPanel);
            Name = "MainForm";
            Text = "CID & BPP Tool Renamer";
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            rootPanel.ResumeLayout(false);
            rootPanel.PerformLayout();
            optionsPanel.ResumeLayout(false);
            optionsPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            ((ISupportInitialize)gridTools).EndInit();
            statusPanel.ResumeLayout(false);
            statusPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
