using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CidToolRenamer
{
    public partial class MainForm : Form
    {
        private readonly BindingList<ToolMappingEntry> _toolMappings = new();
        private readonly Dictionary<string, ToolMappingEntry> _mappingByOldName = new();
        private readonly List<string> _errorLog = new();

        // Match TOOLNAME=<value> case-insensitively; group 1 preserves prefix spacing/casing, group 2 is the tool value.
        private static readonly Regex ToolRegex = new(@"(ToolName\s*=\s*)([^\s;,\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MainForm()
        {
            InitializeComponent();
            gridTools.AutoGenerateColumns = false;
            gridTools.DataSource = _toolMappings;
            lblStatus.Text = "Ready";
        }

        private void btnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select root folder containing CID files"
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
            var files = EnumerateCidFiles(rootFolder, searchOption);
            lblStatus.Text = "Scanning tools...";

            foreach (var file in files)
            {
                string? text = TryReadText(file);
                if (text is null)
                {
                    continue;
                }

                foreach (Match match in ToolRegex.Matches(text))
                {
                    var toolName = match.Groups[2].Value.Trim();
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

            lblStatus.Text = $"Found {_toolMappings.Count} unique tools.";
            ShowErrorsIfAny();
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
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
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
                var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
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

            var confirm = MessageBox.Show(this,
                "This will process CID files and change Tool= entries (unless Dry Run is checked). Continue?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _errorLog.Clear();
            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = EnumerateCidFiles(rootFolder, searchOption);
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

                string newText = ToolRegex.Replace(originalText, match =>
                {
                    string prefix = match.Groups[1].Value;
                    string oldTool = match.Groups[2].Value;
                    if (mappingOldToNew.TryGetValue(oldTool, out var mapped))
                    {
                        return prefix + mapped;
                    }
                    return match.Value;
                });

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

        private static IEnumerable<string> EnumerateCidFiles(string rootFolder, SearchOption searchOption)
        {
            try
            {
                return Directory.EnumerateFiles(rootFolder, "*.cid", searchOption);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private string? TryReadText(string file)
        {
            try
            {
                return File.ReadAllText(file, Encoding.UTF8);
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
                File.WriteAllText(file, content, Encoding.UTF8);
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
