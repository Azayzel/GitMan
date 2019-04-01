﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepoZ.Api.Git;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace RepoZ.Api.Git
{
	[DebuggerDisplay("{Name} @{Path}")]
	public class RepositoryView : INotifyPropertyChanged
	{
		private string _cachedRepositoryStatusCode;
		private string _cachedRepositoryStatus;
		private string _cachedRepositoryStatusWithBranch;
		private bool _isSynchronizing;

		public event PropertyChangedEventHandler PropertyChanged;

		public RepositoryView(Repository repository)
		{
			Repository = repository ?? throw new ArgumentNullException(nameof(repository));
			UpdateStampUtc = DateTime.UtcNow;
		}

		public bool MatchesRegexFilter(string pattern) => MatchesFilter(pattern, useRegex: true);

		public bool MatchesFilter(string filter) => MatchesFilter(filter, useRegex: false);

		private bool MatchesFilter(string filter, bool useRegex)
		{
			if (string.IsNullOrEmpty(filter))
				return true;

			string filterProperty = null;

			if (filter.StartsWith("n ", StringComparison.OrdinalIgnoreCase))
				filterProperty = Name;
			else if (filter.StartsWith("b ", StringComparison.OrdinalIgnoreCase))
				filterProperty = CurrentBranch;
			else if (filter.StartsWith("p ", StringComparison.OrdinalIgnoreCase))
				filterProperty = Path;

			if (filterProperty == null)
				filterProperty = Name;
			else
				filter = filter.Substring(2);

			if (string.IsNullOrEmpty(filter))
				return true;

			if (useRegex)
				return Regex.IsMatch(filterProperty, filter, RegexOptions.IgnoreCase);

			return filterProperty.IndexOf(filter, StringComparison.OrdinalIgnoreCase) > -1;
		}

		public override bool Equals(object obj)
		{
			var other = obj as RepositoryView;

			if (other != null)
				return other.Repository.Equals(this.Repository);

			return object.ReferenceEquals(this, obj);
		}

		private void EnsureStatusCache()
		{
			var repositoryStatusCode = Repository.GetStatusCode();

			// compare the status code and not the full status string because the latter one is heavier to calculate
			bool canTakeFromCache = _cachedRepositoryStatusCode == repositoryStatusCode;

			if (!canTakeFromCache)
			{
				var compressor = new StatusCompressor(new StatusCharacterMap());
				_cachedRepositoryStatus = compressor.Compress(Repository);
				_cachedRepositoryStatusWithBranch = compressor.CompressWithBranch(Repository);

				_cachedRepositoryStatusCode = repositoryStatusCode;
			}
		}

		public string Name => (Repository.Name ?? "") + (IsSynchronizing ? SyncAppendix : "");

		public string Path => Repository.Path ?? "";

		public string Location => Repository.Location ?? "";

		public string CurrentBranch => Repository.CurrentBranch ?? "";

		public string AheadBy => Repository.AheadBy?.ToString() ?? "";

		public string BehindBy => Repository.BehindBy?.ToString() ?? "";

		public string[] Branches => Repository.Branches ?? new string[0];

		public string LocalUntracked => Repository.LocalUntracked?.ToString() ?? "";

		public string LocalModified => Repository.LocalModified?.ToString() ?? "";

		public string LocalMissing => Repository.LocalMissing?.ToString() ?? "";

		public string LocalAdded => Repository.LocalAdded?.ToString() ?? "";

		public string LocalStaged => Repository.LocalStaged?.ToString() ?? "";

		public string LocalRemoved => Repository.LocalRemoved?.ToString() ?? "";

		public string LocalIgnored => Repository.LocalIgnored?.ToString() ?? "";

		public bool WasFound => Repository.WasFound;

		public override int GetHashCode() => Repository.GetHashCode();

		public Repository Repository { get; }

		public string Status
		{
			get
			{
				EnsureStatusCache();
				return _cachedRepositoryStatus;
			}
		}

		public string BranchWithStatus
		{
			get
			{
				EnsureStatusCache();
				return _cachedRepositoryStatusWithBranch;
			}
		}

		public bool IsSynchronizing
		{
			get { return _isSynchronizing; }
			set
			{
				_isSynchronizing = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); // Name includes the activity icon
			}
		}

		private string SyncAppendix => "  \u2191\u2193"; // up and down arrows

		public DateTime UpdateStampUtc { get; private set; }

	}
}
