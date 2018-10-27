﻿using System;
using System.Diagnostics;
using System.Management;
using System.Windows;

namespace WimyGit
{
	public partial class ConsoleProgressWindow : Window
	{
		private Process process_;
		private string repository_path_;
		private string command_;
		private bool canceled_ = false;

		public ConsoleProgressWindow(string repository_path, string command)
		{
			repository_path_ = repository_path;
			command_ = command;

			InitializeComponent();
			InitializeProcess();

			this.Closed += ConsoleProgressWindow_Closed;
		}

		private void ConsoleProgressWindow_Closed(object sender, EventArgs e)
		{
			CloseProcess();
		}

		private void Window_Loaded(Object sender, RoutedEventArgs e)
		{
			StartProcess();
		}

		private void AddOutputText(string text)
		{
			string inner_text = text + Environment.NewLine;
			if (textBox.Dispatcher.CheckAccess())
			{
				textBox.Text += inner_text;
				ScrollToEndConsole();
			}
			else
			{
				textBox.Dispatcher.BeginInvoke(new Action(() => {
					textBox.Text += inner_text;
					ScrollToEndConsole();
				}));
			}
		}

		private void InitializeProcess()
		{
			process_ = new Process();
			process_.StartInfo.FileName = ProgramPathFinder.GetGitBin();
			process_.StartInfo.Arguments = command_;
			process_.StartInfo.UseShellExecute = false;
			process_.StartInfo.RedirectStandardInput = true;
			process_.StartInfo.RedirectStandardOutput = true;
			process_.StartInfo.RedirectStandardError = true;
			process_.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
			process_.StartInfo.CreateNoWindow = true;
			process_.StartInfo.WorkingDirectory = repository_path_;

			StringArrayOutput output = new StringArrayOutput();
			process_.OutputDataReceived += (object _, DataReceivedEventArgs console_output) => {
				if (console_output.Data == null)
				{
					return;
				}
				AddOutputText(console_output.Data);
			};
			process_.ErrorDataReceived += (object _, DataReceivedEventArgs error_output) => {
				if (error_output.Data == null)
				{
					return;
				}
				AddOutputText(error_output.Data);
			};
			process_.Exited += (object sender, EventArgs e) => {
				if (button.Dispatcher.CheckAccess())
				{
					Process_Exited();
				}
				else
				{
					button.Dispatcher.BeginInvoke(new Action(() => {
						Process_Exited();
					}));
				}
			};
			process_.EnableRaisingEvents = true;
		}

		private void StartProcess()
		{
			AddOutputText(string.Format("git {0}", command_));

			process_.Start();
			process_.BeginOutputReadLine();
			process_.BeginErrorReadLine();
		}

		private void CloseProcess()
		{
			process_.EnableRaisingEvents = false;
			KillProcessAndChildren(process_.Id);
			process_.Close();
		}

		private void Process_Exited()
		{
			AddOutputText("Process exited");

			button.Content = "Close";

			if (canceled_)
			{
				AddOutputText("Canceled!!!");
				return;
			}
			if (process_.ExitCode != 0)
			{
				AddOutputText("Error!!!");
				return;
			}
			AddOutputText("All ok!!!");
		}

		private static void KillProcessAndChildren(int pid)
		{
			// Cannot close 'system idle process'.
			if (pid == 0)
			{
				return;
			}
			ManagementObjectSearcher searcher = new ManagementObjectSearcher
					("Select * From Win32_Process Where ParentProcessID=" + pid);
			ManagementObjectCollection moc = searcher.Get();
			foreach (ManagementObject mo in moc)
			{
				KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
			}
			try
			{
				Process proc = Process.GetProcessById(pid);
				if (proc != null)
				{
					proc.Kill();
				}
			}
			catch (Exception)
			{
				// Process already exited.
			}
		}

		private void OnButton_Click(Object sender, RoutedEventArgs e)
		{
			if (process_.HasExited)
			{
				this.Close();
				return;
			}
			canceled_ = true;

			KillProcessAndChildren(process_.Id);

			AddOutputText("Cancelling...");
		}

		private void ScrollToEndConsole()
		{
			textBox.ScrollToEnd();
		}

	}
}
