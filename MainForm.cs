using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Rover
{
    public partial class MainForm : Form
    {
        private readonly BindingList<ToolMappingEntry> _toolMappings = new();
        private readonly Dictionary<string, ToolMappingEntry> _mappingByOldName = new();
        private readonly List<string> _errorLog = new();

        // Match TOOLNAME=<value> or Tool=<value> case-insensitively; group 1 preserves prefix spacing/casing, group 2 is the tool value.
        private static readonly Regex ToolRegex = new(@"(Tool(?:Name)?\s*=\s*)([^\s;,\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Match T=<number> specifically for G-code style files.
        private static readonly Regex TEqualsRegex = new(@"\b(T\s*=\s*)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Match compact tool calls like T12 (common in NC/TAP/CNC).
        private static readonly Regex TCompactRegex = new(@"\b(T)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Length offset (G43 H...) and cutter comp (G41/G42 D...) - rewritten when digits match a mapping key (same as T).
        private static readonly Regex HCompactRegex = new(@"\b(H)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DCompactRegex = new(@"\b(D)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // H=/D= forms (some posts), same replacement contract as T=.
        private static readonly Regex HEqualsRegex = new(@"\b(H\s*=\s*)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DEqualsRegex = new(@"\b(D\s*=\s*)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Fused G43H12 / G41D3 / G42D3 - no word boundary between the G-code and H/D, so compact \b(H) misses these.
        private static readonly Regex G43HNoSpaceRegex = new(@"\b(G43)(H)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex G41DNoSpaceRegex = new(@"\b(G41)(D)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex G42DNoSpaceRegex = new(@"\b(G42)(D)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Heuristic parsing for unknown/variant text formats.
        private static readonly Regex HeuristicToolNamedRegex = new(@"\b(?:TOOLNAME|TOOL)\s*[:=]\s*([A-Za-z0-9_./\-]{1,32})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HeuristicToolNumberRegex = new(@"\bT\s*=?\s*(\d{1,4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            TrySetWindowIconFromExecutable();
            ApplyTheme();
            gridTools.AutoGenerateColumns = false;
            gridTools.DataSource = _toolMappings;
            lblStatus.Text = "Ready";
        }

        private void TrySetWindowIconFromExecutable()
        {
            try
            {
                string? path = Application.ExecutablePath;
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                using Icon? extracted = Icon.ExtractAssociatedIcon(path);
                if (extracted is not null)
                {
                    Icon = (Icon)extracted.Clone();
                }
            }
            catch
            {
                // Ignore icon failures (e.g., unusual hosting scenarios).
            }
        }

        private void ApplyTheme()
        {
            // Navy-focused theme to match CAD UI styling while preserving current layout.
            Color appBg = Color.FromArgb(5, 17, 42);
            Color panelBg = Color.FromArgb(9, 29, 64);
            Color panelBgAlt = Color.FromArgb(12, 38, 82);
            Color buttonBg = Color.FromArgb(22, 66, 130);
            Color buttonHover = Color.FromArgb(31, 84, 162);
            Color border = Color.FromArgb(52, 92, 150);
            Color textPrimary = Color.FromArgb(220, 234, 252);
            Color textMuted = Color.FromArgb(166, 189, 224);
            Color gridSelection = Color.FromArgb(59, 120, 204);

            BackColor = appBg;
            ForeColor = textPrimary;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            tableLayoutPanel.BackColor = appBg;
            rootPanel.BackColor = panelBg;
            optionsPanel.BackColor = panelBg;
            actionsPanel.BackColor = panelBg;
            statusPanel.BackColor = panelBgAlt;

            lblRootFolder.ForeColor = textPrimary;
            lblStatus.ForeColor = textMuted;

            txtRootFolder.BackColor = Color.FromArgb(8, 23, 50);
            txtRootFolder.ForeColor = textPrimary;
            txtRootFolder.BorderStyle = BorderStyle.FixedSingle;

            StyleButton(btnBrowseFolder, buttonBg, buttonHover, textPrimary, border);
            StyleButton(btnScanTools, buttonBg, buttonHover, textPrimary, border);
            StyleButton(btnLoadMapping, buttonBg, buttonHover, textPrimary, border);
            StyleButton(btnSaveMapping, buttonBg, buttonHover, textPrimary, border);
            StyleButton(btnApply, Color.FromArgb(26, 84, 156), Color.FromArgb(34, 99, 182), textPrimary, border);

            // Keep solid backgrounds on checkboxes to avoid WinForms transparency ghosting artifacts.
            StyleCheckBox(chkIncludeSubfolders, textPrimary, panelBg);
            StyleCheckBox(chkCreateBackups, textPrimary, panelBg);
            StyleCheckBox(chkDryRun, textPrimary, panelBg);
            StyleCheckBox(chkCidFiles, textPrimary, panelBg);
            StyleCheckBox(chkBppFiles, textPrimary, panelBg);
            StyleCheckBox(chkIsoFiles, textPrimary, panelBg);
            lblGCodeExtensions.BackColor = panelBg;
            lblGCodeExtensions.ForeColor = textMuted;

            // Larger CAD-style action targets.
            btnBrowseFolder.Size = new Size(96, 27);
            btnScanTools.Size = new Size(120, 32);
            btnLoadMapping.Size = new Size(138, 32);
            btnSaveMapping.Size = new Size(138, 32);
            btnApply.Size = new Size(140, 32);
            btnScanTools.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnLoadMapping.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnSaveMapping.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnApply.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);

            progressBarFiles.Style = ProgressBarStyle.Continuous;

            gridTools.BackgroundColor = Color.FromArgb(7, 25, 55);
            gridTools.BorderStyle = BorderStyle.FixedSingle;
            gridTools.GridColor = Color.FromArgb(37, 73, 125);
            gridTools.EnableHeadersVisualStyles = false;
            gridTools.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 48, 96);
            gridTools.ColumnHeadersDefaultCellStyle.ForeColor = textPrimary;
            gridTools.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(17, 48, 96);
            gridTools.ColumnHeadersDefaultCellStyle.SelectionForeColor = textPrimary;
            gridTools.ColumnHeadersHeight = 28;
            gridTools.DefaultCellStyle.BackColor = Color.FromArgb(9, 31, 68);
            gridTools.DefaultCellStyle.ForeColor = textPrimary;
            gridTools.DefaultCellStyle.SelectionBackColor = gridSelection;
            gridTools.DefaultCellStyle.SelectionForeColor = Color.White;
            gridTools.RowHeadersVisible = false;
        }

        private static void StyleButton(Button button, Color baseColor, Color hoverColor, Color foreColor, Color borderColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = borderColor;
            button.BackColor = baseColor;
            button.ForeColor = foreColor;
            button.UseVisualStyleBackColor = false;

            button.MouseEnter += (_, _) => button.BackColor = hoverColor;
            button.MouseLeave += (_, _) => button.BackColor = baseColor;
        }

        private static void StyleCheckBox(CheckBox checkBox, Color foreColor, Color backColor)
        {
            checkBox.ForeColor = foreColor;
            checkBox.BackColor = backColor;
        }

        private void btnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select root folder (CID, BPP, and/or G-code files)"
            };

            if (Directory.Exists(txtRootFolder.Text))
            {
                dialog.SelectedPath = txtRootFolder.Text;
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtRootFolder.Text = dialog.SelectedPath;
            }
        }

        private void btnScanTools_Click(object? sender, EventArgs e)
        {
            if (!ValidateRootFolder(out var rootFolder))
            {
                return;
            }

            _toolMappings.Clear();
            _mappingByOldName.Clear();
            _errorLog.Clear();

            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = EnumerateTargetFiles(rootFolder, searchOption);
            if (files.Count == 0)
            {
                lblStatus.Text = "No matching files found.";
                MessageBox.Show(this, "No matching CID, BPP, or G-code files were found in the selected folder.", "Scan Tools", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            lblStatus.Text = "Scanning tools...";

            foreach (var file in files)
            {
                foreach (var toolName in ExtractToolsFromFile(file))
                {
                    if (string.IsNullOrEmpty(toolName))
                    {
                        continue;
                    }

                    if (!_mappingByOldName.ContainsKey(toolName))
                    {
                        var entry = new ToolMappingEntry
                        {
                            OldName = toolName,
                            NewName = toolName
                        };
                        _mappingByOldName[toolName] = entry;
                        _toolMappings.Add(entry);
                    }
                }
            }

            if (_toolMappings.Count == 0)
            {
                lblStatus.Text = "No tools detected. Manual assist available.";
                var shouldRetry = MessageBox.Show(
                    this,
                    "No tool names were detected automatically.\n\nWould you like to enter one known tool name/number so the app can retry parsing?",
                    "No tools detected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (shouldRetry == DialogResult.Yes)
                {
                    string? knownTool = PromptForKnownTool();
                    if (!string.IsNullOrWhiteSpace(knownTool))
                    {
                        RecoverToolsUsingKnownTool(files, knownTool.Trim());
                    }
                }

                if (_toolMappings.Count == 0)
                {
                    lblStatus.Text = "No tools detected after assisted retry.";
                    if (PromptToShareUnsupportedSample())
                    {
                        MessageBox.Show(
                            this,
                            "Thanks. Sharing a representative sample file can help improve support for this format in a future update.",
                            "Sample file requested",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    ShowErrorsIfAny();
                    return;
                }
            }

            lblStatus.Text = $"Found {_toolMappings.Count} unique tools.";
            ShowErrorsIfAny();
        }

        private IEnumerable<string> ExtractToolsFromFile(string file, string? knownToolHint = null)
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string? text = TryReadText(file);
            if (text == null) return Array.Empty<string>();

            var tools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (extension == ".cid")
            {
                foreach (Match match in ToolRegex.Matches(text))
                {
                    AddToolToken(tools, match.Groups[2].Value);
                }
            }
            else if (extension == ".bpp")
            {
                using var reader = new StringReader(text);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    int targetIndex = GetBppToolIndex(line);
                    if (targetIndex == -1)
                    {
                        continue;
                    }

                    var colonIdx = line.IndexOf(':');
                    if (colonIdx == -1)
                    {
                        continue;
                    }

                    var fields = SplitBppFields(line.Substring(colonIdx + 1));
                    if (fields.Count > targetIndex)
                    {
                        AddToolToken(tools, fields[targetIndex].Trim().Trim('"'));
                    }
                }
            }
            else if (IsGCodeExtension(extension))
            {
                foreach (Match match in TEqualsRegex.Matches(text))
                {
                    AddToolToken(tools, match.Groups[2].Value);
                }

                foreach (Match match in TCompactRegex.Matches(text))
                {
                    AddToolToken(tools, match.Groups[2].Value);
                }
            }

            // If strict parsing found nothing, fall back to a broad best-effort extractor.
            if (tools.Count == 0)
            {
                foreach (string token in ExtractToolsWithCommonSense(text, knownToolHint))
                {
                    AddToolToken(tools, token);
                }
            }

            return tools;
        }

        private void btnSaveMapping_Click(object? sender, EventArgs e)
        {
            if (_toolMappings.Count == 0)
            {
                MessageBox.Show(this, "Nothing to save. Scan tools first or load a mapping.", "Save Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "tool-mapping.json"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var map = new Dictionary<string, string>();
            foreach (var entry in _toolMappings)
            {
                var value = entry.NewName ?? string.Empty;
                map[entry.OldName] = value;
            }

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(map, options);
                File.WriteAllText(dialog.FileName, json, Utf8NoBom);
                lblStatus.Text = "Mapping saved.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to save mapping: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadMapping_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(dialog.FileName, Utf8NoBom);
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                MergeMapping(map);
                gridTools.Refresh();
                lblStatus.Text = "Mapping loaded and merged.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load mapping: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnApply_Click(object? sender, EventArgs e)
        {
            if (!ValidateRootFolder(out var rootFolder))
            {
                return;
            }

            var mappingOldToNew = BuildMappingDictionary();
            if (mappingOldToNew.Count == 0)
            {
                MessageBox.Show(this, "No mapping changes to apply (all tools map to themselves).", "Apply Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool gCodeEnabled = chkIsoFiles.Checked;
            string machineSafety = gCodeEnabled
                ? "\n\n" +
                  "Machine safety: G-code rewriting can change T, H, and D together. If H or D no longer matches " +
                  "your control's offset / wear tables, the program can use the wrong length offset or cutter comp. " +
                  "That can move Z incorrectly at rapids - broken tools, ruined spoil boards or fixtures, or machine damage. " +
                  "Use Dry Run and backups, verify in CAM or simulation, and prove out before cutting."
                : string.Empty;

            var confirm = MessageBox.Show(this,
                "This will process selected file types and apply tool replacements (unless Dry Run is checked).\n\n" +
                "G-code (when enabled): matching T, H, and D numbers are updated when they appear as keys in your mapping " +
                "(including T=/H=/D=, compact T/H/D, and fused G43H / G41D / G42D where applicable)." +
                machineSafety,
                "Confirm apply",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _errorLog.Clear();
            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = EnumerateTargetFiles(rootFolder, searchOption);
            if (files.Count == 0)
            {
                lblStatus.Text = "No matching files found.";
                MessageBox.Show(this, "No matching CID, BPP, or G-code files were found in the selected folder.", "Apply Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var fileList = new List<string>(files);

            progressBarFiles.Minimum = 0;
            progressBarFiles.Maximum = fileList.Count;
            progressBarFiles.Value = 0;

            int processed = 0;
            int modified = 0;
            bool dryRun = chkDryRun.Checked;
            bool createBackups = chkCreateBackups.Checked && !dryRun;

            foreach (var file in fileList)
            {
                processed++;
                lblStatus.Text = $"Processing {processed}/{fileList.Count}: {Path.GetFileName(file)}";
                progressBarFiles.Value = processed;
                Application.DoEvents();

                var originalText = TryReadText(file);
                if (originalText is null)
                {
                    continue;
                }

                if (createBackups)
                {
                    TryCreateBackup(file);
                }

                string newText = ProcessFileContent(file, originalText, mappingOldToNew);

                if (!string.Equals(originalText, newText, StringComparison.Ordinal))
                {
                    modified++;
                    if (!dryRun)
                    {
                        TryWriteText(file, newText);
                    }
                }
            }

            lblStatus.Text = $"Done. Modified {modified} of {fileList.Count} files. DryRun = {dryRun}.";
            ShowErrorsIfAny();
        }

        private string ProcessFileContent(string file, string content, Dictionary<string, string> mapping)
        {
            string extension = Path.GetExtension(file).ToLower();
            if (extension == ".cid")
            {
                return ToolRegex.Replace(content, match =>
                {
                    string prefix = match.Groups[1].Value;
                    string oldTool = match.Groups[2].Value;
                    if (mapping.TryGetValue(oldTool, out var mapped))
                    {
                        return prefix + mapped;
                    }
                    return match.Value;
                });
            }
            else if (extension == ".bpp")
            {
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = ProcessBppLine(lines[i], mapping);
                }
                // Determine original line ending by checking content
                string lineEnding = content.Contains("\r\n") ? "\r\n" : (content.Contains("\n") ? "\n" : Environment.NewLine);
                return string.Join(lineEnding, lines);
            }
            else if (IsGCodeExtension(extension))
            {
                string updated = TEqualsRegex.Replace(content, match => ReplaceGCodeToolMatch(match, mapping));
                updated = TCompactRegex.Replace(updated, match => ReplaceGCodeToolMatch(match, mapping));
                // Fused forms before compact H/D so we do not depend on a word boundary between G-code and letter.
                updated = G43HNoSpaceRegex.Replace(updated, match => ReplaceGCodeFusedLetterMatch(match, mapping));
                updated = G41DNoSpaceRegex.Replace(updated, match => ReplaceGCodeFusedLetterMatch(match, mapping));
                updated = G42DNoSpaceRegex.Replace(updated, match => ReplaceGCodeFusedLetterMatch(match, mapping));
                updated = HEqualsRegex.Replace(updated, match => ReplaceGCodeToolMatch(match, mapping));
                updated = DEqualsRegex.Replace(updated, match => ReplaceGCodeToolMatch(match, mapping));
                // Same mapping rule as T: only rewrite H/D when digits are an explicit key (typical T/H/D-aligned posts).
                updated = HCompactRegex.Replace(updated, match => ReplaceGCodeToolMatch(match, mapping));
                updated = DCompactRegex.Replace(updated, match => ReplaceGCodeToolMatch(match, mapping));
                return updated;
            }
            return content;
        }

        private static string ReplaceGCodeToolMatch(Match match, Dictionary<string, string> mapping)
        {
            string prefix = match.Groups[1].Value;
            string oldTool = match.Groups[2].Value;

            if (mapping.TryGetValue(oldTool, out var mapped))
            {
                return prefix + PreserveNumericPadding(oldTool, mapped);
            }

            // Allow imported mappings that use keys like "T12" for G-code.
            if (mapping.TryGetValue(prefix + oldTool, out mapped))
            {
                return prefix + PreserveNumericPadding(oldTool, mapped);
            }

            return match.Value;
        }

        /// <summary>Handles G43H12 / G41D3 style tokens: group 1 = G-code, 2 = H or D, 3 = digits.</summary>
        private static string ReplaceGCodeFusedLetterMatch(Match match, Dictionary<string, string> mapping)
        {
            string gWord = match.Groups[1].Value;
            string letter = match.Groups[2].Value;
            string oldDigits = match.Groups[3].Value;

            if (mapping.TryGetValue(oldDigits, out var mapped))
            {
                return gWord + letter + PreserveNumericPadding(oldDigits, mapped);
            }

            if (mapping.TryGetValue(letter + oldDigits, out mapped))
            {
                return gWord + letter + PreserveNumericPadding(oldDigits, mapped);
            }

            return match.Value;
        }

        private static string PreserveNumericPadding(string oldTool, string mapped)
        {
            if (int.TryParse(oldTool, out _) && int.TryParse(mapped, out _))
            {
                int oldLen = oldTool.Length;
                // If mapped is longer, PadLeft keeps full value (no truncation).
                return mapped.PadLeft(oldLen, '0');
            }

            return mapped;
        }

        private static void AddToolToken(HashSet<string> tools, string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            string trimmed = token.Trim().Trim('"', '\'');
            if (trimmed.Length == 0)
            {
                return;
            }

            tools.Add(trimmed);
        }

        private static IEnumerable<string> ExtractToolsWithCommonSense(string text, string? knownToolHint)
        {
            var tools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in HeuristicToolNamedRegex.Matches(text))
            {
                AddToolToken(tools, match.Groups[1].Value);
            }

            foreach (Match match in HeuristicToolNumberRegex.Matches(text))
            {
                AddToolToken(tools, match.Groups[1].Value);
            }

            if (!string.IsNullOrWhiteSpace(knownToolHint))
            {
                string rawHint = knownToolHint.Trim();
                string normalizedHint = rawHint.Trim().Trim('"', '\'');
                if (normalizedHint.Length > 0)
                {
                    AddToolToken(tools, normalizedHint);

                    var exactBoundary = new Regex($@"(?<![A-Za-z0-9_./\-]){Regex.Escape(normalizedHint)}(?![A-Za-z0-9_./\-])", RegexOptions.IgnoreCase);
                    if (exactBoundary.IsMatch(text))
                    {
                        AddToolToken(tools, normalizedHint);
                    }
                }
            }

            return tools;
        }

        private string? PromptForKnownTool()
        {
            using var dialog = new Form();
            dialog.Text = "Assist parser";
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowInTaskbar = false;
            dialog.ClientSize = new System.Drawing.Size(420, 130);

            var lbl = new Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(12, 12),
                Text = "Enter one known tool name/number (example: 12, T12, T05):"
            };

            var txt = new TextBox
            {
                Location = new System.Drawing.Point(12, 38),
                Width = 390
            };

            var btnOk = new Button
            {
                Text = "Retry",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(246, 82),
                Width = 75
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(327, 82),
                Width = 75
            };

            dialog.Controls.Add(lbl);
            dialog.Controls.Add(txt);
            dialog.Controls.Add(btnOk);
            dialog.Controls.Add(btnCancel);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            return dialog.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }

        private void RecoverToolsUsingKnownTool(List<string> files, string knownTool)
        {
            foreach (var file in files)
            {
                foreach (var toolName in ExtractToolsFromFile(file, knownTool))
                {
                    if (_mappingByOldName.ContainsKey(toolName))
                    {
                        continue;
                    }

                    var entry = new ToolMappingEntry
                    {
                        OldName = toolName,
                        NewName = toolName
                    };
                    _mappingByOldName[toolName] = entry;
                    _toolMappings.Add(entry);
                }
            }
        }

        private bool PromptToShareUnsupportedSample()
        {
            using var dialog = new Form();
            dialog.Text = "Help improve format support";
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowInTaskbar = false;
            dialog.ClientSize = new System.Drawing.Size(460, 160);

            var lbl = new Label
            {
                AutoSize = false,
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(436, 52),
                Text = "Parsing still could not identify tools.\r\nIf you share a representative sample file, support for this format can improve in a future update."
            };

            var chkShare = new CheckBox
            {
                AutoSize = true,
                Location = new System.Drawing.Point(15, 74),
                Text = "I can share a sample file to improve future support."
            };

            var btnOk = new Button
            {
                Text = "Continue",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(373, 120),
                Width = 75
            };

            dialog.Controls.Add(lbl);
            dialog.Controls.Add(chkShare);
            dialog.Controls.Add(btnOk);
            dialog.AcceptButton = btnOk;

            return dialog.ShowDialog(this) == DialogResult.OK && chkShare.Checked;
        }

        private string ProcessBppLine(string line, Dictionary<string, string> mapping)
        {
            int colonIdx = line.IndexOf(':');
            if (colonIdx == -1) return line;

            int targetIndex = GetBppToolIndex(line);
            if (targetIndex == -1) return line;

            string prefix = line.Substring(0, colonIdx + 1);
            string payload = line.Substring(colonIdx + 1);

            var fields = SplitBppFields(payload);
            if (fields.Count <= targetIndex) return line;

            string originalField = fields[targetIndex];
            string trimmed = originalField.Trim();
            string toolValue = trimmed.Trim('"');
            if (string.IsNullOrEmpty(toolValue) || !mapping.TryGetValue(toolValue, out string? newValue))
            {
                return line;
            }

            string leadingSpace = originalField.Substring(0, originalField.Length - originalField.TrimStart().Length);
            string trailingSpace = originalField.Substring(originalField.TrimEnd().Length);
            bool quoted = trimmed.Length >= 2 && trimmed.StartsWith('"') && trimmed.EndsWith('"');

            fields[targetIndex] = quoted
                ? $"{leadingSpace}\"{newValue}\"{trailingSpace}"
                : $"{leadingSpace}{newValue}{trailingSpace}";

            return prefix + string.Join(",", fields);
        }

        private int GetBppToolIndex(string line)
        {
            int colonIdx = line.IndexOf(':');
            if (colonIdx == -1) return -1;
            
            string prefix = line.Substring(0, colonIdx);
            // Must contain BG or ROUTG and not be a comment unless it's a ROUTG comment as per CDI
            bool isComment = prefix.TrimStart().StartsWith("'");
            string normalizedPrefix = prefix.ToUpperInvariant();

            if (normalizedPrefix.Contains("BG") && !isComment) return 35;
            if (normalizedPrefix.Contains("ROUTG")) return 49; // ROUTG can be commented as per CDI

            return -1;
        }

        private List<string> SplitBppFields(string payload)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < payload.Length; i++)
            {
                char c = payload[i];
                if (c == '\"') inQuotes = !inQuotes;
                if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            fields.Add(currentField.ToString());
            return fields;
        }

        private static bool IsGCodeExtension(string extension)
        {
            return extension == ".iso" ||
                   extension == ".tap" ||
                   extension == ".nc" ||
                   extension == ".cnc" ||
                   extension == ".mpf" ||
                   extension == ".spf" ||
                   extension == ".ngc" ||
                   extension == ".gcode";
        }

        private bool ValidateRootFolder(out string rootFolder)
        {
            rootFolder = txtRootFolder.Text.Trim();
            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                MessageBox.Show(this, "Please select a valid root folder.", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private List<string> EnumerateTargetFiles(string rootFolder, SearchOption searchOption)
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (chkCidFiles.Checked)
                {
                    AddFiles(files, rootFolder, "*.cid", searchOption);
                }
                if (chkBppFiles.Checked)
                {
                    AddFiles(files, rootFolder, "*.bpp", searchOption);
                }
                if (chkIsoFiles.Checked)
                {
                    AddFiles(files, rootFolder, "*.iso", searchOption);
                    AddFiles(files, rootFolder, "*.tap", searchOption);
                    AddFiles(files, rootFolder, "*.nc", searchOption);
                    AddFiles(files, rootFolder, "*.cnc", searchOption);
                    AddFiles(files, rootFolder, "*.mpf", searchOption);
                    AddFiles(files, rootFolder, "*.spf", searchOption);
                    AddFiles(files, rootFolder, "*.ngc", searchOption);
                    AddFiles(files, rootFolder, "*.gcode", searchOption);
                }
                return files.ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void AddFiles(HashSet<string> files, string rootFolder, string searchPattern, SearchOption searchOption)
        {
            foreach (var file in Directory.GetFiles(rootFolder, searchPattern, searchOption))
            {
                files.Add(file);
            }
        }

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private string? TryReadText(string file)
        {
            try
            {
                return File.ReadAllText(file, Utf8NoBom);
            }
            catch (Exception ex)
            {
                _errorLog.Add($"{file}: {ex.Message}");
                return null;
            }
        }

        private void TryWriteText(string file, string content)
        {
            try
            {
                File.WriteAllText(file, content, Utf8NoBom);
            }
            catch (Exception ex)
            {
                _errorLog.Add($"{file}: {ex.Message}");
            }
        }

        private void TryCreateBackup(string file)
        {
            try
            {
                var backupPath = file + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(file, backupPath, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                _errorLog.Add($"{file}: {ex.Message}");
            }
        }

        private void MergeMapping(Dictionary<string, string> map)
        {
            foreach (var kvp in map)
            {
                var oldName = kvp.Key ?? string.Empty;
                var newName = kvp.Value ?? string.Empty;
                if (_mappingByOldName.TryGetValue(oldName, out var existing))
                {
                    existing.NewName = newName;
                }
                else
                {
                    var entry = new ToolMappingEntry
                    {
                        OldName = oldName,
                        NewName = newName
                    };
                    _mappingByOldName[oldName] = entry;
                    _toolMappings.Add(entry);
                }
            }
        }

        private Dictionary<string, string> BuildMappingDictionary()
        {
            var map = new Dictionary<string, string>();
            foreach (var entry in _toolMappings)
            {
                var oldName = (entry.OldName ?? string.Empty).Trim();
                var newName = (entry.NewName ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(oldName) &&
                    !string.IsNullOrEmpty(newName) &&
                    !string.Equals(oldName, newName, StringComparison.Ordinal))
                {
                    map[oldName] = newName;
                }
            }
            return map;
        }

        private void ShowErrorsIfAny()
        {
            if (_errorLog.Count == 0)
            {
                return;
            }

            var message = string.Join(Environment.NewLine, _errorLog);
            MessageBox.Show(this, message, "Completed with errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
