﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace WimyGit
{
  partial class ViewModel
  {
    public class FileStatus
    {
      public string Status { get; set; }
      public string FilePath { get; set; }
      public string Display { get; set; }
      public bool IsSelected { get; set; }
    }

    public System.Collections.ObjectModel.ObservableCollection<FileStatus> ModifiedList { get; set; }
    public System.Collections.ObjectModel.ObservableCollection<FileStatus> StagedList { get; set; }

    private void InitializePending()
    {
      StageSelected = new DelegateCommand(OnStageSelected);
      ModifiedDiffCommand = new DelegateCommand(OnModifiedDiffCommand);
      StagedDiffCommand = new DelegateCommand(OnStagedDiffCommand);
      CommitCommand = new DelegateCommand(OnCommitCommand);
      RevertCommand = new DelegateCommand(OnRevertCommand);

      ModifiedList = new System.Collections.ObjectModel.ObservableCollection<FileStatus>();
      StagedList = new System.Collections.ObjectModel.ObservableCollection<FileStatus>();
    }

    void RefreshPending()
    {
      var filelist = git_.GetModifiedFileList();
      var modified_backup = new SelectionRecover(ModifiedList);
      var staged_backup = new SelectionRecover(StagedList);
      this.ModifiedList.Clear();
      this.StagedList.Clear();
      foreach (var filestatus in filelist)
      {
        switch (filestatus.State)
        {
          case LibGit2Sharp.FileStatus.Ignored:
            continue;

          case LibGit2Sharp.FileStatus.Added:
            goto case LibGit2Sharp.FileStatus.Staged;
          case LibGit2Sharp.FileStatus.Staged:
            AddStagedList(filestatus, staged_backup);
            break;

          case LibGit2Sharp.FileStatus.Untracked:
            goto case LibGit2Sharp.FileStatus.Modified;
          case LibGit2Sharp.FileStatus.Modified:
            AddModifiedList(filestatus, modified_backup);
            break;

          case LibGit2Sharp.FileStatus.Staged | LibGit2Sharp.FileStatus.Modified:
            AddModifiedList(filestatus, modified_backup);
            AddStagedList(filestatus, staged_backup);
            break;

          // renamed
          case LibGit2Sharp.FileStatus.Staged | LibGit2Sharp.FileStatus.RenamedInIndex:
            AddStagedList(filestatus, staged_backup);
            break;

          case LibGit2Sharp.FileStatus.RenamedInIndex:
            AddStagedList(filestatus, staged_backup);
            break;

          case LibGit2Sharp.FileStatus.RenamedInIndex | LibGit2Sharp.FileStatus.Modified:
            AddStagedList(filestatus, staged_backup);
            AddModifiedList(filestatus, modified_backup);
            break;

          case LibGit2Sharp.FileStatus.Staged | LibGit2Sharp.FileStatus.RenamedInIndex | LibGit2Sharp.FileStatus.Modified:
            AddModifiedList(filestatus, modified_backup);
            AddStagedList(filestatus, staged_backup);
            break;

          case LibGit2Sharp.FileStatus.Missing:
            AddModifiedList(filestatus, modified_backup);
            break;

          case LibGit2Sharp.FileStatus.Removed:
            AddStagedList(filestatus, staged_backup);
            break;

          default:
            System.Diagnostics.Debug.Assert(false);
            AddLog("Cannot execute for filestatus:" + filestatus.State.ToString());
            break;
        }
        AddLog(String.Format("[{0}] {1}", filestatus.State.ToString(), filestatus.FilePath));
      }

      if (ModifiedList.Count == 0 && StagedList.Count == 0)
      {
        AddLog("Nothing changed");
      }
    }

    public ICommand CommitCommand { get; private set; }
    public void OnCommitCommand(object parameter)
    {
      if (String.IsNullOrEmpty(CommitMessage))
      {
        AddLog("Empty commit message. Please fill commit message");
        return;
      }
      if (StagedList.Count == 0)
      {
        AddLog("No staged file");
        return;
      }
      git_.Commit(CommitMessage);
      CommitMessage = "";
      Refresh();
    }

    public void OnModifiedDiffCommand(object parameter)
    {
      foreach (var filepath in SelectedModifiedFilePathList)
      {
        git_.Diff(filepath);
      }
    }

    public void OnStagedDiffCommand(object parameter)
    {
      foreach (var filepath in SelectedStagedFilePathList)
      {
        git_.DiffStaged(filepath);
      }
    }

    void AddModifiedList(LibGit2Sharp.StatusEntry filestatus, SelectionRecover backup_selection)
    {
      FileStatus status = new FileStatus();
      status.Status = filestatus.State.ToString();
      status.FilePath = filestatus.FilePath;
      status.Display = status.FilePath;
      status.IsSelected = backup_selection.WasSelected(filestatus.FilePath);

      ModifiedList.Add(status);
      PropertyChanged(this, new PropertyChangedEventArgs("ModifiedList"));
    }

    void AddStagedList(LibGit2Sharp.StatusEntry filestatus, SelectionRecover backup_selection)
    {
      FileStatus status = new FileStatus();
      status.Status = filestatus.State.ToString();
      status.FilePath = filestatus.FilePath;
      status.Display = status.FilePath;
      if ((filestatus.State == LibGit2Sharp.FileStatus.RenamedInIndex) |
          (filestatus.State == (LibGit2Sharp.FileStatus.RenamedInIndex | LibGit2Sharp.FileStatus.Staged)))
      {
        status.Display = string.Format(" {0} -> {1} [{2}%]",
            filestatus.HeadToIndexRenameDetails.OldFilePath,
            filestatus.HeadToIndexRenameDetails.NewFilePath,
            filestatus.HeadToIndexRenameDetails.Similarity);
      }
      status.IsSelected = backup_selection.WasSelected(filestatus.FilePath);

      StagedList.Add(status);
      PropertyChanged(this, new PropertyChangedEventArgs("StagedList"));
    }

    private string commit_message_;
    public string CommitMessage
    {
      get
      {
        return commit_message_;
      }
      set
      {
        commit_message_ = value;
        NotifyPropertyChanged("CommitMessage");
      }
    }

    public ICommand ModifiedDiffCommand { get; private set; }
    public ICommand StagedDiffCommand { get; private set; }
    public ICommand RevertCommand { get; private set; }
    public void OnRevertCommand(object parameter)
    {
      foreach (var item in SelectedModifiedFilePathList)
      {
        git_.P4Revert(item);
      }
      Refresh();
    }

    void OnStageSelected(object parameter)
    {
      if (SelectedModifiedFilePathList.Count() == 0)
      {
        AddLog("No selected to stage");
      }
      foreach (var filepath in SelectedModifiedFilePathList)
      {
        AddLog("Selected:" + filepath);
      }

      git_.Stage(SelectedModifiedFilePathList);

      Refresh();
    }
    public ICommand StageSelected { get; set; }

    public IEnumerable<string> SelectedModifiedFilePathList
    {
      get { return ModifiedList.Where(o => o.IsSelected).Select(o => o.FilePath); }
    }

    public IEnumerable<string> SelectedStagedFilePathList
    {
      get { return StagedList.Where(o => o.IsSelected).Select(o => o.FilePath); }
    }
  }
}
