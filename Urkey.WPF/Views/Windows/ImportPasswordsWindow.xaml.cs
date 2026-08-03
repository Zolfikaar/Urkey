using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Urkey.Core.Services;
using Urkey.Core.Services.Import;
using Urkey.WPF.Helpers;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Windows
{
    public partial class ImportPasswordsWindow : Window
    {
        private enum Step
        {
            Parsing,
            Mapping,
            Preview,
            Importing,
            Summary
        }

        private readonly string _filePath;
        private ParsedImportTable? _table;
        private List<ImportCandidate>? _candidates;
        private readonly ObservableCollection<ImportPreviewItem> _previewItems = new();
        private Step _step = Step.Parsing;
        private PasswordImportSummary? _summary;
        private bool _cleared;

        public ImportPasswordsWindow(string filePath)
        {
            _filePath = filePath;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            LocalizeDuplicateCombo();
            await ParseAsync();
        }

        private void LocalizeDuplicateCombo()
        {
            DupSkipItem.Content = Loc.Get("Import_Dup_Skip");
            DupImportItem.Content = Loc.Get("Import_Dup_ImportAnyway");
            DupOverwriteItem.Content = Loc.Get("Import_Dup_Overwrite");
            GlobalDuplicateCombo.SelectedIndex = 0;
        }

        private async Task ParseAsync()
        {
            ShowStep(Step.Parsing);
            IdleLockService.NotifyActivity();

            try
            {
                var result = await Task.Run(() => PasswordImportService.ReadImportFile(_filePath));
                IdleLockService.NotifyActivity();

                if (result.IsStructured && result.StructuredCandidates != null)
                {
                    ShowPreviewFromCandidates(result.StructuredCandidates);
                    return;
                }

                var table = result.Table
                    ?? throw new InvalidDataException("Import_Error_UnrecognizedFormat");
                _table = table;

                if (!table.AutoMapping.IsConfident || !table.AutoMapping.HasPassword)
                {
                    PopulateMappingCombos(table);
                    ShowStep(Step.Mapping);
                }
                else
                {
                    BuildPreview(table, table.AutoMapping);
                }
            }
            catch (FileNotFoundException)
            {
                ShowFatalError(Loc.Get("Import_Error_FileNotFound"));
            }
            catch (InvalidDataException ex)
            {
                // Message is a resource key when thrown by our parser.
                string key = ex.Message;
                string msg = key.StartsWith("Import_", StringComparison.Ordinal)
                    ? Loc.Get(key)
                    : Loc.Get("Import_Error_UnrecognizedFormat");
                ShowFatalError(msg);
            }
            catch (DecoderFallbackException)
            {
                ShowFatalError(Loc.Get("Import_Error_Encoding"));
            }
            catch (IOException)
            {
                ShowFatalError(Loc.Get("Import_Error_ReadFailed"));
            }
            catch (UnauthorizedAccessException)
            {
                ShowFatalError(Loc.Get("Import_Error_ReadFailed"));
            }
            catch (Exception)
            {
                // Never log or surface file contents.
                ShowFatalError(Loc.Get("Import_Error_UnrecognizedFormat"));
            }
        }

        private void PopulateMappingCombos(ParsedImportTable table)
        {
            string none = Loc.Get("Import_Mapping_None");
            var options = new List<ImportColumnOption> { ImportColumnOption.None(none) };
            for (int i = 0; i < table.Headers.Count; i++)
            {
                options.Add(new ImportColumnOption
                {
                    Index = i,
                    Display = $"{i + 1}: {table.Headers[i]}"
                });
            }

            void Bind(ComboBox combo, int selectedIndex)
            {
                combo.ItemsSource = options;
                combo.SelectedItem = options.FirstOrDefault(o => o.Index == selectedIndex) ?? options[0];
            }

            var m = table.AutoMapping;
            Bind(MapNameCombo, m.NameIndex);
            Bind(MapUrlCombo, m.UrlIndex);
            Bind(MapUsernameCombo, m.UsernameIndex);
            Bind(MapEmailCombo, m.EmailIndex);
            Bind(MapPasswordCombo, m.PasswordIndex);
            Bind(MapNotesCombo, m.NotesIndex);

            HeaderSubtitle.Text = Loc.Get("Import_Mapping_Description");
            PrimaryButton.Content = Loc.Get("Import_Continue");
            PrimaryButton.IsEnabled = true;
        }

        private ImportColumnMapping ReadMappingFromUi()
        {
            static int Idx(ComboBox combo)
                => (combo.SelectedItem as ImportColumnOption)?.Index ?? -1;

            var mapping = new ImportColumnMapping
            {
                NameIndex = Idx(MapNameCombo),
                UrlIndex = Idx(MapUrlCombo),
                UsernameIndex = Idx(MapUsernameCombo),
                EmailIndex = Idx(MapEmailCombo),
                PasswordIndex = Idx(MapPasswordCombo),
                NotesIndex = Idx(MapNotesCombo)
            };

            bool hasIdentity = mapping.NameIndex >= 0
                               || mapping.UrlIndex >= 0
                               || mapping.UsernameIndex >= 0
                               || mapping.EmailIndex >= 0;
            mapping.IsConfident = mapping.HasPassword && hasIdentity;
            return mapping;
        }

        private void BuildPreview(ParsedImportTable table, ImportColumnMapping mapping)
        {
            if (!mapping.HasPassword)
            {
                ToastService.Warning(Loc.Get("Import_Error_PasswordColumnRequired"));
                if (_step != Step.Mapping)
                {
                    PopulateMappingCombos(table);
                    ShowStep(Step.Mapping);
                }
                return;
            }

            PasswordImportService.ClearCandidates(_candidates);
            _candidates = PasswordImportService.BuildCandidates(table, mapping);
            // Raw CSV cells are no longer needed — drop plaintext passwords from the table buffer.
            table.ClearRawRows();
            ShowPreviewFromCandidates(_candidates);
        }

        private void ShowPreviewFromCandidates(List<ImportCandidate> candidates)
        {
            if (!ReferenceEquals(_candidates, candidates))
            {
                PasswordImportService.ClearCandidates(_candidates);
                _candidates = candidates;
            }

            var existing = App.VaultService.GetEntries();
            _previewItems.Clear();

            int duplicates = 0;

            foreach (var candidate in _candidates)
            {
                if (candidate.HasError)
                    continue;

                var item = new ImportPreviewItem { Candidate = candidate };
                var dupId = PasswordImportService.FindDuplicateId(candidate, existing);
                if (dupId.HasValue)
                {
                    item.IsDuplicate = true;
                    item.ExistingEntryId = dupId;
                    item.DuplicateResolution = DuplicateResolution.Skip;
                    duplicates++;
                }

                _previewItems.Add(item);
            }

            if (_previewItems.Count == 0)
            {
                int failed = _candidates.Count(c => c.HasError);
                ShowFatalError(Loc.Format("Import_Error_NoValidEntries", failed));
                return;
            }

            PreviewGrid.ItemsSource = _previewItems;
            PreviewCountText.Text = Loc.Format("Import_Preview_Count", _previewItems.Count, duplicates);
            HeaderSubtitle.Text = Loc.Get("Import_Preview_Subtitle");
            PrimaryButton.Content = Loc.Get("Import_Confirm");
            PrimaryButton.IsEnabled = true;
            ShowStep(Step.Preview);
        }

        private async void OnPrimaryClick(object sender, RoutedEventArgs e)
        {
            IdleLockService.NotifyActivity();

            if (_step == Step.Mapping)
            {
                if (_table == null) return;
                var mapping = ReadMappingFromUi();
                if (!mapping.HasPassword)
                {
                    ToastService.Warning(Loc.Get("Import_Error_PasswordColumnRequired"));
                    return;
                }

                BuildPreview(_table, mapping);
                return;
            }

            if (_step == Step.Preview)
            {
                await ImportAsync();
                return;
            }

            if (_step == Step.Summary)
            {
                FinishAndClose();
            }
        }

        private async Task ImportAsync()
        {
            var selected = _previewItems.Where(i => i.IsSelected && !i.HasError).ToList();
            if (selected.Count == 0)
            {
                ToastService.Success(Loc.Get("Import_Error_NothingSelected"));
                return;
            }

            ShowStep(Step.Importing);
            ProgressText.Text = Loc.Get("Import_Importing");
            IdleLockService.NotifyActivity();

            var commitItems = selected.Select(i => new PasswordImportCommitItem
            {
                Candidate = i.Candidate,
                Resolution = i.IsDuplicate ? i.DuplicateResolution : DuplicateResolution.ImportAnyway,
                ExistingEntryId = i.ExistingEntryId
            }).ToList();

            // Also count unselected-but-parsed error rows for the summary.
            int preFailed = _candidates?.Count(c => c.HasError) ?? 0;

            try
            {
                var summary = await Task.Run(() =>
                {
                    IdleLockService.NotifyActivity();
                    return App.VaultService.ImportAccounts(commitItems);
                });

                summary.Failed += preFailed;
                if (_candidates != null)
                {
                    foreach (var c in _candidates.Where(c => c.HasError))
                        summary.Failures.Add((c.SourceRowNumber, c.ErrorResourceKey!));
                }

                // Count skipped among selected duplicates that chose Skip — already in summary.
                // Unselected rows are neither imported nor "skipped as duplicates".
                _summary = summary;
                ShowSummary(summary);
            }
            catch (Exception)
            {
                ShowFatalError(Loc.Get("Import_Error_SaveFailed"));
            }
            finally
            {
                ClearSensitiveMemory();
            }
        }

        private void ShowSummary(PasswordImportSummary summary)
        {
            SummaryImported.Text = Loc.Format("Import_Summary_Imported", summary.Imported);
            SummarySkipped.Text = Loc.Format("Import_Summary_Skipped", summary.SkippedDuplicates);
            SummaryOverwritten.Text = Loc.Format("Import_Summary_Overwritten", summary.Overwritten);
            SummaryFailed.Text = Loc.Format("Import_Summary_Failed", summary.Failed);

            if (summary.Failures.Count > 0)
            {
                var lines = summary.Failures
                    .Take(8)
                    .Select(f => Loc.Format("Import_Summary_FailureLine", f.RowNumber, Loc.Get(f.ReasonResourceKey)));
                SummaryFailuresDetail.Text = string.Join(Environment.NewLine, lines);
                if (summary.Failures.Count > 8)
                    SummaryFailuresDetail.Text += Environment.NewLine + Loc.Format("Import_Summary_MoreFailures", summary.Failures.Count - 8);
                SummaryFailuresDetail.Visibility = Visibility.Visible;
            }
            else
            {
                SummaryFailuresDetail.Visibility = Visibility.Collapsed;
            }

            HeaderSubtitle.Text = Loc.Get("Import_Summary_Subtitle");
            PrimaryButton.Content = Loc.Get("Import_Done");
            PrimaryButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Collapsed;
            ShowStep(Step.Summary);
        }

        private void FinishAndClose()
        {
            if (SecureDeleteCheck.IsChecked == true)
            {
                bool deleted = FileHelper.TrySecureDelete(_filePath);
                if (!deleted && File.Exists(_filePath))
                {
                    ToastService.Warning(Loc.Get("Import_SecureDelete_Failed"));
                }
            }

            DialogResult = true;
            Close();
        }

        private void OnSelectAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in _previewItems)
                item.IsSelected = true;
        }

        private void OnDeselectAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in _previewItems)
                item.IsSelected = false;
        }

        private void OnGlobalDuplicateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GlobalDuplicateCombo?.SelectedItem is not ComboBoxItem { Tag: string tag })
                return;

            var resolution = tag switch
            {
                "ImportAnyway" => DuplicateResolution.ImportAnyway,
                "Overwrite" => DuplicateResolution.Overwrite,
                _ => DuplicateResolution.Skip
            };

            foreach (var item in _previewItems.Where(i => i.IsDuplicate))
                item.DuplicateResolution = resolution;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            ClearSensitiveMemory();
            DialogResult = false;
            Close();
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            ClearSensitiveMemory();
        }

        private void ClearSensitiveMemory()
        {
            if (_cleared) return;
            _cleared = true;

            PasswordImportService.ClearCandidates(_candidates);
            _candidates = null;

            foreach (var item in _previewItems)
                item.Candidate.ClearSensitiveData();
            _previewItems.Clear();
            PreviewGrid.ItemsSource = null;

            _table = null;
        }

        private void ShowStep(Step step)
        {
            _step = step;
            ProgressPanel.Visibility = step is Step.Parsing or Step.Importing ? Visibility.Visible : Visibility.Collapsed;
            MappingPanel.Visibility = step == Step.Mapping ? Visibility.Visible : Visibility.Collapsed;
            PreviewPanel.Visibility = step == Step.Preview ? Visibility.Visible : Visibility.Collapsed;
            SummaryPanel.Visibility = step == Step.Summary ? Visibility.Visible : Visibility.Collapsed;

            PrimaryButton.IsEnabled = step is Step.Mapping or Step.Preview or Step.Summary;
            CancelButton.IsEnabled = step is not Step.Importing;

            HeaderSubtitle.Text = step switch
            {
                Step.Parsing => Loc.Get("Import_Parsing"),
                Step.Importing => Loc.Get("Import_Importing"),
                Step.Mapping => Loc.Get("Import_Mapping_Description"),
                Step.Preview => Loc.Get("Import_Preview_Subtitle"),
                Step.Summary => Loc.Get("Import_Summary_Subtitle"),
                _ => HeaderSubtitle.Text
            };
        }

        private void ShowFatalError(string message)
        {
            ToastService.Error(message);

            ClearSensitiveMemory();
            DialogResult = false;
            Close();
        }
    }
}
