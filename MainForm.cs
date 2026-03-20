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

        // Match TOOLNAME=<value> or Tool=<value> case-insensitively; group 1 preserves prefix spacing/casing, group 2 is the tool value.
        private static readonly Regex ToolRegex = new(@"(Tool(?:Name)?\s*=\s*)([^\s;,\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Match T=<number> specifically for ISO files.
        private static readonly Regex TRegex = new(@"\b(T\s*=\s*)(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                Description = "Select root folder containing CID or BPP files"
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

            lblStatus.Text = $"Found {_toolMappings.Count} unique tools.";
            ShowErrorsIfAny();
        }

        private IEnumerable<string> ExtractToolsFromFile(string file)
        {
            string extension = Path.GetExtension(file).ToLower();
            string? text = TryReadText(file);
            if (text == null) yield break;

            if (extension == ".cid")
            {
                foreach (Match match in ToolRegex.Matches(text))
                {
                    yield return match.Groups[2].Value.Trim();
                }
            }
            else if (extension == ".bpp")
            {
                using var reader = new StringReader(text);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    int targetIndex = GetBppToolIndex(line);
                    if (targetIndex != -1)
                    {
                        var colonIdx = line.IndexOf(':');
                        if (colonIdx != -1)
                        {
                            var fields = SplitBppFields(line.Substring(colonIdx + 1));
                            if (fields.Count > targetIndex)
                            {
                                yield return fields[targetIndex].Trim().Trim('"');
                            }
                        }
                    }
                }
            }
            else if (extension == ".iso")
            {
                foreach (Match match in TRegex.Matches(text))
                {
                    yield return match.Groups[2].Value;
                }
            }
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
                "This will process CID, BPP, and ISO files and apply tool replacements (unless Dry Run is checked). Continue?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _errorLog.Clear();
            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = EnumerateTargetFiles(rootFolder, searchOption);
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
            else if (extension == ".iso")
            {
                return TRegex.Replace(content, match =>
                {
                    string prefix = match.Groups[1].Value;
                    string oldTool = match.Groups[2].Value;
                    if (mapping.TryGetValue(oldTool, out var mapped))
                    {
                        // Handle formatting: preserve padding length if target tool number is numeric
                        if (int.TryParse(oldTool, out _) && int.TryParse(mapped, out _))
                        {
                            int oldLen = oldTool.Length;
                            string padded = mapped.PadLeft(oldLen, '0');
                            // If the new number is longer than the old one, PadLeft doesn't truncate, which is correct (1 -> 10 becomes 10, not 0)
                            return prefix + padded;
                        }
                        return prefix + mapped;
                    }
                    return match.Value;
                });
            }
            return content;
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

            string toolValue = fields[targetIndex].Trim().Trim('"');
            if (mapping.TryGetValue(toolValue, out string? newValue))
            {
                fields[targetIndex] = $" \"{newValue}\""; // Preserve space before quote if it was there, or just add one for readability
                // But let's be more precise and preserve the exact spacing if possible
                string originalField = fields[targetIndex];
                string leadingSpace = originalField.Substring(0, originalField.Length - originalField.TrimStart().Length);
                fields[targetIndex] = $"{leadingSpace}\"{newValue}\"";
                
                return prefix + string.Join(",", fields);
            }

            return line;
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

        private IEnumerable<string> EnumerateTargetFiles(string rootFolder, SearchOption searchOption)
        {
            try
            {
                IEnumerable<string> files = Array.Empty<string>();
                if (chkCidFiles.Checked)
                {
                    files = files.Concat(Directory.EnumerateFiles(rootFolder, "*.cid", searchOption));
                }
                if (chkBppFiles.Checked)
                {
                    files = files.Concat(Directory.EnumerateFiles(rootFolder, "*.bpp", searchOption));
                }
                if (chkIsoFiles.Checked)
                {
                    files = files.Concat(Directory.EnumerateFiles(rootFolder, "*.iso", searchOption));
                }
                return files;
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
