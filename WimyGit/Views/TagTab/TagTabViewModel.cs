﻿using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WimyGit.UserControls
{
    public class TagTabViewModel : NotifyBase
    {
        private WeakReference<IGitRepository> _gitRepository;

        public ICommand DeleteTagCommand { get; private set; }

        public ObservableCollection<TagInfo> TagInfos { get; set; }
        public TagInfo SelectedTag { get; set; }

        public TagTabViewModel()
        {
            DeleteTagCommand = new DelegateCommand(OnDeleteTagCommand);

            TagInfos = new ObservableCollection<TagInfo>();
        }

        public void SetGitRepository(IGitRepository gitRepository)
        {
            _gitRepository = new WeakReference<IGitRepository>(gitRepository);
        }

        public void Refresh()
        {
            if (_gitRepository.TryGetTarget(out var gitRepository) == false)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            TagInfos.Clear();
            string cmd = GitCommandCreator.ListTag();
            foreach (var tagInfo in TagParser.Parse(gitRepository.CreateGitRunner().Run(cmd)))
            {
                TagInfos.Add(tagInfo);
            }
            NotifyPropertyChanged("TagInfos");
        }

        public void OnDeleteTagCommand(object sender)
        {
            if (_gitRepository.TryGetTarget(out var gitRepository) == false)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            if (SelectedTag == null)
            {
                return;
            }
            string tagName = SelectedTag.Name;
            string cmd = GitCommandCreator.DeleteTag(tagName);
            gitRepository.CreateGitRunner().RunInConsoleProgressWindow(cmd);

            gitRepository.Refresh();
        }
    }
}
